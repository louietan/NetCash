(**
This is a WORK-IN-PROGRESS.

The purpose of this module is to provide an intuitive interface to
read/write preferences. The meaning of "intuitive" includes but not
limited to that the naming and organization should reflect the UI
elements on preferences dialog in GnuCash GUI and that values should
be strongly-typed (e.g. use Discriminated Unions to model "choices").

Tips for MS Windows: the preferences are stored in registry with path
"Computer\HKEY_USERS\<SID>\SOFTWARE\GSettings\org\gnucash", where
<SID> is a placeholder for "Security Identifier", which can be found
by invoking "whoami /user" in command line.
*)

module NetCash.Preferences

open System
open System.ComponentModel
open System.Runtime.InteropServices

open FSharp.Reflection

open NetCash.Marshalling

[<Literal>]
let GNC_PREFS_GROUP_GENERAL = "general"

[<Literal>]
let GNC_PREFS_GROUP_GENERAL_REGISTER = "general.register"

[<Literal>]
let GNC_PREFS_GROUP_GENERAL_REPORT = "general.report"

[<Literal>]
let GNC_PREFS_GROUP_DIALOG_TOTD = "dialogs.totd"

[<Literal>]
let GNC_PREFS_GROUP_ACCT_SUMMARY = "window.pages.account-tree.summary"

[<Literal>]
let GNC_PREFS_GROUP_INVOICE = "dialogs.business.invoice"

[<Literal>]
let GNC_PREFS_GROUP_BILL = "dialogs.business.bill"

[<Literal>]
let GNC_PREFS_GROUP = "dialogs.new-hierarchy"

[<Literal>]
let GNC_PREFS_GROUP_SEARCH_GENERAL = "dialogs.search"

[<Literal>]
let GNC_PREFS_GROUP_IMPORT = "dialogs.import.generic"

[<Literal>]
let GNC_PREF_SHOW_ON_NEW_FILE = "show-on-new-file"

[<Literal>]
let GNC_PREF_NUM_SOURCE = "num-source"

[<Literal>]
let GNC_PREFS_SHOW_TIPS = "show-at-startup"

[<Literal>]
let GNC_PREF_SHOW_SPLASH = "show-splash-screen"

[<Literal>]
let GNC_PREF_START_CHOICE_ABS = "start-choice-absolute"

[<Literal>]
let GNC_PREF_START_CHOICE_REL = "start-choice-relative"

[<Literal>]
let GNC_PREF_END_CHOICE_ABS = "end-choice-absolute"

[<Literal>]
let GNC_PREF_END_CHOICE_REL = "end-choice-relative"

[<Literal>]
let GNC_PREF_START_PERIOD = "start-period"

[<Literal>]
let GNC_PREF_END_PERIOD = "end-period"

[<Literal>]
let GNC_PREF_START_DATE = "start-date"

[<Literal>]
let GNC_PREF_END_DATE = "end-date"

[<Literal>]
let GNC_PREF_GRAND_TOTAL = "grand-total"

[<Literal>]
let GNC_PREF_NON_CURRENCY = "non-currency"

[<Literal>]
let GNC_PREF_ACCOUNT_SEPARATOR = "account-separator"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_NONE = "reversed-accounts-none"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_CREDIT = "reversed-accounts-credit"

[<Literal>]
let GNC_PREF_REVERSED_ACCTS_INC_EXP = "reversed-accounts-incomeexpense"

[<Literal>]
let GNC_PREF_ACCOUNTING_LABELS = "use-accounting-labels"

[<Literal>]
let GNC_PREF_CURRENCY_CHOICE_LOCALE = "currency-choice-locale"

[<Literal>]
let GNC_PREF_CURRENCY_CHOICE_OTHER = "currency-choice-other"

[<Literal>]
let GNC_PREF_CURRENCY_OTHER = "currency-other"

[<Literal>]
let GNC_PREF_ACCOUNT_COLOR = "show-account-color"

[<Literal>]
let GNC_PREF_TAB_COLOR = "show-account-color-tabs"

[<Literal>]
let GNC_PREF_ACCUM_SPLITS = "accumulate-splits"

[<Literal>]
let GNC_PREF_EXTRA_TOOLBUTTONS = "enable-toolbuttons"

[<Literal>]
let GNC_PREF_INV_PRINT_RPT = "invoice-printreport"

[<Literal>]
let GNC_PREF_NOTIFY_WHEN_DUE = "notify-when-due"

