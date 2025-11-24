using AgentFramework.Models;
using AgentFramework.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgentFramework.Agents;

/// <summary>
/// Agent that analyzes and assigns priorities to tasks
/// </summary>
public class PriorityAnalyzerAgent
{
    private readonly TodoState _state;
    private readonly Kernel _kernel;
    public string Name => "PriorityAnalyzer";

    public PriorityAnalyzerAgent(TodoState state, Kernel kernel)
    {
        _state = state;
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Analyzes a task and assigns priority based on keywords, deadline, and description")]
    public string AnalyzePriority([Description("The ID of the todo item to analyze")] string todoId)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        var priority = DeterminePriority(todo);
        
        _state.UpdateTodo(todoId, t => t.Priority = priority);

        _state.EnqueueMessage(new AgentMessage
        {
            FromAgent = Name,
            ToAgent = "TodoManager",
            MessageType = MessageTypes.PriorityAssigned,
            Content = $"Priority assigned: {todo.Title} -> {priority}",
            Data = new Dictionary<string, object> 
            { 
                ["todoId"] = todoId,
                ["priority"] = priority.ToString()
            }
        });

        _state.LogActivity($"{Name}: Assigned {priority} priority to '{todo.Title}'");

        return $"? Analyzed and assigned {priority} priority to '{todo.Title}'";
    }

    [KernelFunction]
    [Description("Sets or updates the priority of a todo item manually")]
    public string SetPriority(
        [Description("The ID of the todo item")] string todoId,
        [Description("Priority level: low, medium, high, critical")] string priority)
    {
        if (!Enum.TryParse<TodoPriority>(priority, true, out var priorityEnum))
        {
            return $"Invalid priority: {priority}. Use: low, medium, high, or critical";
        }

        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        _state.UpdateTodo(todoId, t => t.Priority = priorityEnum);
        _state.LogActivity($"{Name}: Manually set {priorityEnum} priority for '{todo.Title}'");

        return $"? Priority set to {priorityEnum} for '{todo.Title}'";
    }

    [KernelFunction]
    [Description("Analyzes all pending tasks and suggests priority adjustments")]
    public string AnalyzeAllPriorities()
    {
        var todos = _state.GetAllTodos()
            .Where(t => t.Status == TodoStatus.Pending || t.Status == TodoStatus.InProgress)
            .ToList();

        if (todos.Count == 0)
        {
            return "No active tasks to analyze.";
        }

        var results = new List<string>();
        foreach (var todo in todos)
        {
            var suggestedPriority = DeterminePriority(todo);
            if (todo.Priority != suggestedPriority)
            {
                results.Add($"'{todo.Title}': {todo.Priority} -> {suggestedPriority}");
                _state.UpdateTodo(todo.Id, t => t.Priority = suggestedPriority);
            }
        }

        if (results.Count == 0)
        {
            return "? All task priorities are already optimal!";
        }

        return $"? Adjusted {results.Count} task priorities:\n" + string.Join("\n", results);
    }

    private TodoPriority DeterminePriority(TodoItem todo)
    {
        var score = 0;
        var text = $"{todo.Title} {todo.Description}".ToLower();

        // Keyword analysis
        if (text.Contains("urgent") || text.Contains("asap") || text.Contains("critical") || text.Contains("emergency"))
            score += 3;
        if (text.Contains("important") || text.Contains("priority") || text.Contains("high"))
            score += 2;
        if (text.Contains("bug") || text.Contains("error") || text.Contains("fix") || text.Contains("broken"))
            score += 2;
        if (text.Contains("security") || text.Contains("vulnerability") || text.Contains("breach"))
            score += 3;
        if (text.Contains("when possible") || text.Contains("low priority") || text.Contains("nice to have"))
            score -= 2;

        // Deadline urgency
        if (todo.DueDate.HasValue)
        {
            var daysUntilDue = (todo.DueDate.Value - DateTime.UtcNow).TotalDays;
            if (daysUntilDue < 1) score += 3;      // Due today or overdue
            else if (daysUntilDue < 3) score += 2; // Due within 3 days
            else if (daysUntilDue < 7) score += 1; // Due within a week
        }

        // Existing status
        if (todo.Status == TodoStatus.Blocked)
            score += 1; // Blocked items might need attention

        // Determine final priority
        return score switch
        {
            >= 5 => TodoPriority.Critical,
            >= 3 => TodoPriority.High,
            >= 1 => TodoPriority.Medium,
            _ => TodoPriority.Low
        };
    }

    /// <summary>
    /// Processes messages from other agents
    /// </summary>
    public void ProcessMessage(AgentMessage message)
    {
        if (message.MessageType == MessageTypes.TaskCreated && message.Data.ContainsKey("todoId"))
        {
            var todoId = message.Data["todoId"].ToString();
            if (!string.IsNullOrEmpty(todoId))
            {
                // Automatically analyze new tasks
                AnalyzePriority(todoId);
            }
        }
    }
}
