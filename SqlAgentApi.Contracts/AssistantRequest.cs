namespace SqlAgentApi.Contracts
{
    public class AssistantRequest
    {
        public string Question { get; set; } = "";
        public string? ThreadId { get; set; }
    }

    public class AssistantResponse
    {
        public string Sql { get; set; } = "";
        public string ThreadId { get; set; } = "";
    }
}
