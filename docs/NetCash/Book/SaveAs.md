# Book.SaveAs method

Saves current book to another URI.

```csharp
public Book SaveAs(GnuCashUri uri)
```

## Return Value

A new book or current book when uri is the same.

## Exceptions

| exception | condition |
| --- | --- |
| GnuCashBackendException | Throws when an error occured while saving the book. |

## Remarks

WARNING: current book closes after being saved to a different uri, so never use the original book after this operation.

## See Also

* class [GnuCashUri](../GnuCashUri.md)
* class [Book](../Book.md)
* namespace [NetCash](../../netcash.md)

<!-- DO NOT EDIT: generated by xmldocmd for netcash.dll -->