using MediatR;
using SqlAgentApi.Contracts;

namespace SqlAgentApi.Application.Assistants
{
    public record GenerateSqlCommand(string Question, string? ThreadId) : IRequest<AssistantResponse>;
}