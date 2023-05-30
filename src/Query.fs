namespace NetCash

open System
open System.Collections
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Reflection

module private QueryHelper =
    let typeMapping =
        dict [ (typeof<Account>, Marshal.StringToCoTaskMemUTF8 Bindings.GNC_ID_ACCOUNT)
               (typeof<Split>, Marshal.StringToCoTaskMemUTF8 Bindings.GNC_ID_SPLIT) ]

    let getEntityId<'a> () =
        match typeMapping.TryGetValue(typeof<'a>) with
        | false, _ -> raise (NotSupportedException(sprintf "Query for type %s is not supported" typeof<'a>.FullName))
        | _, id -> id

    // Modify q1 to include books that q2 searches for.
    let includeBooks q1 q2 =
        Bindings.g_list_foreach (
            Bindings.qof_query_get_books (q2),
            Bindings.GFunc(fun book _ -> Bindings.qof_query_set_book (q1, book)),
            IntPtr.Zero
        )

[<AbstractClass>]
type BaseQuery<'a, 'b when 'a :> IGnuCashEntity and 'b :> BaseQuery<'a, 'b>> internal () =
    let mutable handle = Bindings.qof_query_create ()
    let typeId = QueryHelper.getEntityId<'a> ()
    do Bindings.qof_query_search_for (handle, typeId)

    new(book: Book) as self =
        new BaseQuery<'a, 'b>()
        then self.IncludeBooks(book) |> ignore

    member internal _.Handle = handle
    member internal _.SearchFor = typeId

    /// Includes more books for this query to search.
    member self.IncludeBooks([<ParamArray>] books: Book []) : 'b =
        for book in books do
            Bindings.qof_query_set_book (handle, GnuCashObject.nativeHandle book)

        downcast self

    member self.Invert() : 'b =
        let oldHandle = handle
        handle <- Bindings.qof_query_invert (handle)
        Bindings.qof_query_destroy (oldHandle)
        downcast self

    /// Combines with another query.
    member self.Combine(another: 'b, logicalOp) : 'b =
        Bindings.qof_query_merge_in_place (handle, another.Handle, logicalOp)
        downcast self

    /// Sets the maximum result for this query.
    member self.Limit(n) : 'b =
        Bindings.qof_query_set_max_results (handle, n)
        downcast self

    member val private PrimaryQuery = Unchecked.defaultof<nativeint> with get, set

    /// Creates a subquery. A subquery runs over the current results instead of the book.
    member self.NewSubquery() =
        let subQuery =
            Activator.CreateInstance(
                typeof<'b>,
                BindingFlags.NonPublic ||| BindingFlags.Instance,
                null,
                Array.empty<_>,
                null
            )
            :?> 'b

        subQuery.PrimaryQuery <- self.Handle
        QueryHelper.includeBooks subQuery.Handle self.Handle
        subQuery

    /// Executes this query.
    member self.Run() : IReadOnlyCollection<'a> =
        if isUncheckedDefault self.PrimaryQuery then
            Bindings
                .qof_query_run(handle)
                .Map(GnuCashObject.Registry.getOrCreate<_>)
        else
            Bindings
                .qof_query_run_subquery(self.Handle, self.PrimaryQuery)
                .Map(GnuCashObject.Registry.getOrCreate<'a>)

    /// Resets this query.
    member self.Reset() : 'b =
        Bindings.qof_query_clear (handle)
        Bindings.qof_query_search_for (handle, typeId)
        downcast self

    override _.Finalize() = Bindings.qof_query_destroy (handle)

type SplitQuery internal () =
    inherit BaseQuery<Split, SplitQuery>()

    new(book: Book) =
        new SplitQuery()
        then ``base``.IncludeBooks(book) |> ignore

    /// Limit splits to *any* of the specified accounts.
    member self.MatchAccounts([<ParamArray>] accounts: Account []) =
        Bindings.xaccQueryAddAccountMatch (
            self.Handle,
            new Bindings.OwnedGList(accounts |> Seq.map GnuCashObject.nativeHandle),
            Bindings.QofGuidMatch.QOF_GUID_MATCH_ANY,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for transactions' descriptions.
    member self.MatchDescription(text, [<Optional; DefaultParameterValue false>] useRegex: bool) =
        Bindings.xaccQueryAddDescriptionMatch (
            self.Handle,
            text,
            false,
            useRegex,
            Bindings.QofQueryCompare.QOF_COMPARE_CONTAINS,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for transactions' notes.
    member self.MatchNote(text, [<Optional; DefaultParameterValue false>] useRegex: bool) =
        Bindings.xaccQueryAddNotesMatch (
            self.Handle,
            text,
            false,
            useRegex,
            Bindings.QofQueryCompare.QOF_COMPARE_CONTAINS,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for transactions' numbers.
    member self.MatchNumber(text) =
        Bindings.xaccQueryAddNumberMatch (
            self.Handle,
            text,
            false,
            false,
            Bindings.QofQueryCompare.QOF_COMPARE_EQUAL,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for transactions' actions.
    member self.MatchAction(text) =
        Bindings.xaccQueryAddActionMatch (
            self.Handle,
            text,
            false,
            false,
            Bindings.QofQueryCompare.QOF_COMPARE_EQUAL,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for splits' memos.
    member self.MatchMemo(text, [<Optional; DefaultParameterValue false>] useRegex: bool) =
        Bindings.xaccQueryAddMemoMatch (
            self.Handle,
            text,
            false,
            useRegex,
            Bindings.QofQueryCompare.QOF_COMPARE_CONTAINS,
            Bindings.QofQueryOp.QOF_QUERY_AND
        )

        self

    /// Refines filtering for splits' values.
    member self.MatchValue(value, side, how) =
        Bindings.xaccQueryAddValueMatch (self.Handle, value, side, how, Bindings.QofQueryOp.QOF_QUERY_AND)
        self

    /// Refines filtering for splits' share prices.
    member self.MatchSharePrice(value, how) =
        Bindings.xaccQueryAddSharePriceMatch (self.Handle, value, how, Bindings.QofQueryOp.QOF_QUERY_AND)
        self

    /// Refines filtering for splits' shares.
    member self.MatchShares(value, how) =
        Bindings.xaccQueryAddSharesMatch (self.Handle, value, how, Bindings.QofQueryOp.QOF_QUERY_AND)
        self

    /// Refines filtering for (un)balanced transactions.
    member self.MatchBalanced(balanced) =
        Bindings.xaccQueryAddBalanceMatch (self.Handle, (if balanced then 1 else 0), Bindings.QofQueryOp.QOF_QUERY_AND)
        self
