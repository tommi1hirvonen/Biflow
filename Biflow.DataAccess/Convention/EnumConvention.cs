using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Biflow.DataAccess.Convention;

internal class EnumConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    var type = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                    var converter = Activator.CreateInstance(type, new ConverterMappingHints()) as ValueConverter;
                    property.Builder.HasConversion(converter);
                    property.Builder.HasMaxLength(50);
                    property.Builder.IsUnicode(false);
                }
                else if (Nullable.GetUnderlyingType(property.ClrType)?.IsEnum == true)
                {
                    var type = typeof(EnumToStringConverter<>).MakeGenericType(Nullable.GetUnderlyingType(property.ClrType)!);
                    var converter = Activator.CreateInstance(type, new ConverterMappingHints()) as ValueConverter;
                    property.Builder.HasConversion(converter);
                    property.Builder.HasMaxLength(50);
                    property.Builder.IsUnicode(false);
                }
            }
        }
    }
}