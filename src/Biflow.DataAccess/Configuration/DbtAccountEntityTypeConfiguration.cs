namespace Biflow.DataAccess.Configuration;

internal class DbtAccountEntityTypeConfiguration : IEntityTypeConfiguration<DbtAccount>
{
    public void Configure(EntityTypeBuilder<DbtAccount> builder)
    {
        builder.ToTable("DbtAccount")
            .HasKey(x => x.DbtAccountId);

        builder.HasMany(c => c.Steps)
            .WithOne(s => s.DbtAccount);
    }
}
