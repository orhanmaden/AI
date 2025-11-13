using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP.Tools;

[McpServerToolType]
public class ProgrammerTools()
{
    [McpServerTool, Description("Get the programmer greeting.")]
    public static string GetGreeting()
        => "Hellow1 World";
}