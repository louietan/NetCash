namespace NetCash.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public static class NetCashExtensions
    {
        public static Commodity TestingCurrency { get => Book.Current.CommodityTable.ISOCurrencies.XTS; }

        public static IEnumerable<Account> FindImbalanceAccounts(this Book self) =>
            // GnuCash automatically creates an account of type
            // ACCT_TYPE_BANK with name "Imbalance - CURRENCY" at the top level to put the unbalanced amount.
            self.RootAccount.Children.Where(acct => acct.Type == Bindings.GNCAccountType.ACCT_TYPE_BANK);

        public static IEnumerable<Account> FindAccountsWithFreeSplits(this Book self) =>
            self.Accounts
                .Where(acct => acct.Type == Bindings.GNCAccountType.ACCT_TYPE_STOCK || acct.Type == Bindings.GNCAccountType.ACCT_TYPE_MUTUAL)
                .Where(acct => Scrubber.GetFreeSplits(acct).Any())
                .ToArray();

        public static TransactionEditor WithTestingCurrency(this TransactionEditor self) => self.SetCurrency(TestingCurrency);
    }

    public static class Enumerables
    {
        public static IEnumerable<T> Of<T>(params T[] items) => items.AsEnumerable();

        /// <summary>
        /// Poor man's Option ;-)
        /// </summary>
        public static IEnumerable<T> Maybe<T>(T value) where T : class => Of(value).Where(value => value != null);

        public static IEnumerable<U> SelectOrdered<T, U>(this IEnumerable<T> self, Func<T, U> selector) => self.Select(selector).OrderBy(x => x);

        // https://stackoverflow.com/a/39997157
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> self) => self.Select((item, index) => (item, index));

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self) action(item);
            return self;
        }

        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> self) => self ?? Enumerable.Empty<T>();
    }
}

namespace Xunit
{
    using System.Linq;
    using NetCash;
    using NetCash.Tests;

    public partial class Assert
    {
        internal static void BackendErrorCode(Bindings.QofBackendError expected, GnuCashBackendException ex)
        {
            Assert.Equal(expected, ex.Code);
        }

        internal static void AccountsShouldBalance(Book book)
        {
            var imbalanceAccounts = book.FindImbalanceAccounts();
            Assert.False(imbalanceAccounts.Any(), "There are imbalance accounts: " + string.Join(", ", imbalanceAccounts.Select(x => x.Name)));
        }
    }
}
