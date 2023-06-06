# netcash assembly

## NetCash namespace

| public type | description |
| --- | --- |
| class [Account](./NetCash/Account.md) | An account. |
| abstract class [AccountFinder](./NetCash/AccountFinder.md) |  |
| class [AccountNotFoundException](./NetCash/AccountNotFoundException.md) |  |
| abstract class [BaseQuery&lt;a,b&gt;](./NetCash/BaseQuery-2.md) |  |
| static class [Bindings](./NetCash/Bindings.md) | Partial bindings for native libraries (libgnucash and glibc). |
| class [Book](./NetCash/Book.md) | A book is the container for data stored in a gnucash database. In the design of gnucash, Session and Book are two separate abstractions. Session represents the connection to the backend storage, while Book represents the container for domain objects, like Accounts, Transactions, Splits etc. For simplicity, NetCash combined the two concepts into one and just called it Book. Additionally, Book also serves as the "Factory" to create some accounting objects, like Accounts and Transactions. |
| class [Commodity](./NetCash/Commodity.md) | Commodity |
| class [CommodityNamespace](./NetCash/CommodityNamespace.md) |  |
| class [CommodityTable](./NetCash/CommodityTable.md) | For manipulating commodity-related data like commodities, currencies, commodity namespaces. |
| class [CommodityType](./NetCash/CommodityType.md) | Type of the commodity. Technically the subset of GNCAccountType. |
| struct [GncNumeric](./NetCash/GncNumeric.md) | Wrapper for native gnc_numeric. |
| class [GncNumericException](./NetCash/GncNumericException.md) |  |
| class [GNCPolicy](./NetCash/GNCPolicy.md) | Accounting Policy. The Accounting Policy determines how splits are assigned to lots. |
| class [GnuCashBackendException](./NetCash/GnuCashBackendException.md) |  |
| static class [GnuCashEngine](./NetCash/GnuCashEngine.md) |  |
| static class [GnuCashObject](./NetCash/GnuCashObject.md) |  |
| class [GnuCashUri](./NetCash/GnuCashUri.md) |  |
| interface [IGnuCashEntity](./NetCash/IGnuCashEntity.md) |  |
| interface [INativeWrapper](./NetCash/INativeWrapper.md) |  |
| class [ISOCurrencies](./NetCash/ISOCurrencies.md) |  |
| static class [ISOCurrencyCodes](./NetCash/ISOCurrencyCodes.md) |  |
| static class [LibraryLoader](./NetCash/LibraryLoader.md) | This module is responsible for the loading of native libraries. |
| static class [Logging](./NetCash/Logging.md) | Toy logging module that SHOULD ONLY be used for debugging purposes. |
| abstract class [LogOutputType](./NetCash/LogOutputType.md) | Log output types for gnucash. |
| class [Lot](./NetCash/Lot.md) | Lot |
| static class [Marshalling](./NetCash/Marshalling.md) |  |
| static class [Preferences](./NetCash/Preferences.md) |  |
| class [Price](./NetCash/Price.md) |  |
| class [PriceDB](./NetCash/PriceDB.md) |  |
| class [QuoteSource](./NetCash/QuoteSource.md) |  |
| enum [ReconciliationFlags](./NetCash/ReconciliationFlags.md) |  |
| static class [Scrubber](./NetCash/Scrubber.md) | Data scrubbing. |
| class [Split](./NetCash/Split.md) | Split |
| class [SplitQuery](./NetCash/SplitQuery.md) |  |
| class [Transaction](./NetCash/Transaction.md) | A read-only transaction. To make changes, call BeginEdit to get a TransactionEditor. |
| class [TransactionEditor](./NetCash/TransactionEditor.md) | The transaction editor. |
| class [VersionInfo](./NetCash/VersionInfo.md) | Version information for gnucash installation. |

## NetCash.Helpers namespace

| public type | description |
| --- | --- |
| class [Extensions](./NetCash.Helpers/Extensions.md) |  |
| static class [UI](./NetCash.Helpers/UI.md) | Helper for UI-related stuff. |

<!-- DO NOT EDIT: generated by xmldocmd for netcash.dll -->