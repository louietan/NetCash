namespace NetCash

open System
open System.Collections.Generic
open System.Runtime.InteropServices

open NetCash.Marshalling

/// For manipulating commodity-related data like commodities, currencies, commodity namespaces.
type CommodityTable internal (book, handle) as self =
    interface INativeWrapper with
        member _.NativeHandle = handle

    // Commodity -------------------------------------------------------------------

    interface IEnumerable<Commodity> with
        member _.GetEnumerator() : IEnumerator<Commodity> = self.Commodities.GetEnumerator()

        member _.GetEnumerator() : Collections.IEnumerator = self.Commodities.GetEnumerator()

    /// Gets all the commodities.
    member _.Commodities =
        let commodities = ResizeArray()

        Bindings.gnc_commodity_table_foreach_commodity (
            handle,
            Bindings.CommidityCallback (fun comm _ ->
                commodities.Add(GnuCashObject.Registry.getOrCreate<Commodity> comm)
                true),
            IntPtr.Zero
        )
        |> ignore

        commodities

    /// Gets ISO currencies.
    member val ISOCurrencies = ISOCurrencies handle

    /// Gets a commodity by it's mnemonic.
    member _.FindCommodity(commodityNamespace, mnemonic) =
        Bindings.gnc_commodity_table_lookup (handle, commodityNamespace, mnemonic)
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// Gets a commodity by its unique name.
    member _.FindCommodityByUniqueName(uniqueName) =
        Bindings.gnc_commodity_table_lookup_unique (handle, uniqueName)
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// Adds a new commodity.
    member _.AddCommodity
        (
            fullName,
            commodityNamespace,
            mnemonic,
            [<Optional; DefaultParameterValue 1>] fraction,
            [<Optional>] identificationCode
        ) =
        let comm =
            Bindings.gnc_commodity_new (book, fullName, commodityNamespace, mnemonic, identificationCode, fraction)

        Bindings.gnc_commodity_table_insert (handle, comm)
        |> GnuCashObject.Registry.getOrCreate<Commodity>

    /// Rmoves a commodity.
    member _.RemoveCommodity(comm: Commodity) =
        Bindings.gnc_commodity_table_remove (handle, GnuCashObject.nativeHandle comm)

    /// Finds quotable commodities (those with price retrieval).
    member _.QuotableCommodities =
        use wrapper = Bindings.gnc_commodity_table_get_quotable_commodities handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<Commodity>

    // Namespace -------------------------------------------------------------------

    /// Gets names of commodity namespaces.
    member _.NamespaceNames =
        use wrapper = Bindings.gnc_commodity_table_get_namespaces handle
        wrapper.Map String.fromBorrowed

    /// Gets namespace objects.
    member _.Namespaces =
        use wrapper = Bindings.gnc_commodity_table_get_namespaces_list handle
        wrapper.Map GnuCashObject.Registry.getOrCreate<CommodityNamespace>

    /// Gets whether a namespace exists.
    member _.HasNamespace ns =
        Bindings.gnc_commodity_table_has_namespace (handle, ns) = 1

    /// Adds a namespace.
    member _.AddNamespace ns =
        Bindings.gnc_commodity_table_add_namespace (handle, ns, book)
        |> GnuCashObject.Registry.getOrCreate<CommodityNamespace>

    /// Gets a namespace object.
    member _.FindNamespace ns =
        Bindings.gnc_commodity_table_find_namespace (handle, ns)
        |> GnuCashObject.Registry.getOrCreate<CommodityNamespace>

    /// <summary>Deletes a commodity namespace.</summary>
    /// <remarks>
    /// CAUTION: the underlying routine will also destroy commodities included in the namespace,
    /// leaving corresponding managed Commodity objects in an invalid state.
    /// </remarks>
    member _.DeleteNamespace ns =
        Bindings.gnc_commodity_table_delete_namespace (handle, ns)
