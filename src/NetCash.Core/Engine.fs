namespace NetCash

open System
open System.IO
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open System.Diagnostics

open NetCash.Marshalling

/// Version information for gnucash installation.
type VersionInfo =
    { VersionString: string
      BuildID: string
      Revision: string
      RevisionDate: string
      MajorVersion: int }

/// Log output types for gnucash.
type LogOutputType =
    | StdOut
    | StdErr
    | File of path: string

    member self.FileName =
        match self with
        | StdOut -> "stdout"
        | StdErr -> "stderr"
        | File path -> path

[<Sealed; AbstractClass>]
type GnuCashEngine() =
    static let registerObjectAllocators () =
        [ (typeof<Account>, Bindings.xaccMallocAccount)
          (typeof<Split>, Bindings.xaccMallocSplit)
          (typeof<Transaction>, Bindings.xaccMallocTransaction) ]
        |> Seq.iter GnuCashObject.nativeAllocators.Add

    static let mutable version: VersionInfo = Unchecked.defaultof<_>
    static member Version = version

    /// <summary>
    /// Initializes the GnuCash engine. This requires only once.
    /// </summary>
    /// <param name="gnucashCli">
    /// The path to gnucash-cli, e.g. "/opt/gnucash-unstable/bin/gnucash-cli".
    /// <para>
    /// You only have to set this variable if the `gnucash-cli` executable is not available from the PATH environment variable.
    /// You can verify the availability using commands like `which gnucash-cli` on *nix or `gcm gnucash-cli` in PowerShell on Windows.
    /// </para>
    /// </param>
    static member Initialize ([<Optional; DefaultParameterValue "gnucash-cli">]gnucashCli: string) =
        LibraryLoader.activate gnucashCli

        Bindings.gnc_environment_setup ()
        Bindings.gnc_engine_init (IntPtr.Zero, [| IntPtr.Zero |])
        Bindings.gnc_prefs_init ()
        Bindings.gnc_module_init_backend_dbi ()

        Bindings.qof_init ()

        registerObjectAllocators ()

        version <- 
            { VersionString = Bindings.gnc_version () |> String.fromBorrowed
              BuildID = Bindings.gnc_build_id () |> String.fromBorrowed
              Revision = Bindings.gnc_vcs_rev () |> String.fromBorrowed
              RevisionDate =
                Bindings.gnc_vcs_rev_date ()
                |> String.fromBorrowed
              MajorVersion = Bindings.gnc_gnucash_major_version () }

    static member SetLogLevel (``module``, level) =
        Bindings.qof_log_set_level (``module``, level)

    /// <summary>Sets the log file to write to.</summary>
    static member SetLogOutput (output: LogOutputType) =
        Bindings.qof_log_init_filename_special output.FileName

    static member Shutdown () =
        Bindings.gnc_module_finalize_backend_dbi ()
        Bindings.gnc_engine_shutdown ()

        Bindings.qof_close ()
