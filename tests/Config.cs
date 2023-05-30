namespace NetCash.Tests;

using System.Collections.Generic;

public static class Config
{
    /// <summary>
    /// This controls which store backends to test.
    /// Missing backends (like Postgre, MySQL) doesn't mean NetCash not supporting them, it's just our testing infrastructure,
    /// supporting these backends requires additional setup and tweak in testing code.
    /// </summary>
    public static readonly IEnumerable<string> SupportedBackends = new[] { "xml", "sqlite3" };
}
