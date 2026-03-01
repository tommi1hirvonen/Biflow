namespace Biflow.DataAccess.Configuration;

public class VmStepEntityTypeConfiguration : IEntityTypeConfiguration<VmStep>
{
    public void Configure(EntityTypeBuilder<VmStep> builder)
    {
        builder.Property(x => x.AzureCredentialId).HasColumnName("AzureCredentialId");
        builder.Property(x => x.VirtualMachineResourceId).HasColumnName("VmResourceId").IsUnicode(false);
        builder.Property(x => x.Operation).HasColumnName("VmOperation");
    }
}
