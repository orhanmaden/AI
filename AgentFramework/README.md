# Multi-Agent TODO List System

A sophisticated TODO list management system powered by **Microsoft Semantic Kernel** and **OpenAI** that demonstrates multi-agent collaboration using AI.

## ?? Agents

The system includes four specialized agents that work together:

### 1. **TodoManager Agent**
- Creates, reads, updates, and deletes TODO items
- Manages task status (pending, in progress, completed, blocked, cancelled)
- Coordinates communication between all agents

### 2. **PriorityAnalyzer Agent**
- Automatically analyzes new tasks and assigns priorities
- Uses intelligent keyword analysis and deadline urgency scoring
- Identifies high-priority, urgent, and critical tasks
- Can re-analyze all tasks to optimize priorities

### 3. **DeadlineMonitor Agent**
- Tracks task deadlines and due dates
- Sends alerts for overdue and upcoming deadlines
- Provides deadline summaries (overdue, due today, this week, later)
- Monitors tasks with urgent deadlines

### 4. **TaskDecomposer Agent**
- Breaks down complex tasks into manageable subtasks using AI
- Identifies tasks that would benefit from decomposition
- Suggests task breakdowns for project planning
- Helps with task planning and organization

## ??? Architecture

```
AgentFramework/
??? Program.cs                    # Main entry point with interactive CLI
??? AgentFramework.csproj         # Project file (.NET 10, C# 14)
??? Agents/
?   ??? TodoManagerAgent.cs       # CRUD operations
?   ??? PriorityAnalyzerAgent.cs  # Priority analysis
?   ??? DeadlineMonitorAgent.cs   # Deadline tracking
?   ??? TaskDecomposerAgent.cs    # Task decomposition
??? Models/
?   ??? TodoItem.cs               # Todo data model
?   ??? AgentMessage.cs           # Inter-agent messaging
??? Services/
    ??? TodoState.cs              # Shared state management
    ??? AgentOrchestrator.cs      # Agent coordination
```

## ?? Getting Started

### Prerequisites
- .NET 10.0 SDK
- OpenAI API Key

### Configuration

1. Set your OpenAI API key using user secrets:
```bash
cd AgentFramework
dotnet user-secrets set "CHAT_GPT_API_KEY" "your-openai-api-key-here"
```

2. Run the application:
```bash
dotnet run
```

## ?? Example Commands

### Task Management
- `Create a task to build a website by next Friday`
- `Add a todo: Fix the login bug`
- `List all my tasks`
- `Show me pending tasks`
- `Update task <ID> status to completed`
- `Delete task <ID>`

### Priority Management
- `Analyze priorities of all tasks`
- `Set priority of task <ID> to high`
- `Which tasks need priority review?`

### Deadline Management
- `Check my deadlines`
- `Show me overdue tasks`
- `Set deadline for task <ID> to 2024-12-31`
- `Give me a deadline summary`

### Task Decomposition
- `Break down task <ID> into subtasks`
- `Identify complex tasks that need breakdown`
- `Suggest breakdown for: Build e-commerce platform`

### System Commands
- `status` - Show system status
- `demo` - Run demonstration scenario
- `logs` / `activity` - Show activity log
- `help` - Show help
- `quit` / `exit` - Exit the application

## ?? How Agents Collaborate

1. **User creates a task** ? TodoManager creates it
2. **TodoManager** sends message to PriorityAnalyzer
3. **PriorityAnalyzer** automatically analyzes and assigns priority
4. If task has a deadline ? **DeadlineMonitor** is notified
5. If task is complex ? **TaskDecomposer** can break it down
6. Agents communicate through a shared message queue
7. **AgentOrchestrator** coordinates all agent interactions

## ?? Features

- ? Intelligent priority assignment based on keywords and deadlines
- ? Automatic deadline monitoring with alerts
- ? AI-powered task decomposition
- ? Inter-agent communication and collaboration
- ? Shared state management across agents
- ? Activity logging for audit trail
- ? Natural language interface
- ? Automatic function calling via Semantic Kernel

## ?? Key Technologies

- **Microsoft Semantic Kernel 1.67.1** - Agent framework and orchestration
- **OpenAI GPT-4** - Natural language understanding and task decomposition
- **.NET 10** with C# 14 - Modern language features
- **Dependency Injection** - Clean architecture
- **User Secrets** - Secure API key management

## ?? Demo Scenario

Type `demo` in the application to see a complete demonstration of:
1. Creating a complex task
2. Automatic priority analysis
3. Task decomposition into subtasks
4. Creating an urgent task
5. Deadline monitoring and alerts
6. System status reporting

## ?? License

This is a demonstration project for learning Microsoft Agent Framework and Semantic Kernel.
