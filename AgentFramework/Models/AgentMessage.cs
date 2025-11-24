namespace AgentFramework.Models;

/// <summary>
/// Represents a message passed between agents
/// </summary>
public class AgentMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromAgent { get; set; } = string.Empty;
    public string ToAgent { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Common message types for agent communication
/// </summary>
public static class MessageTypes
{
    public const string TaskCreated = "task_created";
    public const string TaskUpdated = "task_updated";
    public const string TaskCompleted = "task_completed";
    public const string PriorityAssigned = "priority_assigned";
    public const string DeadlineAlert = "deadline_alert";
    public const string TaskDecomposed = "task_decomposed";
    public const string AgentRequest = "agent_request";
    public const string AgentResponse = "agent_response";
}
