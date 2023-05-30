namespace NetCash.Tests;

using System;
using System.Linq;

using Xunit;

public class ObjectFixture
{
    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Should_Maintain_Mapping_Between_Managed_And_Native_Objects(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            var equity = book.FindAccountByName("Opening Balances");
            var cash = book.FindAccountByName("Cash in Wallet");
            var checking = book.FindAccountByName("Checking Account");
            var income = book.FindAccountByName("Income");

            equity.TransferTo(cash, 100);
            income.TransferTo(checking, 100);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.Contains(NetCashExtensions.TestingCurrency, book.CommodityTable);
            Assert.Contains(book.DefaultCurrency, book.CommodityTable);

            foreach (var account in book.Accounts)
            {
                Assert.Contains(account, account.Parent.Children);
                Assert.Same(account.Parent, account.Parent.Children.First().Parent);

                Assert.Contains(account.Currency, book.CommodityTable);

                foreach (var transaction in account.Transactions)
                {
                    Assert.Same(transaction, transaction.Splits.First().Transaction);
                    Assert.Contains(transaction.Currency, book.CommodityTable);
                }
            }
        }

        // When there's no book open, the registry should be empty.
        Assert.Empty(GnuCashObject.Registry.objectStore);
    }
}
