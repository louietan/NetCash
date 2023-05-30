namespace NetCash.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

public class GncNumericFixture
{
    [Fact]
    public void Zero()
    {
        Assert.Equal("0/1", GncNumeric.Zero.ToString());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(2, 4)]
    public void Should_Work_For_Explicit_Numerator_And_Denominator(long num, long denom)
    {
        var n = new GncNumeric(num, denom);

        Assert.Equal($"{num}/{denom}", n.ToString());
        Assert.Equal(num, n.value.num);
        Assert.Equal(denom, n.value.denom);
    }

    [Fact]
    public void Should_Work_For_Integers()
    {
        var n = (GncNumeric)3;
        Assert.Equal("3/1", n.ToString());
        Assert.Equal(3, n.value.num);
        Assert.Equal(1, n.value.denom);
    }

    [Fact]
    public void Can_Convert_From_Doubles()
    {
        var num = GncNumeric.Approximate(3.1, 20, Bindings.GncNumericFlags.GNC_HOW_RND_NEVER);
        Assert.Equal("62/20", num.ToString());
        Assert.Equal(62, num.value.num);
        Assert.Equal(20, num.value.denom);

        num = GncNumeric.Approximate(1 / 3.0, 10000000000, Bindings.GncNumericFlags.GNC_HOW_RND_FLOOR);
        Assert.Equal("3333333333/10000000000", num.ToString());
        Assert.Equal(3333333333, num.value.num);
        Assert.Equal(10000000000, num.value.denom);

        num = GncNumeric.Approximate(1 / 3.0, 10000000000, Bindings.GncNumericFlags.GNC_HOW_RND_CEIL);
        Assert.Equal("3333333334/10000000000", num.ToString());
        Assert.Equal(3333333334, num.value.num);
        Assert.Equal(10000000000, num.value.denom);

        num = GncNumeric.Approximate(3.1);
        Assert.Equal("31/10", num.ToString());
        Assert.Equal(31, num.value.num);
        Assert.Equal(10, num.value.denom);
    }

    [Fact]
    public void Can_Convert_From_Strings()
    {
        var num = GncNumeric.Parse("3.1");
        Assert.Equal("31/10", num.ToString());
        Assert.Equal(31, num.value.num);
        Assert.Equal(10, num.value.denom);

        num = GncNumeric.Parse("1/3");
        Assert.Equal("1/3", num.ToString());
        Assert.Equal(1, num.value.num);
        Assert.Equal(3, num.value.denom);

        Assert.Throws<FormatException>(() => GncNumeric.Parse(""));

        Assert.False(GncNumeric.TryParse("", out var _));

        Assert.True(GncNumeric.TryParse("1/3", out var oneThird));
        Assert.Equal("1/3", oneThird.ToString());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.1)]
    [InlineData(-1.1)]
    [InlineData(1 / 3.0)]
    public void Can_Convert_To_Doubles(double value)
    {
        Assert.Equal(value, (double)GncNumeric.Approximate(value));
    }

    [Fact]
    public void Can_Reduce_And_Convert()
    {
        Assert.Equal("1/2", GncNumeric.Parse("2/4").Reduce().ToString());
        Assert.Equal("50/100", GncNumeric.Parse("2/4").Convert(100, Bindings.GncNumericFlags.GNC_HOW_RND_NEVER).ToString());
        Assert.Equal("34/100", GncNumeric.Parse("1/3").Convert(100, Bindings.GncNumericFlags.GNC_HOW_RND_CEIL).ToString());
        Assert.Equal("33/100", GncNumeric.Parse("1/3").Convert(100, Bindings.GncNumericFlags.GNC_HOW_RND_FLOOR).ToString());
    }
}
