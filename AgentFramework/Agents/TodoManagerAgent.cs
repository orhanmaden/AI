using AgentFramework.Models;
using AgentFramework.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgentFramework.Agents;

/// <summary>
/// Main agent that coordinates todo list management
/// </summary>
public class TodoManagerAgent
{
    private readonly TodoState _state;
    private readonly Kernel _kernel;
    public string Name => "TodoManager";

    public TodoManagerAgent(TodoState state, Kernel kernel)
    {
        _state = state;
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Creates a new todo item")]
    public string CreateTodo(
        [Description("The title of the todo item")] string title,
        [Description("Optional description of the todo item")] string? description = null,
        [Description("Optional due date in ISO format (YYYY-MM-DD)")] string? dueDate = null)
    {
        var todo = new TodoItem
        {
            Title = title,
            Description = description ?? string.Empty,
            DueDate = string.IsNullOrEmpty(dueDate) ? null : DateTime.Parse(dueDate),
            AssignedAgent = Name
        };

        _state.AddTodo(todo);

        // Notify other agents
        _state.EnqueueMessage(new AgentMessage
        {
            FromAgent = Name,
            ToAgent = "PriorityAnalyzer",
            MessageType = MessageTypes.TaskCreated,
            Content = $"New task created: {title}",
            Data = new Dictionary<string, object> { ["todoId"] = todo.Id }
        });

        if (todo.DueDate.HasValue)
        {
            _state.EnqueueMessage(new AgentMessage
            {
                FromAgent = Name,
                ToAgent = "DeadlineMonitor",
                MessageType = MessageTypes.TaskCreated,
                Content = $"Task with deadline: {title}",
                Data = new Dictionary<string, object> { ["todoId"] = todo.Id }
            });
        }

        return $"? Todo created successfully! ID: {todo.Id}, Title: '{title}'";
    }

    [KernelFunction]
    [Description("Lists all todo items or filters by status")]
    public string ListTodos(
        [Description("Optional filter by status: pending, inprogress, completed, blocked, cancelled")] string? status = null)
    {
        var todos = _state.GetAllTodos();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<TodoStatus>(status, true, out var statusEnum))
            {
                todos = todos.Where(t => t.Status == statusEnum).ToList();
            }
        }

        if (todos.Count == 0)
        {
            return "No todos found.";
        }

        var result = $"Found {todos.Count} todo(s):\n";
        foreach (var todo in todos.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate))
        {
            result += $"\n[{todo.Priority}] {todo.Title}";
            result += $"\n  ID: {todo.Id}";
            result += $"\n  Status: {todo.Status}";
            if (todo.DueDate.HasValue)
            {
                result += $"\n  Due: {todo.DueDate:yyyy-MM-dd}";
            }
            if (todo.SubTasks.Count > 0)
            {
                result += $"\n  Subtasks: {todo.SubTasks.Count}";
            }
            result += "\n";
        }

        return result;
    }

    [KernelFunction]
    [Description("Updates the status of a todo item")]
    public string UpdateTodoStatus(
        [Description("The ID of the todo item")] string todoId,
        [Description("New status: pending, inprogress, completed, blocked, cancelled")] string status)
    {
        if (!Enum.TryParse<TodoStatus>(status, true, out var statusEnum))
        {
            return $"Invalid status: {status}";
        }

        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        _state.UpdateTodo(todoId, t =>
        {
            t.Status = statusEnum;
            if (statusEnum == TodoStatus.Completed)
            {
                t.CompletedAt = DateTime.UtcNow;
            }
        });

        _state.EnqueueMessage(new AgentMessage
        {
            FromAgent = Name,
            ToAgent = "All",
            MessageType = MessageTypes.TaskUpdated,
            Content = $"Task status updated: {todo.Title} -> {statusEnum}",
            Data = new Dictionary<string, object> { ["todoId"] = todoId }
        });

        return $"? Todo '{todo.Title}' status updated to {statusEnum}";
    }

    [KernelFunction]
    [Description("Deletes a todo item")]
    public string DeleteTodo([Description("The ID of the todo item to delete")] string todoId)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        var title = todo.Title;
        _state.RemoveTodo(todoId);
        return $"? Todo '{title}' deleted successfully";
    }

    [KernelFunction]
    [Description("Gets detailed information about a specific todo item")]
    public string GetTodoDetails([Description("The ID of the todo item")] string todoId)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        var details = $"Todo Details:\n";
        details += $"  Title: {todo.Title}\n";
        details += $"  ID: {todo.Id}\n";
        details += $"  Description: {todo.Description}\n";
        details += $"  Priority: {todo.Priority}\n";
        details += $"  Status: {todo.Status}\n";
        details += $"  Created: {todo.CreatedAt:yyyy-MM-dd HH:mm}\n";
        
        if (todo.DueDate.HasValue)
        {
            details += $"  Due Date: {todo.DueDate:yyyy-MM-dd}\n";
        }
        
        if (todo.CompletedAt.HasValue)
        {
            details += $"  Completed: {todo.CompletedAt:yyyy-MM-dd HH:mm}\n";
        }

        if (todo.SubTasks.Count > 0)
        {
            details += $"  Subtasks ({todo.SubTasks.Count}):\n";
            foreach (var subtask in todo.SubTasks)
            {
                details += $"    - {subtask}\n";
            }
        }

        return details;
    }
}
