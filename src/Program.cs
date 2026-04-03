using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

if (args.Length < 2 || args[0] != "-p")
{
    throw new Exception("Usage: program -p <prompt>");
}

var prompt = args[1];

if (string.IsNullOrEmpty(prompt))
{
    throw new Exception("Prompt must not be empty");
}

var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var baseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? "https://openrouter.ai/api/v1";

if (string.IsNullOrEmpty(apiKey))
{
    throw new Exception("OPENROUTER_API_KEY is not set");
}

var client = new ChatClient(
    model: "anthropic/claude-haiku-4.5",
    credential: new ApiKeyCredential(apiKey),
    options: new OpenAIClientOptions { Endpoint = new Uri(baseUrl) }
);

var readTool = ChatTool.CreateFunctionTool(
    functionName: "Read",
    functionParameters: BinaryData.FromBytes(
        """
        {
          "type": "function",
          "function": {
            "name": "Read",
            "description": "Read and return the contents of a file",
            "parameters": {
              "type": "object",
              "properties": {
                "file_path": {
                  "type": "string",
                  "description": "The path to the file to read"
                }
              },
              "required": ["file_path"]
            }
          }
        }
        """u8.ToArray())
    );

ChatCompletionOptions tools = new() { Tools = { readTool } };
ChatMessage[] messages = [new UserChatMessage(prompt)];
ChatCompletion response = client.CompleteChat(messages, tools);

if (response.Content == null || response.Content.Count == 0)
{
    throw new Exception("No choices in response");
}

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.Error.WriteLine("Logs from your program will appear here!");

var tool_calls = response.ToolCalls;
if (tool_calls != null && tool_calls.Count > 0)
{
    var tool_call = tool_calls[0];
    var tool_call_function_name = tool_call.function.Name;
    if (tool_call_function_name == "Read") 
    {
        var argsDoc = JsonDocument.Parse(tool_call.FunctionArguments);
        var file_path = argsDoc.RootElement.GetProperty("file_path").GetString();
        var file_content = File.ReadAllText(file_path);
        Console.Write(file_content);
    }
}
else if (response.Content != null && response.Content.Count > 0)
{
    Console.Write(response.Content[0].Text);
}

