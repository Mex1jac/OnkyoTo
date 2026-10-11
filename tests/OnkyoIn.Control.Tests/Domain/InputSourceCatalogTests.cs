using OnkyoIn.Control.Domain.Entities;

namespace OnkyoIn.Control.Tests.Domain;

public class InputSourceCatalogTests
{
    [Fact]
    public void Catalog_ContainsAllTxNr6100Sources()
    {
        Assert.Equal(13, InputSourceCatalog.Sources.Count);
    }

    [Theory]
    [InlineData("bd-dvd", "10")]
    [InlineData("cbl-sat", "01")]
    [InlineData("game", "02")]
    [InlineData("aux", "03")]
    [InlineData("pc", "05")]
    [InlineData("strm-box", "11")]
    [InlineData("tv", "12")]
    [InlineData("phono", "22")]
    [InlineData("cd", "23")]
    [InlineData("am", "25")]
    [InlineData("fm", "24")]
    [InlineData("network", "2B")]
    [InlineData("bluetooth", "2E")]
    public void FindById_ReturnsSourceWithExpectedSliCode(string id, string sliCode)
    {
        var source = InputSourceCatalog.FindById(id);

        Assert.NotNull(source);
        Assert.Equal(sliCode, source!.SliCode);
    }

    [Fact]
    public void FindById_IsCaseInsensitive()
    {
        Assert.NotNull(InputSourceCatalog.FindById("BD-DVD"));
    }

    [Fact]
    public void FindById_UnknownId_ReturnsNull()
    {
        Assert.Null(InputSourceCatalog.FindById("hdmi-arc"));
    }

    [Fact]
    public void FindBySliCode_ReturnsSource()
    {
        var source = InputSourceCatalog.FindBySliCode("2B");

        Assert.NotNull(source);
        Assert.Equal("network", source!.Id);
    }

    [Fact]
    public void FindBySliCode_UnknownCode_ReturnsNull()
    {
        Assert.Null(InputSourceCatalog.FindBySliCode("FF"));
    }

    [Fact]
    public void IdsAndSliCodes_AreUnique()
    {
        Assert.Equal(
            InputSourceCatalog.Sources.Count,
            InputSourceCatalog.Sources.Select(source => source.Id).Distinct().Count());

        Assert.Equal(
            InputSourceCatalog.Sources.Count,
            InputSourceCatalog.Sources.Select(source => source.SliCode).Distinct().Count());
    }
}
