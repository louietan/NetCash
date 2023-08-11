# NetCash

NetCash is a set of tools for GnuCash powered by the [native GnuCash C API](https://wiki.gnucash.org/wiki/Using_the_API) and was built with .NET.

The project consists of

1. Reusable core library (for .NET)
2. Command-line interface
3. Mobile friendly Web UI (_coming sooon_)
4. JSON-RPC server (_coming sooon_)

**Status**

- Source in the `main` branch is usable, but is a WIP and is far from
  stable. There might be bugs and the interface is going to change.

- It was initially developed against GnuCash 4.12 and works with 5.3
  at the time of writing.

- The CLI and web UI are not as featureful and fancy as slimiar tools
  like ledger/hledger/beancount/fava. They were made for my personal
  needs and for the demonstration of building apps on top of the core.
  
  If you would like more features, consider contributing.

## Disclaimer

This library is a free software and provides NO WARRANTY, even though
there hasn't been any known faults that cause data corruption, it's
never a bad idea to back up your financial data before making
changes. We hold NO RESPONSIBILITY for anything unexpected that
might occur to your data.

## Requirements

- GnuCash >= 4.12

This project is not available as binaries right now, to use it just clone
this repo and compile on your own.

When released, the cli and (web)server will be distributed as single file
native executables, `netcash-cli` and `netcash-server`
respectively, without the installation of the .NET runtime.

## The core library

The core library is a .NET wrapper / partial binding for the C API.

It aims to provide an easy-to-use interface that hides the nuts and
bolts of the underlying mechanics.

By using the core library, you are able to build your own accouting
automation tools that serve your personal needs.

### Example

_Tip: if you've cloned this repo, you can run this example locally with `dotnet run --project examples`
and open the generated book with GnuCash to see if it actually works._

```csharp
using NetCash;
using NetCash.Helpers;

// Initializes the engine.
GnuCashEngine.Initialize();

// Use the following overload if gnucash-cli is not available from PATH.
GnuCashEngine.Initialize("/opt/gnucash-unstable/bin/gnucash-cli");
GnuCashEngine.Initialize(@"X:\Program Files (x86)\gnucash\bin\gnucash-cli.exe");

var path = Path.Join(Directory.GetCurrentDirectory(), "netcash-demo.gnucash");

// Creates an new empty book.
using var book = Book.Create(path, overwrite: true);

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

Check out the [tests](./tests) and [examples](./examples) folder.

### API reference

See [docs](./docs/netcash.md)

## Command-line interface (CLI)

The sole command is `bal` (or `balance`), which is similar to the `bal` command in ledger-like tools.

e.g. `netcash-cli bal --tree netcash-demo.gnucash` outputs:

             $1,040.00  Assets
             $1,040.00    Checking
                 $0.00    Prepaid Expenses
                 $0.00      Rent
             $3,960.00  Expenses
               $900.00    Food
             $3,000.00    Rent
                $60.00    Utilities
                $50.00      Electric
                $10.00      Water
            -$5,000.00  Income
            -$5,000.00    Salary

## Web UI & JSON-RPC (_coming soon_)

The web UI is a self-hosted companion app to update your book when you are away from your desktop computer.

The RPC server provides a language-agnostic way to program your
accounting tools via [JSON-RPC](https://www.jsonrpc.org/specification).

- Start the web and RPC server: `netcash-server --listen :58426 book.gnucash`
- Start the RPC server only: `netcash-server --listen :58426 --rpc book.gnucash`

**Deployment**

Tip: to deploy the server on a home computer and make it accessbile from
the internet you can use SSH port forwarding.

It's also recommended to use a reverse proxy such as Apache, Nginx or
Caddy for HTTPS and other security measurements.

## Development

- Install [.NET SDK](https://get.dot.net). (Use x86 variant for Windows)
- `dotnet tool restore` to restore local dotnet tools.
- `dotnet run --project docs` to generate documentation files.
