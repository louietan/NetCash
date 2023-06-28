namespace NetCash.Tests;

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

public class BookFixture
{
    [Theory]
    [SetupTestingBook(UsePremade = "simple")]
    public void Can_Read_A_Simple_Book(TestingBook testingBook)
    {
        using var book = Book.OpenRead(testingBook);
        var rootAccount = book.RootAccount;
        var allAccounts = rootAccount.Descendants;

        Assert.Equal(9, allAccounts.Count());

        var openingBalanceAcct = book.FindAccountByName("Opening Balances");
        var checkingAcct = book.FindAccountByName("Checking Account");
        var incomeAcct = book.FindAccountByName("Income");
        var expenseAcct = book.FindAccountByName("Expenses");

        Assert.Equal(-100.0, openingBalanceAcct.Balance);
        Assert.Equal(580.0, checkingAcct.Balance);
        Assert.Equal(-500.0, incomeAcct.Balance);
        Assert.Equal(20.0, expenseAcct.Balance);
    }

    [Theory]
    [SetupTestingBook]
    public void Can_Create_A_New_Book(TestingBook testingBook)
    {
        using (Book.Create(testingBook)) { }

        if (testingBook.Uri.IsFile)
            Assert.True(File.Exists(testingBook.Uri.Path));

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.NotNull(book.RootAccount);
            Assert.Empty(book.RootAccount.Descendants);
        }
    }

    [Theory]
    [SetupTestingBookWithInlineData("Assets", true, UsePremade = "simple")]
    [SetupTestingBookWithInlineData("Spaceship", false, UsePremade = "simple")]
    public void Can_Find_Accounts(string accountName, bool exists, TestingBook testingBook)
    {
        using var book = Book.OpenRead(testingBook);
        if (exists)
        {
            Assert.Equal(accountName, book.FindAccountByName(accountName).Name);
            Assert.True(book.TryFindAccountByName(accountName, out Account account));
            Assert.Equal(accountName, account.Name);
        }
        else
        {
            Assert.Throws<AccountNotFoundException>(() => book.FindAccountByName(accountName));
            Assert.False(book.TryFindAccountByName(accountName, out Account _));
        }
    }

    [Theory]
    [SetupTestingBook]
    public void Can_Overwrite_An_Existing_Book(TestingBook testingBook)
    {
        var oldRootAccountId = default(Guid);

        using (var book = Book.Create(testingBook))
        {
            oldRootAccountId = GnuCashObject.objectId(book.RootAccount);
            Assert.NotNull(oldRootAccountId);
        }

        if (testingBook.Uri.IsFile)
            Assert.True(File.Exists(testingBook.Uri.Path));

        using (Book.Create(testingBook, true)) { }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.NotEqual(oldRootAccountId, GnuCashObject.objectId(book.RootAccount));
        }
    }

    [Theory]
    [SetupTestingBook(CopyPremade = "simple")]
    public void Can_Delete_Accounts(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            var currentAssets = book.FindAccountByName("Current Assets");
            Assert.Throws<InvalidOperationException>(() => book.DeleteAccount(currentAssets));

            var cashInWallet = book.FindAccountByName("Cash in Wallet");
            book.DeleteAccount(cashInWallet);
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.Throws<AccountNotFoundException>(() => book.FindAccountByName("Cash in Wallet"));
        }
    }

    [Theory]
    [SetupTestingBook(CopyPremade = "simple")]
    public void Should_Return_The_Original_Book_If_Save_As_The_SameUri(TestingBook testingBook)
    {
        using var book = Book.Open(testingBook);
        Assert.Same(book, book.SaveAs(testingBook));
    }

    [Theory]
    [SetupTestingBook(UsePremade = "no-such-book")]
    public void Should_Throw_If_Book_Does_Not_Exist(TestingBook testingBook)
    {
        var exception = Assert.Throws<GnuCashBackendException>(() => Book.OpenRead(testingBook));
        Assert.BackendErrorCode(
            Bindings.gnc_uri_is_file_scheme(testingBook.Uri.Scheme)
                ? Bindings.QofBackendError.ERR_FILEIO_FILE_NOT_FOUND
                : Bindings.QofBackendError.ERR_BACKEND_NO_SUCH_DB,
            exception);
    }

    [Theory]
    [SetupTestingBookWithInlineData(3, UsePremade = "simple")]
    public void Can_Get_Currently_Opened_Book(int times, TestingBook testingBook)
    {
        Book openBook() => Book.OpenRead(testingBook);

        for (var i = 0; i < times; ++i)
        {
            Assert.Null(Book.Current);
            Assert.False(Bindings.gnc_current_session_exist());

            using var book = openBook();

            Assert.Equal(book, Book.Current);
            Assert.Equal(Bindings.gnc_get_current_session(), book.SessionHandle);
            Assert.Throws<InvalidOperationException>(openBook);
        }
    }
}