[<Literal>]
let GNC_PREF_DAYS_IN_ADVANCE = "days-in-advance"

[<Literal>]
let GNC_PREF_TAX_INCL = "tax-included"

[<Literal>]
let GNC_PREF_AUTO_PAY = "auto-pay"

[<Literal>]
let GNC_PREF_USE_NEW = "use-new-window"

[<Literal>]
let GNC_PREF_GRID_LINES_HORIZONTAL = "grid-lines-horizontal"

[<Literal>]
let GNC_PREF_GRID_LINES_VERTICAL = "grid-lines-vertical"

[<Literal>]
let GNC_PREF_FILE_COMPRESSION = "file-compression"

[<Literal>]
let GNC_PREF_AUTOSAVE_SHOW_EXPLANATION = "autosave-show-explanation"

[<Literal>]
let GNC_PREF_AUTOSAVE_INTERVAL = "autosave-interval-minutes"

[<Literal>]
let GNC_PREF_SAVE_CLOSE_EXPIRES = "save-on-close-expires"

[<Literal>]
let GNC_PREF_SAVE_CLOSE_WAIT_TIME = "save-on-close-wait-time"

[<Literal>]
let GNC_PREF_RETAIN_TYPE_NEVER = "retain-type-never"

[<Literal>]
let GNC_PREF_RETAIN_TYPE_DAYS = "retain-type-days"

[<Literal>]
let GNC_PREF_RETAIN_TYPE_FOREVER = "retain-type-forever"

[<Literal>]
let GNC_PREF_RETAIN_DAYS = "retain-days"

[<Literal>]
let GNC_DOC_LINK_PATH_HEAD = "assoc-head"

[<Literal>]
let GNC_PREF_NEW_SEARCH_LIMIT = "new-search-limit"

[<Literal>]
let GNC_PREF_ENABLE_SKIP = "enable-skip"

[<Literal>]
let GNC_PREF_ENABLE_UPDATE = "enable-update"

[<Literal>]
let GNC_PREF_USE_BAYES = "use-bayes"

[<Literal>]
let GNC_PREF_MATCH_THRESHOLD = "match-threshold"

type DateChoice =
    | Specific of DateOnly

    | Today

    | StartOfThisMonth
    | StartOfPreviousMonth
    | StartOfThisQuarter
    | StartOfPreviousQuarter
    | StartOfThisYear
    | StartOfPreviousYear

    | EndOfThisMonth
    | EndOfPreviousMonth
    | EndOfThisQuarter
    | EndOfPreviousQuarter
    | EndOfThisYear
    | EndOfPreviousYear

    static member private Cases = FSharpType.GetUnionCases(typeof<DateChoice>)

    static member private ReadTag = FSharpValue.PreComputeUnionTagReader(typeof<DateChoice>)

    static member internal FromGncAccountingPeriod isEnd (period: Bindings.GncAccountingPeriod) =
        let offset = (+) (if isEnd then 6 else 0)

        let make =
            DateChoice.Cases[int period |> (+) 1 |> offset]
            |> FSharpValue.PreComputeUnionConstructor

        make Array.empty :?> DateChoice

    member internal self.AsGncAccountingPeriod =
        let idx =
            (DateChoice.Cases
             |> Seq.findIndex (fun x -> x.Tag = DateChoice.ReadTag self))
            - 1 // Specific automatically becomes GNC_ACCOUNTING_PERIOD_INVALID

        let last = int Bindings.GncAccountingPeriod.GNC_ACCOUNTING_PERIOD_CYEAR_LAST

        if idx >= last then
            (idx + 1) % last
        else
            idx
        |> enum<Bindings.GncAccountingPeriod>

type ReverseBalancedAccount =
    | [<Description(GNC_PREF_REVERSED_ACCTS_NONE)>] None
    | [<Description(GNC_PREF_REVERSED_ACCTS_CREDIT)>] CreditAccounts
    | [<Description(GNC_PREF_REVERSED_ACCTS_INC_EXP)>] IncomeAndExpense

    static member internal Cases = FSharpType.GetUnionCases(typeof<ReverseBalancedAccount>)

type CurrencyChoice =
    | Locale
    | Other of string

    member self.CurrencyCode =
        match self with
        | Locale ->
            Bindings.gnc_locale_default_iso_currency_code ()
            |> String.fromBorrowed
        | Other code -> code

// ---------------------------------------------------------------------
// Preference groups

