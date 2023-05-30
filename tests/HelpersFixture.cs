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

using NetCash.Helpers;

public class HelpersFixture
{
    [Theory]
    [SetupTestingBookWithInlineData(Bindings.GNCAccountType.ACCT_TYPE_ASSET)]
    public void Can_Automatically_Create_Missing_Parent_Accounts(Bindings.GNCAccountType accountType, TestingBook testingBook)
    {
        using (var book = Book.Create(testingBook))
        {
            Assert.NotNull(book.GetOrMakeAccount(accountType, "A"));
            Assert.Equal(accountType, book.FindAccountByFullName("A").Type);

            Assert.NotNull(book.GetOrMakeAccount(accountType, "A", "AA", "AAA"));
            Assert.Equal(accountType, book.FindAccountByFullName("A", "AA", "AAA").Type);

            Assert.NotNull(book.GetOrMakeAccount(accountType, "AAA", "AAAA", "AAAAA"));
            Assert.Equal(accountType, book.FindAccountByFullName("A", "AA", "AAA", "AAAA", "AAAAA").Type);
        }
    }

    [Theory]
    [SetupTestingBookWithInlineData(10, 3, 0)]
    [SetupTestingBookWithInlineData(11, 3, 0)]
    [SetupTestingBookWithInlineData(1000000, 12 * 33, 13000)]
    public void Can_Create_Accrual_Transactions(double prepaid, int terms, int balance, TestingBook testingBook)
    {
        using (var book = Book.Create(testingBook))
        {
            var prepaidExpense = book.GetOrMakeAssetAccount("Assets", "Prepaid Expense");
            var creditcard = book.GetOrMakeLiabilityAccount("Liability", "Credit Card");
            var expenses = book.GetOrMakeExpenseAccount("Expenses");

            creditcard.TransferTo(prepaidExpense, (GncNumeric)prepaid);
            book.CreateAccrualTransactions(prepaidExpense, expenses, terms, targetBalance: (GncNumeric)balance);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            var prepaidExpense = book.FindAccountByName("Prepaid Expense");
            var expenses = book.FindAccountByName("Expenses");
            Assert.Equal(balance, prepaidExpense.Balance);
            Assert.Equal(GncNumeric.Approximate(prepaid - balance), expenses.Balance);
        }
    }
}
