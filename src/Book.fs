namespace rec NetCash

open System
open System.Collections.ObjectModel
open System.Collections.Generic
open System.Runtime.InteropServices

open NetCash.Marshalling

module private Session =
    let raiseBackendErrorMaybe sess =
        let code = Bindings.qof_session_get_error sess

        let message =
            sess
            |> Bindings.qof_session_get_error_message
            |> String.fromBorrowed

        Bindings.qof_session_pop_error sess |> ignore

        if code
           <> Bindings.QofBackendError.ERR_BACKEND_NO_ERR then
            raise (GnuCashBackendException(code, message))

    // We don't use the static current session, but some functions
    // in gnucash does, such as `gnc_default_currency`.
    // If we don't maintain this variable, it might leak.
    let replaceCurrent = Bindings.gnc_set_current_session

    let start (uri: GnuCashUri) (mode: Bindings.SessionOpenMode) =
        let book = Bindings.qof_book_new ()
        let session = Bindings.qof_session_new book
        Bindings.qof_session_begin (session, string uri, mode)

        let creating =
            (mode
             &&& Bindings.SessionOpenMode.SESSION_NEW_STORE) = Bindings.SessionOpenMode.SESSION_NEW_STORE

        if creating then
            Bindings.gnc_account_create_root book |> ignore
        else
            Bindings.xaccLogDisable ()
            Bindings.qof_session_load (session, IntPtr.Zero)
            Bindings.xaccLogEnable ()

        raiseBackendErrorMaybe session

        replaceCurrent session

        session

module private BookStack =
    let books = Stack<Book>()

    let tryPeek = books.TryPeek

    let push (book: Book) =
        books.Push book
        Bindings.gnc_hook_run (Bindings.HOOK_BOOK_OPENED, book.SessionHandle)

    let drop () =
        let book = books.Pop()

        match tryPeek () with
        | false, _ -> Session.replaceCurrent IntPtr.Zero
        | _, book -> Session.replaceCurrent book.SessionHandle

        Bindings.gnc_hook_run (Bindings.HOOK_BOOK_CLOSED, book.SessionHandle)

type AccountFinder =
    | ByGuid of Guid
    | ByName of string
    | ByFullName of string []
    | ByCode of string

    member self.Find(book) =
        match self with
        | ByName name -> Bindings.gnc_account_lookup_by_name (Bindings.gnc_book_get_root_account (book), name)
        | ByGuid guid ->
            guid
            |> Guid.toSafeHandle
            |> SafeHandle.using (fun ptr -> Bindings.xaccAccountLookup (ptr, book))
        | ByCode code -> Bindings.gnc_account_lookup_by_code (Bindings.gnc_book_get_root_account (book), code)
        | ByFullName names ->
            let rec findByFullName (parent: Account) (childName: string []) =
                if childName.Length = 0 then
                    Unchecked.defaultof<_>
                else
                    match parent.Children
                          |> Seq.tryFind (fun acc -> acc.Name = childName[0])
                        with
                    | Some child ->
                        if childName.Length = 1 then
                            GnuCashObject.nativeHandle child
                        else
                            findByFullName child (childName[1..])
                    | None -> Unchecked.defaultof<_>

            findByFullName
                (book
                 |> Bindings.gnc_book_get_root_account
                 |> GnuCashObject.Registry.getOrCreate<Account>)
                names

