using NetCash;
using NetCash.Helpers;

// Initializes the engine.
GnuCashEngine.Initialize();

void Demo()
{
    var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                             path: Path.Join(Directory.GetCurrentDirectory(), "netcash-demo.gnucash"));

    // Creates an new empty book.
    using var book = Book.Create(uri, true);

    // Creates chart of accounts.
    var checking = book
        .NewAccount("Assets", Bindings.GNCAccountType.ACCT_TYPE_ASSET)
        .Invoke(assets => assets.IsPlaceholder = true)
        .NewChildAccount("Checking");

    // With short-hand extension method
    var salary = book.GetOrMakeIncomeAccount("Income", "Salary");
    var prepaidRent = book.GetOrMakeAssetAccount("Assets", "Prepaid Expenses", "Rent");

    var expenses = book.GetOrMakeExpenseAccount("Expenses");
    var rent = expenses.NewChildAccount("Rent");
    var food = expenses.NewChildAccount("Food");
    var utilities = expenses.NewChildAccount("Utilities");
    var water = utilities.NewChildAccount("Water");
    var electric = utilities.NewChildAccount("Electric");

    /*
       Now we should have the following account tree:

       |- Assets
       |  |- Checking
       |  `- Prepaid Expenses
       |     `- Rent
       |- Income
       |  `- Salary
       `- Expenses
          |- Food
          |- Rent
          `- Utilities
             |- Water
             `- Electric
     */

    // Records paycheck.
    // A typical 2-split transaction.
    salary.TransferTo(checking, 5000);

    var monthyRent = 1000;

    // Records expenses (rent, food and utilities).
    // A multi-split transaction.
    book
        .NewTransaction()
        .AddSplit(account: food, value: 900)
        .AddSplit(account: rent, value: monthyRent)
        // Prepay rent for the next 2 months
        .AddSplit(account: prepaidRent, value: monthyRent * 2)
        .AddSplit(account: water, value: 10)
        .AddSplit(account: electric, value: 50)
        // Create a balancing split in Assets:Checking
        // we could just call `.AddSplit(account: checking, value: manualValue)` instead
        .AddBalancingSplit(checking)
        // Don't forget to save.
        .Save();

    // Creates future transactions for rent expenses for the following 2 months.
    var nextMonth = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
    book.CreateAccrualTransactions(prepaidRent, rent, 2, nextMonth);

    // Saves changes. (optional, books will save before close.)
    book.Save();
}

#region More examples

void Adjust_Account_Tree()
{
    var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                             path: Path.Join(Directory.GetCurrentDirectory(), "netcash-adjust-tree.gnucash"));

    using var book = Book.Create(uri, true);

    var investments = book.GetOrMakeAssetAccount("Assets", "Investments");
    investments.NewCommodityAccount(CommodityType.Fund, "Fund - QWER", "Funds", "qwer");
    investments.NewCommodityAccount(CommodityType.Fund, "Fund - ASDF", "Funds", "asdf");
    investments.NewCommodityAccount(CommodityType.Stock, "Stock - ZXCV", "Stocks", "zxcv");
    investments.NewCommodityAccount(CommodityType.Stock, "Stock - UIOP", "Stocks", "uiop");
    investments.NewCommodityAccount(CommodityType.Stock, "Crypto - HJKL", "Crypto", "hjkl");

    /*
        Now the tree will be:

        `- Assets
           `- Investments
              |- Fund - QWER
              |- Fund - ASDF
              |- Stock - ZXCV
              |- Stock - UIOP
              `- Crypto - HJKL

        We want to change it into:

        `- Assets
           `- Investments
              |- Crypto <--------------(placeholder)
              |  `- HJKL
              |- Fund   <--------------(placeholder)
              |  |- QWER
              |  `- ASDF
              `- Stock  <--------------(placeholder)
                 |- ZXCV
                 `- UIOP
     */

    var pattern = new System.Text.RegularExpressions.Regex(@"(?<category>[^-]+)\s*-\s*(?<name>[^-]+)");
    foreach (var account in book.Accounts)
    {
        var match = pattern.Match(account.Name);
        if (match.Success)
        {
            var category = book.GetOrMakeAssetAccount("Assets", "Investments", match.Groups["category"].Value.Trim());
            category.IsPlaceholder = true;
            account.Parent = category;
            account.Name = match.Groups["name"].Value.Trim();
        }
    }

    // That's it.
}

