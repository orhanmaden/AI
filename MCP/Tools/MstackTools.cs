using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP.Tools;

[McpServerToolType]
public class MstackTools()
{
    [McpServerTool, Description("Get the mstack employees.")]
    public static string GetEmployees()
        => "Orhan and Willem";
}