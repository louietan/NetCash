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

module GnuCashEngine =
    let private version =
        lazy
            ({ VersionString = Bindings.gnc_version () |> String.fromBorrowed
               BuildID = Bindings.gnc_build_id () |> String.fromBorrowed
               Revision = Bindings.gnc_vcs_rev () |> String.fromBorrowed
               RevisionDate =
                 Bindings.gnc_vcs_rev_date ()
                 |> String.fromBorrowed
               MajorVersion = Bindings.gnc_gnucash_major_version () })

    let Version = version.Force()

    let private registerObjectAllocators () =
        [ (typeof<Account>, Bindings.xaccMallocAccount)
          (typeof<Split>, Bindings.xaccMallocSplit)
          (typeof<Transaction>, Bindings.xaccMallocTransaction) ]
        |> Seq.iter GnuCashObject.nativeAllocators.Add

    /// <summary>
    /// Initializes the GnuCash engine. This requires only once.
    /// </summary>
    let Initialize () =
        LibraryLoader.activate ()

        Bindings.gnc_environment_setup ()
        Bindings.gnc_engine_init (IntPtr.Zero, [| IntPtr.Zero |])
        Bindings.gnc_prefs_init ()
        Bindings.gnc_module_init_backend_dbi ()

        Bindings.qof_init ()

        registerObjectAllocators ()

    let SetLogLevel (``module``, level) =
        Bindings.qof_log_set_level (``module``, level)

    /// <summary>Sets the log file to write to.</summary>
    let SetLogOutput (output: LogOutputType) =
        Bindings.qof_log_init_filename_special output.FileName

    let Shutdown () =
        Bindings.gnc_module_finalize_backend_dbi ()
        Bindings.gnc_engine_shutdown ()

        Bindings.qof_close ()
