

using SqlAgent.Domain;

namespace SqlAgent.Application.Interfaces
{
    public interface IAssistantService
    {
        Task InitializeAssistantAsync(List<TableSchema> schema);
        Task<(string response, string threadId)> GenerateSqlResponseAsync(string question, string? threadId);
    }
}
