using Microsoft.Extensions.Configuration;
using SqlAgent.Application.Interfaces;
using SqlAgent.Domain;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace SqlAgent.Infrastructure.AiServices
{
    public class AssistantService : IAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string? _assistantId;

        public AssistantService(string apiKey, IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            _configuration = configuration;
        }

        public async Task InitializeAssistantAsync(List<TableSchema> schema)
        {
            _assistantId = _configuration["OpenAIAssistantId"];
            if (!string.IsNullOrEmpty(_assistantId))
            {
                Console.WriteLine($"Found existing Assistant ID: {_assistantId}. Reusing it.");
                return;
            }

            Console.WriteLine("No Assistant ID found in configuration. Creating a new one...");
            string schemaContent = string.Join("\n\n", schema.Select(t => t.ToString()));
            string tempSchemaFilePath = Path.Combine(Path.GetTempPath(), $"schema_{Guid.NewGuid()}.txt");
            await File.WriteAllTextAsync(tempSchemaFilePath, schemaContent);
            string fileId = await UploadFileAsync(tempSchemaFilePath);
            File.Delete(tempSchemaFilePath);

            string vectorStoreId = await CreateVectorStoreAsync();
            await AddFileToVectorStoreAsync(vectorStoreId, fileId);
            await PollVectorStoreFileUntilReadyAsync(vectorStoreId, fileId);


            string assistantInstructions = """
    You are an expert assistant that writes SQL queries for a **Microsoft SQL Server (T-SQL)** database. Your primary goal is to be a precise, fact-based query generator.

    **CRITICAL DIRECTIVE: The knowledge file provided via `file_search` is your ONLY source of truth for the database schema. You MUST adhere to it strictly.**

    RULES:
    1.  **EXACT NAMING RULE: You MUST use the exact table and column names as they appear in the schema file. Do not invent, assume, or substitute column names based on common patterns.** For example, if the schema specifies `PostalCode`, you MUST use `PostalCode` and never `ZipCode`. If it specifies `Address1`, you MUST use `Address1` and never `Address`. This is not optional.
    2.  **JOIN LOGIC:** Pay very close attention to the explicit Foreign Key (FK) constraints mentioned in the schema. Do NOT assume a direct join is possible unless an FK relationship is stated.
    3.  **JUNCTION TABLE RULE:** If a user asks to connect two tables and there is no direct FK between them, you MUST actively look for a third "junction" or "linking" table to bridge the relationship.
    4.  **INTERPRETATION RULE:** Distinguish between simple requests (e.g., "top 10 products" -> `SELECT TOP 10 *`) and analytical requests ("top 10 most selling" -> requires joins and aggregates).
    5.  **CONTEXT RULE:** When refining a query, use the previous query from the conversation history as context.
    6.  **OUTPUT FORMAT:** Your output MUST be ONLY the T-SQL query, with no extra explanations or formatting.
    """;



            _assistantId = await CreateAssistantAsync("SQL Agent", assistantInstructions, vectorStoreId);

         
        }

        public async Task<(string response, string threadId)> GenerateSqlResponseAsync(string question, string? threadId)
        {
            if (string.IsNullOrEmpty(_assistantId))
                throw new InvalidOperationException("Assistant is not initialized.");

            if (string.IsNullOrEmpty(threadId))
            {
                threadId = await CreateThreadAsync();
            }

            await AddMessageToThreadAsync(threadId, question);
            string runId = await CreateRunAsync(threadId, _assistantId);
            await PollRunUntilCompleteAsync(threadId, runId);
            string response = await GetLastAssistantMessageAsync(threadId);

            return (response, threadId);
        }

        private async Task<string> UploadFileAsync(string filePath)
        {
            using var content = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(filePath);
            content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
            content.Add(new StringContent("assistants"), "purpose");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/files", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json!.RootElement.GetProperty("id").GetString()!;
        }

        private async Task<string> CreateVectorStoreAsync()
        {
            var requestBody = new { name = "SQL Agent Knowledge Base" };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/vector_stores", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json!.RootElement.GetProperty("id").GetString()!;
        }

        private async Task AddFileToVectorStoreAsync(string vectorStoreId, string fileId)
        {
            var requestBody = new { file_id = fileId };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task PollVectorStoreFileUntilReadyAsync(string vectorStoreId, string fileId)
        {
            var status = "";
            int attempts = 0;
            do
            {
                if (attempts > 20) throw new TimeoutException("The file took too long to be processed.");
                await Task.Delay(1000);
                var response = await _httpClient.GetAsync($"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files/{fileId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<VectorStoreFileResponse>();
                status = json?.Status;
                Console.WriteLine($"  > File processing status: {status}");
                attempts++;
            } while (status == "in_progress");

            if (status != "completed")
                throw new Exception($"File processing failed with status: {status}");
        }

        private async Task<string> CreateAssistantAsync(string name, string instructions, string vectorStoreId)
        {
            var requestBody = new { instructions, name, tools = new[] { new { type = "file_search" } }, model = "gpt-4-turbo", tool_resources = new { file_search = new { vector_store_ids = new[] { vectorStoreId } } } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/assistants", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json!.RootElement.GetProperty("id").GetString()!;
        }

        private async Task<string> CreateThreadAsync()
        {
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/threads", null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json!.RootElement.GetProperty("id").GetString()!;
        }

        private async Task AddMessageToThreadAsync(string threadId, string message)
        {
            var requestBody = new { role = "user", content = message };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{threadId}/messages", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> CreateRunAsync(string threadId, string assistantId)
        {
            var requestBody = new { assistant_id = assistantId };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return json!.RootElement.GetProperty("id").GetString()!;
        }

        private async Task PollRunUntilCompleteAsync(string threadId, string runId)
        {
            string status = "";
            int attempts = 0;
            do
            {
                if (attempts > 30) throw new TimeoutException("The run took too long to complete.");
                await Task.Delay(1000);
                var response = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                status = json!.RootElement.GetProperty("status").GetString();
                attempts++;
            } while (status == "queued" || status == "in_progress");

            if (status != "completed") throw new Exception($"Run finished with status: {status}");
        }

        private async Task<string> GetLastAssistantMessageAsync(string threadId)
        {
            var response = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
            response.EnsureSuccessStatusCode();
            var messageList = await response.Content.ReadFromJsonAsync<MessageListResponse>();
            var assistantMessage = messageList?.Data?.FirstOrDefault(m => m.Role == "assistant");
            var textContent = assistantMessage?.Content?.FirstOrDefault()?.Text?.Value;
            return textContent ?? "No response from assistant.";
        }

        private class VectorStoreFileResponse { [JsonPropertyName("status")] public string? Status { get; set; } }
        private class MessageListResponse { [JsonPropertyName("data")] public List<MessageData>? Data { get; set; } }
        private class MessageData { [JsonPropertyName("role")] public string? Role { get; set; } [JsonPropertyName("content")] public List<MessageContent>? Content { get; set; } }
        private class MessageContent { [JsonPropertyName("text")] public TextContent? Text { get; set; } }
        private class TextContent { [JsonPropertyName("value")] public string? Value { get; set; } }
    }
}
