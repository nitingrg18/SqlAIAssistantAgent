using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SqlAgent.Application.Interfaces;
using SqlAgent.Domain;

namespace SqlAgent.Infrastructure.Persistence
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly string _connectionString;

        public SchemaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<List<TableSchema>> GetSchemaAsync()
        {
            // This is a much more advanced T-SQL query that now retrieves Primary Key (PK)
            // and Foreign Key (FK) relationship information from the database metadata.
            const string sql = @"
                SELECT 
                    t.name AS TableName,
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    p.value AS Description,
                    cons.type_desc AS ConstraintType,
                    ref_t.name AS ReferencedTableName,
                    ref_c.name AS ReferencedColumnName
                FROM sys.tables AS t
                INNER JOIN sys.columns c ON t.object_id = c.object_id
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                LEFT JOIN sys.extended_properties p ON p.major_id = t.object_id AND p.minor_id = c.column_id AND p.name = 'MS_Description'
                LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = t.object_id AND fkc.parent_column_id = c.column_id
                LEFT JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
                LEFT JOIN sys.tables ref_t ON ref_t.object_id = fkc.referenced_object_id
                LEFT JOIN sys.columns ref_c ON ref_c.object_id = fkc.referenced_object_id AND ref_c.column_id = fkc.referenced_column_id
                LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = t.object_id AND kc.unique_index_id = c.column_id AND kc.type = 'PK'
                LEFT JOIN sys.objects cons ON cons.object_id = ISNULL(fk.object_id, kc.object_id)
                WHERE t.is_ms_shipped = 0
                ORDER BY t.name, c.column_id;";

            using var connection = new SqlConnection(_connectionString);
            var schemaData = await connection.QueryAsync<SchemaRawData>(sql);

            // Group the flat list of columns into a hierarchical structure of tables.
            return schemaData
                .GroupBy(s => s.TableName)
                .Select(g => new TableSchema
                {
                    Name = g.Key,
                    Description = $"Table containing information for {g.Key}.",
                    Columns = g.Select(c =>
                    {
                        // Build a rich description string that includes PK/FK info.
                        var finalDescription = c.Description ?? $"{c.ColumnName} of type {c.DataType}";

                        if (c.ConstraintType == "PRIMARY_KEY_CONSTRAINT")
                        {
                            finalDescription += " (Primary Key)";
                        }
                        else if (c.ConstraintType == "FOREIGN_KEY_CONSTRAINT")
                        {
                            finalDescription += $" (Foreign Key to {c.ReferencedTableName}.{c.ReferencedColumnName})";
                        }

                        return new ColumnSchema
                        {
                            Name = c.ColumnName,
                            Type = c.DataType,
                            Description = finalDescription
                        };
                    }).ToList()
                }).ToList();
        }

        // The helper class now includes fields for the relationship data.
        private class SchemaRawData
        {
            public string TableName { get; set; } = "";
            public string ColumnName { get; set; } = "";
            public string DataType { get; set; } = "";
            public string? Description { get; set; }
            public string? ConstraintType { get; set; }
            public string? ReferencedTableName { get; set; }
            public string? ReferencedColumnName { get; set; }
        }
    }
}