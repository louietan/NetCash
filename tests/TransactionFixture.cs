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

public class TransactionFixture
{
    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Can_Transfer(TestingBook testingBook)
    {
        var incomeBalance = 0d;
        var checkingBalance = 0d;

        using (var book = Book.Open(testingBook))
        {
            var income = book.FindAccountByName("Income");
            incomeBalance = income.Balance;
            var checking = book.FindAccountByName("Checking Account");
            checkingBalance = checking.Balance;

            var amount = 3000;
            income.TransferTo(checking, amount);
            incomeBalance -= amount;
            checkingBalance += amount;
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var income = book.FindAccountByName("Income");
            var checking = book.FindAccountByName("Checking Account");
            Assert.Equal(incomeBalance, income.Balance);
            Assert.Equal(checkingBalance, checking.Balance);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Can_Reverse_Transactions(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            var expense = book.FindAccountByName("Expenses");
            Assert.NotEqual(GncNumeric.Zero, expense.Balance);
            foreach (var trans in expense.Transactions)
                trans.CreateReversingTransaction();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var expense = book.FindAccountByName("Expenses");
            Assert.Equal(GncNumeric.Zero, expense.Balance);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Can_Create_Transactions_With_Multiple_Splits(TestingBook testingBook)
    {
        var openingBalance = 0d;
        var checkingBalance = 0d;
        var cashBalance = 0d;

        using (var book = Book.Open(testingBook))
        {
            var opening = book.FindAccountByName("Opening Balances");
            openingBalance = opening.Balance;

            var checking = book.FindAccountByName("Checking Account");
            checkingBalance = checking.Balance;

            var cash = book.FindAccountByName("Cash in Wallet");
            cashBalance = cash.Balance;

            var editor = book
                .NewTransaction(currency: NetCashExtensions.TestingCurrency)
                .SetDescription("multi-split transaction test");

            var totalAmount = -10000;
            var amountToChecking = 7000;
            var amountToCash = 3000;

            openingBalance += totalAmount;
            checkingBalance += amountToChecking;
            cashBalance += amountToCash;

            editor.AddSplit(account: opening,
                            value: (GncNumeric)totalAmount,
                            memo: "split1",
                            reconcile: ReconciliationFlags.CLEARED)

                  .AddSplit(account: checking,
                            value: (GncNumeric)amountToChecking,
                            memo: "split2",
                            reconcile: ReconciliationFlags.RECONCILED)

                  .AddSplit(account: cash,
                            value: (GncNumeric)amountToCash,
                            memo: "split3",
                            reconcile: ReconciliationFlags.NEW)

                  .Save();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var opening = book.FindAccountByName("Opening Balances");
            var checking = book.FindAccountByName("Checking Account");
            var cash = book.FindAccountByName("Cash in Wallet");

            Assert.Equal(openingBalance, opening.Balance);
            Assert.Equal(checkingBalance, checking.Balance);
            Assert.Equal(cashBalance, cash.Balance);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Can_Transfer_Between_Different_Currencies(TestingBook testingBook)
    {
        var cashBalance = 0d;
        var cashUSDBalance = 0d;

        using (var book = Book.Open(testingBook))
        {
            var opening = book.FindAccountByName("Opening Balances");

            var cash = book.FindAccountByName("Cash in Wallet");
            var value = 500;
            opening.TransferTo(cash, value);
            cashBalance = cash.Balance;

            var cashUSD = book.FindAccountByName("Cash in Wallet (USD)");
            cashUSDBalance = cashUSD.Balance;

            var toAmount = 15.71;
            var fromAmount = 100.0;
            cash.TransferTo(cashUSD, GncNumeric.Approximate(fromAmount), GncNumeric.Approximate(toAmount));
            cashBalance -= fromAmount;
            cashUSDBalance += toAmount;
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var cash = book.FindAccountByName("Cash in Wallet");
            var cashUSD = book.FindAccountByName("Cash in Wallet (USD)");

            Assert.Equal(cashBalance, cash.Balance);
            Assert.Equal(cashUSDBalance, cashUSD.Balance);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "investment", Copy = true)]
    public void Can_Create_Transactions_With_Realized_Gains(TestingBook testingBook)
    {
        var startingBankBalance = 0d;
        var buyPrice = 10;
        var sellPrice = 13;
        var shares = 100;
        var profit = (sellPrice - buyPrice) * shares;

        // setup required accounts
        using (var book = Book.Open(testingBook))
        {
            book.CommodityTable.AddCommodity("Linux, Inc.", "US", "LNX");

            book
                .FindAccountByFullName("Assets", "Investments", "Brokerage Account", "Stock")
                .NewCommodityAccount(CommodityType.Stock, "GNU, Inc.", "US", "GNU");

            book
                .FindAccountByName("Realized Gains")
                .NewChildAccount("GNU");
        }

        // buy
        using (var book = Book.Open(testingBook))
        {
            var bank = book.FindAccountByName("Bank ABC");
            var gnu = book.FindAccountByName("GNU, Inc.");

            startingBankBalance = bank.Balance;

            var value = (GncNumeric)shares * buyPrice;

            book.NewTransaction(new DateOnly(2020, 1, 1), NetCashExtensions.TestingCurrency)

                .AddSplit(account: bank,
                          value: -value)

                .AddSplit(account: gnu,
                          value: value,
                          amount: (GncNumeric)shares)

                .Save();
        }

        // sell
        using (var book = Book.Open(testingBook))
        {
            var bank = book.FindAccountByName("Bank ABC");
            var gnu = book.FindAccountByName("GNU, Inc.");

            var value = (GncNumeric)shares * sellPrice;

            book.NewTransaction(new DateOnly(2021, 1, 1), NetCashExtensions.TestingCurrency)

                .AddSplit(account: bank,
                          value: value)

                .AddSplit(account: gnu,
                          value: -value,
                          amount: -(GncNumeric)shares)

                .Save();
        }

        // record realized gains
        using (var book = Book.Open(testingBook))
        {
            var gains = book.FindAccountByFullName("Income", "Realized Gains", "GNU");
            var gnu = book.FindAccountByName("GNU, Inc.");

            book.NewTransaction(new DateOnly(2021, 1, 1), NetCashExtensions.TestingCurrency)

                .AddSplit(account: gains,
                          value: -profit)

                .AddSplit(account: gnu,
                          value: profit,
                          amount: GncNumeric.Zero)

                .Save();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var bank = book.FindAccountByName("Bank ABC");
            var gnu = book.FindAccountByName("GNU, Inc.");
            var gains = book.FindAccountByFullName("Income", "Realized Gains", "GNU");
            Assert.Equal(0d, gnu.Balance);
            Assert.Equal(startingBankBalance + profit, bank.Balance);
            Assert.Equal(profit, -gains.Balance);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Should_Create_Imbalance_Accounts(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            var equity = book.FindAccountByName("Opening Balances");
            var cash = book.FindAccountByName("Cash in Wallet");

            book.NewTransaction(currency: NetCashExtensions.TestingCurrency)

                .AddSplit(account: equity,
                          value: -100)

                .AddSplit(account: cash,
                          value: 90)

                .Save();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var imbalanceAccounts = book.FindImbalanceAccounts();
            Assert.NotEmpty(imbalanceAccounts);
            Assert.Equal(imbalanceAccounts.Sum(x => x.Balance), 10);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Should_Create_Balancing_Split(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            var equity = book.FindAccountByName("Opening Balances");
            var cash = book.FindAccountByName("Cash in Wallet");
            var checking = book.FindAccountByName("Checking Account");

            book.NewTransaction(currency: NetCashExtensions.TestingCurrency)

                .AddSplit(account: cash,
                          value: 66)

                .AddSplit(account: checking,
                          value: 8888)

                .AddBalancingSplit(account: equity)

                .Save();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);
        }
    }
}
