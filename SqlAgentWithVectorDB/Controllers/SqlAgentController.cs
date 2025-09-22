using MediatR;
using Microsoft.AspNetCore.Mvc;
using SqlAgentApi.Application.Assistants;
using SqlAgentApi.Contracts;


namespace SqlAgentWithVectorDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Sets the base route to be "api/sqlagent"
    public class SqlAgentController : ControllerBase
    {
        // We inject the INTERFACE, not the concrete class.
        private readonly ISender _sender;

        public SqlAgentController(ISender sender)
        {
            _sender = sender;
        }


        [HttpPost("assistant")]
        public async Task<IActionResult> GenerateSqlWithAssistant([FromBody] AssistantRequest request)
        {
            var command = new GenerateSqlCommand(request.Question, request.ThreadId);
            var result = await _sender.Send(command);
            return Ok(result);
        }

    }
}
