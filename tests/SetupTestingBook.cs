namespace NetCash.Tests;

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

public record TestingBook(GnuCashUri Uri)
{
    public GnuCashUri BaseBookUri { get; set; }

    public static implicit operator GnuCashUri(TestingBook testBook) => testBook.Uri;
}

/// <summary>
/// Provides URIs for pre-made books or automaticaly generate unique URIs for each testcase.
/// </summary>
public class SetupTestingBookAttribute : DataAttribute
{
    static readonly string RootPath = Path.Join(Directory.GetCurrentDirectory(), "Books");

    public string BookName { get; init; }

    public bool Copy { get; init; }

    GnuCashUri MakeUri(string scheme, string bookName)
    {
        if (Bindings.gnc_uri_is_file_scheme(scheme))
        {
            var path = Path.Join(RootPath, $"{bookName}.{scheme}.gnucash");
            return new GnuCashUri(scheme: scheme, path: path);
        }
        else
        {
            return new GnuCashUri(String.Join(Uri.SchemeDelimiter, scheme, bookName));
        }
    }

    protected virtual IEnumerable<object[]> GetAdditionalData(MethodInfo testMethod) => Enumerable.Empty<object[]>();

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var autoBookName = $"{testMethod.DeclaringType.Name}~{testMethod.Name}~{Guid.NewGuid().ToString("n").Substring(0, 8)}";
        var bookName = this.Copy ? autoBookName : this.BookName ?? autoBookName;

        object[] makeDataRow(string scheme)
        {
            var testingBook = new TestingBook(MakeUri(scheme, bookName));

            if (this.Copy)
            {
                testingBook.BaseBookUri = this.MakeUri(scheme, this.BookName);

                // Due to this side-effect, we have to turn off "preEnumerateTheories" in xunit.runner.json to prevent repeated generation of test data.
                using var _ = Book.OpenRead(testingBook.BaseBookUri).SaveAs(testingBook.Uri);
            }

            return new[] { testingBook };
        }

        var dataRowsForBackends = Config.SupportedBackends.Select(makeDataRow);
        var additionalData = this.GetAdditionalData(testMethod);

        if (!additionalData.Any())
            return dataRowsForBackends;
        else
            return additionalData.SelectMany(additionalRow => dataRowsForBackends.Select(row => additionalRow.Concat(row).ToArray()));
    }
}

public class SetupTestingBookWithInlineDataAttribute : SetupTestingBookAttribute
{
    InlineDataAttribute innerAttribute;

    public SetupTestingBookWithInlineDataAttribute(params object[] data) => this.innerAttribute = new InlineDataAttribute(data);

    protected override IEnumerable<object[]> GetAdditionalData(MethodInfo testMethod) => this.innerAttribute.GetData(testMethod);
}

public class SetupTestingBookWithClassDataAttribute : SetupTestingBookAttribute
{
    ClassDataAttribute innerAttribute;

    public SetupTestingBookWithClassDataAttribute(Type @class) => this.innerAttribute = new ClassDataAttribute(@class);

    protected override IEnumerable<object[]> GetAdditionalData(MethodInfo testMethod) => this.innerAttribute.GetData(testMethod);
}

public class SetupTestingBookWithMemberDataAttribute : SetupTestingBookAttribute
{
    MemberDataAttribute innerAttribute;

    public SetupTestingBookWithMemberDataAttribute(string memberName, params object[] parameters)
        => this.innerAttribute = new MemberDataAttribute(memberName, parameters);

    protected override IEnumerable<object[]> GetAdditionalData(MethodInfo testMethod) => this.innerAttribute.GetData(testMethod);
}
