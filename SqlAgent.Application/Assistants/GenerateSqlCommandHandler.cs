using MediatR;
using SqlAgent.Application.Interfaces;
using SqlAgentApi.Application.Assistants;
using SqlAgentApi.Contracts;

namespace SqlAgent.Application.Assistants
{
    public class GenerateSqlCommandHandler : IRequestHandler<GenerateSqlCommand, AssistantResponse>
    {
        private readonly IAssistantService _assistantService;

        public GenerateSqlCommandHandler(IAssistantService assistantService)
        {
            _assistantService = assistantService;
        }

        public async Task<AssistantResponse> Handle(GenerateSqlCommand request, CancellationToken cancellationToken)
        {
            var (response, newThreadId) = await _assistantService.GenerateSqlResponseAsync(request.Question, request.ThreadId);
            return new AssistantResponse { Sql = response, ThreadId = newThreadId };
        }
    }
}
