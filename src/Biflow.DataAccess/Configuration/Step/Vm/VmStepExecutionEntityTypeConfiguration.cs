namespace Biflow.DataAccess.Configuration;

public class VmStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<VmStepExecution>
{
    public void Configure(EntityTypeBuilder<VmStepExecution> builder)
    {
        builder.Property(x => x.AzureCredentialId).HasColumnName("AzureCredentialId");
        builder.Property(x => x.VirtualMachineResourceId).HasColumnName("VmResourceId").IsUnicode(false);
        builder.Property(x => x.Operation).HasColumnName("VmOperation");
    }
}
