using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.Plugins;
using System.Globalization;

// Set invariant culture for the entire application
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var deploymentName = "gpt-4.1";
var apiKey = configuration["CHAT_GPT_API_KEY"] ?? throw new InvalidOperationException("CHAT_GPT_API_KEY not found in configuration");


// Make the kernel builder
var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(deploymentName, apiKey);
builder.Services.TryAddTransient<HttpClient>();

// build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Register the plugin
kernel.Plugins.AddFromType<WeatherPlugin>("WeatherPlugin", serviceProvider: builder.Services.BuildServiceProvider());

// Set the parameter so that functions are automatically called by SemanticKernel
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),

};

var history = new ChatHistory();
string? userInput;
do
{
    Console.Write("User > ");
    userInput = Console.ReadLine();

    if (!string.IsNullOrEmpty(userInput))
    {
        // Add the input to the history
        history.AddUserMessage(userInput!);

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        Console.WriteLine("Assistant > " + result);

        // Add the answer to the history
        history.AddMessage(result.Role, $"Assistant response {result.Content}");
    }
} while (!string.IsNullOrEmpty(userInput));


// Below is for demo purposes so that you can see the chat history that includes the interaction
// by SemanticKernel for automatic function calling
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Exiting. Full chat history:");
var entryNumber = 0;
foreach (var item in history)
{
    entryNumber++;
    if (item.InnerContent != null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{entryNumber} - InnerContent: {item.InnerContent}");
    }

    if (!string.IsNullOrWhiteSpace(item.ToString()))
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{entryNumber} - Content: {item}");
    }
}