type SummarybarContent() =
    member _.IncludeGrandTotal
        with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, GNC_PREF_GRAND_TOTAL)

        and set (value: bool) =
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, GNC_PREF_GRAND_TOTAL, value)
            |> ignore

    member _.IncludeNonCurrencyTotals
        with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, GNC_PREF_NON_CURRENCY)

        and set (value: bool) =
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, GNC_PREF_NON_CURRENCY, value)
            |> ignore

type AccountColor() =
    member _.ShowTheAccountColorAsBackground
        with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNT_COLOR)

        and set (value: bool) =
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNT_COLOR, value)
            |> ignore

    member _.ShowTheAccountColorOnTabs
        with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_TAB_COLOR)

        and set (value: bool) =
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_TAB_COLOR, value)
            |> ignore

module Business =
    type General() =
        member _.EnableExtraButtons
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_EXTRA_TOOLBUTTONS)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_EXTRA_TOOLBUTTONS, value)
                |> ignore

        member _.OpenInNewWindow
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_USE_NEW)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_USE_NEW, value)
                |> ignore

        member _.AccumulateSplitsOnPost
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_ACCUM_SPLITS)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_ACCUM_SPLITS, value)
                |> ignore

    type NotifyType =
        | DaysInAdvance of int
        | Off

    type ReportForPrintingType =
        | Invalid = -1
        | PrintableInvoice = 0
        | TaxInvoice = 1
        | EasyInvoice = 2
        | FancyInvoice = 3

    type Invoices() =
        member _.NotifyWhenDue
            with get () =
                let notify =
                    Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_NOTIFY_WHEN_DUE)

                if not notify then
                    NotifyType.Off
                else
                    let days =
                        Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_INVOICE, GNC_PREF_DAYS_IN_ADVANCE)

                    DaysInAdvance(int days)

            and set (value: NotifyType) =
                match value with
                | Off -> Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_NOTIFY_WHEN_DUE, false)
                | DaysInAdvance days ->
                    Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_INVOICE, GNC_PREF_DAYS_IN_ADVANCE, float days)
                |> ignore

        member _.ReportForPrinting
            with get () =
                let value =
                    Bindings.gnc_prefs_get_int (GNC_PREFS_GROUP_INVOICE, GNC_PREF_INV_PRINT_RPT)

                if value >= 0 && value < 4 then
                    enum<ReportForPrintingType> value
                else
                    ReportForPrintingType.Invalid

            and set (value: ReportForPrintingType) =
                if value = ReportForPrintingType.Invalid then
                    invalidArg (nameof value) "value can not be ReportForPrintingType.Invalid"

                Bindings.gnc_prefs_set_int (GNC_PREFS_GROUP_INVOICE, GNC_PREF_INV_PRINT_RPT, int value)
                |> ignore

        member _.TaxIncluded
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_TAX_INCL)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_TAX_INCL, value)
                |> ignore

        member _.ProcessPaymentsOnPosting
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_AUTO_PAY)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_INVOICE, GNC_PREF_AUTO_PAY, value)
                |> ignore

    type Bills() =
        member _.NotifyWhenDue
            with get () =
                let notify =
                    Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_NOTIFY_WHEN_DUE)

                if not notify then
                    NotifyType.Off
                else
                    let days =
                        Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_BILL, GNC_PREF_DAYS_IN_ADVANCE)

                    DaysInAdvance(int days)

            and set (value: NotifyType) =
                match value with
                | Off -> Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_NOTIFY_WHEN_DUE, false)
                | DaysInAdvance days ->
                    Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_BILL, GNC_PREF_DAYS_IN_ADVANCE, float days)
                |> ignore

        member _.TaxIncluded
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_TAX_INCL)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_TAX_INCL, value)
                |> ignore

        member _.ProcessPaymentsOnPosting
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_AUTO_PAY)

            and set (value) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_BILL, GNC_PREF_AUTO_PAY, value)
                |> ignore

