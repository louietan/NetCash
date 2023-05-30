namespace rec NetCash

open System
open System.Runtime.InteropServices

open NetCash.Marshalling

// Why not using System.Uri and System.UriBuilder from BCL?
// - System.UriBuilder replaces \ with / unconditionally when setting Path, which is problematic on Windows.
// - System.Uri requires registration of custom URI parsers for unknown schemes, which is cumbersome.

type GnuCashUri
    (
        [<Optional>] scheme: string,
        [<Optional>] host: string,
        [<Optional>] port: Bindings.gint32,
        [<Optional>] userName: string,
        [<Optional>] password: string,
        [<Optional>] path: string
    ) =
    let asString =
        let str = Bindings.gnc_uri_create_uri (scheme, host, port, userName, password, path)

        if str = IntPtr.Zero then
            failwith "Failed to create uri"

        String.fromOwned str

    new(uriString) =
        let mutable scheme = Unchecked.defaultof<_>
        let mutable hostname = Unchecked.defaultof<_>
        let mutable port = Unchecked.defaultof<_>
        let mutable username = Unchecked.defaultof<_>
        let mutable password = Unchecked.defaultof<_>
        let mutable path = Unchecked.defaultof<_>

        Bindings.gnc_uri_get_components (uriString, &scheme, &hostname, &port, &username, &password, &path)

        GnuCashUri(
            String.fromOwned scheme,
            String.fromOwned hostname,
            port,
            String.fromOwned username,
            String.fromOwned password,
            String.fromOwned path
        )

    member _.Scheme = scheme
    member _.Host = host
    member _.Port = port
    member _.UserName = userName
    member _.Password = password
    member _.Path = path

    member self.IsFile = Bindings.gnc_uri_is_file_scheme self.Scheme

    override _.ToString() = asString

    override _.Equals obj =
        match obj with
        | :? GnuCashUri as other -> string other = asString
        | _ -> false

    override _.GetHashCode() = HashCode.Combine(asString)

    static member UriSchemeFile = "file"
    static member UriSchemeXml = "xml"
    static member UriSchemeSqlite = "sqlite3"
    static member UriSchemeMySQL = "mysql"
    static member UriSchemePostgreSQL = "postgres"
