open System
open System.IO
open System.Reflection

open XmlDocMarkdown.Core

[<EntryPoint>]
let main _ =
    let wd =
        Assembly.GetExecutingAssembly().Location
        |> Path.GetDirectoryName

    let docs =
        let rec locateRoot dir =
            if File.Exists(Path.Join(dir, "NetCash.sln")) then
                dir
            else
                Path.GetDirectoryName dir |> locateRoot

        Path.Join(locateRoot wd, "docs")

    XmlDocMarkdownApp.Run [| Path.Join(wd, "netcash.dll")
                             docs
                             "--clean"
                             "--obsolete" |]
