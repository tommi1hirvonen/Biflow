namespace Biflow.DataAccess.Configuration;

internal class StepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionParameterBase>
{
    public void Configure(EntityTypeBuilder<StepExecutionParameterBase> builder)
    {
        builder.ToTable("ExecutionStepParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.Property(x => x.ParameterValue)
            .HasColumnType("sql_variant");

        builder.Property(x => x.ExecutionParameterValue)
            .HasColumnType("sql_variant");

        builder.HasOne(p => p.InheritFromExecutionParameter)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey(p => new { p.ExecutionId, p.InheritFromExecutionParameterId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<SqlStepExecutionParameter>(ParameterType.Sql)
            .HasValue<PackageStepExecutionParameter>(ParameterType.Package)
            .HasValue<JobStepExecutionParameter>(ParameterType.Job)
            .HasValue<ExeStepExecutionParameter>(ParameterType.Exe)
            .HasValue<FunctionStepExecutionParameter>(ParameterType.Function)
            .HasValue<PipelineStepExecutionParameter>(ParameterType.Pipeline)
            .HasValue<EmailStepExecutionParameter>(ParameterType.Email);

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
    }
}
