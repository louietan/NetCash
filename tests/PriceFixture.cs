namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public class PriceFixture
{
    [Theory]
    [SetupTestingBook(CopyPremade = "investment")]
    public void Can_Update_PriceDB(TestingBook testingBook)
    {
        var prices = new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5 };
        var initialDate = new DateOnly(2020, 9, 27);

        // record transaction prices
        using (var book = Book.Open(testingBook))
        {
            book
                .FindAccountByFullName("Assets", "Investments", "Brokerage Account", "Stock")
                .NewCommodityAccount(CommodityType.Stock, "GNU, Inc.", "US", "GNU");

            var bank = book.FindAccountByName("Bank ABC");
            var gnu = book.FindAccountByName("GNU, Inc.");

            foreach (var (price, offset) in prices.WithIndex())
            {
                var shares = 100;
                var value = GncNumeric.Approximate(shares * price);

                book.NewTransaction(initialDate.AddDays(offset), NetCashExtensions.TestingCurrency)
                    .AddSplit(account: bank, value: -value)
                    .AddSplit(account: gnu, value: value, amount: (GncNumeric)shares)
                    .RecordCommodityPrice(true)
                    .Save();
            }
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            var transPrices = book.PriceDB.FindPricesForCommodity(gnu, NetCashExtensions.TestingCurrency);

            Assert.Equal(prices.Count(), transPrices.Count());
        }

        // add price manually
        using (var book = Book.Open(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");

            book.PriceDB.AddPrice(2, DateOnly.FromDateTime(DateTime.Today), gnu, NetCashExtensions.TestingCurrency);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            var gnuPrices = book.PriceDB.FindPricesForCommodity(gnu, NetCashExtensions.TestingCurrency);
            Assert.Equal(prices.Count() + 1, gnuPrices.Count());

            var latestPrice = book.PriceDB.FindLatestPriceForCommodity(gnu, NetCashExtensions.TestingCurrency);
            Assert.True(latestPrice.Value > GncNumeric.Approximate(prices.Last()));
        }

        // remove old prices
        using (var book = Book.Open(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            book.PriceDB.RemoveOldPrices(new[] { gnu }, DateOnly.FromDateTime(DateTime.Today));
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            var gnuPrices = book.PriceDB.FindPricesForCommodity(gnu, NetCashExtensions.TestingCurrency);
            Assert.Equal(1, gnuPrices.Count());
        }

        // remove a specific price
        using (var book = Book.Open(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            var singlePrice = book.PriceDB.FindPricesForCommodity(gnu, NetCashExtensions.TestingCurrency).Single();
            book.PriceDB.RemovePrice(singlePrice);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var gnu = book.CommodityTable.FindCommodity("US", "GNU");
            var gnuPrices = book.PriceDB.FindPricesForCommodity(gnu, NetCashExtensions.TestingCurrency);
            Assert.Equal(0, gnuPrices.Count());
        }
    }
}
