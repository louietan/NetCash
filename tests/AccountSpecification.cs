namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

/// <summary>
/// Setup and verification of a account tree.
/// </summary>
public record AccountSpecification(
    string Name,
    Bindings.GNCAccountType AccountType,
    string Currency,
    bool IsPlaceholder,
    params AccountSpecification[] Children
)
{
    public AccountSpecification(params AccountSpecification[] Children)
        : this("Root Account", Bindings.GNCAccountType.ACCT_TYPE_ROOT, null, false, Children)
    {
    }

    public void Apply(Book book, Account root = null)
    {
        var thisAccount = root == null
            ? book.RootAccount
            : root
                .NewChildAccount(this.Name)
                .SetType(this.AccountType)
                .SetCurrency(book.CommodityTable.ISOCurrencies[this.Currency])
                .SetPlaceholder(this.IsPlaceholder);

        foreach (var child in this.Children.OrEmpty())
        {
            child.Apply(book, thisAccount);
        }
    }

    public void Verify(Book book, Account thisAccount = null)
    {
        thisAccount = thisAccount ?? book.RootAccount;

        Assert.Equal(this.Name, thisAccount.Name);
        Assert.Equal(this.AccountType, thisAccount.Type);
        Assert.Equal(this.Currency, thisAccount.Currency?.Mnemonic);
        Assert.Equal(this.IsPlaceholder, thisAccount.IsPlaceholder);

        Assert.Equal(this.Children.OrEmpty().Count(), thisAccount.Children.Count());

        foreach (
            var (spec, account) in
                Enumerable.Zip(
                    this.Children.OrEmpty().OrderBy(x => x.Name),
                    thisAccount.Children.OrderBy(x => x.Name)
                )
        )
        {
            spec.Verify(book, account);
        }
    }
}
