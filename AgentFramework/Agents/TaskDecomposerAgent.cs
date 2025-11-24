using AgentFramework.Models;
using AgentFramework.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgentFramework.Agents;

/// <summary>
/// Agent that breaks down complex tasks into manageable subtasks
/// </summary>
public class TaskDecomposerAgent
{
    private readonly TodoState _state;
    private readonly Kernel _kernel;
    public string Name => "TaskDecomposer";

    public TaskDecomposerAgent(TodoState state, Kernel kernel)
    {
        _state = state;
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Analyzes a task and breaks it down into subtasks")]
    public async Task<string> DecomposeTask([Description("The ID of the todo item to decompose")] string todoId)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        // Use AI to generate subtasks based on the task title and description
        var subtasks = await GenerateSubtasksWithAI(todo);

        _state.UpdateTodo(todoId, t =>
        {
            t.SubTasks.AddRange(subtasks);
        });

        _state.EnqueueMessage(new AgentMessage
        {
            FromAgent = Name,
            ToAgent = "TodoManager",
            MessageType = MessageTypes.TaskDecomposed,
            Content = $"Task decomposed: {todo.Title} into {subtasks.Count} subtasks",
            Data = new Dictionary<string, object>
            {
                ["todoId"] = todoId,
                ["subtaskCount"] = subtasks.Count
            }
        });

        _state.LogActivity($"{Name}: Decomposed '{todo.Title}' into {subtasks.Count} subtasks");

        var result = $"? Decomposed '{todo.Title}' into {subtasks.Count} subtasks:\n";
        for (int i = 0; i < subtasks.Count; i++)
        {
            result += $"{i + 1}. {subtasks[i]}\n";
        }

        return result;
    }

    [KernelFunction]
    [Description("Adds a custom subtask to a todo item")]
    public string AddSubtask(
        [Description("The ID of the todo item")] string todoId,
        [Description("The subtask description")] string subtask)
    {
        var todo = _state.GetTodo(todoId);
        if (todo == null)
        {
            return $"Todo not found with ID: {todoId}";
        }

        _state.UpdateTodo(todoId, t => t.SubTasks.Add(subtask));
        _state.LogActivity($"{Name}: Added subtask to '{todo.Title}': {subtask}");

        return $"? Subtask added to '{todo.Title}': {subtask}";
    }

    [KernelFunction]
    [Description("Suggests task breakdown for complex projects")]
    public async Task<string> SuggestBreakdown([Description("Description of the complex task or project")] string taskDescription)
    {
        var prompt = $@"Break down the following task into 3-7 actionable subtasks. 
Each subtask should be specific, measurable, and achievable.
Return only the list of subtasks, one per line, without numbering.

Task: {taskDescription}

Subtasks:";

        try
        {
            var response = await _kernel.InvokePromptAsync(prompt);
            var subtasks = response.ToString()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimStart('-', '*', ' ', '\t'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var result = $"?? Suggested breakdown for '{taskDescription}':\n\n";
            for (int i = 0; i < subtasks.Count; i++)
            {
                result += $"{i + 1}. {subtasks[i]}\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            _state.LogActivity($"{Name}: Error generating breakdown: {ex.Message}");
            return GenerateBasicBreakdown(taskDescription);
        }
    }

    [KernelFunction]
    [Description("Analyzes all tasks and identifies which ones need decomposition")]
    public string IdentifyComplexTasks()
    {
        var todos = _state.GetAllTodos()
            .Where(t => t.Status == TodoStatus.Pending || t.Status == TodoStatus.InProgress)
            .ToList();

        var complexTasks = todos
            .Where(t => IsComplexTask(t) && t.SubTasks.Count == 0)
            .ToList();

        if (complexTasks.Count == 0)
        {
            return "? No complex tasks found that need decomposition.";
        }

        var result = $"Found {complexTasks.Count} complex task(s) that could benefit from decomposition:\n\n";
        foreach (var task in complexTasks)
        {
            result += $"• '{task.Title}' (ID: {task.Id})\n";
            result += $"  Reason: {GetComplexityReason(task)}\n\n";
        }

        return result;
    }

    private async Task<List<string>> GenerateSubtasksWithAI(TodoItem todo)
    {
        var prompt = $@"Break down the following task into 3-5 clear, actionable subtasks.
Each subtask should be specific and achievable.
Return only the list of subtasks, one per line, without numbering or bullet points.

Task Title: {todo.Title}
Description: {todo.Description}

Subtasks:";

        try
        {
            var response = await _kernel.InvokePromptAsync(prompt);
            var subtasks = response.ToString()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.TrimStart('-', '*', ' ', '\t', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(7) // Maximum 7 subtasks
                .ToList();

            if (subtasks.Count == 0)
            {
                return GenerateBasicSubtasks(todo);
            }

            return subtasks;
        }
        catch (Exception ex)
        {
            _state.LogActivity($"{Name}: AI decomposition failed, using rule-based approach: {ex.Message}");
            return GenerateBasicSubtasks(todo);
        }
    }

    private List<string> GenerateBasicSubtasks(TodoItem todo)
    {
        // Fallback rule-based decomposition
        var subtasks = new List<string>
        {
            $"Research and plan: {todo.Title}",
            $"Gather required resources for {todo.Title}",
            $"Execute main work for {todo.Title}",
            $"Review and test {todo.Title}",
            $"Finalize and complete {todo.Title}"
        };

        return subtasks;
    }

    private string GenerateBasicBreakdown(string taskDescription)
    {
        var result = $"?? Basic suggested breakdown for '{taskDescription}':\n\n";
        result += $"1. Research and plan the approach\n";
        result += $"2. Gather required resources and tools\n";
        result += $"3. Execute the main work\n";
        result += $"4. Review and test the results\n";
        result += $"5. Finalize and document\n";
        return result;
    }

    private bool IsComplexTask(TodoItem todo)
    {
        var text = $"{todo.Title} {todo.Description}".ToLower();
        
        // Check for complexity indicators
        var complexityKeywords = new[] 
        { 
            "implement", "develop", "create", "build", "design", "refactor",
            "migrate", "upgrade", "integrate", "project", "system", "feature"
        };

        var hasComplexityKeyword = complexityKeywords.Any(k => text.Contains(k));
        var isLongDescription = todo.Description.Length > 100;
        var hasMultipleSteps = text.Contains("and") || text.Contains("then") || text.Contains("also");

        return hasComplexityKeyword || isLongDescription || hasMultipleSteps;
    }

    private string GetComplexityReason(TodoItem todo)
    {
        var reasons = new List<string>();
        var text = $"{todo.Title} {todo.Description}".ToLower();

        if (text.Contains("implement") || text.Contains("develop") || text.Contains("build"))
            reasons.Add("development task");
        if (todo.Description.Length > 100)
            reasons.Add("detailed description");
        if (text.Contains("and") || text.Contains("then"))
            reasons.Add("multiple steps implied");
        if (todo.Priority == TodoPriority.High || todo.Priority == TodoPriority.Critical)
            reasons.Add("high priority");

        return reasons.Count > 0 ? string.Join(", ", reasons) : "appears complex";
    }
}
