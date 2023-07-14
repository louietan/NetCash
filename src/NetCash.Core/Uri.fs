namespace NetCash

open System
open System.IO

open NetCash.Marshalling

[<Struct>]
type private Components =
    val mutable Scheme: nativeint
    val mutable Hostname: nativeint
    val mutable Port: Bindings.gint32
    val mutable Username: nativeint
    val mutable Password: nativeint
    val mutable Path: nativeint

    interface IDisposable with
        member self.Dispose() =
            [ self.Scheme; self.Hostname; self.Username; self.Password; self.Path ]
            |> Seq.iter Bindings.g_free

    member self.``scheme://hostname/path`` =
        (String.maybeBorrowed self.Scheme, String.maybeBorrowed self.Hostname, String.maybeBorrowed self.Path)

// Why not using System.Uri and System.UriBuilder from BCL?
// - System.UriBuilder replaces \ with / unconditionally when setting Path, which is problematic on Windows.
// - System.Uri requires registration of custom URI parsers for unknown schemes, which is cumbersome.

type GnuCashUri =
    { Scheme: string
      Host: string
      Port: Bindings.gint32
      Username: string
      Password: string
      Path: string }

    override self.ToString() =
        let { Scheme = scheme
              Host = host
              Port = port
              Username = username
              Password = password
              Path = path } =
            self

        let str = Bindings.gnc_uri_create_uri (scheme, host, port, username, password, path)

        if str = IntPtr.Zero then
            failwith "Failed to create uri"

        String.fromOwned str

    member self.IsFile = Bindings.gnc_uri_is_file_scheme self.Scheme

    static member SchemeFile = "file"
    static member SchemeXml = "xml"
    static member SchemeSqlite = "sqlite3"
    static member SchemeMySQL = "mysql"
    static member SchemePostgreSQL = "postgres"

    // Parses a string to GnuCashUri.
    static member Parse(uri) =
        if isNull uri then
            nullArg (nameof uri)

        use mutable com = new Components()

        Bindings.gnc_uri_get_components (
            uri,
            &com.Scheme,
            &com.Hostname,
            &com.Port,
            &com.Username,
            &com.Password,
            &com.Path
        )

        let valid =
            match com.``scheme://hostname/path`` with
            | Some s, None, Some _ -> Bindings.gnc_uri_is_file_scheme s
            | None, None, Some _
            | Some _, Some _, Some _ -> true
            | _ -> false

        if valid then
            { Scheme = String.fromBorrowed com.Scheme
              Path = String.fromBorrowed com.Path
              Host = String.fromBorrowed com.Hostname
              Port = com.Port
              Username = String.fromBorrowed com.Username
              Password = String.fromBorrowed com.Password }
        else
            raise (UriFormatException <| sprintf "%s is not a valid gnucash uri" uri)

    static member op_Implicit(uri) = GnuCashUri.Parse uri