[<AbstractClass>]
type internal DateChoiceConfig() =
    abstract AbsChoiceKey: string
    abstract RelChoiceKey: string
    abstract AbsValueKey: string
    abstract RelValueKey: string
    abstract ToDateChoice: (Bindings.GncAccountingPeriod -> DateChoice)

    member self.Read() =
        let abs =
            Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, self.AbsChoiceKey)

        if abs then
            Bindings.gnc_prefs_get_int64 (GNC_PREFS_GROUP_ACCT_SUMMARY, self.AbsValueKey)
            |> Marshalling.DateOnly.fromTimestamp
            |> Specific
        else
            Bindings.gnc_prefs_get_int (GNC_PREFS_GROUP_ACCT_SUMMARY, self.RelValueKey)
            |> enum<Bindings.GncAccountingPeriod>
            |> self.ToDateChoice

    member self.Write(value: DateChoice) =
        match value with
        | Specific date ->
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, self.AbsChoiceKey, true)
            |> ignore

            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, self.RelChoiceKey, false)
            |> ignore

            Bindings.gnc_prefs_set_int64 (
                GNC_PREFS_GROUP_ACCT_SUMMARY,
                self.AbsValueKey,
                Marshalling.DateOnly.toTimestamp date
            )
            |> ignore
        | relative ->
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, self.AbsChoiceKey, false)
            |> ignore

            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_ACCT_SUMMARY, self.RelChoiceKey, true)
            |> ignore

            Bindings.gnc_prefs_set_int (
                GNC_PREFS_GROUP_ACCT_SUMMARY,
                self.RelValueKey,
                int relative.AsGncAccountingPeriod
            )
            |> ignore

type internal StartDateChoiceConfig() =
    inherit DateChoiceConfig()
    override _.AbsChoiceKey = GNC_PREF_START_CHOICE_ABS
    override _.RelChoiceKey = GNC_PREF_START_CHOICE_REL
    override _.AbsValueKey = GNC_PREF_START_DATE
    override _.RelValueKey = GNC_PREF_START_PERIOD
    override _.ToDateChoice = DateChoice.FromGncAccountingPeriod false

type internal EndDateChoiceConfig() =
    inherit DateChoiceConfig()
    override _.AbsChoiceKey = GNC_PREF_END_CHOICE_ABS
    override _.RelChoiceKey = GNC_PREF_END_CHOICE_REL
    override _.AbsValueKey = GNC_PREF_END_DATE
    override _.RelValueKey = GNC_PREF_END_PERIOD
    override _.ToDateChoice = DateChoice.FromGncAccountingPeriod true

[<AbstractClass; Sealed>]
type AccountingPeriod() =
    static member StartDate
        with get () = Singleton<StartDateChoiceConfig>.Instance.Read ()
        and set (value: DateChoice) = Singleton<StartDateChoiceConfig>.Instance.Write (value)

    static member EndDate
        with get () = Singleton<EndDateChoiceConfig>.Instance.Read ()
        and set (value: DateChoice) = Singleton<EndDateChoiceConfig>.Instance.Write (value)

    static member val SummarybarContent = Singleton<SummarybarContent>.Instance

[<AbstractClass; Sealed>]
type Accounts() =
    static member SeparatorCharacter
        with get () =
            Bindings.gnc_prefs_get_string (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNT_SEPARATOR)
            |> SafeHandle.wrap Bindings.g_free
            |> SafeHandle.using (
                Bindings.gnc_normalize_account_separator
                >> String.fromOwned
            )

        and set (value) =
            Bindings.gnc_prefs_set_string (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNT_SEPARATOR, value)
            |> ignore

    static member ReverseBalancedAccounts
        with get () =
            ReverseBalancedAccount.Cases
            |> Seq.tryFind (fun case ->
                let key =
                    (case.GetCustomAttributes(typeof<DescriptionAttribute>)
                     |> Seq.cast<DescriptionAttribute>
                     |> Seq.head)
                        .Description

                Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, key))
            |> Option.map (fun case -> FSharpValue.PreComputeUnionConstructor case)
            |> Option.map (fun make -> make Array.empty :?> ReverseBalancedAccount)
            |> Option.defaultValue ReverseBalancedAccount.None

        and set (value: ReverseBalancedAccount) =
            let caseInfo, _ = FSharpValue.GetUnionFields(value, typeof<ReverseBalancedAccount>)

            let key =
                caseInfo.GetCustomAttributes(typeof<DescriptionAttribute>)
                |> Seq.cast<DescriptionAttribute>
                |> Seq.map (fun x -> x.Description)
                |> Seq.head

            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, key, true)
            |> ignore

    static member UseFormalAccountingLabels
        with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNTING_LABELS)

        and set (value: bool) =
            Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_ACCOUNTING_LABELS, value)
            |> ignore

    static member DefaultCurrency
        with get () =
            if Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_CHOICE_LOCALE) then
                Locale
            else
                Bindings.gnc_prefs_get_string (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_OTHER)
                |> String.fromOwned
                |> Other

        and set (value: CurrencyChoice) =
            match value with
            | Locale ->
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_CHOICE_LOCALE, true)
                |> ignore

                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_CHOICE_OTHER, false)
                |> ignore
            | Other code ->
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_CHOICE_LOCALE, false)
                |> ignore

                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_CHOICE_OTHER, true)
                |> ignore

                Bindings.gnc_prefs_set_string (GNC_PREFS_GROUP_GENERAL, GNC_PREF_CURRENCY_OTHER, code)
                |> ignore

    static member val AccountColor = Singleton<AccountColor>.Instance

