# Book.DeleteAccount method

Deletes an account.

```csharp
public void DeleteAccount(Account acct)
```

## Remarks

* The underlying native resource gets destroyed after this operation, although the managed Account object is still accessible, it becomes invalid, any usage afterwards would get unexpected result.
* The account has to be empty before being deleted, otherwise an exception is thrown.

## See Also

* class [Account](../Account.md)
* class [Book](../Book.md)
* namespace [NetCash](../../netcash.md)

<!-- DO NOT EDIT: generated by xmldocmd for netcash.dll -->