/// A book is the container for data stored in a gnucash database.
/// In the design of gnucash, Session and Book are two separate abstractions.
/// Session represents the connection to the backend storage, while
/// Book represents the container for domain objects, like Accounts, Transactions, Splits etc.
/// For simplicity, NetCash combined the two concepts into one and just called it Book.
/// Additionally, Book also serves as the "Factory" to create some accounting objects, like Accounts and Transactions.
type Book private (session: nativeint) as self =
    let handle = Bindings.qof_session_get_book session

    let commodities =
        lazy CommodityTable(handle, Bindings.gnc_commodity_table_get_table handle)

    let mutable closed = false

    do BookStack.push self

    interface INativeWrapper with
        member _.NativeHandle = handle

    /// <summary>Openes an existing book.</summary>
    /// <param name="uri">URI to the book.</param>
    /// <param name="ignoreLock">true to take over the existing lock if necessary.</param>
    /// <exception cref="GnuCashBackendException">Throws when an error occured while opening the book.</exception>
    /// <remarks>
    /// <para>
    /// For XML backend, when it opens a book for writing, it creates a backup file with timestamp "YYYYMMDDHHMMSS" in the name.
    /// So if your code tries to open/close a book twice within one second, the latter session will try to make a backup file
    /// with the same name created by previous session, and it's going to fail with ERR_FILEIO_BACKUP_ERROR,
    /// because the backup file is created using the combination of O_CREAT and O_EXCL, which disallows overwriting an existing file.
    /// </para>
    /// <para>
    /// There is a quick fix that I don't recommend, which is to call `Bindings.gnc_prefs_set_file_retention_policy(Bindings.XMLFileRetentionType.XML_RETAIN_NONE)`
    /// (it only updates the in-memory value without touching the preference database). I have to warn you
    /// that this is not to disable the backup, it is to tell the backend to delete all the backup and log files of the book when finishes saving!!!
    /// which might not be what you want!
    /// </para>
    /// </remarks>
    static member Open(uri: GnuCashUri, [<Optional; DefaultParameterValue false>] ignoreLock: bool) =
        new Book(
            Session.start
                uri
                (if ignoreLock then
                     Bindings.SessionOpenMode.SESSION_BREAK_LOCK
                 else
                     Bindings.SessionOpenMode.SESSION_NORMAL_OPEN)
        )

    /// <summary>Openes an existing book for read-only.</summary>
    /// <param name="uri">URI to the book.</param>
    /// <exception cref="GnuCashBackendException">Throws when an error occured while opening the book.</exception>
    static member OpenRead(uri: GnuCashUri) =
        new Book(Session.start uri Bindings.SessionOpenMode.SESSION_READ_ONLY)

    /// <summary>Creates and opens a book.</summary>
    /// <param name="uri">URI to the book.</param>
    /// <param name="overwrite">true to overwrite the existing book.</param>
    /// <exception cref="GnuCashBackendException">Throws when an error occured while opening the book.</exception>
    static member Create(uri: GnuCashUri, [<Optional; DefaultParameterValue false>] overwrite: bool) =
        new Book(
            Session.start
                uri
                (if overwrite then
                     Bindings.SessionOpenMode.SESSION_NEW_OVERWRITE
                 else
                     Bindings.SessionOpenMode.SESSION_NEW_STORE)
        )

    /// <summary>Gets the currently opened book. Returns null when there's no book currently opened.</summary>
    /// <remarks>
    /// All the opened books are recorded on a stack. This property actually returns the top book from the stack.
    /// The stack pops when the top book gets disposed, you have to make sure every book is disposed timely and in proper order,
    /// otherwise it's going to be messed up :-/
    /// </remarks>
    static member Current =
        match BookStack.tryPeek () with
        | false, _ -> Unchecked.defaultof<_>
        | _, book -> book

    /// Gets the native pointer to session.
    member _.SessionHandle = session

    /// Gets whether this book was opened as readonly.
    member _.IsReadOnly = Bindings.qof_book_is_readonly handle

    /// Makes this book readonly.
    member _.MarkReadonly() = Bindings.qof_book_mark_readonly handle

    /// Gets whether this book was configured to use trading accounts.
    member _.UseTradingAccounts = Bindings.qof_book_use_trading_accounts handle

    /// Gets the name of the currency of this book.
    member _.GetBookCurrencyName() =
        Bindings.qof_book_get_book_currency_name handle
        |> String.fromBorrowed

    /// Gets the default gains policy.
    member _.GetDefaultGainsPolicy() =
        Bindings.qof_book_get_default_gains_policy handle
        |> String.fromBorrowed

    /// Gets the GUID for the gain/loss account.
    member _.GetDefaultGainLossAcctGuid() =
        Bindings.qof_book_get_default_gain_loss_acct_guid handle
        |> Guid.fromPointer

    /// Gets whether this book is empty.
    member _.IsEmpty = Bindings.qof_book_empty handle

    member internal _.TryFindAccount(finder: AccountFinder, account: outref<Account>) =
        let accountHandle = finder.Find handle

        if isUncheckedDefault accountHandle then
            false
        else
            account <- GnuCashObject.Registry.getOrCreate<Account> accountHandle
            true

    member internal self.FindAccount(finder) =
        match self.TryFindAccount(finder) with
        | false, _ -> raise AccountNotFoundException
        | _, account -> account

    member internal self.FindAccountMaybe(finder) =
        finder |> self.TryFindAccount |> Option.ofPair

    /// <summary>Finds an account or throws an exception. If you don't want it to throw, use the TryXXX version.</summary>
    /// <returns>The account.</returns>
    /// <exception cref="AccountNotFoundException">Throws when the specified account can't be found.</exception>
    member self.FindAccountByName(name) =
        self.FindAccount(AccountFinder.ByName name)

    member self.TryFindAccountByName(name, account: outref<Account>) =
        self.TryFindAccount(AccountFinder.ByName name, &account)

    /// <summary>Finds an account or throws an exception. If you don't want it to throw, use the TryXXX version.</summary>
    /// <returns>The account.</returns>
    /// <exception cref="AccountNotFoundException">Throws when the specified account can't be found.</exception>
    member self.FindAccountByFullName([<ParamArray>] names) =
        self.FindAccount(AccountFinder.ByFullName names)

    member self.TryFindAccountByFullName([<ParamArray>] names, account: outref<Account>) =
        self.TryFindAccount(AccountFinder.ByFullName names, &account)

    /// <summary>Finds an account or throws an exception. If you don't want it to throw, use the TryXXX version.</summary>
    /// <returns>The account.</returns>
    /// <exception cref="AccountNotFoundException">Throws when the specified account can't be found.</exception>
    member self.FindAccountByGuid(guid) =
        self.FindAccount(AccountFinder.ByGuid guid)

    member self.TryFindAccountByGuid(guid, account: outref<Account>) =
        self.TryFindAccount(AccountFinder.ByGuid guid, &account)

    /// <summary>Finds an account or throws an exception. If you don't want it to throw, use the TryXXX version.</summary>
    /// <returns>The account.</returns>
    /// <exception cref="AccountNotFoundException">Throws when the specified account can't be found.</exception>
    member self.FindAccountByCode(code) =
        self.FindAccount(AccountFinder.ByCode code)

    member self.TryFindAccountByCode(code, account: outref<Account>) =
        self.TryFindAccount(AccountFinder.ByCode code, &account)

    /// Gets the (hidden) root account.
    member _.RootAccount =
        handle
        |> Bindings.gnc_book_get_root_account
        |> GnuCashObject.Registry.getOrCreate<Account>

    /// Gets all accounts in this book.
    member self.Accounts = self.RootAccount.Descendants

    /// <summary>Deletes an account.</summary>
    /// <remarks><list type="bullet">
    /// <item><description>The underlying native resource gets destroyed after this operation, although the managed Account object is still accessible, it becomes invalid, any usage afterwards would get unexpected result.</description></item>
    /// <item><description>The account has to be empty before being deleted, otherwise an exception is thrown.</description></item>
    /// </list></remarks>
    member _.DeleteAccount(acct: Account) =
        if not acct.IsEmpty then
            invalidOp
                "This account can not be deleted, because it still contains splits or sub-accounts. You have to move them to another account before deletion."

        let a = GnuCashObject.nativeHandle acct
        Bindings.xaccAccountBeginEdit a
        Bindings.xaccAccountDestroy a

    /// <summary>Creates a new transaction.</summary>
    member self.NewTransaction([<Optional>] date, [<Optional>] currency: Commodity) =
        let editor =
            (GnuCashObject.make<Transaction> handle)
                .BeginEdit()

        editor.SetDate(defaultArg (Option.ofUncheckedDefault date) DateOnly.Today)
        |> ignore

        editor.SetCurrency(defaultArg (Option.ofUncheckedDefault currency) self.DefaultCurrency)

    member internal self.SetDirtyCallback(cb) =
        Bindings.qof_book_set_dirty_cb (handle, cb, IntPtr.Zero)

    /// Gets the default currency.
    member self.DefaultCurrency =
        Bindings.gnc_default_currency ()
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// Gets the commodity table.
    member _.CommodityTable = commodities.Value

    /// Gets the price database.
    member _.PriceDB =
        GnuCashObject.Registry.getOrCreate<PriceDB> (Bindings.gnc_pricedb_get_db handle)

    /// <summary>Saves changes made to this book.</summary>
    /// <exception cref="GnuCashBackendException">Throws when an error occured while saving the book.</exception>
    member self.Save() =
        Bindings.qof_session_save (session, Bindings.QofBePercentageFunc(fun _ _ -> ()))
        Session.raiseBackendErrorMaybe session

    /// <summary>Saves current book to another URI.</summary>
    /// <returns>A new book or current book when uri is the same.</returns>
    /// <remarks>WARNING: current book closes after being saved to a different uri, so never use the original book after this operation.</remarks>
    /// <exception cref="GnuCashBackendException">Throws when an error occured while saving the book.</exception>
    member self.SaveAs(uri: GnuCashUri) =
        if uri.Equals self.URI then
            self.Save()
            self
        else
            let newSession = Bindings.qof_session_new IntPtr.Zero
            Bindings.qof_session_begin (newSession, string uri, Bindings.SessionOpenMode.SESSION_NEW_STORE)
            Session.raiseBackendErrorMaybe newSession

            Bindings.qof_event_suspend ()

            Bindings.qof_session_swap_data (session, newSession)
            Bindings.qof_book_mark_session_dirty (Bindings.qof_session_get_book newSession)

            Bindings.qof_event_resume ()

            Session.raiseBackendErrorMaybe newSession

            Bindings.qof_session_save (newSession, Bindings.QofBePercentageFunc(fun _ _ -> ()))
            Session.raiseBackendErrorMaybe newSession

            self.Close()
            Session.replaceCurrent newSession

            new Book(newSession)

    /// Gets the file path for this book if it's a file-based backend.
    member self.FilePath =
        session
        |> Bindings.qof_session_get_file_path
        |> String.fromBorrowed

    /// Gets the URI for this book.
    member self.URI =
        session
        |> Bindings.qof_session_get_url
        |> String.fromBorrowed
        |> GnuCashUri

    /// <summary>Creates a new account at top-level with default currency.</summary>
    member self.NewAccount(name, accountType) =
        GnuCashObject
            .make<Account>(GnuCashObject.nativeHandle self)
            .SetName(name)
            .SetParent(self.RootAccount)
            .SetType(accountType)
            .SetCurrency(self.DefaultCurrency)

    member internal self.SaveInProgress = Bindings.qof_session_save_in_progress session

    /// Closes this book.
    member self.Close() =
        if not closed then
            self.Save()
            BookStack.drop ()
            Bindings.qof_session_end session
            // Destroying a session also destroys it's associated
            // book, which subsequently destroys all objects
            // inside the book.
            Bindings.qof_session_destroy session
            closed <- true

    override self.Finalize() = self.Close()

    interface IDisposable with
        member self.Dispose() = self.Close()
