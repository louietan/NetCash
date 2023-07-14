namespace NetCash.Tests;

using System;
using Xunit;

public class UriFixture
{
    [Theory]
    [InlineData("/tmp/test.gnucash", "file:///tmp/test.gnucash")]
    [InlineData(@"C:\Users\user\Documents\test.gnucash", @"file:///C:\Users\user\Documents\test.gnucash")]
    [InlineData("file:///tmp/test.gnucash", true)]
    [InlineData("xml:///tmp/test.gnucash", true)]
    [InlineData("sqlite3:///tmp/test.gnucash", true)]
    [InlineData("mysql://root@127.0.0.1:5432/gnucash", true)]
    [InlineData("postgres://postgres@127.0.0.1:3306/gnucash", true)]
    [InlineData("mysql://127.0.0.1:5432", false)] // no path
    [InlineData("postgres://postgres@127.0.0.1:3306", false)] // no path
    [InlineData("xxx://test.gnucash", false)] // invalid file scheme
    public void Can_Parse_From_Strings(string input, object expectancy)
    {
        var parse = () => GnuCashUri.Parse(input);

        switch (expectancy)
        {
            case false:
                Assert.Throws<UriFormatException>(parse);
                break;
            case true:
                Assert.Equal(input, parse().ToString());
                break;
            case string e:
                Assert.Equal(e, parse().ToString());
                break;
        }
    }
}
