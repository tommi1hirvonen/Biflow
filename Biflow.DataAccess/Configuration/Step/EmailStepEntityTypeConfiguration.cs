namespace Biflow.DataAccess.Configuration;

internal class EmailStepEntityTypeConfiguration : IEntityTypeConfiguration<EmailStep>
{
    public void Configure(EntityTypeBuilder<EmailStep> builder)
    {
        builder.Property(x => x.Recipients).HasColumnName("EmailRecipients");
        builder.Property(x => x.Subject).HasColumnName("EmailSubject");
        builder.Property(x => x.Body).HasColumnName("EmailBody");
    }
}
