using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

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
                break;
            case DropColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case AlterColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case CreateTableOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
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
                break;
            case DropIndexOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case RenameColumnOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
            case RenameIndexOperation op when op.Schema is null:
                op.Schema = DefaultSchema;
                break;
        }

        return operation;
    }
}
