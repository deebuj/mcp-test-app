using GitHubMcpServer.Services;

namespace GitHubMcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var server = new McpServer();
                await server.RunAsync();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
