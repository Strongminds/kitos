using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Tools.MigrateSQLServer2Postgres.Migration;

internal sealed record TableRef(string Schema, string Name)
{
    public override string ToString()
    {
        return $"{Schema}.{Name}";
    }
}

internal sealed record TableMatch(TableRef Source, TableRef Target);
internal sealed record SqlServerColumnDefinition(
    string Name,
    string DataType,
    int? MaxLength,
    int? Precision,
    int? Scale,
    bool IsNullable,
    bool IsIdentity,
    bool IsComputed,
    int Ordinal);
internal sealed record SqlServerTableDefinition(TableRef Table, IReadOnlyList<SqlServerColumnDefinition> Columns);

internal static class SchemaDiscovery
{
    public static async Task<List<TableRef>> GetSqlServerTablesAsync(SqlConnection connection)
    {
        const string query = @"
SELECT s.name, t.name
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;";

        var result = new List<TableRef>();
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TableRef(reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    public static async Task<List<TableRef>> GetPostgresTablesAsync(NpgsqlConnection connection)
    {
        const string query = @"
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name;";

        var result = new List<TableRef>();
        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TableRef(reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    public static async Task<List<SqlServerTableDefinition>> GetSqlServerTableDefinitionsAsync(SqlConnection connection)
    {
        const string query = @"
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id AS Ordinal,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    c.is_computed AS IsComputed
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;";

        var rows = new List<(TableRef Table, SqlServerColumnDefinition Column)>();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var table = new TableRef(reader.GetString(0), reader.GetString(1));
            var column = new SqlServerColumnDefinition(
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetInt16(5),
                reader.IsDBNull(6) ? null : reader.GetByte(6),
                reader.IsDBNull(7) ? null : reader.GetByte(7),
                reader.GetBoolean(8),
                reader.GetBoolean(9),
                reader.GetBoolean(10),
                reader.GetInt32(2));

            rows.Add((table, column));
        }

        var definitions = rows
            .GroupBy(row => row.Table)
            .Select(group => new SqlServerTableDefinition(
                group.Key,
                group.Select(item => item.Column)
                    .OrderBy(column => column.Ordinal)
                    .ToList()))
            .OrderBy(definition => definition.Table.Schema)
            .ThenBy(definition => definition.Table.Name)
            .ToList();

        return definitions;
    }

    public static async Task<List<string>> GetSqlServerColumnsAsync(SqlConnection connection, TableRef table)
    {
        const string query = @"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName
  AND t.name = @tableName
ORDER BY c.column_id;";

        var result = new List<string>();
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", table.Schema);
        command.Parameters.AddWithValue("@tableName", table.Name);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public static async Task<List<string>> GetPostgresColumnsAsync(NpgsqlConnection connection, TableRef table)
    {
        const string query = @"
SELECT column_name
FROM information_schema.columns
WHERE table_schema = @schemaName
  AND table_name = @tableName
ORDER BY ordinal_position;";

        var result = new List<string>();
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@schemaName", table.Schema);
        command.Parameters.AddWithValue("@tableName", table.Name);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public static async Task<List<(TableRef Child, TableRef Parent)>> GetSqlServerForeignKeyEdgesAsync(SqlConnection connection)
    {
        const string query = @"
SELECT childSchema.name, childTable.name, parentSchema.name, parentTable.name
FROM sys.foreign_keys fk
INNER JOIN sys.tables childTable ON fk.parent_object_id = childTable.object_id
INNER JOIN sys.schemas childSchema ON childTable.schema_id = childSchema.schema_id
INNER JOIN sys.tables parentTable ON fk.referenced_object_id = parentTable.object_id
INNER JOIN sys.schemas parentSchema ON parentTable.schema_id = parentSchema.schema_id
WHERE childTable.is_ms_shipped = 0 AND parentTable.is_ms_shipped = 0;";

        var result = new List<(TableRef Child, TableRef Parent)>();
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var child = new TableRef(reader.GetString(0), reader.GetString(1));
            var parent = new TableRef(reader.GetString(2), reader.GetString(3));
            result.Add((child, parent));
        }

        return result;
    }

    public static List<TableMatch> MatchTables(IReadOnlyCollection<TableRef> sourceTables, IReadOnlyCollection<TableRef> targetTables)
    {
        var targetByName = targetTables
            .GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var matches = new List<TableMatch>();
        foreach (var sourceTable in sourceTables)
        {
            if (!targetByName.TryGetValue(sourceTable.Name, out var candidates))
            {
                continue;
            }

            var targetTable = candidates.FirstOrDefault(candidate =>
                candidate.Schema.Equals(sourceTable.Schema, StringComparison.OrdinalIgnoreCase))
                ?? candidates.First();

            matches.Add(new TableMatch(sourceTable, targetTable));
        }

        return matches;
    }

    public static List<TableRef> TopologicalSortTables(
        IReadOnlyCollection<TableRef> tables,
        IReadOnlyCollection<(TableRef Child, TableRef Parent)> foreignKeyEdges)
    {
        var tableSet = new HashSet<TableRef>(tables);
        var inDegree = tables.ToDictionary(table => table, _ => 0);
        var adjacency = tables.ToDictionary(table => table, _ => new List<TableRef>());

        foreach (var edge in foreignKeyEdges)
        {
            if (!tableSet.Contains(edge.Child) || !tableSet.Contains(edge.Parent))
            {
                continue;
            }

            if (edge.Child == edge.Parent)
            {
                continue;
            }

            adjacency[edge.Parent].Add(edge.Child);
            inDegree[edge.Child]++;
        }

        var queue = new Queue<TableRef>(inDegree.Where(pair => pair.Value == 0)
            .Select(pair => pair.Key)
            .OrderBy(table => table.Schema)
            .ThenBy(table => table.Name));

        var result = new List<TableRef>(tables.Count);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var dependent in adjacency[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (result.Count == tables.Count)
        {
            return result;
        }

        // Cycles remain (often through self references). Append remaining tables deterministically.
        var missing = tables.Except(result)
            .OrderBy(table => table.Schema)
            .ThenBy(table => table.Name)
            .ToList();
        result.AddRange(missing);
        return result;
    }

    public static string QuoteSqlServerIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    public static string QuotePostgresIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    public static string QualifySqlServerTable(TableRef table)
    {
        return $"{QuoteSqlServerIdentifier(table.Schema)}.{QuoteSqlServerIdentifier(table.Name)}";
    }

    public static string QualifyPostgresTable(TableRef table)
    {
        return $"{QuotePostgresIdentifier(table.Schema)}.{QuotePostgresIdentifier(table.Name)}";
    }
}