void Move_Transactions_By_Description()
{
    var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                             path: Path.Join(Directory.GetCurrentDirectory(), "netcash-move-transactions.gnucash"));

    using var book = Book.Create(uri, true);
    var creditCard = book.GetOrMakeLiabilityAccount("Credit Card");
    var expenses = book.GetOrMakeExpenseAccount("Expenses");
    var auto = book.GetOrMakeLiabilityAccount("Expenses", "Auto");

    creditCard.TransferTo(expenses, 200, description: "#auto Gas");
    creditCard.TransferTo(expenses, 1500, description: "#auto Repair");
    creditCard.TransferTo(expenses, 3000, description: "#auto ECU tuning");
    creditCard.TransferTo(expenses, 8000, description: "#auto Change wheels");

    // Now move those transactions with #auto in description to more specific category Expenses:Auto 
    // and remove #auto
    foreach (var split in new SplitQuery(book).MatchDescription("#auto").MatchAccounts(expenses).Run())
    {
        split.Account = auto;
        var trans = split.Transaction.BeginEdit();
        trans.SetDescription(split.Transaction.Description.Replace("#auto ", "")).Save();
    }

    // DONE! That's it.
}

void Hide_Zero_Balance_Stocks()
{
    var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                             path: Path.Join(Directory.GetCurrentDirectory(), "netcash-hide-zero-balance-stocks.gnucash"));

    using var book = Book.Create(uri, true);

    var checking = book.GetOrMakeAssetAccount("Assets", "Checking");
    var loan = book.GetOrMakeLiabilityAccount("Loan");
    var foo = book.GetOrMakeAssetAccount("Assets", "Mutual Funds").NewCommodityAccount(CommodityType.Fund, "Foo fund", "Funds", "foo");
    var bar = book.GetOrMakeAssetAccount("Assets", "Stocks").NewCommodityAccount(CommodityType.Stock, "Bar Inc.", "Stocks", "bar");

    // buy
    book.NewTransaction()
        .AddSplit(account: foo, value: 18000, amount: 1000)
        .AddSplit(account: bar, value: 13600, amount: 20000)
        .AddBalancingSplit(loan)
        .RecordCommodityPrice(true)
        .Save();

    // sell
    book.NewTransaction()
        .AddSplit(account: bar, value: -16200, amount: -20000)
        .AddBalancingSplit(checking)
        .RecordCommodityPrice(true)
        .Save();

    // Now hide accounts that we have sold out.
    foreach (
        var account
            in book.Accounts.Where(acc => acc.Type == Bindings.GNCAccountType.ACCT_TYPE_MUTUAL
                || acc.Type == Bindings.GNCAccountType.ACCT_TYPE_STOCK)
    )
    {
        account.Hidden = account.Balance.IsZero;
    }

    // That's it.
}

void Scrub_Realized_Gains()
{
    var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                             path: Path.Join(Directory.GetCurrentDirectory(), "netcash-scrub-realized-gains.gnucash"));

    using var book = Book.Create(uri, true);

    var checking = book.GetOrMakeAssetAccount("Assets", "Checking");
    var loan = book.GetOrMakeLiabilityAccount("Loan");
    var foo = book.GetOrMakeAssetAccount("Assets", "Stocks").NewCommodityAccount(CommodityType.Stock, "Foo Corp.", "Stocks", "foo");
    var bar = book.GetOrMakeAssetAccount("Assets", "Stocks").NewCommodityAccount(CommodityType.Stock, "Bar Inc.", "Stocks", "bar");

    // buy
    book.NewTransaction()
        .AddSplit(account: foo, value: 18000, amount: 1000)
        .AddSplit(account: bar, value: 13600, amount: 20000)
        .AddBalancingSplit(loan)
        .RecordCommodityPrice(true)
        .Save();

    // sell
    book.NewTransaction()
        .AddSplit(account: foo, value: -20000, amount: -1000)
        .AddSplit(account: bar, value: -16200, amount: -20000)
        .AddBalancingSplit(checking)
        .RecordCommodityPrice(true)
        .Save();

    // Scrub all accounts under Assets:Stocks
    Scrubber.ScrubLots(book.FindAccountByName("Stocks"), true);

    // Move splits from auto-generated "Orphaned Gains - CURRENCY" to "Income:Realized Gains"
    var realizedGains = book.GetOrMakeIncomeAccount("Income", "Realized Gains");
    var orphanGains = book.Accounts.First(a => a.Name.StartsWith("Orphaned Gains"));
    orphanGains.MoveSplits(realizedGains);
    // Delete the "Orphaned Gains - CURRENCY" account
    book.DeleteAccount(orphanGains); // FIXME: ERROR <qof.engine> [qof_commit_edit()] unbalanced call - resetting (was -1)

    // That's it.
}

#endregion

// Choose which example you'd like to run.
Demo();
Adjust_Account_Tree();
Move_Transactions_By_Description();
Hide_Zero_Balance_Stocks();
Scrub_Realized_Gains();
