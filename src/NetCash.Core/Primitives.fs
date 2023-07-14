namespace rec NetCash

open System
open System.Reflection
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open NetCash.Marshalling

/// <summary>Type of the commodity.
/// Technically the subset of GNCAccountType.
/// </summary>
type CommodityType =
    | Fund
    | Stock

    /// Converts to enum AsGNCAccountType.
    member self.AsGNCAccountType =
        match self with
        | Fund -> Bindings.GNCAccountType.ACCT_TYPE_MUTUAL
        | Stock -> Bindings.GNCAccountType.ACCT_TYPE_STOCK

/// An account.
type Account private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    /// Gets the (future) balance for the account itself only.
    member self.Balance = self.GetFutureBalance(false)

    /// Gets the future balance.
    member self.GetFutureBalance
        (
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetBalanceInCurrency (
            GnuCashObject.nativeHandle self,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the balance before a certain time.
    member self.GetBalanceBeforeDate
        (
            date: DateTime,
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetBalanceAsOfDateInCurrency (
            GnuCashObject.nativeHandle self,
            Marshalling.DateTime.toTimestamp date,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the cleared blanace.
    member self.GetClearedBalance
        (
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetClearedBalanceInCurrency (
            GnuCashObject.nativeHandle self,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the reconciled blanace.
    member self.GetReconciledBalance
        (
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetReconciledBalanceInCurrency (
            GnuCashObject.nativeHandle self,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the present blanace.
    member self.GetPresentBalance
        (
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetPresentBalanceInCurrency (
            GnuCashObject.nativeHandle self,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the projected minimum blanace.
    member self.GetProjectedMinimumBalance
        (
            [<Optional; DefaultParameterValue false>] recursive: bool,
            [<Optional>] currency: Commodity
        ) =
        Bindings.xaccAccountGetProjectedMinimumBalanceInCurrency (
            GnuCashObject.nativeHandle self,
            self.Currency
            |> defaultArg (Option.ofUncheckedDefault currency)
            |> GnuCashObject.nativeHandle,
            recursive
        )
        |> GncNumeric

    /// Gets the name.
    member _.Name
        with get () =
            Bindings.xaccAccountGetName (handle)
            |> String.fromBorrowed
        and set (name) = Bindings.xaccAccountSetName (handle, name)

    /// Gets the full name.
    member _.FullName =
        Bindings.gnc_account_get_full_name (handle)
        |> String.fromOwned

    /// Gets the code.
    member self.Code
        with get () =
            Bindings.xaccAccountGetCode (handle)
            |> String.fromBorrowed
        and set (code: string) = Bindings.xaccAccountSetCode (handle, code)

    /// Gets the notes.
    member _.Notes
        with get () =
            Bindings.xaccAccountGetNotes (handle)
            |> String.fromBorrowed
        and set (notes: string) = Bindings.xaccAccountSetNotes (handle, notes)

    /// Gets the description.
    member _.Description
        with get () =
            Bindings.xaccAccountGetDescription (handle)
            |> String.fromBorrowed
        and set (desc: string) = Bindings.xaccAccountSetDescription (handle, desc)

    /// Gets the type.
    member self.Type
        with get () = Bindings.xaccAccountGetType (handle)
        and set (actType) = Bindings.xaccAccountSetType (handle, actType)

    /// Gets the color.
    member self.Color
        with get () =
            Bindings.xaccAccountGetColor handle
            |> String.fromBorrowed
        and set (value) = Bindings.xaccAccountSetColor (handle, value)

    /// Gets the filter.
    member self.Filter
        with get () =
            Bindings.xaccAccountGetFilter handle
            |> String.fromBorrowed
        and set (value) = Bindings.xaccAccountSetFilter (handle, value)

    /// Gets the policy.
    member self.Policy =
        Bindings.gnc_account_get_policy (handle)
        |> Marshal.PtrToStructure
        |> GNCPolicy

    /// Gets the last num.
    member self.LastNum =
        Bindings.xaccAccountGetLastNum handle
        |> String.fromBorrowed

    /// Gets whether the balance computation is defered.
    member self.IsBalanceComputationDefered =
        Bindings.gnc_account_get_defer_bal_computation handle

    /// Gets the gains account in specified currency.
    /// (There's no setter because the code in GnuCash was written in C++ rather than C.)
    member self.GetGainsAccountInCurrency(currency: Commodity) =
        Bindings.xaccAccountGainsAccount (handle, GnuCashObject.nativeHandle currency)
        |> GnuCashObject.Registry.getOrCreate<Account>

    /// Gets whether this account is a placeholder.
    member self.IsPlaceholder
        with set (value) = Bindings.xaccAccountSetPlaceholder (handle, value)
        and get () = Bindings.xaccAccountGetPlaceholder (handle)

    /// Gets whether this account is hidden.
    member self.Hidden
        with set (hidden) = Bindings.xaccAccountSetHidden (handle, hidden)
        and get () = Bindings.xaccAccountGetHidden (handle)

    /// Gets descendant accounts of this account.
    member self.Descendants =
        use wrapper = Bindings.gnc_account_get_descendants handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Account>

    member self.HasChild(childName) =
        Bindings.gnc_account_get_children handle
        |> SafeHandle.map (fun children ->
            Bindings.g_list_find_custom (
                children,
                IntPtr.Zero,
                Bindings.GCompareFunc (fun c _ ->
                    String
                        .fromBorrowed(Bindings.xaccAccountGetName (c))
                        .CompareTo(childName))
            )
            |> SafeHandle.wrap Bindings.g_list_free
            |> SafeHandle.map (fun list -> list <> IntPtr.Zero))

    /// Gets the immediate children accounts.
    member self.Children =
        use wrapper = Bindings.gnc_account_get_children handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Account>

    /// Gets whether this account is empty.
    member self.IsEmpty =
        Seq.isEmpty self.Splits
        && Seq.isEmpty self.Children

    /// Gets the parent account.
    member self.Parent
        with get () =
            handle
            |> Bindings.gnc_account_get_parent
            |> GnuCashObject.Registry.getOrCreate<Account>

        and set (parent: Account) = Bindings.gnc_account_append_child (GnuCashObject.nativeHandle parent, handle)

    /// Gets the currency of this account or its parent or default currency.
    member self.CurrencyOrDefault =
        let mutable currencyFromSelf = false

        Bindings.gnc_account_or_default_currency (handle, &currencyFromSelf)
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// Gets or sets the currency.
    member self.Currency
        with get () =
            handle
            |> Bindings.xaccAccountGetCommodity
            |> GnuCashObject.Registry.getOrCreate<Commodity>

        and set (value: Commodity) = Bindings.xaccAccountSetCommodity (handle, GnuCashObject.nativeHandle value)

    /// Gets splits (journal entries).
    member _.Splits =
        use wrapper = Bindings.xaccAccountGetSplitList handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Split>

    /// Move splits to another account.
    member _.MoveSplits(toAccount: Account) =
        Bindings.xaccAccountMoveAllSplits (handle, GnuCashObject.nativeHandle toAccount)

    /// Gets lots.
    member _.Lots =
        use wrapper = Bindings.xaccAccountGetLotList handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Lot>

    /// Gets transactions.
    member _.Transactions: seq<Transaction> =
        let result = ResizeArray()

        Bindings.xaccAccountForEachTransaction (
            handle,
            Bindings.TransactionCallback (fun trans _ ->
                trans
                |> GnuCashObject.Registry.getOrCreate<Transaction>
                |> result.Add

                0),
            IntPtr.Zero
        )
        |> ignore

        result

    /// <summary>Creates a typical 2-split transaction.</summary>
    /// <param name="fromAmount">Amount (positive) to withdraw from this account.</param>
    /// <param name="toAccount">The credit account.</param>
    /// <param name="toAmount">Amount to deposit. Defaults to fromAmount.</param>
    /// <param name="date">The date. Defaults to today.</param>
    member self.TransferTo
        (
            toAccount: Account,
            fromAmount: GncNumeric,
            [<Optional>] toAmount: GncNumeric,
            [<Optional>] date: DateOnly,
            [<Optional>] description: string
        ) =
        let trans =
            (GnuCashObject.make<Transaction> (GnuCashObject.bookHandle self))
                .BeginEdit()

        trans.SetDate(defaultArg (Option.ofUncheckedDefault date) DateOnly.Today)
        |> ignore

        trans.SetCurrency self.Currency |> ignore

        let toAmt = defaultArg (Option.ofUncheckedDefault toAmount) fromAmount

        trans.AddSplit(self, -fromAmount) |> ignore

        trans.AddSplit(toAccount, fromAmount, amount = toAmt)
        |> ignore

        if not (isUncheckedDefault description) then
            trans.SetDescription description |> ignore

        trans.Save()

    // Method chaining -------------------------------------------------------------

    member self.SetName(name) =
        self.Name <- name
        self

    member self.SetParent(parent) =
        self.Parent <- parent
        self

    member self.SetCurrency(currency) =
        self.Currency <- currency
        self

    member self.SetType(accountType) =
        self.Type <- accountType
        self

    member self.SetPlaceholder(isPlaceholder) =
        self.IsPlaceholder <- isPlaceholder
        self

    /// Sets parent along with currency and type from parent.
    member self.Inherit(parent) =
        self
            .SetParent(parent)
            .SetCurrency(parent.CurrencyOrDefault)
            .SetType(parent.Type)

    member self.Invoke(config: Action<Account>) =
        config.Invoke self
        self

    // -----------------------------------------------------------------------------

    /// <summary>Creates a child account.</summary>
    /// <param name="name">The name.</param>
    member self.NewChildAccount(name) =
        GnuCashObject
            .make<Account>(GnuCashObject.bookHandle self)
            .SetName(name)
            .Inherit(self)

    /// <summary>Creates a commodity type and an associated account.</summary>
    member self.NewCommodityAccount
        (
            kind: CommodityType,
            fullName,
            ns,
            symbol,
            [<Optional; DefaultParameterValue(1)>] fraction,
            [<Optional>] uniqueCode
        ) =
        let book = GnuCashObject.bookHandle self

        let mutable comm =
            Bindings.gnc_commodity_new (book, fullName, ns, symbol, uniqueCode, fraction)

        comm <- Bindings.gnc_commodity_table_insert (Bindings.gnc_commodity_table_get_table (book), comm)

        self
            .NewChildAccount(fullName)
            .SetType(kind.AsGNCAccountType)
            .SetCurrency(GnuCashObject.Registry.getOrCreate<Commodity> comm)

    /// Gets or sets the account separator.
    static member Separator
        with get () =
            Bindings.gnc_get_account_separator_string ()
            |> String.fromBorrowed
        and set (sep) = Bindings.gnc_set_account_separator sep

/// Accounting Policy.  The Accounting Policy determines how splits are assigned to lots.
type GNCPolicy(policy: Bindings.gncpolicy_s) =
    member _.Name = policy.name |> String.fromBorrowed
    member _.Description = policy.description |> String.fromBorrowed
    member _.Hint = policy.hint |> String.fromBorrowed

    member _.GetLot =
        Marshal.GetDelegateForFunctionPointer<Bindings.PolicyGetLot> policy.PolicyGetLot

    member _.GetSplit =
        Marshal.GetDelegateForFunctionPointer<Bindings.PolicyGetSplit> policy.PolicyGetSplit

    member _.GetLotOpening =
        Marshal.GetDelegateForFunctionPointer<Bindings.PolicyGetLotOpening> policy.PolicyGetLotOpening

    member _.IsOpeningSplit =
        Marshal.GetDelegateForFunctionPointer<Bindings.PolicyIsOpeningSplit> policy.PolicyIsOpeningSplit

type QuoteSource private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    member _.Type = Bindings.gnc_quote_source_get_type handle

    member _.Index = Bindings.gnc_quote_source_get_index handle

    member _.UserName =
        Bindings.gnc_quote_source_get_user_name handle
        |> String.fromBorrowed

    member _.InternalName =
        Bindings.gnc_quote_source_get_internal_name handle
        |> String.fromBorrowed

    static member FinancialQuoteInstalled = Bindings.gnc_quote_source_fq_installed ()

    static member FinancialQuoteVersion =
        Bindings.gnc_quote_source_fq_version ()
        |> String.fromBorrowed

type CommodityNamespace private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    member _.IsISO = Bindings.gnc_commodity_namespace_is_iso handle

    member _.Name =
        Bindings.gnc_commodity_namespace_get_name handle
        |> String.fromBorrowed

    member _.Commodities =
        use wrapper = Bindings.gnc_commodity_namespace_get_commodity_list handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Commodity>

/// Commodity
type Commodity private (handle: nativeint) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    override _.Equals other =
        match other with
        | :? Commodity as comm -> Bindings.gnc_commodity_equal (handle, GnuCashObject.nativeHandle comm)
        | :? IntPtr as ptr -> Bindings.gnc_commodity_equal (handle, ptr)
        | _ -> false

    override _.GetHashCode() = int handle

    /// Gets the mnemonic, for publicly traded stocks this is typically the ticker symbol.
    member self.Mnemonic
        with get () =
            Bindings.gnc_commodity_get_mnemonic (handle)
            |> String.fromBorrowed
        and set value = Bindings.gnc_commodity_set_mnemonic (handle, value)

    /// Gets the identification code (such as CUSIP or ISIN).
    member self.IdentificationCode
        with get () =
            Bindings.gnc_commodity_get_cusip (handle)
            |> String.fromBorrowed
        and set value = Bindings.gnc_commodity_set_cusip (handle, value)

    /// Gets the namespace.
    member self.Namespace =
        Bindings.gnc_commodity_get_namespace_ds (handle)
        |> GnuCashObject.Registry.getOrCreate<CommodityNamespace>

    /// Gets the full name.
    member self.FullName
        with get () =
            Bindings.gnc_commodity_get_fullname (handle)
            |> String.fromBorrowed
        and set value = Bindings.gnc_commodity_set_fullname (handle, value)

    /// Gets the print name.
    member self.PrintName =
        Bindings.gnc_commodity_get_printname (handle)
        |> String.fromBorrowed

    /// Gets the unique name.
    member self.UniqueName =
        Bindings.gnc_commodity_get_unique_name (handle)
        |> String.fromBorrowed

    /// Gets the fraction, i.e. the smallest division of this commodity allowed.
    member _.Fraction
        with get () = Bindings.gnc_commodity_get_fraction handle
        and set (value) = Bindings.gnc_commodity_set_fraction (handle, value)

    /// Get whether the automatic price retrival is enabled.
    member _.IsAutomaticPriceRetrivalEnabled
        with get () = Bindings.gnc_commodity_get_quote_flag handle
        and set (value) = Bindings.gnc_commodity_set_quote_flag (handle, value)

type ReconciliationFlags =
    /// The Split has been cleared (c)
    | CLEARED = 99uy
    /// The Split has been reconciled (y)
    | RECONCILED = 121uy
    /// frozen into accounting period (f)
    | FROZEN = 102uy
    /// not reconciled or cleared (n)
    | NEW = 110uy
    /// split is void (v)
    | VOID = 118uy

/// Split
type Split private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    /// Gets the account.
    member self.Account
        with get () =
            handle
            |> Bindings.xaccSplitGetAccount
            |> GnuCashObject.Registry.getOrCreate<Account>

        and set (acct: Account) = Bindings.xaccSplitSetAccount (handle, GnuCashObject.nativeHandle acct)

    /// Gets the transaction.
    member self.Transaction
        with get () =
            handle
            |> Bindings.xaccSplitGetParent
            |> GnuCashObject.Registry.getOrCreate<Transaction>

        and set (trans: Transaction) = Bindings.xaccSplitSetParent (handle, GnuCashObject.nativeHandle trans)

    /// Gets the memo.
    member self.Memo
        with get () =
            Bindings.xaccSplitGetMemo (handle)
            |> String.fromBorrowed
        and set (memo) = Bindings.xaccSplitSetMemo (handle, memo)

    /// Gets the action.
    member self.Action
        with get () =
            Bindings.xaccSplitGetAction (handle)
            |> String.fromBorrowed
        and set (action) = Bindings.xaccSplitSetAction (handle, action)

    /// Gets the reconciliation status.
    member _.ReconciliationStatus
        with get () =
            Bindings.xaccSplitGetReconcile handle
            |> LanguagePrimitives.EnumOfValue<_, ReconciliationFlags>
        and set (flag: ReconciliationFlags) =
            Bindings.xaccSplitSetReconcile (handle, LanguagePrimitives.EnumToValue<_, _> flag)

    /// Gets the reconciliation date.
    member _.DateReconciled
        with get () =
            Bindings.xaccSplitGetDateReconciled handle
            |> Marshalling.DateOnly.fromTimestamp
        and set (date) = Bindings.xaccSplitSetDateReconciledSecs (handle, Marshalling.DateOnly.toTimestamp date)

    // - If account and transaction shares the same commodity, set both amount and value
    // - Otherwise, only set amount
    member internal self.SetBaseValue(value: GncNumeric) =
        Bindings.xaccSplitSetBaseValue (handle, value.value, GnuCashObject.nativeHandle self.Account.Currency)

    /// Gets the amount, which is the amount of the account's commodity involved.
    member _.Amount
        with set (amount: GncNumeric) = Bindings.xaccSplitSetAmount (handle, amount.value)
        and get () = Bindings.xaccSplitGetAmount (handle) |> GncNumeric

    /// Gets the value, which is the amount of the transaction balancing commodity (i.e. currency) involved.
    member _.Value
        with set (value: GncNumeric) = Bindings.xaccSplitSetValue (handle, value.value)
        and get () = Bindings.xaccSplitGetValue (handle) |> GncNumeric

    /// Gets the lot.
    member _.Lot =
        handle
        |> Bindings.xaccSplitGetLot
        |> GnuCashObject.Registry.getOrCreate<Lot>

/// Lot
type Lot private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    /// Gets the balance.
    member _.Balance = Bindings.gnc_lot_get_balance handle |> GncNumeric

    /// Gets whether this lot is closed.
    member _.Closed = Bindings.gnc_lot_is_closed handle

    /// Gets the title.
    member _.Title =
        Bindings.gnc_lot_get_title handle
        |> String.fromBorrowed

    /// Gets the notes.
    member _.Notes =
        Bindings.gnc_lot_get_notes handle
        |> String.fromBorrowed

    /// Gets the splits.
    member _.Splits =
        use wrapper = Bindings.gnc_lot_get_split_list handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Split>

    /// Assigns a split to this lot.
    member _.AddSplit(split: Split) =
        Bindings.gnc_lot_add_split (handle, GnuCashObject.nativeHandle split)

    /// Removes a split from this lot.
    member _.RemoveSplit(split: Split) =
        Bindings.gnc_lot_remove_split (handle, GnuCashObject.nativeHandle split)

    /// <summary>Gets the open date of this lot.</summary>
    /// <exception cref="InvalidOperationException">Throws when this lot doesn't have any splits.</exception>
    member self.OpenDate =
        if Seq.isEmpty self.Splits then
            invalidOp "Try to get open date on a lot without any splits."

        handle
        |> Bindings.gnc_lot_get_earliest_split
        |> Bindings.xaccSplitGetParent
        |> Bindings.xaccTransGetDate
        |> Marshalling.DateTime.fromTimestamp

    /// <summary>Gets the closing date of this lot.</summary>
    /// <exception cref="InvalidOperationException">Throws when this lot doesn't have any splits or is still open.</exception>
    member self.ClosingDate =
        if self.Splits |> Seq.isEmpty then
            invalidOp "Try to get closing date on a lot without any splits."

        if not self.Closed then
            invalidOp "Try to get closing date on a open lot."

        handle
        |> Bindings.gnc_lot_get_latest_split
        |> Bindings.xaccSplitGetParent
        |> Bindings.xaccTransGetDate
        |> Marshalling.DateTime.fromTimestamp

    /// Gets the realized gains of this lot.
    member self.RealizedGains =
        let currencyEquals =
            let mutable gainsCurrency = IntPtr.Zero

            fun split ->
                let currency =
                    split
                    |> GnuCashObject.nativeHandle
                    |> Bindings.xaccSplitGetParent
                    |> Bindings.xaccTransGetCurrency

                if gainsCurrency = IntPtr.Zero then
                    gainsCurrency <- currency

                Bindings.gnc_commodity_equal (currency, gainsCurrency)

        query {
            for split in self.Splits do
                where (split.Amount.IsZero && currencyEquals split)
                sumBy split.Value
        }

/// The transaction editor.
type TransactionEditor internal (trans: Transaction) =
    let book = GnuCashObject.bookHandle trans
    let handle = GnuCashObject.nativeHandle trans
    let mutable recordPrice = false
    do Bindings.xaccTransBeginEdit handle

    /// Sets the date.
    member self.SetDate(date: DateOnly) =
        Bindings.xaccTransSetDatePostedSecsNormalized (handle, Marshalling.DateOnly.toTimestamp date)
        self

    /// Sets the description.
    member self.SetDescription(desc) =
        Bindings.xaccTransSetDescription (handle, desc)
        self

    /// Sets the currency.
    member self.SetCurrency(currency: Commodity) =
        Bindings.xaccTransSetCurrency (handle, GnuCashObject.nativeHandle currency)
        self

    /// If called with true, when saving the transaction, the price database will be inserted with records of the commodity price.
    member self.RecordCommodityPrice(record) =
        recordPrice <- record
        self

    /// <summary>Adds a split.</summary>
    /// <param name="account">The account to add split.</param>
    /// <param name="value">The value. Positive means to debit, negative means to credit.</param>
    /// <param name="amount">If the currency of account is the same as of transaction, then this can be omitted. Otherwise this parameter must be provided.</param>
    /// <param name="memo">Memo.</param>
    /// <param name="action">Action.</param>
    /// <param name="reconcile">Reconciliation status.</param>
    /// <param name="dateReconciled">Date of reconciliation status.</param>
    member self.AddSplit
        (
            account: Account,
            value: GncNumeric,
            [<Optional>] amount: GncNumeric,
            [<Optional>] memo: string,
            [<Optional>] action: string,
            [<Optional; DefaultParameterValue(ReconciliationFlags.NEW)>] reconcile: ReconciliationFlags,
            [<Optional>] dateReconciled: DateOnly
        ) =
        let split = GnuCashObject.make<Split> book

        split.Transaction <- trans
        split.Account <- account
        split.Value <- value

        // if account and transaction share the same commodity, this will set both amount and value;
        // otherwise, it only sets the amount, although incorrect, the caller should specify amount in this case
        split.SetBaseValue value

        if not (isUncheckedDefault amount) then
            split.Amount <- amount

        if not (isUncheckedDefault action) then
            split.Action <- action

        if not (isUncheckedDefault memo) then
            split.Memo <- memo

        split.ReconciliationStatus <- reconcile

        if not (isUncheckedDefault dateReconciled) then
            split.DateReconciled <- dateReconciled

        self

    /// <summary>Add an auto-balancing split for the specified account.</summary>
    member self.AddBalancingSplit(account: Account) =
        Bindings.xaccTransScrubImbalance (
            handle,
            Bindings.gnc_book_get_root_account book,
            GnuCashObject.nativeHandle account
        )

        self

    /// <summary>Saves the changes.</summary>
    /// <param name="recordPrice">Whether or not to save commodity price to price DB.</param>
    member _.Save() =
        Bindings.xaccTransCommitEdit handle

        if recordPrice then
            Bindings.xaccTransRecordPrice (handle, Bindings.PriceSource.PRICE_SOURCE_USER_PRICE)

    /// <summary>
    /// Rollbacks changes made to the transaction.
    /// </summary>
    /// <remarks>
    /// NOTE: This might not rollback all the changes, according to the comment in GnuCash codebase:
    /// "The Rollback function is terribly complex, and, what's worse,
    /// it only rolls back the basics.  The TransCommit functions did a bunch
    /// of Lot/Cap-gains scrubbing that don't get addressed/undone here, and
    /// so the rollback can potentially leave a bit of a mess behind."
    /// </remarks>
    member _.Discard() = Bindings.xaccTransRollbackEdit handle

/// <summary>
/// A read-only transaction. To make changes, call <see cref="BeginEdit" /> to get a <see cref="TransactionEditor" />.
/// </summary>
type Transaction private (handle) as self =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    /// Creates a TransactionEditor to make changes to this transaction.
    member _.BeginEdit() : TransactionEditor = TransactionEditor self

    /// Gets the date.
    member _.Date =
        handle
        |> Bindings.xaccTransGetDate
        |> Marshalling.DateTime.fromTimestamp

    /// Gets the description.
    member _.Description =
        handle
        |> Bindings.xaccTransGetDescription
        |> String.fromBorrowed

    /// Gets the currency.
    member _.Currency =
        handle
        |> Bindings.xaccTransGetCurrency
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// <summary>Creates a reversing transaction to this one.</summary>
    member _.CreateReversingTransaction([<Optional>] date: DateOnly) =
        let trans =
            handle
            |> Bindings.xaccTransReverse
            |> GnuCashObject.Registry.getOrCreate<Transaction>

        let editor = trans.BeginEdit()

        editor.SetDate(defaultArg (Option.ofUncheckedDefault date) DateOnly.Today)
        |> ignore

        editor.Save()
        trans

    member _.Splits =
        use wrapper = Bindings.xaccTransGetSplitList handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Split>
