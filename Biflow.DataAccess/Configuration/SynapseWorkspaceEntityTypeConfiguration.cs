using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class SynapseWorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<SynapseWorkspace>
{
    public void Configure(EntityTypeBuilder<SynapseWorkspace> builder)
    {
        builder.Property(x => x.SynapseWorkspaceUrl).IsUnicode(false);
    }
}
