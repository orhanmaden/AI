using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentFramework.Services;
using AgentFramework.Agents;
using System.Globalization;

// Set invariant culture for the entire application
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

Console.WriteLine("?? Multi-Agent TODO List System");
Console.WriteLine("================================\n");

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var deploymentName = "gpt-4.1";
var apiKey = configuration["CHAT_GPT_API_KEY"] 
    ?? throw new InvalidOperationException("CHAT_GPT_API_KEY not found in configuration. Please set it using: dotnet user-secrets set \"CHAT_GPT_API_KEY\" \"your-key-here\"");

// Create kernel with OpenAI chat completion
var builder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(deploymentName, apiKey);

var kernel = builder.Build();

// Initialize shared state and agents
var todoState = new TodoState();
var todoManager = new TodoManagerAgent(todoState, kernel);
var priorityAnalyzer = new PriorityAnalyzerAgent(todoState, kernel);
var deadlineMonitor = new DeadlineMonitorAgent(todoState, kernel);
var taskDecomposer = new TaskDecomposerAgent(todoState, kernel);

// Create orchestrator
var orchestrator = new AgentOrchestrator(
    todoState,
    kernel,
    todoManager,
    priorityAnalyzer,
    deadlineMonitor,
    taskDecomposer
);

Console.WriteLine("? All agents initialized successfully!");
Console.WriteLine("\nAvailable Agents:");
Console.WriteLine("  • TodoManager - Manages your todo list");
Console.WriteLine("  • PriorityAnalyzer - Analyzes and assigns task priorities");
Console.WriteLine("  • DeadlineMonitor - Tracks deadlines and sends alerts");
Console.WriteLine("  • TaskDecomposer - Breaks down complex tasks into subtasks");
Console.WriteLine("\n?? Try commands like:");
Console.WriteLine("  - Create a task to implement user authentication");
Console.WriteLine("  - List all my tasks");
Console.WriteLine("  - Check my deadlines");
Console.WriteLine("  - Show me the system status");
Console.WriteLine("  - Run a demo scenario");
Console.WriteLine("  - Analyze priorities of all tasks");
Console.WriteLine("\nType 'help' for more examples, 'demo' for a demonstration, or 'quit' to exit.\n");

// Chat history for conversation context
var history = new ChatHistory();
history.AddSystemMessage(@"You are a coordinator for a multi-agent TODO list management system with four specialized agents:

1. TodoManager: Handles CRUD operations for tasks (create, list, update status, delete, get details)
2. PriorityAnalyzer: Analyzes tasks and assigns priorities based on keywords and deadlines
3. DeadlineMonitor: Monitors deadlines, checks for overdue tasks, and provides deadline summaries
4. TaskDecomposer: Breaks down complex tasks into manageable subtasks

When users make requests:
- Automatically call the appropriate agent functions to fulfill their needs
- You can call multiple agents in sequence if needed
- Be proactive in suggesting agent actions (e.g., if a new urgent task is created, suggest checking priorities)
- Provide clear, friendly responses
- When creating tasks, always acknowledge what the agents have done

Be conversational and helpful. The agents will automatically collaborate through inter-agent messaging.");

// Main interaction loop
string? userInput;
do
{
    Console.Write("\n?? You > ");
    userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
        continue;

    if (userInput.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    if (userInput.Trim().Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        ShowHelp();
        continue;
    }

    if (userInput.Trim().Equals("demo", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n?? Running demonstration scenario...\n");
        var demoResult = await orchestrator.RunDemoScenario();
        Console.WriteLine(demoResult);
        continue;
    }

    if (userInput.Trim().Equals("status", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n" + orchestrator.GetSystemStatus());
        continue;
    }

    if (userInput.Trim().Equals("logs", StringComparison.OrdinalIgnoreCase) ||
        userInput.Trim().Equals("activity", StringComparison.OrdinalIgnoreCase))
    {
        ShowActivityLog(todoState);
        continue;
    }

    try
    {
        // Process user input through the orchestrator
        var response = await orchestrator.ProcessUserInput(userInput, history);
        Console.WriteLine($"\n?? Assistant > {response}");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n? Error: {ex.Message}");
        Console.ResetColor();
        todoState.LogActivity($"Error: {ex.Message}");
    }

} while (true);

// Exit summary
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n\n?? Final System Status:");
Console.WriteLine("======================");
Console.ResetColor();
Console.WriteLine(orchestrator.GetSystemStatus());

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n?? Activity Log:");
Console.WriteLine("===============");
Console.ResetColor();
ShowActivityLog(todoState);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n\n?? Thank you for using the Multi-Agent TODO System!");
Console.ResetColor();

// Helper methods
static void ShowHelp()
{
    Console.WriteLine("\n?? Help - Example Commands:");
    Console.WriteLine("\n?? Task Management:");
    Console.WriteLine("  • Create a task to build a website by next Friday");
    Console.WriteLine("  • Add a todo: Fix the login bug");
    Console.WriteLine("  • List all my tasks");
    Console.WriteLine("  • Show me pending tasks");
    Console.WriteLine("  • Update task <ID> status to completed");
    Console.WriteLine("  • Delete task <ID>");
    Console.WriteLine("  • Show details of task <ID>");
    
    Console.WriteLine("\n?? Priority Management:");
    Console.WriteLine("  • Analyze priorities of all tasks");
    Console.WriteLine("  • Set priority of task <ID> to high");
    Console.WriteLine("  • Which tasks need priority review?");
    
    Console.WriteLine("\n?? Deadline Management:");
    Console.WriteLine("  • Check my deadlines");
    Console.WriteLine("  • Show me overdue tasks");
    Console.WriteLine("  • Set deadline for task <ID> to 2024-12-31");
    Console.WriteLine("  • Give me a deadline summary");
    
    Console.WriteLine("\n?? Task Decomposition:");
    Console.WriteLine("  • Break down task <ID> into subtasks");
    Console.WriteLine("  • Identify complex tasks that need breakdown");
    Console.WriteLine("  • Suggest breakdown for: Build e-commerce platform");
    Console.WriteLine("  • Add subtask to task <ID>: Create database schema");
    
    Console.WriteLine("\n?? System Commands:");
    Console.WriteLine("  • status - Show system status");
    Console.WriteLine("  • demo - Run demonstration scenario");
    Console.WriteLine("  • logs / activity - Show activity log");
    Console.WriteLine("  • help - Show this help");
    Console.WriteLine("  • quit / exit - Exit the application");
}

static void ShowActivityLog(TodoState state)
{
    var logs = state.GetActivityLog();
    if (logs.Count == 0)
    {
        Console.WriteLine("No activity recorded yet.");
        return;
    }

    var recentLogs = logs.TakeLast(20).ToList();
    foreach (var log in recentLogs)
    {
        Console.WriteLine($"  {log}");
    }
    
    if (logs.Count > 20)
    {
        Console.WriteLine($"\n  ... and {logs.Count - 20} earlier entries");
    }
}
