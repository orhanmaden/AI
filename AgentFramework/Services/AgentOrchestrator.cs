using AgentFramework.Agents;
using AgentFramework.Models;
using AgentFramework.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgentFramework.Services;

/// <summary>
/// Orchestrates communication and coordination between multiple agents
/// </summary>
public class AgentOrchestrator
{
    private readonly TodoState _state;
    private readonly Kernel _kernel;
    private readonly TodoManagerAgent _todoManager;
    private readonly PriorityAnalyzerAgent _priorityAnalyzer;
    private readonly DeadlineMonitorAgent _deadlineMonitor;
    private readonly TaskDecomposerAgent _taskDecomposer;
    private readonly IChatCompletionService _chatService;

    public AgentOrchestrator(
        TodoState state,
        Kernel kernel,
        TodoManagerAgent todoManager,
        PriorityAnalyzerAgent priorityAnalyzer,
        DeadlineMonitorAgent deadlineMonitor,
        TaskDecomposerAgent taskDecomposer)
    {
        _state = state;
        _kernel = kernel;
        _todoManager = todoManager;
        _priorityAnalyzer = priorityAnalyzer;
        _deadlineMonitor = deadlineMonitor;
        _taskDecomposer = taskDecomposer;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Register all agent functions with the kernel
        RegisterAgentFunctions();
    }

    private void RegisterAgentFunctions()
    {
        _kernel.Plugins.Clear();
        _kernel.Plugins.AddFromObject(_todoManager, "TodoManager");
        _kernel.Plugins.AddFromObject(_priorityAnalyzer, "PriorityAnalyzer");
        _kernel.Plugins.AddFromObject(_deadlineMonitor, "DeadlineMonitor");
        _kernel.Plugins.AddFromObject(_taskDecomposer, "TaskDecomposer");
    }

    /// <summary>
    /// Processes user input and coordinates agent responses
    /// </summary>
    public async Task<string> ProcessUserInput(string userInput, ChatHistory history)
    {
        // Add user message to history
        history.AddUserMessage(userInput);

        // Configure automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.7,
            MaxTokens = 2000
        };

