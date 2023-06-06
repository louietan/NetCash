/// This module is responsible for the loading of native libraries.
module NetCash.LibraryLoader

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Reflection
open System.Runtime.InteropServices

module Executables =
    let inline executableFileName file =
        if OperatingSystem.IsWindows() then
            file + ".exe"
        else
            file

    let CLI = executableFileName "gnucash-cli"
    let GUI = executableFileName "gnucash"

type GnuCashRuntime =
    { BinaryPath: string
      LibraryPath: string }

let private runtimeEnvironmentIntrospect cli =
    let procInfo =
        ProcessStartInfo(
            FileName = defaultUncheckedArg Executables.CLI cli,
            Arguments = "--paths",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        )

    use proc = Process.Start procInfo
    let output = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()

    if String.IsNullOrWhiteSpace output then
        failwithf "\"gnucash-cli --paths\" produced empty result, make sure the installed gnucash is at least 4.12"

    let mutable libPathO = None
    let mutable binPathO = None

    for line in String.lines output do
        match String.tryLocate [ "GNC_LIB:"; "GNC_BIN:" ] line with
        | Some ("GNC_LIB:", _, e) -> libPathO <- line[ e + 1 .. ].Trim() |> Path.ToOption
        | Some ("GNC_BIN:", _, e) -> binPathO <- line[ e + 1 .. ].Trim() |> Path.ToOption
        | _ -> ()

    match (libPathO, binPathO) with
    | Some libPath, Some binPath ->
        if OperatingSystem.IsWindows() then
            { BinaryPath = binPath
              LibraryPath = libPath }
        else
            { BinaryPath = binPath
              LibraryPath = Path.GetDirectoryName libPath }
    | _ -> failwithf "Unable to obtain path info from \"gnucash-cli --paths\":\n%s" output

/// Activates the library loader for gnucash native libraries.
[<CompiledName "Activate">]
let activate (cli: string) =
    let { LibraryPath = libPath
          BinaryPath = binPath } =
        runtimeEnvironmentIntrospect cli

    let resolver =
        let resolved = Dictionary<string, nativeint>()

        DllImportResolver (fun lib asm path ->
            match resolved.TryGetValue lib with
            | true, handle -> handle
            | _ ->
                let tryLoadAndMemo variants =
                    variants
                    |> Seq.map (fun libFile ->
                        let (success, handle) as result = NativeLibrary.TryLoad(libFile, asm, path)

                        if success then
                            resolved.Add(lib, handle)

                        result)
                    |> Seq.tryFind fst
                    |> Option.map snd

                match NativeLibraries.tryFindLibraryFileNames lib with
                | Some fileNames ->
                    fileNames
                    |> Seq.map (fun variant -> Path.Join(libPath, variant))
                    |> tryLoadAndMemo
                    |> Option.defaultWith (fun () ->
                        fileNames
                        |> tryLoadAndMemo
                        |> Option.defaultValue IntPtr.Zero)
                | None -> NativeLibrary.Load(lib, asm, path))

    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), resolver)

    // The DLL resolver has been registered, now we are able to call native functions.
    // There's one more step, which is to initialize BinReloc.
    //
    // When gnucash initializes, some modules are loaded programmatically
    // (e.g. gncmod-backend-xml and gncmod-backend-dbi are loaded in `gnc_engine_init_part2`),
    // this process requires the knowledge of where the gnucash binaries reside,
    // which is determined by BinReloc (binreloc.c) using some magic.
    //
    // Comment in binreloc.c says that for library authors they should call function `gnc_gbr_init_lib`,
    // which in fact does not exist.
    //
    // Anyway, BinReloc allows users to skip its magic and do their own lookup
    // by providing function `gnc_gbr_set_exe` to feed the result.
    //
    // There is an alternative, which is to set environment variables GNC_UNINSTALLED and GNC_BUILDDIR.
    // But this approach is for testing purposes and it makes the library unable to read preferences (see gnc-gsettings.cpp).
    Bindings.gnc_gbr_set_exe (Path.Join(binPath, Executables.GUI))
