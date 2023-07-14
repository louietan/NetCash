namespace NetCash

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open NetCash.Marshalling

type GncNumericException(code) =
    inherit Exception(code
                      |> Bindings.gnc_numeric_errorCode_to_string
                      |> String.fromBorrowed)

    member _.Code = code

/// Wrapper for native gnc_numeric.
[<Struct; CustomComparison; CustomEquality>]
type GncNumeric =
    val mutable internal value: Bindings.gnc_numeric

    new(n) =
        let err = Bindings.gnc_numeric_check n

        if err <> Bindings.GNCNumericErrorCode.GNC_ERROR_OK then
            raise (GncNumericException err)

        { value = n }

    new(n) = { value = Bindings.gnc_numeric_create (n, 1) }

    new(num, denom) = { value = Bindings.gnc_numeric_create (num, denom) }

    override self.ToString() =
        Bindings.gnc_numeric_to_string (self.value)
        |> String.fromBorrowed

    interface IComparable<GncNumeric> with
        member self.CompareTo(other: GncNumeric) =
            Bindings.gnc_numeric_compare (self.value, other.value)

    interface IEquatable<GncNumeric> with
        member self.Equals(other: GncNumeric) =
            Bindings.gnc_numeric_compare (self.value, other.value) = 0

    static member inline private gnc_numeric_op_auto
        (
            op: Bindings.gnc_numeric * Bindings.gnc_numeric * Bindings.gint64 * Bindings.GncNumericFlags -> Bindings.gnc_numeric,
            a,
            b
        ) =
        op (a, b, Bindings.GNC_DENOM_AUTO, Bindings.GncNumericFlags.GNC_HOW_RND_NEVER)

    static member Zero = GncNumeric(Bindings.gnc_numeric_zero ())

    static member Approximate
        (
            d,
            [<Optional; DefaultParameterValue 0L>] denom: Bindings.gint64,
            [<Optional; DefaultParameterValue(Bindings.GncNumericFlags.GNC_HOW_RND_NEVER)>] how: Bindings.GncNumericFlags
        ) =
        Bindings.double_to_gnc_numeric (d, denom, how)
        |> GncNumeric

    static member private FromString(s) =
        let mutable n = Bindings.gnc_numeric_zero ()
        (Bindings.string_to_gnc_numeric (s, &n), n)

    static member Parse(s) =
        let (success, n) = GncNumeric.FromString s

        if not success then
            raise (FormatException $"Failed to convert from {s} to GncNumeric")

        GncNumeric n

    static member TryParse(s, result: outref<GncNumeric>) =
        let (success, n) = GncNumeric.FromString s
        result <- GncNumeric n
        success

    static member op_UnaryNegation(n: GncNumeric) =
        GncNumeric(Bindings.gnc_numeric_neg n.value)

    static member op_Implicit(n: GncNumeric) = Bindings.gnc_numeric_to_double n.value

    static member op_Implicit(n: GncNumeric) = n.value

    static member op_Implicit(n: int32) = GncNumeric(int64 n)

    static member op_Implicit(n: int64) = GncNumeric n

    static member op_Equality(a: GncNumeric, b: GncNumeric) = Bindings.gnc_numeric_compare (a, b) = 0

    static member op_LessThan(a: GncNumeric, b: GncNumeric) = Bindings.gnc_numeric_compare (a, b) < 0

    static member op_LessThanOrEqual(a: GncNumeric, b: GncNumeric) =
        Bindings.gnc_numeric_compare (a, b) <= 0

    static member op_GreaterThan(a: GncNumeric, b: GncNumeric) = Bindings.gnc_numeric_compare (a, b) > 0

    static member op_GreaterThanOrEqual(a: GncNumeric, b: GncNumeric) =
        Bindings.gnc_numeric_compare (a, b) >= 0

    static member op_Addition(a: GncNumeric, b: GncNumeric) =
        GncNumeric.gnc_numeric_op_auto (Bindings.gnc_numeric_add, a, b)
        |> GncNumeric

    static member op_Subtraction(a: GncNumeric, b: GncNumeric) =
        GncNumeric.gnc_numeric_op_auto (Bindings.gnc_numeric_sub, a, b)
        |> GncNumeric

    static member op_Multiply(a: GncNumeric, b: GncNumeric) =
        GncNumeric.gnc_numeric_op_auto (Bindings.gnc_numeric_mul, a, b)
        |> GncNumeric

    static member op_Division(a: GncNumeric, b: GncNumeric) =
        GncNumeric.gnc_numeric_op_auto (Bindings.gnc_numeric_div, a, b)
        |> GncNumeric

    member self.IsZero = Bindings.gnc_numeric_zero_p self

    member self.Abs() =
        Bindings.gnc_numeric_abs self |> GncNumeric

    member self.Convert(newDenom: Bindings.gint64, how: Bindings.GncNumericFlags) =
        Bindings.gnc_numeric_convert (self, newDenom, how)
        |> GncNumeric

    member self.Reduce() =
        Bindings.gnc_numeric_reduce self |> GncNumeric