        try
        {
            // Get AI response with automatic function calling
            var result = await _chatService.GetChatMessageContentAsync(
                history,
                executionSettings: executionSettings,
                kernel: _kernel);

            // Add assistant response to history
            history.AddMessage(result.Role, result.Content ?? string.Empty);

            // Process any pending inter-agent messages
            await ProcessAgentMessages();

            return result.Content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            _state.LogActivity($"Orchestrator: Error processing input: {ex.Message}");
            return $"? Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Processes messages between agents
    /// </summary>
    private async Task ProcessAgentMessages()
    {
        var processedCount = 0;
        var maxMessages = 20; // Prevent infinite loops

        while (processedCount < maxMessages && _state.TryDequeueMessage(out var message))
        {
            if (message == null) break;

            processedCount++;

            try
            {
                // Route message to appropriate agent
                switch (message.ToAgent)
                {
                    case "PriorityAnalyzer":
                        _priorityAnalyzer.ProcessMessage(message);
                        break;
                    case "DeadlineMonitor":
                        _deadlineMonitor.ProcessMessage(message);
                        break;
                    case "TodoManager":
                        // TodoManager doesn't need to process messages actively
                        break;
                    case "All":
                        // Broadcast to all agents
                        _priorityAnalyzer.ProcessMessage(message);
                        _deadlineMonitor.ProcessMessage(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _state.LogActivity($"Orchestrator: Error processing message from {message.FromAgent}: {ex.Message}");
            }

            // Small delay to prevent overwhelming the system
            await Task.Delay(10);
        }

        if (processedCount > 0)
        {
            _state.LogActivity($"Orchestrator: Processed {processedCount} inter-agent messages");
        }
    }

    /// <summary>
    /// Runs automated agent tasks (periodic checks)
    /// </summary>
    public async Task RunPeriodicTasks()
    {
        try
        {
            // Check for overdue deadlines
            var deadlineCheck = _deadlineMonitor.CheckDeadlines();
            if (deadlineCheck.Contains("OVERDUE") || deadlineCheck.Contains("DUE TODAY"))
            {
                _state.LogActivity($"Automated: {deadlineCheck}");
            }

            // Analyze priorities of pending tasks
            var todos = _state.GetAllTodos()
                .Where(t => t.Status == TodoStatus.Pending && t.Priority == TodoPriority.Medium)
                .ToList();

            foreach (var todo in todos.Take(5)) // Limit to prevent overwhelming
            {
                _priorityAnalyzer.AnalyzePriority(todo.Id);
            }

            await ProcessAgentMessages();
        }
        catch (Exception ex)
        {
            _state.LogActivity($"Orchestrator: Error in periodic tasks: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a status summary from all agents
    /// </summary>
    public string GetSystemStatus()
    {
        var todos = _state.GetAllTodos();
        var deadlineSummary = _deadlineMonitor.GetDeadlineSummary();
        var activityLog = _state.GetActivityLog().TakeLast(10).ToList();

        var status = "?? Multi-Agent System Status:\n\n";
        status += $"?? Total Tasks: {todos.Count}\n";
        status += $"   ? Completed: {todos.Count(t => t.Status == TodoStatus.Completed)}\n";
        status += $"   ? In Progress: {todos.Count(t => t.Status == TodoStatus.InProgress)}\n";
        status += $"   ?? Pending: {todos.Count(t => t.Status == TodoStatus.Pending)}\n";
        status += $"   ?? Blocked: {todos.Count(t => t.Status == TodoStatus.Blocked)}\n\n";
        
        status += $"?? Priority Distribution:\n";
        status += $"   ?? Critical: {todos.Count(t => t.Priority == TodoPriority.Critical)}\n";
        status += $"   ?? High: {todos.Count(t => t.Priority == TodoPriority.High)}\n";
        status += $"   ?? Medium: {todos.Count(t => t.Priority == TodoPriority.Medium)}\n";
        status += $"   ?? Low: {todos.Count(t => t.Priority == TodoPriority.Low)}\n\n";

        status += deadlineSummary + "\n\n";

        if (activityLog.Count > 0)
        {
            status += "?? Recent Activity:\n";
            foreach (var log in activityLog)
            {
                status += $"   {log}\n";
            }
        }

        return status;
    }

    /// <summary>
    /// Demonstrates agent collaboration with a sample scenario
    /// </summary>
    public async Task<string> RunDemoScenario()
    {
        var results = new List<string>();
        
        results.Add("?? Starting Multi-Agent Demo Scenario...\n");

        // Create a complex task
        var createResult = _todoManager.CreateTodo(
            "Implement user authentication system",
            "Build a secure authentication system with JWT tokens, password hashing, and role-based access control",
            DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd")
        );
        results.Add($"1?? TodoManager: {createResult}");

        // Process inter-agent messages
        await ProcessAgentMessages();
        await Task.Delay(100);

        // Get the created todo ID (parse from result)
        var todoId = _state.GetAllTodos().Last().Id;

        // Decompose the task
        var decomposeResult = await _taskDecomposer.DecomposeTask(todoId);
        results.Add($"\n2?? TaskDecomposer: {decomposeResult}");

        // Create an urgent task
        var urgentResult = _todoManager.CreateTodo(
            "Fix critical security bug in login",
            "URGENT: Users can bypass authentication",
            DateTime.UtcNow.ToString("yyyy-MM-dd")
        );
        results.Add($"\n3?? TodoManager: {urgentResult}");

        await ProcessAgentMessages();
        await Task.Delay(100);

        // Check deadlines
        var deadlineResult = _deadlineMonitor.CheckDeadlines();
        results.Add($"\n4?? DeadlineMonitor: {deadlineResult}");

        // Get system status
        results.Add($"\n5?? System Status:\n{GetSystemStatus()}");

        return string.Join("\n", results);
    }
}
