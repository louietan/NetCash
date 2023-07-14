[<AutoOpen>]
module internal NetCash.Extensions

open System
open System.IO
open System.Runtime.CompilerServices
open System.ComponentModel
open System.Collections.Generic

type Option<'a> with
    static member ofUncheckedDefault obj =
        if isUncheckedDefault obj then
            None
        else
            Some obj

    static member ofPair(pair: bool * 'a) =
        match pair with
        | false, _ -> None
        | _, a -> Some a

    static member cast<'b> opt = opt |> Option.map unbox<'b>

    static member apply fOpt xOpt =
        match (fOpt, xOpt) with
        | Some f, Some x -> Some(f x)
        | _ -> None

[<Extension>]
type SystemExtensions =
    [<Extension>]
    static member GetDescription(e: Enum) =
        ((e.GetType()).GetField(e.ToString()))
            .GetCustomAttributes(typeof<DescriptionAttribute>, false)
        |> Seq.tryHead
        |> Option.map (fun a -> (a :?> DescriptionAttribute).Description)
        |> Option.toObj

    [<Extension>]
    static member Lines(s: string) =
        seq {
            use reader = new StringReader(s)
            let mutable line = reader.ReadLine()

            while not (isNull line) do
                yield line
                line <- reader.ReadLine()
        }

    [<Extension>]
    static member GetValueMaybe(dict: IDictionary<'k, 'v>, key: 'k) = dict.TryGetValue key |> Option.ofPair

    [<Extension>]
    static member AsDateTime(d: DateOnly) = DateTime(d.Year, d.Month, d.Day)

type Path with
    /// Turn a path string into a string option, Some if it exists, otherwise None.
    static member ToOption path =
        Some path
        |> Option.filter (fun p -> File.Exists p || Directory.Exists p)

type DateOnly with
    static member Today = DateOnly.FromDateTime DateTime.Today

type String with
    static member lines(s: string) = s.Lines()

    static member tryLocate (subs: string seq) (str: string) =
        subs
        |> Seq.map (fun sub -> (sub, str.IndexOf(sub)))
        |> Seq.tryFind (fun (_, idx) -> idx > -1)
        |> Option.map (fun (sub, idx) -> (sub, idx, idx + sub.Length - 1))
