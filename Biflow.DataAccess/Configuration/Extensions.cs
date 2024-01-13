namespace Biflow.DataAccess.Configuration;

internal static class Extensions
{
    internal static void ApplyConfigurations(this ModelBuilder modelBuilder, AppDbContext dbContext)
    {
        // Use reflection to apply all IEntityTypeConfigurations defined in the assembly.

        var entityTypeConfigurationType = typeof(IEntityTypeConfiguration<>);

        // Get the generic method ModelBuilder.ApplyConfiguration<>() for applying configurations.
        var applyEntityConfigurationMethod = typeof(ModelBuilder)
            .GetMethods()
            .Single(e =>
                e is { Name: nameof(ModelBuilder.ApplyConfiguration), ContainsGenericParameters: true } &&
                e.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == entityTypeConfigurationType);

        // Iterate types that are concrete classes.
        foreach (var type in typeof(EntityTypeConfigurationEntryPoint).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            // Try and find the IEntityTypeConfigurations interface.
            var @interface = type
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == entityTypeConfigurationType);
            if (@interface is null)
            {
                continue;
            }
            // Create a typed method for applying the configuration.
            var target = applyEntityConfigurationMethod.MakeGenericMethod(@interface.GenericTypeArguments[0]);
            object? entityTypeConfiguration;
            if (type.GetConstructor(Type.EmptyTypes) is not null)
            {
                // Configuration has empty constructor
                entityTypeConfiguration = Activator.CreateInstance(type);
            }
            else if (type.GetConstructor([typeof(AppDbContext)]) is not null)
            {
                // Configuration takes AppDbContext as parameter
                entityTypeConfiguration = Activator.CreateInstance(type, dbContext);
            }
            else
            {
                throw new ApplicationException($"Unsupported constructor in type implementing IEntityTypeConfiguration: {type.Name}");
            }
            target.Invoke(modelBuilder, [entityTypeConfiguration]);
        }
    }
}