[<AbstractClass; Sealed>]
type Business() =
    static member val General = Singleton<Business.General>.Instance

    static member val Invoices = Singleton<Business.Invoices>.Instance

    static member val Bills = Singleton<Business.Bills>.Instance

module General =
    type General() =
        member _.DisplayTipOfTheDayDialog
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_DIALOG_TOTD, GNC_PREFS_SHOW_TIPS)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_DIALOG_TOTD, GNC_PREFS_SHOW_TIPS, value)
                |> ignore

        member _.ShowSplashScreen
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SHOW_SPLASH)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SHOW_SPLASH, value)
                |> ignore

        member _.PerformAccountListSetupOnNewFile
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP, GNC_PREF_SHOW_ON_NEW_FILE)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP, GNC_PREF_SHOW_ON_NEW_FILE, value)
                |> ignore

        member _.SetBookOptionOnNewFilesToUseSplitActionFieldForNumFieldOnRegistersReports
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_NUM_SOURCE)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_NUM_SOURCE, value)
                |> ignore

        member _.EnableHorizontalGridLinesOnTableDisplays
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_GRID_LINES_HORIZONTAL)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_GRID_LINES_HORIZONTAL, value)
                |> ignore

        member _.EnableVerticalGridLinesOnTableDisplays
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_GRID_LINES_VERTICAL)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_GRID_LINES_VERTICAL, value)
                |> ignore

    type TimeoutOnSaveChangesOnClosingQuestionOption =
        | Yes of TimeToWaitForAnswer: int
        | No

    type Files() =
        member _.CompressFiles
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_FILE_COMPRESSION)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_FILE_COMPRESSION, value)
                |> ignore

        member _.ShowAutoSaveConfirmationQuestion
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_AUTOSAVE_SHOW_EXPLANATION)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_AUTOSAVE_SHOW_EXPLANATION, value)
                |> ignore

        member _.AutoSaveTimeInterval
            with get () = Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_GENERAL, GNC_PREF_AUTOSAVE_INTERVAL)

            and set (value: float) =
                Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_GENERAL, GNC_PREF_AUTOSAVE_INTERVAL, value)
                |> ignore

        member _.EnableTimeoutOnSaveChangesOnClosingQuestion
            with get () =
                let enabled =
                    Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SAVE_CLOSE_EXPIRES)

                if enabled then
                    Bindings.gnc_prefs_get_int (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SAVE_CLOSE_WAIT_TIME)
                    |> TimeoutOnSaveChangesOnClosingQuestionOption.Yes
                else
                    TimeoutOnSaveChangesOnClosingQuestionOption.No

            and set (value: TimeoutOnSaveChangesOnClosingQuestionOption) =
                match value with
                | Yes wait ->
                    Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SAVE_CLOSE_EXPIRES, true)
                    |> ignore

                    Bindings.gnc_prefs_set_int (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SAVE_CLOSE_WAIT_TIME, wait)
                    |> ignore
                | No ->
                    Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_SAVE_CLOSE_EXPIRES, false)
                    |> ignore

    type PathHeadForLinkedFilesRelativePathsOption =
        | Path of string
        | None

        static member internal fromString str =
            if String.IsNullOrWhiteSpace str then
                PathHeadForLinkedFilesRelativePathsOption.None
            else
                PathHeadForLinkedFilesRelativePathsOption.Path str

    type LinkedFiles() =
        member _.PathHeadForLinkedFilesRelativePaths
            with get () =
                Bindings.gnc_prefs_get_string (GNC_PREFS_GROUP_GENERAL, GNC_DOC_LINK_PATH_HEAD)
                |> String.fromOwned
                |> PathHeadForLinkedFilesRelativePathsOption.fromString

            and set (value: PathHeadForLinkedFilesRelativePathsOption) =
                match value with
                | Path path ->
                    let path' =
                        if String.IsNullOrWhiteSpace path then
                            String.Empty
                        else
                            if not <| IO.Directory.Exists path then
                                invalidArg (nameof (value)) $"Directory {path} doesn't exist"

                            Bindings.g_file_new_for_path path
                            |> SafeHandle.wrap Bindings.g_object_unref
                            |> SafeHandle.using (Bindings.g_file_get_uri >> String.fromOwned)

                    Bindings.gnc_prefs_set_string (GNC_PREFS_GROUP_GENERAL, GNC_DOC_LINK_PATH_HEAD, path')
                    |> ignore
                | None ->
                    Bindings.gnc_prefs_set_string (GNC_PREFS_GROUP_GENERAL, GNC_DOC_LINK_PATH_HEAD, String.Empty)
                    |> ignore

    type SearchDialog() =
        member _.NewSearchLimit
            with get () =
                Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_SEARCH_GENERAL, GNC_PREF_NEW_SEARCH_LIMIT)
                |> int
            and set (value: int) =
                Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_SEARCH_GENERAL, GNC_PREF_NEW_SEARCH_LIMIT, float value)
                |> ignore

