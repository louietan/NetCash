namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

public class CommodityFixture
{
    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void TERRIBLY_WRITTEN_TESTCASE_FOR_COMMODITYTABLE(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            book.CommodityTable.AddCommodity("Linux, Inc.", "US", "LNX", identificationCode: "1662684960");
            book.CommodityTable.AddCommodity("Banana, Corp.", "Asia Pacific", "BNN", fraction: 100, identificationCode: "98971109711097");
            book.CommodityTable.AddCommodity("Puppycoin", "Crypto", "PPC", fraction: 1000);
            book.CommodityTable.AddCommodity("Kittycoin", "Crypto", "KTC", fraction: 10000);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var lnx = book.CommodityTable.FindCommodity("US", "LNX");
            Assert.NotNull(lnx);
            Assert.Equal("Linux, Inc.", lnx.FullName);
            Assert.Equal("US", lnx.Namespace.Name);
            Assert.Equal(1, lnx.Fraction);
            Assert.Equal("1662684960", lnx.IdentificationCode);

            var bnn = book.CommodityTable.FindCommodity("Asia Pacific", "BNN");
            Assert.NotNull(bnn);
            Assert.Equal("Banana, Corp.", bnn.FullName);
            Assert.Equal("Asia Pacific", bnn.Namespace.Name);
            Assert.Equal(100, bnn.Fraction);
            Assert.Equal("98971109711097", bnn.IdentificationCode);

            var ppc = book.CommodityTable.FindCommodity("Crypto", "PPC");
            Assert.NotNull(ppc);
            Assert.Equal("Puppycoin", ppc.FullName);
            Assert.Equal("Crypto", ppc.Namespace.Name);
            Assert.Equal(1000, ppc.Fraction);

            var ktc = book.CommodityTable.FindCommodity("Crypto", "KTC");
            Assert.NotNull(ktc);
            Assert.Equal("Kittycoin", ktc.FullName);
            Assert.Equal("Crypto", ktc.Namespace.Name);
            Assert.Equal(10000, ktc.Fraction);

            Assert.Equal(new[] { "KTC", "PPC" }, ktc.Namespace.Commodities.SelectOrdered(x => x.Mnemonic));
            Assert.Equal(new[] { "KTC", "PPC" }, book.CommodityTable.FindNamespace("Crypto").Commodities.SelectOrdered(x => x.Mnemonic));

            Assert.All(
                new[] { lnx, bnn, ppc, ktc }.Select(c => c.Namespace.Name),
                n => Assert.True(book.CommodityTable.HasNamespace(n)));

            book.CommodityTable.DeleteNamespace("US");
            Assert.Null(book.CommodityTable.SingleOrDefault(c => c.Mnemonic == "LNX"));

            book.CommodityTable.RemoveCommodity(bnn);
            Assert.Null(book.CommodityTable.SingleOrDefault(c => c.Mnemonic == "BNN"));

            ppc.FullName = "Puppercoin";
            Assert.Equal("Puppercoin", book.CommodityTable.FindCommodity("Crypto", "PPC").FullName);
        }
    }

    [Theory]
    [SetupTestingBookWithInlineData(ISOCurrencyCodes.XTS, true, "Code for testing purposes", BookName = "simple")]
    [SetupTestingBookWithInlineData("LOL", false, null, BookName = "simple")]
    public void Can_Find_Currencies(string currencyCode, bool exists, string currencyFullName, TestingBook testingBook)
    {
        using var book = Book.OpenRead(testingBook);
        var currency = book.CommodityTable.ISOCurrencies[currencyCode];

        if (exists)
        {
            Assert.NotNull(currency);
            Assert.Equal(currencyFullName, currency.FullName);
        }
        else
        {
            Assert.Null(currency);
        }
    }
}
