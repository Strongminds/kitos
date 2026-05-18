using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;

namespace Infrastructure.DataAccess;

/// <summary>
/// Overrides the default Npgsql migrations SQL generator to automatically qualify
/// all table/column/index operations with the "dbo" schema when no schema is explicitly
/// specified. This allows EF Core migration Up/Down methods to use standard constructs
/// (AddColumn, DropColumn, etc.) without needing schema-aware raw SQL.
///
/// The "public" schema is intentionally left untouched so that PostgreSQL extensions
/// (e.g. citext) that live in "public" are not affected.
/// </summary>
public sealed class KitosNpgsqlMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    private const string DefaultSchema = "dbo";
    private const string SqlServerIdentityAnnotationName = "SqlServer:Identity";
    private const string DuplicateOptionUuidIndexName = "UX_Option_Uuid";

    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        var patched = operations.Select(PatchSchema).ToList();
        return base.Generate(patched, model, options);
    }

    private static MigrationOperation PatchSchema(MigrationOperation operation)
    {
        switch (operation)
        {
            case AddColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                PatchColumn(op);
                break;
            case DropColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AlterColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                PatchColumn(op);
                break;
            case CreateTableOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                foreach (var column in op.Columns)
                {
                    PatchColumn(column);
                }
                foreach (var fk in op.ForeignKeys)
                {
                    if (fk.Schema is null) fk.Schema = DefaultSchema;
                    if (fk.PrincipalSchema is null) fk.PrincipalSchema = DefaultSchema;
                }
                break;
            case DropTableOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case RenameTableOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AddForeignKeyOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                if (op.PrincipalSchema is null) op.PrincipalSchema = DefaultSchema;
                break;
            case DropForeignKeyOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AddPrimaryKeyOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case DropPrimaryKeyOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AddUniqueConstraintOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case DropUniqueConstraintOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AddCheckConstraintOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case DropCheckConstraintOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case CreateIndexOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                PatchIndexName(op);
                break;
            case DropIndexOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                PatchIndexName(op);
                break;
            case RenameColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case RenameIndexOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                PatchIndexName(op);
                break;
        }

        return operation;
    }

    private static void PatchColumn(ColumnOperation column)
    {
        column.ColumnType = column.ColumnType switch
        {
            "nvarchar(max)" => "text",
            "datetime2" => "timestamp without time zone",
            "bit" => "boolean",
            "uniqueidentifier" => "uuid",
            var type when type is not null && type.StartsWith("nvarchar(", System.StringComparison.OrdinalIgnoreCase)
                => type.Replace("nvarchar", "character varying", System.StringComparison.OrdinalIgnoreCase),
            _ => column.ColumnType
        };

        if (column[SqlServerIdentityAnnotationName] is not null)
        {
            column.RemoveAnnotation(SqlServerIdentityAnnotationName);
            column[NpgsqlAnnotationNames.ValueGenerationStrategy] = NpgsqlValueGenerationStrategy.IdentityByDefaultColumn;
        }
    }

    private static void PatchIndexName(CreateIndexOperation operation)
    {
        operation.Name = GetPatchedIndexName(operation.Name, operation.Table);
    }

    private static void PatchIndexName(DropIndexOperation operation)
    {
        operation.Name = GetPatchedIndexName(operation.Name, operation.Table);
    }

    private static void PatchIndexName(RenameIndexOperation operation)
    {
        operation.Name = GetPatchedIndexName(operation.Name, operation.Table);
        operation.NewName = GetPatchedIndexName(operation.NewName, operation.Table);
    }

    private static string? GetPatchedIndexName(string? indexName, string? tableName)
    {
        if (!string.Equals(indexName, DuplicateOptionUuidIndexName, System.StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(tableName))
        {
            return indexName;
        }

        return $"{DuplicateOptionUuidIndexName}_{tableName}";
    }
}
