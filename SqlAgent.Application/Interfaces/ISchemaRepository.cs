using SqlAgent.Domain;

namespace SqlAgent.Application.Interfaces
{
    public interface ISchemaRepository
    {
        Task<List<TableSchema>> GetSchemaAsync();
    }
}
