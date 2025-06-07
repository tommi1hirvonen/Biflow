namespace Biflow.DataAccess.Configuration;

internal class EmailStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<EmailStepExecution>
{
    public void Configure(EntityTypeBuilder<EmailStepExecution> builder)
    {
        builder.Property(x => x.Recipients).HasColumnName("EmailRecipients");
        builder.Property(x => x.Subject).HasColumnName("EmailSubject");
        builder.Property(x => x.Body).HasColumnName("EmailBody");
    }
}
