using GitHubMcpServer.Models;
using GitHubMcpServer.Tools;
using System.Text.Json;
using Octokit;

namespace GitHubMcpServer.Services
{
    public class McpServer
    {
        private readonly Dictionary<string, IGitHubTool> _tools;
        private readonly GitHubClient _gitHubClient;

        public McpServer()
        {
            // Initialize GitHub client with token from environment
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("GITHUB_TOKEN environment variable is required");
            }

            _gitHubClient = new GitHubClient(new ProductHeaderValue("GitHubMcpServer"))
            {
                Credentials = new Credentials(token)
            };

            // Initialize tools
            _tools = new Dictionary<string, IGitHubTool>
            {
                { "github_analyze_repository", new GitHubRepositoryAnalyzerTool(_gitHubClient) },
                { "github_review_pull_request", new GitHubPullRequestReviewerTool(_gitHubClient) },
                { "github_get_repository_contents", new GitHubRepositoryContentsTool(_gitHubClient) }
            };
        }

        public async Task RunAsync()
        {
            await Console.Error.WriteLineAsync("GitHub MCP Server starting...");
            
            string? line;
            while ((line = await Console.In.ReadLineAsync()) != null)
            {
                try
                {
                    var request = JsonSerializer.Deserialize<McpRequest>(line);
                    if (request != null)
                    {
                        var response = await HandleRequestAsync(request);
                        var responseJson = JsonSerializer.Serialize(response);
                        await Console.Out.WriteLineAsync(responseJson);
                    }
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Error processing request: {ex.Message}");
                    var errorResponse = new McpResponse
                    {
                        Id = null,
                        Error = new McpError
                        {
                            Code = -32603,
                            Message = "Internal error",
                            Data = ex.Message
                        }
                    };
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                }
            }
        }

        private async Task<McpResponse> HandleRequestAsync(McpRequest request)
        {
            var response = new McpResponse { Id = request.Id };

            try
            {
                switch (request.Method)
                {
                    case "initialize":
                        response.Result = HandleInitialize();
                        break;

                    case "tools/list":
                        response.Result = HandleToolsList();
                        break;

                    case "tools/call":
                        response.Result = await HandleToolCallAsync(request.Params);
                        break;

                    case "notifications/initialized":
                        // Client acknowledges initialization
                        response.Result = new { };
                        break;

                    default:
                        response.Error = new McpError
                        {
                            Code = -32601,
                            Message = $"Method not found: {request.Method}"
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                };
            }

            return response;
        }

        private InitializeResult HandleInitialize()
        {
            return new InitializeResult
            {
                ServerInfo = new ServerInfo
                {
                    Name = "GitHub MCP Server",
                    Version = "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new { }
                }
            };
        }

        private object HandleToolsList()
        {
            var tools = _tools.Values.Select(tool => new ToolInfo
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema
            }).ToArray();

            return new { tools };
        }

        private async Task<object> HandleToolCallAsync(JsonElement? paramsElement)
        {
            if (!paramsElement.HasValue)
            {
                throw new ArgumentException("Missing parameters for tool call");
            }

            var toolCallRequest = JsonSerializer.Deserialize<ToolCallRequest>(paramsElement.Value.GetRawText());
            if (toolCallRequest == null || string.IsNullOrEmpty(toolCallRequest.Name))
            {
                throw new ArgumentException("Invalid tool call request");
            }

            if (!_tools.TryGetValue(toolCallRequest.Name, out var tool))
            {
                throw new ArgumentException($"Unknown tool: {toolCallRequest.Name}");
            }

            try
            {
                var result = await tool.ExecuteAsync(toolCallRequest.Arguments);
                var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                return new ToolCallResponse
                {
                    Content = new List<ContentBlock>
                    {
                        new ContentBlock
                        {
                            Type = "text",
                            Text = resultJson
                        }
                    },
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new ToolCallResponse
                {
                    Content = new List<ContentBlock>
                    {
                        new ContentBlock
                        {
                            Type = "text",
                            Text = $"Error executing tool: {ex.Message}"
                        }
                    },
                    IsError = true
                };
            }
        }
    }
}
