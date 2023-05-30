namespace NetCash

open System
open System.Runtime.InteropServices

open NetCash
open NetCash.Marshalling

type Price internal (handle, owns) =
    interface INativeWrapper with
        member _.NativeHandle = handle

    [<DefaultValue>]
    val mutable private _released: bool

    member _.Value
        with get () = Bindings.gnc_price_get_value handle |> GncNumeric
        and set (value: GncNumeric) = Bindings.gnc_price_set_value (handle, value.value)

    member _.Time
        with get () =
            Bindings.gnc_price_get_time64 handle
            |> DateOnly.fromTimestamp
        and set (value) = Bindings.gnc_price_set_time64 (handle, DateOnly.toTimestamp value)

    member _.Source
        with get () = Bindings.gnc_price_get_source (handle)
        and set (value) = Bindings.gnc_price_set_source (handle, value)

    member self.SourceString = self.Source.GetDescription()

    member _.Currency
        with get () =
            handle
            |> Bindings.gnc_price_get_currency
            |> GnuCashObject.Registry.getOrCreate<Commodity>
        and set (value: Commodity) = Bindings.gnc_price_set_currency (handle, GnuCashObject.nativeHandle value)

    member _.Commodity
        with get () =
            handle
            |> Bindings.gnc_price_get_commodity
            |> GnuCashObject.Registry.getOrCreate<Commodity>
        and set (value: Commodity) = Bindings.gnc_price_set_commodity (handle, GnuCashObject.nativeHandle value)

    member _.Type
        with get () =
            Bindings.gnc_price_get_typestr handle
            |> String.fromBorrowed
        and set (value) = Bindings.gnc_price_set_typestr (handle, value)

    member internal self.Release() =
        if owns && not self._released then
            Bindings.gnc_price_unref (handle)
            self._released <- true

type PriceDB private (handle) =
    interface IGnuCashEntity with
        member _.NativeHandle = handle

    member _.Prices: seq<Price> =
        let result = ResizeArray()

        Bindings.gnc_pricedb_foreach_price (
            handle,
            Bindings.GncPriceForeachFunc (fun p _ ->
                result.Add(Price(p, false))
                true),
            IntPtr.Zero,
            false
        )
        |> ignore

        result

    member _.FindPricesForCommodity(commodity: Commodity, currency: Commodity) =
        use wrapper =
            Bindings.gnc_pricedb_get_prices (
                handle,
                GnuCashObject.nativeHandle commodity,
                GnuCashObject.nativeHandle currency
            )

        wrapper.Map(fun ptr -> Price(ptr, false))

    member _.FindLatestPriceForCommodity(commodity: Commodity, currency: Commodity) =
        let ptr =
            Bindings.gnc_pricedb_lookup_latest (
                handle,
                GnuCashObject.nativeHandle commodity,
                GnuCashObject.nativeHandle currency
            )

        let p = Price(ptr, true)
        // `gnc_pricedb_lookup_latest` increases the ref count before it returns,
        // so I guess it's correct to drop the ref count here.
        p.Release()
        p

    member self.AddPrice
        (
            value: GncNumeric,
            time: DateOnly,
            commodity: Commodity,
            currency: Commodity,
            [<Optional; DefaultParameterValue(Bindings.PRICE_TYPE_LAST)>] typeStr: string,
            [<Optional; DefaultParameterValue(Bindings.PriceSource.PRICE_SOURCE_USER_PRICE)>] source: Bindings.PriceSource
        ) =
        let p = Price(Bindings.gnc_price_create (GnuCashObject.bookHandle self), true)

        p.Value <- value
        p.Time <- time
        p.Commodity <- commodity
        p.Currency <- currency
        p.Source <- source
        p.Type <- typeStr

        Bindings.gnc_pricedb_add_price (handle, GnuCashObject.nativeHandle p)
        |> ignore

        // Now the price db takes the ownership, it's OK to drop the ref.
        p.Release()

    member _.RemovePrice(p: Price) =
        Bindings.gnc_pricedb_remove_price (handle, GnuCashObject.nativeHandle p)
        |> ignore

    member _.RemoveOldPrices
        (
            comms: seq<Commodity>,
            cutoff: DateOnly,
            [<Optional; DefaultParameterValue(Bindings.PriceRemoveSourceFlags.PRICE_REMOVE_SOURCE_USER)>] sourceFlags,
            [<Optional; DefaultParameterValue(Bindings.PriceRemoveKeepOptions.PRICE_REMOVE_KEEP_NONE)>] keepOptions
        ) =
        use wrapper = new Bindings.OwnedGList(Seq.map GnuCashObject.nativeHandle comms)

        Bindings.gnc_pricedb_remove_old_prices (
            handle,
            wrapper.DangerousGetHandle(),
            IntPtr.Zero,
            Marshalling.DateOnly.toTimestamp cutoff,
            sourceFlags,
            keepOptions
        )
        |> ignore
