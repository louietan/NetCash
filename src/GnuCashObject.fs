namespace NetCash

open System
open System.Reflection
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open NetCash.Marshalling

type INativeWrapper =
    abstract NativeHandle: nativeint

(* Constraints:
   1. Implementations should have a private unary constructor that takes the native pointer.
   2. That unary constructor SHOULD ONLY be called by the GnuCashObject module.
*)
type IGnuCashEntity =
    inherit INativeWrapper

module GnuCashObject =
    let internal nativeHandle (gncObj: INativeWrapper) =
        if isUncheckedDefault gncObj then
            IntPtr.Zero
        else
            gncObj.NativeHandle

    let internal bookHandle (gncEnt: IGnuCashEntity) =
        Bindings.qof_instance_get_book gncEnt.NativeHandle

    let internal objectId (gncEnt: IGnuCashEntity) =
        gncEnt.NativeHandle
        |> Bindings.qof_instance_get_guid
        |> Guid.fromPointer

    let internal callUnaryCtor<'a> ptr setup =
        let inst =
            Activator.CreateInstance(
                typeof<'a>,
                BindingFlags.NonPublic ||| BindingFlags.Instance,
                null,
                [| box ptr |],
                null
            )
            :?> 'a

        setup inst
        inst

    module Registry =
        (* This module maintains a 1-to-1 mapping between native object pointers and managed objects,
           so that we can get the same object of the same identity,
           for example in cross-reference: `account.Splits.First().Account` returns the same object as `account`.
           This provides several benefits, including less allocation and consistency.

           To implement this correctly, we have to:
           1. Allow the managed objects to be GC'ed.
           2. Clean the registry when native objects get destryed.

           For #1, the `WeakReference` is used.

           For #2, there's QOF_EVENT_DESTROY to notify the destruction of native objects.
           However, not every objects trigger this event, such as splits.
           So the mapping is designed to be partitioned by book pointer, when a book closes,
           the corresponding partition gets removed. *)

        let private objectStore =
            Dictionary<nativeint, Dictionary<nativeint, WeakReference<obj>>>()

        let removeBook =
            Bindings.GFunc (fun session _ ->
                session
                |> Bindings.qof_session_get_book
                |> objectStore.Remove
                |> ignore)

        do Bindings.gnc_hook_add_dangler (Bindings.HOOK_BOOK_CLOSED, removeBook, null, IntPtr.Zero)

        let internal getOrCreatePartition ptr =
            let book = Bindings.qof_instance_get_book ptr

            objectStore.GetValueMaybe book
            |> Option.defaultWith (fun () ->
                let entMap = Dictionary<nativeint, WeakReference<_>>()
                objectStore.Add(book, entMap)
                entMap)

        /// Gets or creates the managed object that wraps the native pointer.
        [<CompiledName "GetOrCreate">]
        let getOrCreate<'a when 'a :> IGnuCashEntity> (ptr: nativeint) =
            if ptr = IntPtr.Zero then
                Unchecked.defaultof<_>
            else
                let part = getOrCreatePartition ptr

                match part.TryGetValue ptr with
                // not registered
                | false, _ -> callUnaryCtor<'a> ptr (fun inst -> part.Add(ptr, WeakReference<_> inst))
                // registered
                | _, weakRef ->
                    weakRef.TryGetTarget()
                    |> Option.ofPair
                    |> Option.cast
                    |> Option.defaultWith (fun () -> callUnaryCtor<'a> ptr weakRef.SetTarget)

    let internal nativeAllocators = Dictionary<Type, nativeint -> nativeint>()

    let make<'a> book =
        match nativeAllocators.TryGetValue typeof<'a> with
        | false, _ -> failwithf "No naitve allocator defined for type %s" typeof<'a>.FullName
        | _, alloc ->
            let ptr = alloc book

            callUnaryCtor<'a> ptr (fun inst ->
                (Registry.getOrCreatePartition ptr)
                    .Add(ptr, WeakReference<obj> inst))
