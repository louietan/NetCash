module internal NetCash.NativeLibraries

open System
open System.IO

[<Literal>]
let glib = "glib"

[<Literal>]
let gio = "gio"

[<Literal>]
let gobject = "gobject"

[<Literal>]
let gncEngine = "gncEngine"

[<Literal>]
let gncCoreUtils = "gncCoreUtils"

[<Literal>]
let gncAppUtils = "gncAppUtils"

[<Literal>]
let gncmodBackendDBI = "gncmodBackendDBI"


let EXTENSION =
    if OperatingSystem.IsLinux() then
        "so"
    elif OperatingSystem.IsMacOS() then
        "dylib"
    elif OperatingSystem.IsWindows() then
        "dll"
    else
        raise (PlatformNotSupportedException())

let variants =
    Map [ (glib,
           Map [ ("so", [ "libglib-2.0" ])
                 ("dylib", [ "libglib-2.0" ])
                 ("dll", [ "libglib-2.0-0" ]) ])

          (gobject,
           Map [ ("so", [ "libgobject-2.0" ])
                 ("dylib", [ "libgobject-2.0" ])
                 ("dll", [ "libgobject-2.0-0" ]) ])

          (gio,
           Map [ ("so", [ "libgio-2.0" ])
                 ("dylib", [ "libgio-2.0" ])
                 ("dll", [ "libgio-2.0-0" ]) ])

          (gncEngine,
           Map [ ("so", [ "libgnc-engine" ])
                 ("dylib", [ "libgnc-engine" ])
                 ("dll", [ "libgnc-engine" ]) ])

          (gncCoreUtils,
           Map [ ("so", [ "libgnc-core-utils" ])
                 ("dylib", [ "libgnc-core-utils" ])
                 ("dll", [ "libgnc-core-utils" ]) ])

          (gncAppUtils,
           Map [ ("so", [ "libgnc-app-utils" ])
                 ("dylib", [ "libgnc-app-utils" ])
                 ("dll", [ "libgnc-app-utils" ]) ])

          (gncmodBackendDBI,
           Map [ ("so", [ "gnucash/libgncmod-backend-dbi" ])
                 ("dylib", [ "gnucash/libgncmod-backend-dbi" ])
                 ("dll", [ "libgncmod-backend-dbi" ]) ]) ]

let tryFindLibraryFileNames name =
    Map.tryFind name variants
    |> Option.map (Map.find EXTENSION)
    |> Option.map (Seq.map (fun file -> file + "." + EXTENSION))
