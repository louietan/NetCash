namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

public class TradingJournal
{
    public class Entry
    {
        [DataMember(Order = 0)]
        public DateOnly Date;

        [DataMember(Order = 1)]
        public string Capital;

        [DataMember(Order = 2)]
        public double Qty;

        [DataMember(Order = 3)]
        public string Commodity;

        [DataMember(Order = 4)]
        public double Price;
    }

    IEnumerable<Entry> _entries;

    public TradingJournal(string table)
    {
        this._entries = TableReader.Read<Entry>(table);
    }

    void RecordTransaction(Book book, Entry entry)
    {
        var capital = book.FindAccountByName(entry.Capital);
        var commodity = book.FindAccountByName(entry.Commodity);
        var value = GncNumeric.Approximate(entry.Price * entry.Qty);

        book.NewTransaction(entry.Date, NetCashExtensions.TestingCurrency)
            .AddSplit(account: capital, value: -value)
            .AddSplit(value: value, amount: GncNumeric.Approximate(entry.Qty), account: commodity)
            .Save();
    }

    public void Replay(Book book)
    {
        foreach (var entry in this._entries)
        {
            RecordTransaction(book, entry);
        }
    }

    public double ProfitAndLoss => -this._entries.Sum(x => x.Qty * x.Price);
}