type RetainLogAndBackupFilesOption =
    | [<Description(GNC_PREF_RETAIN_TYPE_NEVER)>] Never
    | [<Description(GNC_PREF_RETAIN_TYPE_DAYS)>] ForDays of int
    | [<Description(GNC_PREF_RETAIN_TYPE_FOREVER)>] Forever

    static member internal Cases = FSharpType.GetUnionCases typeof<RetainLogAndBackupFilesOption>

    static member internal ReadTag =
        FSharpValue.PreComputeUnionTagReader typeof<RetainLogAndBackupFilesOption>

[<AbstractClass; Sealed>]
type General() =
    static member val General = Singleton<General.General>.Instance

    static member val Files = Singleton<General.Files>.Instance

    static member RetainLogAndBackupFiles
        with get () =
            if (Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_RETAIN_TYPE_NEVER)) then
                RetainLogAndBackupFilesOption.Never
            elif (Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_GENERAL, GNC_PREF_RETAIN_TYPE_DAYS)) then
                Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_GENERAL, GNC_PREF_RETAIN_DAYS)
                |> int
                |> RetainLogAndBackupFilesOption.ForDays
            else
                RetainLogAndBackupFilesOption.Forever

        and set (value: RetainLogAndBackupFilesOption) =
            RetainLogAndBackupFilesOption.Cases
            |> Seq.iter (fun case ->
                let key =
                    (case.GetCustomAttributes(typeof<DescriptionAttribute>)
                     |> Seq.cast<DescriptionAttribute>
                     |> Seq.head)
                        .Description

                Bindings.gnc_prefs_set_bool (
                    GNC_PREFS_GROUP_GENERAL,
                    key,
                    RetainLogAndBackupFilesOption.ReadTag(value) = case.Tag
                )
                |> ignore)

            match value with
            | ForDays days ->
                Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_GENERAL, GNC_PREF_RETAIN_DAYS, float days)
                |> ignore
            | _ -> ()

    static member val LinkedFiles = Singleton<General.LinkedFiles>.Instance

    static member val SearchDialog = Singleton<General.SearchDialog>.Instance

module Import =
    type General() =
        member _.EnableSkipTransactionAction
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_ENABLE_SKIP)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_ENABLE_SKIP, value)
                |> ignore

        member _.EnableUpdateMatchAction
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_ENABLE_UPDATE)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_ENABLE_UPDATE, value)
                |> ignore

        member _.EnableBayesianMatching
            with get () = Bindings.gnc_prefs_get_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_USE_BAYES)

            and set (value: bool) =
                Bindings.gnc_prefs_set_bool (GNC_PREFS_GROUP_IMPORT, GNC_PREF_USE_BAYES, value)
                |> ignore

        member _.MatchDisplayThreshold
            with get () =
                Bindings.gnc_prefs_get_float (GNC_PREFS_GROUP_IMPORT, GNC_PREF_MATCH_THRESHOLD)
                |> int

            and set (value: int) =
                Bindings.gnc_prefs_set_float (GNC_PREFS_GROUP_IMPORT, GNC_PREF_MATCH_THRESHOLD, float value)
                |> ignore

[<AbstractClass; Sealed>]
type internal Import() =
    static member val General = Singleton<Import.General>.Instance
