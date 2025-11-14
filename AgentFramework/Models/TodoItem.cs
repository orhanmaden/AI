namespace AgentFramework.Models;

/// <summary>
/// Represents a todo item in the system
/// </summary>
public class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public TodoStatus Status { get; set; } = TodoStatus.Pending;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<string> SubTasks { get; set; } = new();
    public string? AssignedAgent { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum TodoPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TodoStatus
{
    Pending,
    InProgress,
    Completed,
    Blocked,
    Cancelled
}
