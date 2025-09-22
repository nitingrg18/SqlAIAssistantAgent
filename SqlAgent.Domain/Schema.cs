namespace SqlAgent.Domain
{
    public class TableSchema
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ColumnSchema> Columns { get; set; } = new();

        public override string ToString()
        {
            var columns = string.Join("\n", Columns.Select(c => $"  - {c.Name} ({c.Type}): {c.Description}"));
            return $"Table: {Name}\nDescription: {Description}\nColumns:\n{columns}";
        }
    }

    public class ColumnSchema
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
