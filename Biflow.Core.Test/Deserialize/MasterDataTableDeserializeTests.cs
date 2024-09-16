using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class MasterDataTableDeserializeTests
{
    private static readonly MasterDataTable table = GetDeserializedTable();

    [Fact]
    public void Lookups_NotEmpty()
    {
        Assert.NotEmpty(table.Lookups);
    }

    [Fact]
    public void HiddenColumns_NotEmpty()
    {
        Assert.NotEmpty(table.HiddenColumns);
    }

    [Fact]
    public void LockedColumns_NotEmpty()
    {
        Assert.NotEmpty(table.LockedColumns);
    }

    [Fact]
    public void ColumnOrder_NotEmpty()
    {
        Assert.NotEmpty(table.ColumnOrder);
    }

    private static MasterDataTable GetDeserializedTable()
    {
        var json = JsonSerializer.Serialize(CreateTable(), EnvironmentSnapshot.JsonSerializerOptions);
        var table = JsonSerializer.Deserialize<MasterDataTable>(json, EnvironmentSnapshot.JsonSerializerOptions);
        return table;
    }

    private static MasterDataTable CreateTable()
    {
        var table = new MasterDataTable();
        table.Lookups.Add(new MasterDataTableLookup());
        table.HiddenColumns.Add("Test");
        table.LockedColumns.Add("Test");
        table.ColumnOrder.Add("Test");
        return table;
    }
}
