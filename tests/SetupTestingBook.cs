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

/// <summary>
/// Provides URIs for pre-made books or automaticaly generate unique URIs for each testcase.
/// </summary>
public class SetupTestingBookAttribute : DataAttribute
{
    /// <summary>
    /// Copy from a premade book. Mutually-exclusive with UsePremade.
    /// </summary>
    public string CopyPremade { get; init; }

    /// <summary>
    /// Use a premade book (as readonly). Mutually-exclusive with UsePremade.
    /// </summary>
    public string UsePremade { get; init; }

    protected virtual IEnumerable<object[]> GetAdditionalData(MethodInfo testMethod) => Enumerable.Empty<object[]>();

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        // Appended with a GUID because a single test method could be attributed with multiple InlineData or MemberData.
        var autoBookName = $"{testMethod.DeclaringType.Name}~{testMethod.Name}~{Guid.NewGuid().ToString("n").Substring(0, 8)}"; 
        var bookName = UsePremade ?? autoBookName;

        object[] makeDataRow(string scheme)
        {
            var testingBook = new TestingBook(TestingBook.MakeUri(scheme, bookName));

            if (UsePremade == null)
            {
                DbHelper.EnsureDatabase(scheme, testingBook.Uri.Path);
            }

            if (this.CopyPremade != null)
            {
                testingBook.BaseBookUri = TestingBook.MakeUri(scheme, this.CopyPremade);

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
