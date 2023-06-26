namespace NetCash.Tests;

using System;
using System.Linq;

using Xunit;

public class LotFixture
{
    [Theory]
    [SetupTestingBook(CopyPremade = "investment")]
    public void Can_Scrub_Accounts(TestingBook testingBook)
    {
        var journal = new TradingJournal(@"
  |       Time | Capital  |   Qty | Commodity     | Price |
  |------------+----------+-------+---------------+-------|
  | 2020-01-01 | Bank ABC |   100 | GNU, Inc.     |    10 |
  | 2020-01-02 | Bank ABC |   100 | GNU, Inc.     |    11 |
  | 2020-01-03 | Bank ABC |   100 | GNU, Inc.     |    12 |
  | 2020-01-04 | Bank ABC |  -140 | GNU, Inc.     |    13 |
  | 2020-01-05 | Bank ABC |  -160 | GNU, Inc.     |    14 |
  |------------+----------+-------+---------------+-------|
  | 2020-01-01 | Bank ABC |   100 | Banana, Corp. |  1.00 |
  | 2020-01-02 | Bank ABC |   200 | Banana, Corp. |  0.90 |
  | 2020-01-03 | Bank ABC |   300 | Banana, Corp. |  0.81 |
  | 2020-01-04 | Bank ABC |   400 | Banana, Corp. |  0.73 |
  | 2020-01-05 | Bank ABC | -1000 | Banana, Corp. |  0.66 |
");

        // account setup
        using (var book = Book.Open(testingBook))
        {
            var stock = book.FindAccountByFullName("Assets", "Investments", "Brokerage Account", "Stock");

            stock.NewCommodityAccount(CommodityType.Stock, "Banana, Corp.", "Asia Pacific", "BNN");
            stock.NewCommodityAccount(CommodityType.Stock, "GNU, Inc.", "US", "GNU");
        }

        // make trades
        using (var book = Book.Open(testingBook))
        {
            journal.Replay(book);
        }

        // scrub
        using (var book = Book.Open(testingBook))
        {
            var stock = book.FindAccountByFullName("Assets", "Investments", "Brokerage Account", "Stock");
            Assert.Equal(stock.Children.Count(), book.FindAccountsWithFreeSplits().Count());

            Scrubber.ScrubLots(stock, treeWise: true);
        }

        // verify
        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);
            Assert.Empty(book.FindAccountsWithFreeSplits());

            var gnu = book.FindAccountByName("GNU, Inc.");
            var bnn = book.FindAccountByName("Banana, Corp.");

            var allLots = new[] { gnu, bnn }.SelectMany(x => x.Lots);

            // every lot should be closed
            Assert.True(allLots.Select(x => x.Closed).Aggregate((a, b) => a && b));

            var lotGains = allLots
                .Select(x => x.RealizedGains)
                .Aggregate((sum, gains) => sum + gains);

            var gains1 = gnu.GetGainsAccountInCurrency(NetCashExtensions.TestingCurrency);
            var gains2 = bnn.GetGainsAccountInCurrency(NetCashExtensions.TestingCurrency);

            Assert.NotNull(gains1);
            Assert.NotNull(gains2);

            // Orphaned Gains - XTS
            Assert.Equal(gains1.FullName, gains2.FullName);

            Assert.Equal(-gains1.Balance, lotGains);
            Assert.Equal(-gains1.Balance, GncNumeric.Approximate(journal.ProfitAndLoss));
        }
    }
}
