using AgentFramework.Models;
using AgentFramework.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgentFramework.Agents;

/// <summary>
/// Agent that monitors deadlines and sends alerts
/// </summary>
public class DeadlineMonitorAgent
{
    private readonly TodoState _state;
    private readonly Kernel _kernel;
    public string Name => "DeadlineMonitor";

    public DeadlineMonitorAgent(TodoState state, Kernel kernel)
    {
        _state = state;
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Checks all tasks for upcoming or overdue deadlines")]
    public string CheckDeadlines()
    {
        var todos = _state.GetAllTodos()
            .Where(t => t.DueDate.HasValue && t.Status != TodoStatus.Completed && t.Status != TodoStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ToList();

        if (todos.Count == 0)
        {
            return "No active tasks with deadlines.";
        }

        var now = DateTime.UtcNow;
        var overdue = new List<string>();
        var dueToday = new List<string>();
        var dueSoon = new List<string>();
        var upcoming = new List<string>();

        foreach (var todo in todos)
        {
            var daysUntilDue = (todo.DueDate!.Value - now).TotalDays;
            var status = $"'{todo.Title}' (Due: {todo.DueDate:yyyy-MM-dd}, Priority: {todo.Priority})";

            if (daysUntilDue < 0)
            {
                overdue.Add($"?? OVERDUE by {Math.Abs(daysUntilDue):F0} days: {status}");
                SendDeadlineAlert(todo, "OVERDUE");
            }
            else if (daysUntilDue < 1)
            {
                dueToday.Add($"?? DUE TODAY: {status}");
                SendDeadlineAlert(todo, "DUE TODAY");
            }
            else if (daysUntilDue < 3)
            {
                dueSoon.Add($"?? Due in {daysUntilDue:F0} days: {status}");
                SendDeadlineAlert(todo, "DUE SOON");
            }
            else if (daysUntilDue < 7)
            {
                upcoming.Add($"?? Due in {daysUntilDue:F0} days: {status}");
            }
        }

        var result = "Deadline Status Report:\n\n";
        
        if (overdue.Count > 0)
        {
            result += "OVERDUE TASKS:\n" + string.Join("\n", overdue) + "\n\n";
        }
        
        if (dueToday.Count > 0)
        {
            result += "DUE TODAY:\n" + string.Join("\n", dueToday) + "\n\n";
        }
        
        if (dueSoon.Count > 0)
        {
            result += "DUE SOON (Within 3 days):\n" + string.Join("\n", dueSoon) + "\n\n";
        }
        
        if (upcoming.Count > 0)
        {
            result += "UPCOMING (Within 7 days):\n" + string.Join("\n", upcoming) + "\n";
        }

        if (overdue.Count == 0 && dueToday.Count == 0 && dueSoon.Count == 0 && upcoming.Count == 0)
        {
            result += "? All deadlines are more than a week away!";
        }

        return result;
    }

    [KernelFunction]
    [Description("Gets tasks that are overdue")]
    public string GetOverdueTasks()
    {
        var todos = _state.GetAllTodos()
            .Where(t => t.DueDate.HasValue 
                && t.DueDate.Value < DateTime.UtcNow 
                && t.Status != TodoStatus.Completed 
                && t.Status != TodoStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ToList();

        if (todos.Count == 0)
        {
            return "? No overdue tasks!";
        }

        var result = $"Found {todos.Count} overdue task(s):\n\n";
        foreach (var todo in todos)
        {
            var daysOverdue = (DateTime.UtcNow - todo.DueDate!.Value).TotalDays;
            result += $"?? '{todo.Title}'\n";
            result += $"   Due: {todo.DueDate:yyyy-MM-dd} ({daysOverdue:F0} days overdue)\n";
            result += $"   Priority: {todo.Priority}\n";
            result += $"   Status: {todo.Status}\n\n";
        }

        return result;
    }

    [KernelFunction]
    [Description("Sets a deadline for a specific task")]
    public string SetDeadline(
        [Description("The ID of the todo item")] string todoId,
        [Description("The deadline in ISO format (YYYY-MM-DD)")] string deadline)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        if (!DateTime.TryParse(deadline, out var deadlineDate))
        {
            return $"Invalid date format: {deadline}. Use YYYY-MM-DD format.";
        }

        _state.UpdateTodo(todoId, t => t.DueDate = deadlineDate);
        _state.LogActivity($"{Name}: Set deadline {deadlineDate:yyyy-MM-dd} for '{todo.Title}'");

        // Check if it's urgent
        var daysUntilDue = (deadlineDate - DateTime.UtcNow).TotalDays;
        if (daysUntilDue < 3)
        {
            SendDeadlineAlert(todo, daysUntilDue < 1 ? "URGENT - Due within 24 hours" : "Due soon");
        }

        return $"? Deadline set to {deadlineDate:yyyy-MM-dd} for '{todo.Title}'";
    }

    [KernelFunction]
    [Description("Gets a summary of all tasks grouped by deadline urgency")]
    public string GetDeadlineSummary()
    {
        var todos = _state.GetAllTodos()
            .Where(t => t.DueDate.HasValue && t.Status != TodoStatus.Completed && t.Status != TodoStatus.Cancelled)
            .ToList();

        if (todos.Count == 0)
        {
            return "No active tasks with deadlines.";
        }

        var now = DateTime.UtcNow;
        var overdue = todos.Count(t => t.DueDate!.Value < now);
        var today = todos.Count(t => t.DueDate!.Value.Date == now.Date);
        var thisWeek = todos.Count(t => t.DueDate!.Value > now && t.DueDate.Value <= now.AddDays(7));
        var later = todos.Count(t => t.DueDate!.Value > now.AddDays(7));

        var result = "?? Deadline Summary:\n\n";
        result += $"?? Overdue: {overdue}\n";
        result += $"?? Due today: {today}\n";
        result += $"?? Due this week: {thisWeek}\n";
        result += $"?? Due later: {later}\n";
        result += $"\nTotal active tasks with deadlines: {todos.Count}";

        return result;
    }

    private void SendDeadlineAlert(TodoItem todo, string alertType)
    {
        _state.EnqueueMessage(new AgentMessage
        {
            FromAgent = Name,
            ToAgent = "TodoManager",
            MessageType = MessageTypes.DeadlineAlert,
            Content = $"{alertType}: {todo.Title}",
            Data = new Dictionary<string, object>
            {
                ["todoId"] = todo.Id,
                ["alertType"] = alertType,
                ["dueDate"] = todo.DueDate!.Value
            }
        });

        _state.LogActivity($"{Name}: Sent {alertType} alert for '{todo.Title}'");
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
                var todo = _state.GetTodo(todoId);
                if (todo?.DueDate.HasValue == true)
                {
                    // Check if newly created task has urgent deadline
                    var daysUntilDue = (todo.DueDate.Value - DateTime.UtcNow).TotalDays;
                    if (daysUntilDue < 3)
                    {
                        SendDeadlineAlert(todo, "New task with urgent deadline");
                    }
                }
            }
        }
    }
}
