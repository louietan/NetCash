/// Partial bindings for native libraries (libgnucash and glibc).
module NetCash.Bindings

open System
open System.Runtime.InteropServices
open System.Collections.Generic

// -----------------------------------------------------------------------------
// libglib

type gint = int
type gint32 = int32
type gint64 = int64
type gpointer = nativeint
type guint = unativeint
type guint32 = uint32
type gunichar = guint32

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type GFunc = delegate of gpointer * gpointer -> unit

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type GHFunc = delegate of gpointer * gpointer * gpointer -> unit

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type GCompareFunc = delegate of  gpointer * gpointer -> gint

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type GDestroyNotify = delegate of gpointer -> unit

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_strdup (nativeint str)

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_uri_parse_scheme ([<MarshalAs(UnmanagedType.LPUTF8Str)>]string uri)

[<DllImport(NativeLibraries.glib)>]
extern gpointer g_malloc(nativeint size)

[<DllImport(NativeLibraries.glib)>]
extern void g_free(nativeint list)

[<DllImport(NativeLibraries.glib)>]
extern void g_list_free_full(nativeint list, GDestroyNotify free_func)

[<DllImport(NativeLibraries.glib)>]
extern void g_list_foreach(nativeint list, GFunc func, gpointer user_data)

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_list_prepend (nativeint list, gpointer data)

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_list_find_custom (nativeint list, gpointer data, GCompareFunc func)

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_list_reverse (nativeint list)

[<DllImport(NativeLibraries.glib)>]
extern guint g_list_length(nativeint list)

[<DllImport(NativeLibraries.glib)>]
extern void g_list_free(nativeint list)

[<DllImport(NativeLibraries.glib)>]
extern nativeint g_slist_prepend (nativeint list, gpointer data)

[<DllImport(NativeLibraries.glib)>]
extern void g_hash_table_foreach(nativeint hash_table, GHFunc func, gpointer user_data)

[<DllImport(NativeLibraries.glib)>]
extern void g_hash_table_destroy(nativeint hash_table)

// -----------------------------------------------------------------------------
// libgobject

[<DllImport(NativeLibraries.gobject)>]
extern void g_object_unref (nativeint object)

// -----------------------------------------------------------------------------
// libgio

[<DllImport(NativeLibraries.gio)>]
extern nativeint g_file_new_for_path ([<MarshalAs(UnmanagedType.LPUTF8Str)>]string path)

[<DllImport(NativeLibraries.gio)>]
extern nativeint g_file_get_uri (nativeint file)

// -----------------------------------------------------------------------------
// Managed safe handles

type SafeNativeHandle internal (releaseOpt) as self = 
    inherit Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid (Option.isSome releaseOpt)

    internal new (size) as self =
        let handle = g_malloc size
        new SafeNativeHandle (Some g_free)
        then self.SetHandle handle
    
    internal new (release, handle) as self =
        new SafeNativeHandle (Some release)
        then self.SetHandle handle

    override _.ReleaseHandle () = 
        Option.apply
            releaseOpt
            (Some (self.DangerousGetHandle()))
        |> ignore

        true

[<AbstractClass>]
type AbstractGListWrapper internal (releaseOpt: (nativeint -> unit) option) as self =
    inherit SafeNativeHandle (releaseOpt)

    member _.Map(mapper) : IReadOnlyCollection<'a> = 
        let result = ResizeArray()

        g_list_foreach (
            self.DangerousGetHandle(),
            GFunc(fun data _ -> result.Add(mapper data)),
            IntPtr.Zero
        )

        result.AsReadOnly()

type OwnedGList internal(release) =
    inherit AbstractGListWrapper (Some release)

    new () = new OwnedGList (g_list_free)

    new (source: seq<nativeint>) as self =
        let handle = Seq.foldBack (fun data list -> g_list_prepend (list, data)) source IntPtr.Zero
        new OwnedGList ()
        then self.SetHandle handle

type BorrowedGList() = inherit AbstractGListWrapper (None)

type BorrowedGSList (source: seq<nativeint>) =
    inherit SafeNativeHandle(None)
    let handle = Seq.foldBack (fun data list -> g_slist_prepend (list, data)) source IntPtr.Zero

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-numeric.h

/// An rational-number type
[<Struct; StructLayout(LayoutKind.Sequential)>]
type gnc_numeric =
    new (num, denom) = { num = num; denom = denom }
    val mutable num: gint64
    val mutable denom: gint64

type GncNumericFlags =
    /// Round toward -infinity
    | GNC_HOW_RND_FLOOR            = 0x01

    /// Round toward +infinity
    | GNC_HOW_RND_CEIL             = 0x02

    /// Truncate fractions (round toward zero)
    | GNC_HOW_RND_TRUNC            = 0x03

    /// Promote fractions (round away from zero)
    | GNC_HOW_RND_PROMOTE          = 0x04

    /// Round to the nearest integer, rounding toward zero
    /// when there are two equidistant nearest integers.
    | GNC_HOW_RND_ROUND_HALF_DOWN  = 0x05

    /// Round to the nearest integer, rounding away from zero
    /// when there are two equidistant nearest integers.
    | GNC_HOW_RND_ROUND_HALF_UP    = 0x06

    /// Use unbiased ("banker's") rounding. This rounds to the
    /// nearest integer, and to the nearest even integer when there
    /// are two equidistant nearest integers. This is generally the
    /// one you should use for financial quantities.
    | GNC_HOW_RND_ROUND            = 0x07

    /// <summary>Never round at all, and signal an error if there is a
    /// fractional result in a computation.</summary>
    | GNC_HOW_RND_NEVER            = 0x08

    /// Use any denominator which gives an exactly correct ratio of
    /// numerator to denominator. Use EXACT when you do not wish to
    /// lose any information in the result but also do not want to
    /// spend any time finding the "best" denominator.
    | GNC_HOW_DENOM_EXACT          = 0x10

    /// Reduce the result value by common factor elimination,
    /// using the smallest possible value for the denominator that
    /// keeps the correct ratio. The numerator and denominator of
    /// the result are relatively prime.
    | GNC_HOW_DENOM_REDUCE         = 0x20

    /// Find the least common multiple of the arguments' denominators
    /// and use that as the denominator of the result.
    | GNC_HOW_DENOM_LCD            = 0x30

    /// All arguments are required to have the same denominator,
    /// that denominator is to be used in the output, and an error
    /// is to be signaled if any argument has a different denominator.
    | GNC_HOW_DENOM_FIXED          = 0x40  // by skimming the GnuCash source, it seems to me that no code actually checks this flag at all

    /// Round to the number of significant figures given in the rounding
    /// instructions by the GNC_HOW_DENOM_SIGFIGS () macro.
    | GNC_HOW_DENOM_SIGFIG         = 0x50

/// Compute an appropriate denominator automatically. Flags in
/// the 'how' argument will specify how to compute the denominator.
[<Literal>]
let GNC_DENOM_AUTO = 0L

type GNCNumericErrorCode =
    | GNC_ERROR_OK         =  0
    | GNC_ERROR_ARG        = -1
    | GNC_ERROR_OVERFLOW   = -2
    | GNC_ERROR_DENOM_DIFF = -3
    | GNC_ERROR_REMAINDER  = -4

let inline GNC_HOW_DENOM_SIGFIGS n = (((n &&& 0xff) <<< 8) ||| int GncNumericFlags.GNC_HOW_DENOM_SIGFIG)
let inline GNC_HOW_GET_SIGFIGS a = ((a &&& 0xff00) >>> 8)

[<DllImport(NativeLibraries.gncEngine)>]
extern double gnc_numeric_to_double(gnc_numeric n)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric double_to_gnc_numeric(double n, gint64 denom, GncNumericFlags how)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_numeric_to_string(gnc_numeric n)

[<DllImport(NativeLibraries.gncEngine)>]
extern GNCNumericErrorCode  gnc_numeric_check(gnc_numeric a)

