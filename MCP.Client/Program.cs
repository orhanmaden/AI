using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.SemanticKernel.Extensions;
using Microsoft.Extensions.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// credentials
var deploymentName = "gpt-4.1";
var apiKey = configuration["CHAT_GPT_API_KEY"] ?? throw new InvalidOperationException("CHAT_GPT_API_KEY not found in configuration");

// create the kernel
var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(deploymentName, apiKey);

// build the kernel
Kernel kernel = builder.Build();
await kernel.Plugins.AddMcpFunctionsFromStdioServerAsync("MCP", "..\\..\\..\\..\\MCP\\bin\\Debug\\net10.0\\MCP.exe");

// Set parameters so that plugin methods are automatically called
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
};

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
string? userInput;
do
{
    Console.Write("User > ");
    userInput = Console.ReadLine();
    if (!string.IsNullOrEmpty(userInput))
    {
        // Add input to the history
        history.AddUserMessage(userInput!);
        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);
        Console.WriteLine("Assistant > " + result);

        // Add answer to the history
        history.AddMessage(result.Role, result.Content ?? string.Empty);
    }
} while (!String.IsNullOrEmpty(userInput));
