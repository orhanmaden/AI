using AgentFramework.Models;
using System.Collections.Concurrent;

namespace AgentFramework.Services;

/// <summary>
/// Shared state management for all agents
/// </summary>
public class TodoState
{
    private readonly ConcurrentDictionary<string, TodoItem> _todos = new();
    private readonly ConcurrentQueue<AgentMessage> _messageQueue = new();
    private readonly ConcurrentQueue<string> _activityLog = new();

    public IReadOnlyDictionary<string, TodoItem> Todos => _todos;

    public void AddTodo(TodoItem todo)
    {
        _todos.TryAdd(todo.Id, todo);
        LogActivity($"Todo created: {todo.Title} (ID: {todo.Id})");
    }

    public void UpdateTodo(string id, Action<TodoItem> updateAction)
    {
        if (_todos.TryGetValue(id, out var todo))
        {
            updateAction(todo);
            LogActivity($"Todo updated: {todo.Title} (ID: {id})");
        }
    }

    public TodoItem? GetTodo(string id)
    {
        _todos.TryGetValue(id, out var todo);
        return todo;
    }

    public List<TodoItem> GetAllTodos()
    {
        return _todos.Values.ToList();
    }

    public void RemoveTodo(string id)
    {
        if (_todos.TryRemove(id, out var todo))
        {
            LogActivity($"Todo removed: {todo.Title} (ID: {id})");
        }
    }

    public void EnqueueMessage(AgentMessage message)
    {
        _messageQueue.Enqueue(message);
        LogActivity($"Message queued: {message.FromAgent} -> {message.ToAgent} ({message.MessageType})");
    }

    public bool TryDequeueMessage(out AgentMessage? message)
    {
        return _messageQueue.TryDequeue(out message);
    }

    public void LogActivity(string activity)
    {
        _activityLog.Enqueue($"[{DateTime.UtcNow:HH:mm:ss}] {activity}");
    }

    public List<string> GetActivityLog()
    {
        return _activityLog.ToList();
    }

    public void ClearActivityLog()
    {
        while (_activityLog.TryDequeue(out _))
        {
            // Drain the queue
        }
    }
}
