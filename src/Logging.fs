/// Toy logging module that SHOULD ONLY be used for debugging purposes.
module NetCash.Logging

open System
open System.IO
open System.Threading.Tasks

type LogLevel =
    | ALL = 0
    | DEBUG = 1
    | INFO = 2
    | WARN = 3
    | ERROR = 4
    | OFF = 5

module Appender =
    type Protocol =
        abstract Append: string -> Task

    /// Creates a file appender.
    [<CompiledName "OfFile">]
    let ofFile file =
        // clear content
        File.WriteAllText(file, String.Empty)

        { new Protocol with
            member _.Append(msg) = File.AppendAllLinesAsync(file, [ msg ]) }

    /// Gets the console appender.
    [<CompiledName "Console">]
    let console =
        { new Protocol with
            member _.Append(msg) = stdout.WriteLineAsync msg }

let mutable private currentLevel = LogLevel.OFF

let mutable private currentAppenders = ResizeArray<Appender.Protocol>()

module Logger =
    let private simpleLayout (level, message) =
        sprintf "%s|%A|%s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")) level message

    let private processor =
        MailboxProcessor<string>.Start
            (fun inbox ->
                let rec loop () =
                    async {
                        let! msg = inbox.Receive()

                        do!
                            currentAppenders
                            |> Seq.map (fun appender -> appender.Append msg)
                            |> Task.WhenAll
                            |> Async.AwaitTask

                        return! loop ()
                    }

                loop ())

    let private LogInternal level message =
        if level >= currentLevel then
            (level, message) |> simpleLayout |> processor.Post

    let internal log level = Printf.ksprintf (LogInternal level)

    let Log (level, format, [<ParamArray>] args: obj []) =
        LogInternal level (String.Format(format, args))

    let Debug (format, [<ParamArray>] args: obj []) =
        LogInternal LogLevel.DEBUG (String.Format(format, args))

    let Info (format, [<ParamArray>] args: obj []) =
        LogInternal LogLevel.INFO (String.Format(format, args))

    let Warn (format, [<ParamArray>] args: obj []) =
        LogInternal LogLevel.WARN (String.Format(format, args))

    let Error (format, [<ParamArray>] args: obj []) =
        LogInternal LogLevel.ERROR (String.Format(format, args))

let Config (minLevel, [<ParamArray>] appenders: Appender.Protocol []) =
    currentLevel <- minLevel
    currentAppenders.AddRange(appenders)