[<DllImport(NativeLibraries.gncEngine)>]
extern gint gnc_numeric_compare(gnc_numeric a, gnc_numeric b)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool string_to_gnc_numeric([<MarshalAs(UnmanagedType.LPUTF8Str)>]string str, gnc_numeric& n)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_numeric_errorCode_to_string(GNCNumericErrorCode error_code)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_add(gnc_numeric a, gnc_numeric b,
                                   gint64 denom, GncNumericFlags how);

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_sub(gnc_numeric a, gnc_numeric b,
                            gint64 denom, GncNumericFlags how)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_mul(gnc_numeric a, gnc_numeric b,
                            gint64 denom, GncNumericFlags how)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_div(gnc_numeric x, gnc_numeric y,
                            gint64 denom, GncNumericFlags how)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_neg(gnc_numeric a)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_abs(gnc_numeric a)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_convert(gnc_numeric n, gint64 denom,
                                GncNumericFlags how)

let inline gnc_numeric_create (num : gint64, denom: gint64) = gnc_numeric (num, denom)

let inline gnc_numeric_zero() = gnc_numeric_create(0, 1)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_numeric_zero_p(gnc_numeric a)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_invert (gnc_numeric num)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_numeric_reduce(gnc_numeric n)

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-date.h

type time64 = gint64

[<DllImport(NativeLibraries.gncEngine)>]
extern time64 gnc_time64_get_day_end(time64 time_val)

[<DllImport(NativeLibraries.gncEngine)>]
extern time64 gnc_time64_get_day_start(time64 time_val)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofutil.h

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_init ()

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_close ()

// -----------------------------------------------------------------------------
// libgnucash/engine/qofquerycore.h

type QofQueryCompare =
    | QOF_COMPARE_LT = 1
    | QOF_COMPARE_LTE = 2
    | QOF_COMPARE_EQUAL = 3
    | QOF_COMPARE_GT = 4
    | QOF_COMPARE_GTE = 5
    | QOF_COMPARE_NEQ = 6
    | QOF_COMPARE_CONTAINS = 7
    | QOF_COMPARE_NCONTAINS = 8

type QofStringMatch =
    | QOF_STRING_MATCH_NORMAL = 1
    | QOF_STRING_MATCH_CASEINSENSITIVE = 2

type QofDateMatch = 
    | QOF_DATE_MATCH_NORMAL = 1
    | QOF_DATE_MATCH_DAY = 2

type QofNumericMatch = 
    | QOF_NUMERIC_MATCH_DEBIT = 1
    | QOF_NUMERIC_MATCH_CREDIT = 2
    | QOF_NUMERIC_MATCH_ANY = 3

type QofGuidMatch =
    | QOF_GUID_MATCH_ANY = 1
    | QOF_GUID_MATCH_NONE = 2
    | QOF_GUID_MATCH_NULL = 3
    | QOF_GUID_MATCH_ALL = 4
    | QOF_GUID_MATCH_LIST_ANY = 5

type QofCharMatch =
    | QOF_CHAR_MATCH_ANY = 1
    | QOF_CHAR_MATCH_NONE = 2

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_string_predicate (QofQueryCompare how,
                                             [<MarshalAs(UnmanagedType.LPUTF8Str)>]string str,
                                             QofStringMatch options,
                                             [<MarshalAs(UnmanagedType.I1)>]bool is_regex)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_date_predicate (QofQueryCompare how,
                                           QofDateMatch options,
                                           time64 date)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_numeric_predicate (QofQueryCompare how,
                                              QofNumericMatch options,
                                              gnc_numeric value)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_guid_predicate (QofGuidMatch options, OwnedGList guids)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_int32_predicate (QofQueryCompare how, gint32 value)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_int64_predicate (QofQueryCompare how, gint64 value)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_double_predicate (QofQueryCompare how, double value)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_boolean_predicate (QofQueryCompare how, [<MarshalAs(UnmanagedType.I1)>]bool value)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofquery.h

