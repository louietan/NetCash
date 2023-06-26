namespace NetCash.Tests;

using System.Collections.Generic;

public record DatabaseConfig(string Host, int Port, string UserName, string Password);

public static class Config
{
    /// <summary>
    /// This controls which store backends to test.
    /// </summary>
    public static readonly IEnumerable<string> SupportedBackends = new string[] {
        GnuCashUri.UriSchemeXml,
        GnuCashUri.UriSchemeSqlite,
        // GnuCashUri.UriSchemeMySQL,
        // GnuCashUri.UriSchemePostgreSQL,
    };

    public const int MAX_DATABASE_NAME_LENGTH = 63;  // MySQL is 64, PostgreSQL is 63.

    public static readonly DatabaseConfig MySQL = new ("127.0.0.1", 3306, "root", "");

    public static readonly DatabaseConfig PostgreSQL = new ("127.0.0.1", 5432, "postgres", "postgres");
}
