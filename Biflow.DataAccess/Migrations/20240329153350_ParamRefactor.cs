using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ParamRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Add placeholder columns
                alter table app.ExecutionStep                       add ResultCaptureJobParameterValue2 nvarchar(max)
                alter table app.JobParameter                        add ParameterValue2                 nvarchar(max)
                alter table app.ExecutionStepConditionParameter     add ParameterValue2                 nvarchar(max)
                alter table app.ExecutionStepConditionParameter     add ExecutionParameterValue2        nvarchar(max)
                alter table app.ExecutionStepParameter              add ParameterValue2                 nvarchar(max)
                alter table app.ExecutionStepParameter              add ExecutionParameterValue2        nvarchar(max)
                alter table app.StepConditionParameter              add ParameterValue2                 nvarchar(max)
                alter table app.StepParameter                       add ParameterValue2                 nvarchar(max)
                alter table app.ExecutionParameter                  add DefaultValue2                   nvarchar(max)
                alter table app.ExecutionParameter                  add ParameterValue2                 nvarchar(max)

                go

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.StepParameter as a

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionParameter as a

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.StepConditionParameter as a

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.JobParameter as a

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionStepParameter as a

                update a
                set ParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        a.ParameterValueType,
                        '"',
                        case
                            when a.ParameterValue is null then ''
                            else concat(
                                ',"Value', a.ParameterValueType, '":',
                                case
                                    when a.ParameterValue is null then 'null'
                                    when a.ParameterValueType = 'Boolean' then case a.ParameterValue when 1 then 'true' else 'false' end
                                    when a.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), a.ParameterValue)
                                    when a.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, a.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), a.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionStepConditionParameter as a

                update a 
                set ResultCaptureJobParameterValue2 =
                    concat(
                        '{"ValueType":"',
                        d.ParameterValueType,
                        '"',
                        case
                            when b.ParameterValue is null then ''
                            else concat(
                                ',"Value', d.ParameterValueType, '":',
                                case
                                    when b.ParameterValue is null then 'null'
                                    when d.ParameterValueType = 'Boolean' then case b.ParameterValue when 1 then 'true' else 'false' end
                                    when d.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), b.ParameterValue)
                                    when d.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, b.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), b.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionStep as a
                    cross apply (select a.ResultCaptureJobParameterValue as ParameterValue) as b
                    cross apply (select sql_variant_property(a.ResultCaptureJobParameterValue, 'BaseType') as sql_variant_type) as c
                    inner join (values
                        ('Boolean',     'bit'),
                        ('DateTime',    'datetime'),
                        ('Decimal',     'decimal'),
                        ('Decimal',     'numeric'),
                        ('Double',      'float'),
                        ('Int16',       'smallint'),
                        ('Int32',       'int'),
                        ('Int64',       'bigint'),
                        ('Single',      'real'),
                        ('String',      'varchar'),
                        ('String',      'nvarchar')
                    ) as d(ParameterValueType, sql_variant_type) on c.sql_variant_type = d.sql_variant_type
                where ResultCaptureJobParameterValue is not null

                update a 
                set ExecutionParameterValue2 =
                    -- select
                    concat(
                        '{"ValueType":"',
                        d.ParameterValueType,
                        '"',
                        case
                            when b.ParameterValue is null then ''
                            else concat(
                                ',"Value', d.ParameterValueType, '":',
                                case
                                    when b.ParameterValue is null then 'null'
                                    when d.ParameterValueType = 'Boolean' then case b.ParameterValue when 1 then 'true' else 'false' end
                                    when d.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), b.ParameterValue)
                                    when d.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, b.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), b.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    ) 
                from app.ExecutionStepConditionParameter as a
                    cross apply (select a.ExecutionParameterValue as ParameterValue) as b
                    cross apply (select isnull(sql_variant_property(a.ExecutionParameterValue, 'BaseType'), 'nvarchar') as sql_variant_type) as c
                    inner join (values
                        ('Boolean',     'bit'),
                        ('DateTime',    'datetime'),
                        ('Decimal',     'decimal'),
                        ('Decimal',     'numeric'),
                        ('Double',      'float'),
                        ('Int16',       'smallint'),
                        ('Int32',       'int'),
                        ('Int64',       'bigint'),
                        ('Single',      'real'),
                        ('String',      'varchar'),
                        ('String',      'nvarchar')
                    ) as d(ParameterValueType, sql_variant_type) on c.sql_variant_type = d.sql_variant_type


                update a 
                set ExecutionParameterValue2 =
                    -- select 
                    concat(
                        '{"ValueType":"',
                        d.ParameterValueType,
                        '"',
                        case
                            when b.ParameterValue is null then ''
                            else concat(
                                ',"Value', d.ParameterValueType, '":',
                                case
                                    when b.ParameterValue is null then 'null'
                                    when d.ParameterValueType = 'Boolean' then case b.ParameterValue when 1 then 'true' else 'false' end
                                    when d.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), b.ParameterValue)
                                    when d.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, b.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), b.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionStepParameter as a
                    cross apply (select a.ExecutionParameterValue as ParameterValue) as b
                    cross apply (select isnull(sql_variant_property(a.ExecutionParameterValue, 'BaseType'), 'nvarchar') as sql_variant_type) as c
                    inner join (values
                        ('Boolean',     'bit'),
                        ('DateTime',    'datetime'),
                        ('Decimal',     'decimal'),
                        ('Decimal',     'numeric'),
                        ('Double',      'float'),
                        ('Int16',       'smallint'),
                        ('Int32',       'int'),
                        ('Int64',       'bigint'),
                        ('Single',      'real'),
                        ('String',      'varchar'),
                        ('String',      'nvarchar'),
                        ('String',      'uniqueidentifier')
                    ) as d(ParameterValueType, sql_variant_type) on c.sql_variant_type = d.sql_variant_type


                update a 
                set DefaultValue2 =
                    concat(
                        '{"ValueType":"',
                        d.ParameterValueType,
                        '"',
                        case
                            when b.ParameterValue is null then ''
                            else concat(
                                ',"Value', d.ParameterValueType, '":',
                                case
                                    when b.ParameterValue is null then 'null'
                                    when d.ParameterValueType = 'Boolean' then case b.ParameterValue when 1 then 'true' else 'false' end
                                    when d.ParameterValueType in ('Boolean', 'Decimal', 'Double', 'Int16', 'Int32', 'Int64', 'Single') then convert(nvarchar(4000), b.ParameterValue)
                                    when d.ParameterValueType = 'DateTime' then concat('"', format(convert(datetime, b.ParameterValue), 'o'), '"')
                                    else concat('"', string_escape(convert(nvarchar(4000), b.ParameterValue), 'json'), '"')
                                end
                            )
                        end,
                        '}'
                    )
                from app.ExecutionParameter as a
                    cross apply (select a.DefaultValue as ParameterValue) as b
                    cross apply (select isnull(sql_variant_property(a.DefaultValue, 'BaseType'), 'nvarchar') as sql_variant_type) as c
                    inner join (values
                        ('Boolean',     'bit'),
                        ('DateTime',    'datetime'),
                        ('Decimal',     'decimal'),
                        ('Decimal',     'numeric'),
                        ('Double',      'float'),
                        ('Int16',       'smallint'),
                        ('Int32',       'int'),
                        ('Int64',       'bigint'),
                        ('Single',      'real'),
                        ('String',      'varchar'),
                        ('String',      'nvarchar')
                    ) as d(ParameterValueType, sql_variant_type) on c.sql_variant_type = d.sql_variant_type



                alter table app.JobParameter                        alter column ParameterValue2                    nvarchar(max) not null
                alter table app.ExecutionStepConditionParameter     alter column ParameterValue2                    nvarchar(max) not null
                alter table app.ExecutionStepConditionParameter     alter column ExecutionParameterValue2           nvarchar(max) not null
                alter table app.ExecutionStepParameter              alter column ParameterValue2                    nvarchar(max) not null
                alter table app.ExecutionStepParameter              alter column ExecutionParameterValue2           nvarchar(max) not null
                alter table app.StepConditionParameter              alter column ParameterValue2                    nvarchar(max) not null
                alter table app.StepParameter                       alter column ParameterValue2                    nvarchar(max) not null
                alter table app.ExecutionParameter                  alter column DefaultValue2                      nvarchar(max) not null
                alter table app.ExecutionParameter                  alter column ParameterValue2                    nvarchar(max) not null

                -- Drop unnecessary ParameterValueType columns
                alter table app.JobParameter                    drop column ParameterValueType
                alter table app.ExecutionStepConditionParameter drop column ParameterValueType
                alter table app.ExecutionStepParameter          drop column ParameterValueType
                alter table app.StepConditionParameter          drop column ParameterValueType
                alter table app.StepParameter                   drop column ParameterValueType
                alter table app.ExecutionParameter              drop column ParameterValueType

                -- Drop obsolete sql_variant columns
                alter table app.ExecutionStep                       drop column ResultCaptureJobParameterValue
                alter table app.JobParameter                        drop column ParameterValue
                alter table app.ExecutionStepConditionParameter     drop column ParameterValue
                alter table app.ExecutionStepConditionParameter     drop column ExecutionParameterValue
                alter table app.ExecutionStepParameter              drop column ParameterValue
                alter table app.ExecutionStepParameter              drop column ExecutionParameterValue
                alter table app.StepConditionParameter              drop column ParameterValue
                alter table app.StepParameter                       drop column ParameterValue
                alter table app.ExecutionParameter                  drop column DefaultValue
                alter table app.ExecutionParameter                  drop column ParameterValue

                -- Rename placeholders to take the place of the decommissioned columns
                exec sp_rename 'app.ExecutionStep.ResultCaptureJobParameterValue2', 'ResultCaptureJobParameterValue', 'column'
                exec sp_rename 'app.JobParameter.ParameterValue2', 'ParameterValue', 'column'
                exec sp_rename 'app.ExecutionStepConditionParameter.ParameterValue2', 'ParameterValue', 'column'
                exec sp_rename 'app.ExecutionStepConditionParameter.ExecutionParameterValue2', 'ExecutionParameterValue', 'column'
                exec sp_rename 'app.ExecutionStepParameter.ParameterValue2', 'ParameterValue', 'column'
                exec sp_rename 'app.ExecutionStepParameter.ExecutionParameterValue2', 'ExecutionParameterValue', 'column'
                exec sp_rename 'app.StepConditionParameter.ParameterValue2', 'ParameterValue', 'column'
                exec sp_rename 'app.StepParameter.ParameterValue2', 'ParameterValue', 'column'
                exec sp_rename 'app.ExecutionParameter.DefaultValue2', 'DefaultValue', 'column'
                exec sp_rename 'app.ExecutionParameter.ParameterValue2', 'ParameterValue', 'column'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "StepParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "StepParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");

            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "StepConditionParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "StepConditionParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");

            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "JobParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "JobParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");

            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "ExecutionStepParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<object>(
                name: "ExecutionParameterValue",
                schema: "app",
                table: "ExecutionStepParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "ExecutionStepParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");

            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "ExecutionStepConditionParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<object>(
                name: "ExecutionParameterValue",
                schema: "app",
                table: "ExecutionStepConditionParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "ExecutionStepConditionParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");

            migrationBuilder.AlterColumn<object>(
                name: "ResultCaptureJobParameterValue",
                schema: "app",
                table: "ExecutionStep",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<object>(
                name: "ParameterValue",
                schema: "app",
                table: "ExecutionParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<object>(
                name: "DefaultValue",
                schema: "app",
                table: "ExecutionParameter",
                type: "sql_variant",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ParameterValueType",
                schema: "app",
                table: "ExecutionParameter",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "String");
        }
    }
}
