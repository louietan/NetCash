module NetCash.Marshalling

open System
open System.IO
open System.Linq
open System.Collections.Generic
open System.Reflection
open System.Runtime.InteropServices

type SafeHandle with
    /// Creates a SafeHandle object that wraps the given pointer.
    static member wrap release pointer =
        { new SafeHandle(pointer, true) with
            override self.IsInvalid = self.DangerousGetHandle() = IntPtr.Zero

            override self.ReleaseHandle() =
                self.SetHandle IntPtr.Zero
                release pointer
                true }

    static member alloc =
        Bindings.g_malloc
        >> SafeHandle.wrap Bindings.g_free

    /// Lifts (nativeint -> 'a) into (SafeHandle -> 'a)
    static member map mapping (handle: SafeHandle) = handle.DangerousGetHandle() |> mapping

    static member iter action (handle: SafeHandle) =
        if not handle.IsInvalid then
            handle.DangerousGetHandle() |> action

    static member using action handle =
        action |> SafeHandle.map |> using handle

module String =
    /// Creates a managed String object from a native pointer that isn't owned by the caller.
    [<CompiledName("FromBorrowed")>]
    let fromBorrowed ptr = Marshal.PtrToStringUTF8 ptr

    /// Creates a managed String object from a native pointer that is owned by the caller.
    [<CompiledName("FromOwned")>]
    let fromOwned ptr =
        try
            Marshal.PtrToStringUTF8 ptr
        finally
            Bindings.g_free ptr

    let inline internal maybeBorrowed ptr =
        Some ptr |> Option.filter ((<>) IntPtr.Zero) |> Option.map fromBorrowed

    let inline internal maybeOwned ptr =
        Some ptr |> Option.filter ((<>) IntPtr.Zero) |> Option.map fromOwned

module Guid =
    let fromPointer =
        Bindings.guid_to_string
        >> String.fromOwned
        >> Guid

    let toSafeHandle guid =
        let handle = SafeHandle.alloc sizeof<Bindings.GncGUID>

        Bindings.string_to_guid (guid.ToString(), handle.DangerousGetHandle())
        |> ignore

        handle

module Object =
    let fromPointer ctor =
        function
        | 0n -> Unchecked.defaultof<_>
        | ptr -> ctor ptr

    let internal fromPointerToOption ctor = fromPointer ctor >> Option.ofObj

module DateTime =
    let fromTimestamp (time: Bindings.time64) =
        (DateTimeOffset.FromUnixTimeSeconds time)
            .LocalDateTime

    let toTimestamp (time: DateTime) : Bindings.time64 =
        (time.ToUniversalTime() |> DateTimeOffset)
            .ToUnixTimeSeconds()

module DateOnly =
    let fromTimestamp = DateTime.fromTimestamp >> DateOnly.FromDateTime

    let toTimestamp =
        SystemExtensions.AsDateTime
        >> DateTime.toTimestamp
