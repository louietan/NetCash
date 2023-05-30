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

public class AccountFixture
{
    [Theory]
    [SetupTestingBook]
    public void Can_Create_A_Fresh_Chart_Of_Accounts(TestingBook testingBook)
    {
        var rootSpec =
            new AccountSpecification(
                new AccountSpecification(
                    "Assets",
                    Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                    ISOCurrencyCodes.XTS,
                    true,
                    new AccountSpecification(
                        "Current Assets",
                        Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                        ISOCurrencyCodes.XTS,
                        true,
                        new AccountSpecification(
                            "Cash",
                            Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                            ISOCurrencyCodes.XTS,
                            true
                        ),
                        new AccountSpecification(
                            "Checking Account",
                            Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                            ISOCurrencyCodes.XTS,
                            true
                        )
                    ),
                    new AccountSpecification(
                        "Fixed Assets",
                        Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                        ISOCurrencyCodes.XTS,
                        true,
                        new AccountSpecification(
                            "Auto",
                            Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                            ISOCurrencyCodes.XTS,
                            false
                        ),
                        new AccountSpecification(
                            "House",
                            Bindings.GNCAccountType.ACCT_TYPE_ASSET,
                            ISOCurrencyCodes.XTS,
                            false
                        )
                    )
                ),
                new AccountSpecification(
                    "Liabilities",
                    Bindings.GNCAccountType.ACCT_TYPE_LIABILITY,
                    ISOCurrencyCodes.XTS,
                    true,
                    new AccountSpecification(
                        "Loans",
                        Bindings.GNCAccountType.ACCT_TYPE_LIABILITY,
                        ISOCurrencyCodes.XTS,
                        true,
                        new AccountSpecification(
                            "Auto Loan",
                            Bindings.GNCAccountType.ACCT_TYPE_LIABILITY,
                            ISOCurrencyCodes.XTS,
                            false
                        ),
                        new AccountSpecification(
                            "Mortgage Loan",
                            Bindings.GNCAccountType.ACCT_TYPE_LIABILITY,
                            ISOCurrencyCodes.XTS,
                            false
                        )
                    ),
                    new AccountSpecification(
                        "Credit Card",
                        Bindings.GNCAccountType.ACCT_TYPE_CREDIT,
                        ISOCurrencyCodes.XTS,
                        false
                    )
                )
            );

        using (var book = Book.Create(testingBook))
        {
            rootSpec.Apply(book);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            rootSpec.Verify(book);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Can_Move_Splits(TestingBook testingBook)
    {
        GncNumeric checkingBalance;
        GncNumeric cashBalance;

        using (var book = Book.Open(testingBook))
        {
            var checking = book.FindAccountByName("Checking Account");
            checkingBalance = checking.Balance;

            var cash = book.FindAccountByName("Cash in Wallet");
            cashBalance = cash.Balance;

            checking.MoveSplits(cash);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var checking = book.FindAccountByName("Checking Account");
            var cash = book.FindAccountByName("Cash in Wallet");

            Assert.Equal(GncNumeric.Zero, checking.Balance);
            Assert.Equal(checkingBalance, cash.Balance);
        }
    }
}
