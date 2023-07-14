namespace NetCash

open System

type GnuCashBackendException(code: Bindings.QofBackendError, message: string) =
    inherit Exception(message)
    member _.Code = code

exception AccountNotFoundException
