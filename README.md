# NetCash

NetCash is a .NET wrapper for the [native C API](https://wiki.gnucash.org/wiki/Using_the_API) of GnuCash.

**Status**

- It is usable right now, but is a WIP and is far from stable. There might be bugs and the interface is going to change.
- It was developed against GnuCash 4.12 and works with 5.1 at the time of writing.
- It has been tested on Linux and Windows. MacOS probably works too.
- XML and SQLite backends have been tested, MySQL and PostgreSQL should "just work" (probably).

## Getting started

DISCLAIMER: this library is a free software and provies NO WARRANTY, we highly recommend you make backups for your financial data before
making any changes using this library. We hold NO RESPONSIBILITY for anything unexpected that might occur to your data.

### Installation

**Requirements**

- GnuCash >= 4.12
- .NET 6

This library currently is not available on Nuget, to use it just clone
this repo and include it as a Project Reference in your app.

### Example

*Tip: if you've cloned this repo, you can run this example locally with `dotnet run --project example`
and open the generated book with GnuCash to see if it actually works.*

```csharp
using NetCash;
using NetCash.Helpers;

// Sets gnucash installation path if it's a non-standard one.
// For example:
// LibraryLoader.GnuCashInstallationPath = "/opt/gnucash-unstable";
// LibraryLoader.GnuCashInstallationPath = @"D:\Program Files (x86)\gnucash";

// Initializes the engine.
GnuCashEngine.Initialize();

var uri = new GnuCashUri(scheme: GnuCashUri.UriSchemeXml,
                         path: Path.Join(Directory.GetCurrentDirectory(), "netcash-demo.gnucash"));

// Creates an new empty book.
using var book = Book.Create(uri);

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
```

#### Update preferences

```csharp
// Set accounting period
Preferences.AccountingPeriod.StartDate = new Preferences.DateChoice.Specific(DateOnly.Parse("2022/11/23"));
Preferences.AccountingPeriod.EndDate = Preferences.DateChoice.EndOfThisYear;
```

#### More examples

Check out the [tests](./tests) and [example](./example) folder.

### Use in web applications

If you are going to build a webui for GnuCash using this library, you
might run into problems. It's because this library wasn't designed
with concurrency in mind. Additionally, GnuCash wasn't and won't be
(re)designed for server-side use. (see https://wiki.gnucash.org/wiki/WishList#Use_through_web_browser).

That being said, I don't think it's totally impossible. To me, the
fundamental difference between web apps (server-side) and desktop apps
is that the former is stateless, while the latter is stateful. For
this libary to work in web apps would require fine control on
concurrency to prevent the shared state in a single process from being
badly jumbled by concurrent HTTP requests.

### API reference

See [docs](./docs/netcash.md)

### Development

- Install [.NET](https://get.dot.net) if you haven't.
- `dotnet tool restore` to restore local dotnet tools.
- `dotnet build -t:Format src` to format the library, use `dotnet format` to format other projects.
- `dotnet run --project docs` to generate documentation files.
