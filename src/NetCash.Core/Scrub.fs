namespace NetCash

open System.Runtime.InteropServices

/// Data scrubbing.
module Scrubber =
    /// <summary>Scrubs lots for an account.</summary>
    /// <param name="acct">Account to scrub.</param>
    /// <param name="treeWise">true to scrub the entire account tree, false to just scrube this account.</param>
    let ScrubLots (acct: Account, [<Optional; DefaultParameterValue false>] treeWise) =
        let scrub =
            if treeWise then
                Bindings.xaccAccountTreeScrubLots
            else
                Bindings.xaccAccountScrubLots

        acct |> GnuCashObject.nativeHandle |> scrub

    /// Finds splits that aren't assigned to any lot.
    let GetFreeSplits (acct: Account) =
        acct.Splits
        |> Seq.filter (fun s -> isUncheckedDefault s.Lot)
