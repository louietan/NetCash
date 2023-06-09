# LibraryLoader class

This module is responsible for the loading of native libraries.

```csharp
public static class LibraryLoader
```

## Public Members

| name | description |
| --- | --- |
| static [GnuCashInstallationPath](LibraryLoader/GnuCashInstallationPath.md) { get; set; } | The installation path of GnuCash, e.g. "/opt/gnucash-unstable". NetCash will try determine this value by looking for PATH environment variable and conventional locations such as "/opt/gnucash" on *nix or "Program Files (x86)\gnucash" on Windows. If your installation path is none of the above, you have to specify this value before NetCash can work properly. |
| static [Activate](LibraryLoader/Activate.md)() | Activates the library loader for gnucash native libraries. |
| static class [Executables](LibraryLoader.Executables.md) |  |

## See Also

* namespace [NetCash](../netcash.md)

<!-- DO NOT EDIT: generated by xmldocmd for netcash.dll -->
