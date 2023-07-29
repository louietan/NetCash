open System

open Argu

open NetCash

type BalArguments =
    | [<Unique; AltCommandLine("-l")>] Flat
    | [<Unique; AltCommandLine("-t")>] Tree
    | [<Unique; AltCommandLine("-e")>] End of string
    | [<MainCommand; ExactlyOnce; Last>] Book of uri: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Flat -> "show accounts as a flat list (default). Amounts exclude subaccount amounts."
            | Tree -> "show accounts as a tree. Amounts include subaccount amounts."
            | End _ -> "show balance before this date. A valid date string or \"today\""
            | Book uri -> "URI to the book."

[<CliPrefix(CliPrefix.DoubleDash)>]
type CliArguments =
    | [<CustomCommandLine("bal", "balance")>] Bal of ParseResults<BalArguments>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Bal _ -> "Show accounts and their balances."

module CmdHandlers =
    let bal uri tree endDate =
        use book = Book.OpenRead uri

        let filter: seq<Account> -> seq<Account> = Seq.filter (fun acc -> not acc.Hidden)

        let inline printBal (acc: Account) =
            let balance =
                match endDate with
                // TODO: replace with --current (consistent with ledger-cli)
                // --current
                // -c
                // Display only transactions on or before the current date.

                | Some "today" -> acc.GetBalanceBeforeDate(DateTime.Now, tree)
                | Some input ->
                    match DateTime.TryParse input with
                    | false, _ -> failwithf "Invalid end date: %s" input
                    | _, date -> acc.GetBalanceBeforeDate(date, tree)
                | _ -> acc.GetFutureBalance(tree)

            Helpers.UI.FormatAmount(balance, acc.Currency, true)

        let rec collectTree level (root: Account) =
            seq {
                if level > -1 then
                    (printBal root, (String.replicate level "  ") + root.Name)

                yield!
                    root.Children
                    |> filter
                    |> Seq.sortBy (fun x -> x.Name)
                    |> Seq.collect (collectTree (level + 1))
            }

        let inline collectList () =
            book.Accounts
            |> filter
            |> Seq.sortBy (fun x -> x.FullName)
            |> Seq.map (fun acc -> (printBal acc, acc.FullName))

        let rows =
            if tree then
                collectTree -1 book.RootAccount
            else
                collectList ()
            |> Seq.cache

        let rowFmt =
            let padding = 8
            let maxBalWidth = rows |> ((Seq.map (fst >> String.length)) >> Seq.max)
            sprintf "{0,%d}  {1}" (maxBalWidth + padding)

        for balance, fullName in rows do
            stdout.WriteLine(rowFmt, balance, fullName)

[<EntryPoint>]
let main argv =
    GnuCashEngine.Initialize()
    Logging.Config(Logging.LogLevel.WARN, [| Logging.Appender.console |])

    let cliParser = ArgumentParser.Create<CliArguments>()

    try
        let results = cliParser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        match results.GetSubCommand() with
        | Bal args ->
            let tree = args.Contains BalArguments.Tree

            let endDate =
                args.TryGetResult BalArguments.End |> Option.map (fun x -> x.ToLowerInvariant())

            let uri = args.GetResult BalArguments.Book |> GnuCashUri.Parse
            CmdHandlers.bal uri tree endDate
    with e ->
        stdout.WriteLine e.Message

    0
