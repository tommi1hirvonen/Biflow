namespace Biflow.DataAccess.Configuration;

internal class FunctionStepEntityTypeConfiguration : IEntityTypeConfiguration<FunctionStep>
{
    public void Configure(EntityTypeBuilder<FunctionStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.FunctionUrl).IsUnicode(false);
    }
}
