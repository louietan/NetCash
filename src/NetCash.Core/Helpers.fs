namespace NetCash.Helpers

open System
open System.Runtime.InteropServices
open System.Runtime.CompilerServices

open NetCash
open NetCash.Marshalling

/// Helper for UI-related stuff.
module UI =
    let FormatAmount (amount: GncNumeric, currency: Commodity, showSymbol) =
        let printInfo =
            Bindings.gnc_commodity_print_info (GnuCashObject.nativeHandle currency, showSymbol)

        Bindings.xaccPrintAmount (amount, printInfo)
        |> Bindings.g_strdup
        |> String.fromOwned

[<Extension>]
type Extensions =
    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeAccount
        (
            book: Book,
            accountType: Bindings.GNCAccountType,
            name: string,
            [<ParamArray>] names: string []
        ) =

        let mutable currentAccount =
            book.FindAccountMaybe(AccountFinder.ByName name)
            |> Option.defaultWith (fun () -> book.NewAccount(name, accountType))

        for currentName in names do
            currentAccount <-
                currentAccount.Children
                |> Seq.tryFind (fun acct -> acct.Name = currentName)
                |> Option.defaultWith (fun () -> currentAccount.NewChildAccount(currentName))

        currentAccount

    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeEquityAccount(book: Book, name: string, [<ParamArray>] names: string []) =
        Extensions.GetOrMakeAccount(book, Bindings.GNCAccountType.ACCT_TYPE_EQUITY, name, names)

    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeAssetAccount(book: Book, name: string, [<ParamArray>] names: string []) =
        Extensions.GetOrMakeAccount(book, Bindings.GNCAccountType.ACCT_TYPE_ASSET, name, names)

    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeLiabilityAccount(book: Book, name: string, [<ParamArray>] names: string []) =
        Extensions.GetOrMakeAccount(book, Bindings.GNCAccountType.ACCT_TYPE_LIABILITY, name, names)

    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeIncomeAccount(book: Book, name: string, [<ParamArray>] names: string []) =
        Extensions.GetOrMakeAccount(book, Bindings.GNCAccountType.ACCT_TYPE_INCOME, name, names)

    /// Gets or creates an account. (Parent accounts are created on-demand).
    [<Extension>]
    static member GetOrMakeExpenseAccount(book: Book, name: string, [<ParamArray>] names: string []) =
        Extensions.GetOrMakeAccount(book, Bindings.GNCAccountType.ACCT_TYPE_EXPENSE, name, names)

    /// <summary>Create accrual transactions between two accounts. (I really need some feedback on the naming and the description.)</summary>
    /// <remarks>
    /// This is useful for creating future transactions for scenarios like deprecation or prepaid expenses or whatever.
    /// It will create a sequence of transactions for N months until the final balance of the account reaches the target balance.
    /// It also takes care of the balance adjustment:
    ///     Let's say the currency is of fraction 1/100, you're going to distribute some prepaid expense of 1000 into 3 months,
    ///     so there will be 3 expense transactions of value 1000/3 each, GnuCash will try to convert the value to match the fraction of the currency,
    ///     so it will become 33333/100, that is 333.33 in floating-point, as a result, there will be 0.01 left in the balance of the prepaid expense account,
    ///     which is not desired.
    ///     This method will try to adjust the last transaction so it becomes: 333.33   333.33   333.34
    ///     I'm not sure if there is a common practice for this, but I use this method for my personal accounting anyway.
    /// </remarks>
    [<Extension>]
    static member CreateAccrualTransactions
        (
            book: Book,
            fromAccount: Account,
            toAccount: Account,
            months: int,
            [<Optional>] startDate: DateOnly,
            [<Optional>] targetBalance: GncNumeric
        ) =
        let balance = defaultUncheckedArg GncNumeric.Zero targetBalance

        let average =
            (fromAccount.Balance - balance)
            / GncNumeric months

        let date = defaultUncheckedArg DateOnly.Today startDate

        for i = 0 to months - 1 do
            fromAccount.TransferTo(toAccount, average, date = date.AddMonths(i))

        let diff = fromAccount.Balance - balance

        if not diff.IsZero then
            let lastTrans =
                fromAccount.Transactions
                |> Seq.sortBy (fun x -> x.Date)
                |> Seq.last

            let editor = lastTrans.BeginEdit()

            for split in lastTrans.Splits do
                if split.Account = fromAccount then
                    split.Value <- split.Value - diff
                else if split.Account = toAccount then
                    split.Value <- split.Value + diff

            editor.Save()
