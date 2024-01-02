using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class DataObjectEntityTypeConfiguration : IEntityTypeConfiguration<DataObject>
{
    public void Configure(EntityTypeBuilder<DataObject> builder)
    {
        builder.HasIndex(p => p.ObjectUri, "UQ_DataObject")
            .IsUnique();
    }
}