type QofQueryOp =
    | QOF_QUERY_AND = 1
    | QOF_QUERY_OR = 2
    | QOF_QUERY_NAND = 3
    | QOF_QUERY_NOR = 4
    | QOF_QUERY_XOR = 5

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_create ()

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_create_for ([<MarshalAs(UnmanagedType.LPUTF8Str)>]string obj_type)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_destroy (nativeint q)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_clear (nativeint query)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_search_for (nativeint query, nativeint obj_type)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_get_search_for (nativeint q)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_get_books (nativeint q)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_set_book (nativeint q, nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_set_max_results (nativeint q, int n)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_add_term (nativeint query, BorrowedGSList param_list,
                                nativeint pred_data, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList qof_query_run (nativeint query)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList qof_query_last_run (nativeint query)


[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList qof_query_run_subquery (nativeint subquery,
                                             nativeint primary_query)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_invert(nativeint q)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_query_merge(nativeint q1, nativeint q2, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_query_merge_in_place(nativeint q1, nativeint q2, QofQueryOp op)

// -----------------------------------------------------------------------------
// libgnucash/engine/Query.h

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddAccountMatch(nativeint q, OwnedGList account_list,
                                     QofGuidMatch how, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddSingleAccountMatch(nativeint q, nativeint account, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddDescriptionMatch(nativeint q, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string m,
                                         [<MarshalAs(UnmanagedType.I1)>]bool c, [<MarshalAs(UnmanagedType.I1)>]bool r,
                                         QofQueryCompare how, QofQueryOp o)
                                         
[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddNotesMatch(nativeint q, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string m,
                                   [<MarshalAs(UnmanagedType.I1)>]bool c, [<MarshalAs(UnmanagedType.I1)>]bool r,
                                   QofQueryCompare how, QofQueryOp o)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddNumberMatch(nativeint q, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string m,
                                   [<MarshalAs(UnmanagedType.I1)>]bool c, [<MarshalAs(UnmanagedType.I1)>]bool r,
                                   QofQueryCompare how, QofQueryOp o)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddActionMatch(nativeint q, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string m,
                                    [<MarshalAs(UnmanagedType.I1)>]bool c, [<MarshalAs(UnmanagedType.I1)>]bool r,
                                    QofQueryCompare how, QofQueryOp o)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddMemoMatch(nativeint q, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string m,
                                  [<MarshalAs(UnmanagedType.I1)>]bool c, [<MarshalAs(UnmanagedType.I1)>]bool r,
                                  QofQueryCompare how, QofQueryOp o)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddValueMatch(nativeint q, gnc_numeric amt, QofNumericMatch sgn,
                                   QofQueryCompare how, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddSharePriceMatch(nativeint q, gnc_numeric amt, QofQueryCompare how, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddSharesMatch(nativeint q, gnc_numeric amt, QofQueryCompare how, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddBalanceMatch(nativeint q, int bal, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddDateMatch(nativeint q, [<MarshalAs(UnmanagedType.I1)>]bool use_start,
                                  int sday, int smonth, int syear,
                                  [<MarshalAs(UnmanagedType.I1)>]bool use_end, int eday, int emonth, int eyear,
                                  QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddClosingTransMatch(nativeint q, [<MarshalAs(UnmanagedType.I1)>]bool value, QofQueryOp op)

type cleared_match_t =
    | CLEARED_NONE       = 0x0000
    | CLEARED_NO         = 0x0001
    | CLEARED_CLEARED    = 0x0002
    | CLEARED_RECONCILED = 0x0004
    | CLEARED_FROZEN     = 0x0008
    | CLEARED_VOIDED     = 0x0010
    | CLEARED_ALL        = 0x001F

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddClearedMatch(nativeint q, cleared_match_t how, QofQueryOp op)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccQueryAddGUIDMatch(nativeint q, SafeHandle guid,
                                  [<MarshalAs(UnmanagedType.LPUTF8Str)>]string id_type,
                                  QofQueryOp op)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofbackend.h
type QofBackendError =
    | ERR_BACKEND_NO_ERR = 0
    | ERR_BACKEND_NO_HANDLER = 1
    | ERR_BACKEND_NO_BACKEND = 2
    | ERR_BACKEND_BAD_URL = 3
    | ERR_BACKEND_NO_SUCH_DB = 4
    | ERR_BACKEND_CANT_CONNECT = 5
    | ERR_BACKEND_CONN_LOST = 6
    | ERR_BACKEND_LOCKED = 7
    | ERR_BACKEND_STORE_EXISTS = 8
    | ERR_BACKEND_READONLY = 9
    | ERR_BACKEND_TOO_NEW = 10
    | ERR_BACKEND_DATA_CORRUPT = 11
    | ERR_BACKEND_SERVER_ERR = 12
    | ERR_BACKEND_ALLOC = 13
    | ERR_BACKEND_PERM = 14
    | ERR_BACKEND_MODIFIED = 15
    | ERR_BACKEND_MOD_DESTROY = 16
    | ERR_BACKEND_MISC = 17
    | ERR_QOF_OVERFLOW = 18
    | ERR_FILEIO_FILE_BAD_READ = 1000
    | ERR_FILEIO_FILE_EMPTY = 1001
    | ERR_FILEIO_FILE_LOCKERR = 1002
    | ERR_FILEIO_FILE_NOT_FOUND = 1003
    | ERR_FILEIO_FILE_TOO_OLD = 1004
    | ERR_FILEIO_UNKNOWN_FILE_TYPE = 1005
    | ERR_FILEIO_PARSE_ERROR = 1006
    | ERR_FILEIO_BACKUP_ERROR = 1007
    | ERR_FILEIO_WRITE_ERROR = 1008
    | ERR_FILEIO_READ_ERROR = 1009
    | ERR_FILEIO_NO_ENCODING = 1010
    | ERR_FILEIO_FILE_EACCES = 1011
    | ERR_FILEIO_RESERVED_WRITE = 1012
    | ERR_FILEIO_FILE_UPGRADE = 1013
    | ERR_NETIO_SHORT_READ = 2000
    | ERR_NETIO_WRONG_CONTENT_TYPE = 2001
    | ERR_NETIO_NOT_GNCXML = 2002
    | ERR_SQL_MISSING_DATA = 3000
    | ERR_SQL_DB_TOO_OLD = 3001
    | ERR_SQL_DB_TOO_NEW = 3002
    | ERR_SQL_DB_BUSY = 3003
    | ERR_SQL_BAD_DBI = 3004
    | ERR_SQL_DBI_UNTESTABLE = 3005
    | ERR_RPC_HOST_UNK = 4000
    | ERR_RPC_CANT_BIND = 4001
    | ERR_RPC_CANT_ACCEPT = 4002
    | ERR_RPC_NO_CONNECTION = 4003
    | ERR_RPC_BAD_VERSION = 4004
    | ERR_RPC_FAILED = 4005
    | ERR_RPC_NOT_ADDED = 4006

type GNCAccountType =
    /// Not a type
    | ACCT_TYPE_INVALID = -1

    /// Not a type
    | ACCT_TYPE_NONE = -1

    /// The bank account type denotes a savings or checking account held at a bank.
    /// Often interest bearing.
    | ACCT_TYPE_BANK = 0

    /// The cash account type is used to denote a shoe-box or pillowcase stuffed with cash.
    | ACCT_TYPE_CASH = 1

    /// The Credit card account is used to denote credit (e.g. amex) and debit (e.g. visa, mastercard) card accounts.
    | ACCT_TYPE_CREDIT = 3

    /// asset (and liability) accounts indicate generic, generalized accounts that are none of the above.
    | ACCT_TYPE_ASSET = 2

    /// liability (and asset) accounts indicate generic, generalized accounts that are none of the above.
    | ACCT_TYPE_LIABILITY = 4

    /// Stock accounts will typically be shown in registers which show three columns: price, number of shares, and value.
    | ACCT_TYPE_STOCK = 5

    /// Mutual Fund accounts will typically be shown in registers which show three columns: price, number of shares, and value.
    | ACCT_TYPE_MUTUAL = 6

    /// The currency account type indicates that
    /// the account is a currency trading
    /// account.  In many ways, a currency
    /// trading account is like a stock
    /// trading account. It is shown in the
    /// register with three columns: price,
    /// number of shares, and value. Note:
    /// Since version 1.7.0, this account is
    /// no longer needed to exchange currencies
    /// between accounts, so this type is DEPRECATED.
    | [<Obsolete>] ACCT_TYPE_CURRENCY = 7

    /// Income accounts are used to denote income
    | ACCT_TYPE_INCOME = 8

    /// Expense accounts are used to denote expenses.
    | ACCT_TYPE_EXPENSE = 9

    /// Equity account is used to balance the balance sheet.
    | ACCT_TYPE_EQUITY = 10

    /// A/R account type
    | ACCT_TYPE_RECEIVABLE = 11

    /// A/P account type
    | ACCT_TYPE_PAYABLE = 12

    /// The hidden root account of an account tree.
    | ACCT_TYPE_ROOT = 13

    /// Account used to record multiple commodity transactions.
    /// This is not the same as ACCT_TYPE_CURRENCY above.
    /// Multiple commodity transactions have splits in these
    /// accounts to make the transaction balance in each
    /// commodity as well as in total value.
    | ACCT_TYPE_TRADING = 14

    // Commented because they are not ready for use.
    // | NUM_ACCOUNT_TYPES = 15
    // | ACCT_TYPE_CHECKING = 15
    // | ACCT_TYPE_SAVINGS = 16
    // | ACCT_TYPE_MONEYMRKT = 17
    // | ACCT_TYPE_CREDITLINE = 18
    // | ACCT_TYPE_LAST = 19

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type QofBePercentageFunc = delegate of string * float -> unit

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type QofBookDirtyCB = delegate of nativeint * [<MarshalAs(UnmanagedType.I1)>] dirty: bool * gpointer -> unit

//------------------------------------------------------------------------------
// libgnucash/engine/qofevent.h

type QofEventId = gint

[<Literal>]
let QOF_EVENT_NONE: QofEventId =     0x00

[<Literal>]
let QOF_EVENT_CREATE: QofEventId =   0x01

[<Literal>]
let QOF_EVENT_MODIFY: QofEventId =   0x02

[<Literal>]
let QOF_EVENT_DESTROY: QofEventId =  0x04

[<Literal>]
let QOF_EVENT_ADD: QofEventId =      0x08

[<Literal>]
let QOF_EVENT_REMOVE: QofEventId =   0x10

[<Literal>]
let QOF_EVENT__LAST: QofEventId =    0x80

[<Literal>]
let QOF_EVENT_ALL: QofEventId =      0xff

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type QofEventHandler = delegate of ent: nativeint *
                                   event_type: QofEventId *
                                   handler_data: gpointer *
                                   event_data: gpointer
                               -> unit

[<DllImport(NativeLibraries.gncEngine)>]
extern gint qof_event_register_handler (QofEventHandler handler, gpointer handler_data)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_event_unregister_handler (gint handler_id)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_event_suspend ()

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_event_resume ()

//------------------------------------------------------------------------------
// libgnucash/engine/gnc-hooks.h

[<Literal>]
let HOOK_BOOK_OPENED = "hook_book_opened"

[<Literal>]
let HOOK_BOOK_CLOSED = "hook_book_closed"

[<Literal>]
let HOOK_BOOK_SAVED = "hook_book_saved"

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_hook_run([<MarshalAs(UnmanagedType.LPUTF8Str)>] string name,
                         gpointer data)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_hook_add_dangler([<MarshalAs(UnmanagedType.LPUTF8Str)>] string name,
                                 GFunc callback,
                                 GDestroyNotify destroy,
                                 gpointer cb_data)

//------------------------------------------------------------------------------
// libgnucash/app-utils/gnc-ui-util.h

[<Literal>]
let GNC_PREF_CURRENCY_CHOICE_LOCALE = "currency-choice-locale"

[<Literal>]
let GNC_PREF_CURRENCY_CHOICE_OTHER  = "currency-choice-other"

[<Literal>]
let GNC_PREF_CURRENCY_OTHER         = "currency-other"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_NONE    = "reversed-accounts-none"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_CREDIT  = "reversed-accounts-credit"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_INC_EXP = "reversed-accounts-incomeexpense"

[<Literal>]
let GNC_PREF_PRICES_FORCE_DECIMAL   = "force-price-decimal"

#if x86
[<StructLayout(LayoutKind.Explicit, Size=10)>]
#else
[<StructLayout(LayoutKind.Explicit, Size=16)>]
#endif
type GNCPrintAmountInfo = struct end

[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint gnc_default_currency ()

[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint gnc_locale_default_currency_nodefault ()

// The return value is guaranteed to be non-NULL.
[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint gnc_account_or_default_currency(nativeint account, [<MarshalAs(UnmanagedType.I1)>]bool& currency_from_account_found)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern GNCPrintAmountInfo gnc_default_print_info ([<MarshalAs(UnmanagedType.I1)>]bool use_symbol)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern GNCPrintAmountInfo gnc_commodity_print_info (nativeint commodity, [<MarshalAs(UnmanagedType.I1)>]bool use_symbol)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern GNCPrintAmountInfo gnc_account_print_info (nativeint account, [<MarshalAs(UnmanagedType.I1)>]bool use_symbol)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern GNCPrintAmountInfo gnc_integral_print_info ()

[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint xaccPrintAmount (gnc_numeric n, GNCPrintAmountInfo info)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint gnc_print_amount_with_bidi_ltr_isolate (gnc_numeric n, GNCPrintAmountInfo info)

[<DllImport(NativeLibraries.gncAppUtils)>]
extern nativeint gnc_normalize_account_separator (nativeint separator)

// -----------------------------------------------------------------------------
// libgnucash/app-utils/gnc-ui-balances.h

// -----------------------------------------------------------------------------
// libgnucash/app-utils/gnc-accounting-period.h

type GncAccountingPeriod =
    | GNC_ACCOUNTING_PERIOD_INVALID = -1
    | GNC_ACCOUNTING_PERIOD_TODAY = 0
    | GNC_ACCOUNTING_PERIOD_MONTH = 1
    | GNC_ACCOUNTING_PERIOD_MONTH_PREV = 2
    | GNC_ACCOUNTING_PERIOD_QUARTER = 3
    | GNC_ACCOUNTING_PERIOD_QUARTER_PREV = 4
    | GNC_ACCOUNTING_PERIOD_CYEAR = 5
    | GNC_ACCOUNTING_PERIOD_CYEAR_PREV = 6
    | GNC_ACCOUNTING_PERIOD_CYEAR_LAST = 7

    | GNC_ACCOUNTING_PERIOD_FYEAR = 7 // GNC_ACCOUNTING_PERIOD_CYEAR_LAST
    | GNC_ACCOUNTING_PERIOD_FYEAR_PREV = 8
    | GNC_ACCOUNTING_PERIOD_FYEAR_LAST = 9
    | GNC_ACCOUNTING_PERIOD_LAST = 9 // GNC_ACCOUNTING_PERIOD_FYEAR_LAST

[<DllImport(NativeLibraries.gncAppUtils)>]
extern time64 gnc_accounting_period_fiscal_start ()

[<DllImport(NativeLibraries.gncAppUtils)>]
extern time64 gnc_accounting_period_fiscal_end ()

// -----------------------------------------------------------------------------
// libgnucash/backend/dbi/gnc-backend-dbi.h

[<DllImport(NativeLibraries.gncmodBackendDBI, EntryPoint="qof_backend_module_init")>]
extern void gnc_module_init_backend_dbi ()

[<DllImport(NativeLibraries.gncmodBackendDBI, EntryPoint="qof_backend_module_finalize")>]
extern void gnc_module_finalize_backend_dbi ()

// -----------------------------------------------------------------------------
// libgnucash/backend/xml/gnc-backend-xml.h

type XMLFileRetentionType =
    | XML_RETAIN_NONE = 0
    | XML_RETAIN_DAYS = 1
    | XML_RETAIN_ALL = 2

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-uri-utils.h

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_uri_is_uri([<MarshalAs(UnmanagedType.LPUTF8Str)>]string uri)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_uri_get_components ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string uri,
                                    nativeint& scheme,
                                    nativeint& hostname,
                                    gint32& port,
                                    nativeint& username,
                                    nativeint& password,
                                    nativeint& path)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_uri_normalize_uri(string uri, [<MarshalAs(UnmanagedType.I1)>] bool allow_password)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_uri_create_uri ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string scheme,
                                     [<MarshalAs(UnmanagedType.LPUTF8Str)>] string hostname,
                                     gint32 port,
                                     [<MarshalAs(UnmanagedType.LPUTF8Str)>] string username,
                                     [<MarshalAs(UnmanagedType.LPUTF8Str)>] string password,
                                     [<MarshalAs(UnmanagedType.LPUTF8Str)>] string path)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_uri_targets_local_fs ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string uri)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_uri_is_file_scheme ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string scheme)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_uri_is_known_scheme ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string scheme)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_uri_get_path ([<MarshalAs(UnmanagedType.LPUTF8Str)>] string uri)
// -----------------------------------------------------------------------------
// libgnucash/core-utils/gnc-version.h

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_version()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_build_id()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_vcs_rev()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_vcs_rev_date()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern int gnc_gnucash_major_version()

// -----------------------------------------------------------------------------
// libgnucash/core-utils/gnc-prefs.h

[<Literal>]
let GNC_PREFS_GROUP_GENERAL            = "general"

[<Literal>]
let GNC_PREFS_GROUP_GENERAL_REGISTER   = "general.register"

[<Literal>]
let GNC_PREFS_GROUP_GENERAL_REPORT     = "general.report"

[<Literal>]
let GNC_PREFS_GROUP_WARNINGS           = "general.warnings"

[<Literal>]
let GNC_PREFS_GROUP_WARNINGS_TEMP      = "warnings.temporary"

[<Literal>]
let GNC_PREFS_GROUP_WARNINGS_PERM      = "warnings.permanent"

[<Literal>]
let GNC_PREFS_GROUP_ACCT_SUMMARY       = "window.pages.account-tree.summary"

// ---- Early bird functions ----
// Following gnc_prefs_set_xxx functions only update in-memory values.

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_prefs_set_file_save_compressed([<MarshalAs(UnmanagedType.I1)>]bool compressed)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_prefs_set_file_retention_policy(XMLFileRetentionType policy)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern XMLFileRetentionType gnc_prefs_get_file_retention_policy()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern gint gnc_prefs_get_file_retention_days()

// -----------------------------------------------------------------------------

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_is_set_up ()

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_get_bool (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern gint gnc_prefs_get_int (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern gint64 gnc_prefs_get_int64 (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern double gnc_prefs_get_float (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_prefs_get_string (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern gint gnc_prefs_get_enum (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_prefs_get_coords (string group, string pref_name, double& x, double& y)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_bool (string group,
                                string pref_name,
                                [<MarshalAs(UnmanagedType.I1)>]bool value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_int (string group, string pref_name, gint value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_int64 (string group, string pref_name, gint64 value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_float (string group, string pref_name, double value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_string (string group, string pref_name, string value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_enum (string group, string pref_name, gint value)

[<DllImport(NativeLibraries.gncCoreUtils); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_prefs_set_coords (string group, string pref_name, double x, double y)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_prefs_reset (string group, string pref_name)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_prefs_reset_group (string group)

// -----------------------------------------------------------------------------
// libgnucash/engine/guid.h

[<StructLayout(LayoutKind.Explicit, Size=16)>]
type GncGUID = struct end

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint guid_to_string (nativeint guid)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool string_to_guid([<MarshalAs(UnmanagedType.LPUTF8Str)>]string string, nativeint guid)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofid.h

[<Literal>]
let QOF_ID_NONE: string     =     null

[<Literal>]
let QOF_ID_NULL     =     "null"

[<Literal>]
let QOF_ID_BOOK     =     "Book"

[<Literal>]
let QOF_ID_SESSION  =     "Session"

/// Find the entity going only from its guid
[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_collection_lookup_entity (nativeint col, nativeint guid)

/// Callback type for qof_collection_foreach
[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type QofInstanceForeachCB = delegate of nativeint * user_data: gpointer -> unit

/// Call the callback for each entity in the collection.
[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_collection_foreach (nativeint col, QofInstanceForeachCB cb,
                             gpointer user_data)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofinstance.h

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_instance_get_guid (nativeint)

[<DllImport(NativeLibraries.gncEngine)>]
extern int qof_instance_get_editlevel (gpointer ptr)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_instance_get_book (gpointer)

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-commodity.h

[<Literal>]
let GNC_COMMODITY_NS_TEMPLATE  =   "template"

[<Literal>]
let GNC_COMMODITY_NS_CURRENCY  =   "CURRENCY"

[<Literal>]
let GNC_COMMODITY_NS_NONCURRENCY = "NONCURRENCY"

type QuoteSourceType =
    | SOURCE_SINGLE = 0
    | SOURCE_MULTI = 1
    | SOURCE_UNKNOWN = 2
    | SOURCE_MAX = 3
    | SOURCE_CURRENCY = 3

[<DllImport(NativeLibraries.gncEngine)>]
extern QuoteSourceType gnc_quote_source_get_type (nativeint source)

[<DllImport(NativeLibraries.gncEngine)>]
extern gint gnc_quote_source_get_index (nativeint source)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_quote_source_get_user_name (nativeint source)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_quote_source_get_internal_name (nativeint source)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_quote_source_fq_installed ()

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_quote_source_fq_version ()

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_new(nativeint book,
                                   [<MarshalAs(UnmanagedType.LPUTF8Str)>] string fullname,
                                   [<MarshalAs(UnmanagedType.LPUTF8Str)>] string commodity_namespace,
                                   [<MarshalAs(UnmanagedType.LPUTF8Str)>] string mnemonic,
                                   [<MarshalAs(UnmanagedType.LPUTF8Str)>] string cusip,
                                   int fraction)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_insert(nativeint table, nativeint comm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_table_remove(nativeint table, nativeint comm)

[<DllImport(NativeLibraries.gncEngine)>]
extern int gnc_commodity_get_fraction(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_set_fraction(nativeint cm, int smallest_fraction)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_mnemonic(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_set_mnemonic(nativeint cm, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string mnemonic)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_namespace(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_namespace_ds(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_set_namespace(nativeint cm, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string new_namespace)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_fullname(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void  gnc_commodity_set_fullname(nativeint cm, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string fullname)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_printname(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_cusip(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_set_cusip(nativeint cm, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string cusip)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_get_unique_name(nativeint cm)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_commodity_get_quote_flag(nativeint cm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_set_quote_flag(nativeint cm, [<MarshalAs(UnmanagedType.I1)>]bool flag)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_commodity_equal(nativeint  a, nativeint b)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_get_table(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_lookup(nativeint table,
        [<MarshalAs(UnmanagedType.LPUTF8Str)>] string commodity_namespace,
        [<MarshalAs(UnmanagedType.LPUTF8Str)>] string mnemonic)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_lookup_unique(nativeint table,
                                  [<MarshalAs(UnmanagedType.LPUTF8Str)>] string unique_name)

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type CommidityCallback = delegate of nativeint * gpointer -> [<return: MarshalAs(UnmanagedType.I1)>] bool

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_commodity_table_foreach_commodity(
    nativeint table,
    CommidityCallback cb,
    gpointer user_data)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList gnc_commodity_table_get_namespaces(nativeint t)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList gnc_commodity_table_get_namespaces_list(nativeint t)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_commodity_namespace_is_iso(nativeint commodity_namespace)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_namespace_get_name (nativeint ns)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_namespace_get_gui_name (nativeint ns)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList gnc_commodity_namespace_get_commodity_list(nativeint ns)

[<DllImport(NativeLibraries.gncEngine)>]
extern int gnc_commodity_table_has_namespace(
    nativeint table,
    [<MarshalAs(UnmanagedType.LPUTF8Str)>]string commodity_namespace)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_add_namespace(nativeint table,
        [<MarshalAs(UnmanagedType.LPUTF8Str)>]string commodity_namespace,
        nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_commodity_table_find_namespace(nativeint table,
        [<MarshalAs(UnmanagedType.LPUTF8Str)>]string commodity_namespace)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_commodity_table_delete_namespace(nativeint table,
        [<MarshalAs(UnmanagedType.LPUTF8Str)>]string commodity_namespace)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList gnc_commodity_table_get_quotable_commodities(nativeint table)

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-pricedb.h

type PriceSource =
    | [<System.ComponentModel.Description("user:price-editor")>]      PRICE_SOURCE_EDIT_DLG = 0
    | [<System.ComponentModel.Description("Finance::Quote")>]         PRICE_SOURCE_FQ = 1
    | [<System.ComponentModel.Description("user:price")>]             PRICE_SOURCE_USER_PRICE = 2
    | [<System.ComponentModel.Description("user:xfer-dialog")>]       PRICE_SOURCE_XFER_DLG_VAL = 3
    | [<System.ComponentModel.Description("user:split-register")>]    PRICE_SOURCE_SPLIT_REG = 4
    | [<System.ComponentModel.Description("user:split-import")>]      PRICE_SOURCE_SPLIT_IMPORT = 5
    | [<System.ComponentModel.Description("user:stock-split")>]       PRICE_SOURCE_STOCK_SPLIT = 6
    | [<System.ComponentModel.Description("user:stock-transaction")>] PRICE_SOURCE_STOCK_TRANSACTION = 7
    | [<System.ComponentModel.Description("user:invoice-post")>]      PRICE_SOURCE_INVOICE = 8
    | [<System.ComponentModel.Description("temporary")>]              PRICE_SOURCE_TEMP = 9
    | [<System.ComponentModel.Description("invalid")>]                PRICE_SOURCE_INVALID = 10

type PriceRemoveSourceFlags =
    | PRICE_REMOVE_SOURCE_FQ = 1   // this flag is set when added by F:Q checked
    | PRICE_REMOVE_SOURCE_USER = 2 // this flag is set when added by the user checked
    | PRICE_REMOVE_SOURCE_APP = 4  // this flag is set when added by the app checked
    | PRICE_REMOVE_SOURCE_COMM = 8 // this flag is set when we have commodities selected

type PriceRemoveKeepOptions =
    | PRICE_REMOVE_KEEP_NONE = 0           // keep none
    | PRICE_REMOVE_KEEP_LAST_WEEKLY = 1    // leave last one of every week
    | PRICE_REMOVE_KEEP_LAST_MONTHLY = 2   // leave last one of every month
    | PRICE_REMOVE_KEEP_LAST_QUARTERLY = 3 // leave last one of every quarter
    | PRICE_REMOVE_KEEP_LAST_PERIOD = 4    // leave last one of every annual period
    | PRICE_REMOVE_KEEP_SCALED = 5         // leave one every week then one a month

[<Literal>]
let PRICE_TYPE_LAST = "last"

[<Literal>]
let PRICE_TYPE_UNK = "unknown"

[<Literal>]
let PRICE_TYPE_TRN = "transaction"

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type GncPriceForeachFunc = delegate of nativeint * gpointer -> [<return: MarshalAs(UnmanagedType.I1)>]bool

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_pricedb_remove_old_prices(nativeint db, nativeint comm_list,
                                          nativeint fiscal_end_date, time64 cutoff,
                                          PriceRemoveSourceFlags source,
                                          PriceRemoveKeepOptions keep)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_pricedb_get_db(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_price_create(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_ref(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_unref(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern time64 gnc_price_get_time64(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_time64(nativeint p, time64 t)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_price_get_value(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_value(nativeint p, gnc_numeric value)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_price_get_commodity(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_commodity(nativeint p, nativeint c)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_price_get_currency(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_currency(nativeint p, nativeint c)

[<DllImport(NativeLibraries.gncEngine)>]
extern PriceSource gnc_price_get_source(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_source(nativeint p, PriceSource source)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_price_get_typestr(nativeint p)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_set_typestr(nativeint p, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string t)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_price_clone(nativeint p, nativeint book)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_pricedb_add_price(nativeint db, nativeint p)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_pricedb_remove_price(nativeint db, nativeint p)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_pricedb_foreach_price(nativeint db,
                                      GncPriceForeachFunc f,
                                      gpointer user_data,
                                      bool stable_order)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_price_list_destroy(nativeint prices)

type PriceList() = inherit OwnedGList(gnc_price_list_destroy)

[<DllImport(NativeLibraries.gncEngine)>]
extern PriceList gnc_pricedb_get_prices(nativeint db,
                                        nativeint commodity,
                                        nativeint currency)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_pricedb_lookup_latest(nativeint db,
                                           nativeint commodity,
                                           nativeint currency)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_pricedb_destroy(nativeint db)

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-engine.h

[<Literal>]
let GNC_ID_NONE         = QOF_ID_NONE
[<Literal>]
let GNC_ID_BOOK         = QOF_ID_BOOK
[<Literal>]
let GNC_ID_SESSION      = QOF_ID_SESSION
[<Literal>]
let GNC_ID_NULL         = QOF_ID_NULL

[<Literal>]
let GNC_ID_ACCOUNT      = "Account"
[<Literal>]
let GNC_ID_COMMODITY    = "Commodity"
[<Literal>]
let GNC_ID_COMMODITY_NAMESPACE="CommodityNamespace"
[<Literal>]
let GNC_ID_COMMODITY_TABLE="CommodityTable"
[<Literal>]
let GNC_ID_LOT          = "Lot"
[<Literal>]
let GNC_ID_PERIOD       = "Period"
[<Literal>]
let GNC_ID_PRICE        = "Price"
[<Literal>]
let GNC_ID_PRICEDB      = "PriceDB"
[<Literal>]
let GNC_ID_SPLIT        = "Split"
[<Literal>]
let GNC_ID_BUDGET       = "Budget"
[<Literal>]
let GNC_ID_SCHEDXACTION = "SchedXaction"
[<Literal>]
let GNC_ID_SXES         = "SchedXactions"
[<Literal>]
let GNC_ID_SXTG         = "SXTGroup"
[<Literal>]
let GNC_ID_SXTT         = "SXTTrans"
[<Literal>]
let GNC_ID_TRANS        = "Trans"

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_engine_init(nativeint argc, nativeint [] argv)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_engine_shutdown()

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_engine_is_initialized ()

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type TransactionCallback = delegate of nativeint * nativeint -> gint

// -----------------------------------------------------------------------------
// libgnucash/engine/qoflog.h

type QofLogLevel =
    | QOF_LOG_FATAL   = 0b00000100
    | QOF_LOG_ERROR   = 0b00001000
    | QOF_LOG_WARNING = 0b00010000
    | QOF_LOG_MESSAGE = 0b00100000
    | QOF_LOG_INFO    = 0b01000000
    | QOF_LOG_DEBUG   = 0b10000000

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_log_set_level([<MarshalAs(UnmanagedType.LPUTF8Str)>] string m, QofLogLevel level)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_log_init()

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_log_init_filename_special([<MarshalAs(UnmanagedType.LPUTF8Str)>] string log_to_filename)

// -----------------------------------------------------------------------------
// libgnucash/engine/qofbook.h

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_book_new()

[<DllImport(NativeLibraries.gncEngine)>]
extern bool qof_book_is_readonly(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_mark_readonly(nativeint book)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool qof_book_session_not_saved (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern bool qof_book_use_trading_accounts (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_begin_edit(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_commit_edit(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_book_get_book_currency_name (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_book_get_default_gains_policy (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_book_get_default_gain_loss_acct_guid (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern gint64 qof_book_get_session_dirty_time(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern bool qof_book_empty(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_set_dirty_cb(nativeint book, QofBookDirtyCB cb, gpointer user_data)

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type QofCollectionForeachCB = delegate of col: nativeint * user_data: gpointer -> unit

/// Invoke the indicated callback on each collection in the book.
[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_foreach_collection (nativeint book, QofCollectionForeachCB cb, gpointer)

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-session.h

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_get_current_session ()

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_clear_current_session()

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_set_current_session (nativeint session)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_current_session_exist()

// -----------------------------------------------------------------------------
// libgnucash/engine/qofsession.h

// This is actually a bitmask of 3 flags: ignore_lock, create, force
/// Mode for opening sessions.
type SessionOpenMode =
    /// Open will fail if the URI doesn't exist or is locked.
    | SESSION_NORMAL_OPEN   = 0b000
    /// Create a new store at the URI. It will fail if the store already exists and is found to contain data that would be overwritten.
    | SESSION_NEW_STORE     = 0b010
    /// Create a new store at the URI even if a store already exists there.
    | SESSION_NEW_OVERWRITE = 0b011
    /// Open the session read-only, ignoring any existing lock and not creating one if the URI isn't locked.
    | SESSION_READ_ONLY     = 0b100
    /// Open the session, taking over any existing lock.
    | SESSION_BREAK_LOCK    = 0b101

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_session_new(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_session_begin(nativeint session, string new_uri, SessionOpenMode mode)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_session_load(nativeint session, nativeint percentage_func)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_session_end(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_session_destroy(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_session_swap_data (nativeint session_1, nativeint session_2)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_session_get_book(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern void qof_book_mark_session_dirty(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_session_get_file_path (nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_session_get_url(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern bool qof_session_save_in_progress(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern QofBackendError qof_session_get_error (nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_session_get_error_message(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern QofBackendError qof_session_pop_error(nativeint session)

[<DllImport(NativeLibraries.gncEngine)>]
extern  void  qof_session_save (nativeint session, QofBePercentageFunc percentage_func)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint qof_backend_get_registered_access_method_list()

// -----------------------------------------------------------------------------
// libgnucash/engine/policy-p.h

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type PolicyGetLot = delegate of nativeint * nativeint -> nativeint

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type PolicyGetSplit = delegate of nativeint * nativeint -> nativeint

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type PolicyGetLotOpening = delegate of nativeint * nativeint * nativeint * nativeint * nativeint -> unit

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type PolicyIsOpeningSplit = delegate of nativeint * nativeint * nativeint -> [<return: MarshalAs(UnmanagedType.I1)>] bool


[<Struct; StructLayout(LayoutKind.Sequential)>]
type gncpolicy_s =
    [<DefaultValue>]
    val mutable name: nativeint

    [<DefaultValue>]
    val mutable description: nativeint

    [<DefaultValue>]
    val mutable hint: nativeint

    [<DefaultValue>]
    val mutable PolicyGetLot: nativeint

    [<DefaultValue>]
    val mutable PolicyGetSplit: nativeint

    [<DefaultValue>]
    val mutable PolicyGetLotOpening: nativeint

    [<DefaultValue>]
    val mutable PolicyIsOpeningSplit: nativeint

// -----------------------------------------------------------------------------
// libgnucash/engine/Split.h

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccMallocSplit (nativeint book)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccSplitDestroy (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccSplitGetAccount (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetAccount (nativeint s, nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccSplitGetParent (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetParent (nativeint split, nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetMemo (nativeint split, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string memo)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccSplitGetMemo (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetAction (nativeint split, [<MarshalAs(UnmanagedType.LPUTF8Str)>] string action)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint  xaccSplitGetAction (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetReconcile (nativeint split, byte reconciled_flag)

[<DllImport(NativeLibraries.gncEngine)>]
extern byte xaccSplitGetReconcile (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetValue (nativeint split, gnc_numeric value)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetAmount (nativeint split, gnc_numeric amount)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetDateReconciledSecs (nativeint split, time64 time)

[<DllImport(NativeLibraries.gncEngine)>]
extern time64 xaccSplitGetDateReconciled (nativeint split)

// This routine sets both value and amount
// price = value / amount (the price is calculated automatically?)

// Depending on the base_currency, set either the value or the amount
// of this split or both: If the base_currency is the transaction's
// commodity, set the value.  If it is the account's commodity, set the
// amount. If both, set both.
[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccSplitSetBaseValue (nativeint split, gnc_numeric value,
                                   nativeint base_currency)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccSplitGetValue (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccSplitGetAmount (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccSplitGetSharePrice (nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccSplitGetLot (nativeint split)

[<Literal>]
let SPLIT_DATE_RECONCILED="date-reconciled"
[<Literal>]
let SPLIT_BALANCE="balance"
[<Literal>]
let SPLIT_CLEARED_BALANCE="cleared-balance"
[<Literal>]
let SPLIT_RECONCILED_BALANCE="reconciled-balance"
[<Literal>]
let SPLIT_MEMO="memo"
[<Literal>]
let SPLIT_ACTION="action"
[<Literal>]
let SPLIT_RECONCILE="reconcile-flag"
[<Literal>]
let SPLIT_AMOUNT="amount"
[<Literal>]
let SPLIT_SHARE_PRICE="share-price"
[<Literal>]
let SPLIT_VALUE="value"
[<Literal>]
let SPLIT_TYPE="type"
[<Literal>]
let SPLIT_VOIDED_AMOUNT="voided-amount"
[<Literal>]
let SPLIT_VOIDED_VALUE="voided-value"
[<Literal>]
let SPLIT_LOT="lot"
[<Literal>]
let SPLIT_TRANS="trans"
[<Literal>]
let SPLIT_ACCOUNT="account"
[<Literal>]
let SPLIT_ACCOUNT_GUID="account-guid"
[<Literal>]
let SPLIT_ACCT_FULLNAME="acct-fullname"
[<Literal>]
let SPLIT_CORR_ACCT_NAME="corr-acct-fullname"
[<Literal>]
let SPLIT_CORR_ACCT_CODE="corr-acct-code"

// -----------------------------------------------------------------------------
// libgnucash/engine/gnc-lot.h

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric gnc_lot_get_balance (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
[<return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_lot_is_closed (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_lot_get_title (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_lot_get_notes (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_lot_add_split (nativeint lot, nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_lot_remove_split (nativeint lot, nativeint split)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList gnc_lot_get_split_list (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_lot_get_earliest_split (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_lot_get_latest_split (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_lot_destroy (nativeint lot)

// -----------------------------------------------------------------------------
// libgnucash/engine/Transaction.h

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccMallocTransaction (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransBeginEdit (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransSetCurrency (nativeint trans, nativeint curr)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransSetDate (nativeint trans, int day, int mon, int year)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransSetDatePostedSecsNormalized (nativeint trans, time64 time)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransSetDescription (nativeint trans, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string desc)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccTransGetDescription (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern time64 xaccTransGetDate (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccTransGetCurrency (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransSetNotes (nativeint trans, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string notes)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransCommitEdit (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransRollbackEdit (nativeint trans)

let inline xaccTransAppendSplit (t, s) = xaccSplitSetParent(s, t)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccTransLookup (byte[] guid, nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransRecordPrice (nativeint trans, PriceSource source)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccTransReverse(nativeint transaction)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccTransIsBalanced(nativeint trans)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccTransUseTradingAccounts(nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccTransGetImbalanceValue (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList xaccTransGetSplitList (nativeint trans)


[<Literal>]
let TRANS_KVP="kvp"
[<Literal>]
let TRANS_NUM="num"
[<Literal>]
let TRANS_DESCRIPTION="desc"
[<Literal>]
let TRANS_DATE_ENTERED="date-entered"
[<Literal>]
let TRANS_DATE_POSTED="date-posted"
[<Literal>]
let TRANS_DATE_DUE="date-due"
[<Literal>]
let TRANS_IMBALANCE="trans-imbalance"
[<Literal>]
let TRANS_IS_BALANCED="trans-balanced?"
[<Literal>]
let TRANS_IS_CLOSING="trans-is-closing?"
[<Literal>]
let TRANS_NOTES="notes"
[<Literal>]
let TRANS_DOCLINK="doclink"
[<Literal>]
let TRANS_TYPE="type"
[<Literal>]
let TRANS_VOID_STATUS="void-p"
[<Literal>]
let TRANS_VOID_REASON="void-reason"
[<Literal>]
let TRANS_VOID_TIME="void-time"
[<Literal>]
let TRANS_SPLITLIST="split-list"

// -----------------------------------------------------------------------------
// libgnucash/engine/Account.h

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type xaccGetBalanceFn =
    delegate of
        account: nativeint -> gnc_numeric

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type xaccGetBalanceInCurrencyFn =
    delegate of
        account: nativeint *
        report_commodity: nativeint *
        [<MarshalAs(UnmanagedType.I1)>]include_children: bool -> gnc_numeric

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type xaccGetBalanceAsOfDateFn =
    delegate of
        account : nativeint *
        date : time64 -> gnc_numeric

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type AccountCb =
    delegate of
        a : nativeint *
        data : gpointer -> unit

[<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
type AccountCb2 =
    delegate of
        a : nativeint *
        data : gpointer -> gpointer

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_get_account_separator_string ()

[<DllImport(NativeLibraries.gncEngine)>]
extern char gnc_get_account_separator ()

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_set_account_separator ([<MarshalAs(UnmanagedType.LPUTF8Str)>]string separator)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_book_get_root_account(nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_book_set_root_account(nativeint book, nativeint root)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_create_root (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern bool gnc_account_and_descendants_empty (nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_get_currency_or_parent(nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetCommodity (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetCommodity (nativeint account, nativeint comm)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetType (nativeint account, GNCAccountType)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetName (nativeint account, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string name)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetCode (nativeint account, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string code)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetName(nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetCode (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_get_full_name(nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetColor (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetColor (nativeint account, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string color)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetFilter (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetFilter (nativeint account, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string filter)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetSortOrder (nativeint account)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccAccountGetSortReversed (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_get_policy (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetLastNum (nativeint account)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool gnc_account_get_defer_bal_computation (nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGainsAccount (nativeint acc, nativeint curr)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList gnc_account_get_descendants(nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList gnc_account_get_children(nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_account_foreach_descendant (nativeint account,
                                     AccountCb func, gpointer user_data)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccMallocAccount (nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountDestroy (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_account_append_child (nativeint new_parent, nativeint child)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_get_parent (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountBeginEdit (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountCommitEdit (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetHidden (nativeint acc, [<MarshalAs(UnmanagedType.I1)>]bool hidden)

[<DllImport(NativeLibraries.gncEngine)>]
extern bool xaccAccountGetHidden (nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetPlaceholder (nativeint account, [<MarshalAs(UnmanagedType.I1)>]bool placeholder)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccAccountGetPlaceholder (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetBalance (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetClearedBalance (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetPresentBalance (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetProjectedMinimumBalance (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern BorrowedGList xaccAccountGetSplitList (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountMoveAllSplits (nativeint accfrom, nativeint accto)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountLookup (nativeint guid, nativeint book)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_lookup_by_name (nativeint parent, [<MarshalAs(UnmanagedType.LPUTF8Str)>]string name)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_lookup_by_full_name (nativeint any_account,
                                                  [<MarshalAs(UnmanagedType.LPUTF8Str)>]string name)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint gnc_account_lookup_by_code (nativeint parent,
                                             [<MarshalAs(UnmanagedType.LPUTF8Str)>]string code)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetBalanceInCurrency (
    nativeint account, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetClearedBalanceInCurrency (
    nativeint account, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetReconciledBalanceInCurrency (
    nativeint account, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetPresentBalanceInCurrency (
    nativeint account, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetProjectedMinimumBalanceInCurrency (
    nativeint account, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountGetBalanceAsOfDateInCurrency(
    nativeint account, time64 date, nativeint report_commodity,
    [<MarshalAs(UnmanagedType.I1)>]bool include_children)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountConvertBalanceToCurrency(
    nativeint account,
    gnc_numeric balance,
    nativeint balance_currency,
    nativeint new_currency)

[<DllImport(NativeLibraries.gncEngine)>]
extern gnc_numeric xaccAccountConvertBalanceToCurrencyAsOfDate(
    nativeint account,
    gnc_numeric balance, nativeint balance_currency,
    nativeint new_currency, time64 date)

// getters/setters --------------------------------------------------------------------------------

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetNotes (nativeint account)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetNotes (nativeint account, string notes)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountSetDescription (nativeint acc, string str)

[<DllImport(NativeLibraries.gncEngine)>]
extern nativeint xaccAccountGetDescription (nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern GNCAccountType xaccAccountGetType (nativeint account)

// Misc. -----------------------------------------------------------------------
let inline xaccAccountInsertSplit (acc, s) = xaccSplitSetAccount(s, acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern gint xaccAccountForEachTransaction(nativeint account,
                                   TransactionCallback proc,
                                   nativeint data)

[<DllImport(NativeLibraries.gncEngine)>]
extern OwnedGList xaccAccountGetLotList (nativeint account)

// -----------------------------------------------------------------------------
// libgnucash/engine/Account.cpp

[<Struct; StructLayout(LayoutKind.Sequential)>]
type CurrencyBalance =
    val mutable currency: nativeint
    val mutable balance: gnc_numeric
    val mutable fn: xaccGetBalanceFn
    val mutable asOfDateFn: xaccGetBalanceAsOfDateFn
    val mutable date: time64

let xaccAccountGetXxxBalanceInCurrency (acc: nativeint,
                                        fn: xaccGetBalanceFn ,
                                        report_currency: nativeint) =

    xaccAccountConvertBalanceToCurrency(
        acc, fn.Invoke(acc), xaccAccountGetCommodity acc, report_currency)

let xaccAccountGetXxxBalanceAsOfDateInCurrency(acc: nativeint,
                                               date: time64,
                                               fn: xaccGetBalanceAsOfDateFn,
                                               report_commodity: nativeint) =

    xaccAccountConvertBalanceToCurrencyAsOfDate(
       acc, fn.Invoke(acc, date), xaccAccountGetCommodity acc, report_commodity, date)

let xaccAccountBalanceHelper (acc: nativeint, data: gpointer) =
    let mutable cb = Marshal.GetDelegateForFunctionPointer<CurrencyBalance> data
    let mutable balance = gnc_numeric_zero ()

    balance <- xaccAccountGetXxxBalanceInCurrency (acc, cb.fn, cb.currency)

    cb.balance <- gnc_numeric_add (cb.balance, balance,
                                   gnc_commodity_get_fraction (cb.currency),
                                   GncNumericFlags.GNC_HOW_RND_ROUND_HALF_UP)

let xaccAccountBalanceAsOfDateHelper (acc: nativeint, data: gpointer) =
    let mutable cb = Marshal.GetDelegateForFunctionPointer<CurrencyBalance> data
    let mutable balance = gnc_numeric_zero ()

    balance <- xaccAccountGetXxxBalanceAsOfDateInCurrency (acc, cb.date, cb.asOfDateFn, cb.currency)

    cb.balance <- gnc_numeric_add (cb.balance, balance,
                                   gnc_commodity_get_fraction (cb.currency),
                                   GncNumericFlags.GNC_HOW_RND_ROUND_HALF_UP)

let xaccAccountGetXxxBalanceInCurrencyRecursive (acc: nativeint,
                                                 fn: xaccGetBalanceFn,
                                                 report_commodity: nativeint,
                                                 include_children: bool) =

    let mutable balance = gnc_numeric_zero ()
    let mutable commodity = report_commodity

    if acc = IntPtr.Zero then balance
    else
        if commodity = IntPtr.Zero then
            commodity <- xaccAccountGetCommodity (acc)
        if commodity = IntPtr.Zero then
            gnc_numeric_zero()
        else
            balance <- xaccAccountGetXxxBalanceInCurrency (acc, fn, report_commodity)

            if include_children then
                let mutable cb = CurrencyBalance ()
                cb.currency <- commodity
                cb.balance <- balance
                cb.fn <- fn
                cb.date <- 0

                gnc_account_foreach_descendant (acc, FuncConvert.FuncFromTupled xaccAccountBalanceHelper, Marshal.GetFunctionPointerForDelegate cb)

                balance <- cb.balance

            balance

let xaccAccountGetXxxBalanceAsOfDateInCurrencyRecursive (acc: nativeint,
                                                         date: time64,
                                                         fn: xaccGetBalanceAsOfDateFn,
                                                         report_commodity: nativeint,
                                                         include_children: bool) =

    let mutable balance = gnc_numeric_zero ()
    let mutable commodity = report_commodity

    if acc = IntPtr.Zero then balance
    else
        if commodity = IntPtr.Zero then
            commodity <- xaccAccountGetCommodity (acc)
        if commodity = IntPtr.Zero then
            gnc_numeric_zero()
        else
            balance <- xaccAccountGetXxxBalanceAsOfDateInCurrency (acc, date, fn, report_commodity)

            if include_children then
                let mutable cb = CurrencyBalance ()
                cb.currency <- commodity
                cb.balance <- balance
                cb.asOfDateFn <- fn
                cb.date <- date

                gnc_account_foreach_descendant (acc, FuncConvert.FuncFromTupled xaccAccountBalanceAsOfDateHelper, Marshal.GetFunctionPointerForDelegate cb)

                balance <- cb.balance

            balance

// -----------------------------------------------------------------------------
// libgnucash/engine/engine-helpers.h

[<DllImport(NativeLibraries.gncEngine)>]
extern void gnc_set_num_action (nativeint trans, nativeint split,
                                [<MarshalAs(UnmanagedType.LPUTF8Str)>]string num,
                                [<MarshalAs(UnmanagedType.LPUTF8Str)>]string action)

// -----------------------------------------------------------------------------
// libgnucash/core-utils/gnc-path.h

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_path_get_prefix()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_path_get_libdir()

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_path_get_pkglibdir()

// -----------------------------------------------------------------------------
// libgnucash/core-utils/binreloc.h

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_gbr_find_bin_dir(nativeint ptr)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_gbr_find_lib_dir(nativeint default_lib_dir)

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_gbr_set_exe([<MarshalAs(UnmanagedType.LPUTF8Str)>] string ptr)

// -----------------------------------------------------------------------------
// libgnucash/core-utils/gnc-environment.h

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern void gnc_environment_setup()

// -----------------------------------------------------------------------------
// libgnucash/core-utils/gnc-environment.h

[<DllImport(NativeLibraries.gncCoreUtils)>]
extern nativeint gnc_locale_default_iso_currency_code ()

// -----------------------------------------------------------------------------
// libgnucash/app-utils/gnc-prefs-utils.h

[<DllImport(NativeLibraries.gncAppUtils)>]
extern void gnc_prefs_init()

// -----------------------------------------------------------------------------
// libgnucash/engine/TransLog.h

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccLogEnable()

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccLogDisable()

//------------------------------------------------------------------------------
// libgnucash/engine/Scrub.h (convert single-entry accounts to clean double-entry)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransScrubSplits (nativeint trans)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccTransScrubImbalance (nativeint trans, nativeint root,
                              nativeint parent)

//------------------------------------------------------------------------------
// libgnucash/engine/Scrub3.h (High-Level Lot Constraint routines.)

[<DllImport(NativeLibraries.gncEngine); return: MarshalAs(UnmanagedType.I1)>]
extern bool xaccScrubLot (nativeint lot)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountScrubLots (nativeint acc)

[<DllImport(NativeLibraries.gncEngine)>]
extern void xaccAccountTreeScrubLots (nativeint acc)
