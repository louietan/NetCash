namespace NetCash.Tests;

using System;
using System.IO;

public record TestingBook(GnuCashUri Uri)
{
    public GnuCashUri BaseBookUri { get; set; }

    public static implicit operator GnuCashUri(TestingBook testBook) => testBook.Uri;

    public static readonly string RootPath = Path.Join(Directory.GetCurrentDirectory(), "Books");

    public static GnuCashUri MakeUri(string scheme, string bookName)
    {
        if (Bindings.gnc_uri_is_file_scheme(scheme))
        {
            var path = Path.Join(RootPath, $"{bookName}.{scheme}.gnucash");
            return new GnuCashUri(scheme: scheme, path: path);
        }
        else
        {
            var databaseName = "netcash~" + bookName;
            var HASH_LENGTH = 10;

            // Truncate database name if exceeds maximum length.
            if (databaseName.Length > Config.MAX_DATABASE_NAME_LENGTH)
            {
                databaseName =
                    databaseName.Substring(0, Config.MAX_DATABASE_NAME_LENGTH - HASH_LENGTH - 1)
                    + "+" + databaseName.SHA1Sum().Substring(0, HASH_LENGTH);
            }

            // Comment from gnc-backend-dbi.cpp:
            // > Postgres's SQL interface coerces identifiers to lower case, but the
            // > C interface is case-sensitive. This results in a mixed-case dbname
            // > being created (with a lower case name) but then dbi can't connect to
            // > it. To work around this, coerce the name to lowercase first. 
            if (scheme == GnuCashUri.UriSchemePostgreSQL)
                databaseName = databaseName.ToLower();

            var databaseConfig = DbHelper.GetConfig(scheme);

            return new GnuCashUri(scheme: scheme,
                                  host: databaseConfig.Host,
                                  port: databaseConfig.Port,
                                  userName: databaseConfig.UserName,
                                  password: databaseConfig.Password,
                                  path: databaseName);
        }
    }
}
