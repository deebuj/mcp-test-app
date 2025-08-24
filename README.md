# GitHub MCP Server

A Model Context Protocol (MCP) server written in C# that provides GitHub integration tools for AI assistants.

## Features

The server provides the following tools:

1. **github_analyze_repository**: Analyze a GitHub repository and provide a comprehensive summary including:
   - Repository metadata (stars, forks, language, etc.)
   - Project structure analysis
   - Language breakdown
   - Key files identification
   - README content (if available)

2. **github_review_pull_request**: Review GitHub pull requests with detailed analysis:
   - Pull request metadata and status
   - Files changed with diff summaries
   - Commit history
   - Change analysis (additions, deletions, file types)
   - Automated review suggestions

3. **github_get_repository_contents**: Get raw repository contents for client-side analysis:
   - File and directory structure
   - Raw file contents (for text files)
   - Configurable recursion depth
   - File size filtering
   - Binary file detection

## Setup

### Prerequisites

- .NET 8.0 SDK
- GitHub Personal Access Token (classic)

### Installation

1. Clone or download this repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```

### GitHub Token Setup

1. Create a GitHub Personal Access Token (classic) with the following scopes:
   - `repo` (for repository access)
   - `read:org` (for organization repositories, if needed)

2. Set the environment variable:
   ```bash
   # Windows (PowerShell)
   $env:GITHUB_TOKEN = "your_github_token_here"
   
   # Windows (Command Prompt)
   set GITHUB_TOKEN=your_github_token_here
   
   # Linux/macOS
   export GITHUB_TOKEN=your_github_token_here
   ```

### VS Code Integration

To integrate with GitHub Copilot in VS Code:

1. Copy the `.vscode/mcp.json` file to your VS Code workspace
2. Update the project path in the configuration if needed
3. Ensure the `GITHUB_TOKEN` environment variable is set
4. Restart VS Code

The MCP configuration in `.vscode/mcp.json`:
```json
{
  "servers": {
    "github-mcp-server": {
      "type": "stdio",
      "command": "c:\\Dev\\MyApps\\MCP\\mcp-test-app\\Executable\\GitHubMcpServer.exe",
      "args": [],
      "env": {
        "GITHUB_TOKEN": "${env:GITHUB_TOKEN}"
      }
    }
  },
  "inputs": []
}
```

## Usage

### Running the Server

#### Option 1: Using the Executable (Recommended)
```bash
# Build the executable (run once)
.\build-executable.ps1

# Test the executable
.\test-simple.ps1

# The executable is located at:
.\Executable\GitHubMcpServer.exe
```

#### Option 2: Using .NET CLI
```bash
dotnet run
```

The server communicates via JSON-RPC over stdin/stdout.

### Tool Examples

#### Analyze Repository
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "github_analyze_repository",
    "arguments": {
      "owner": "microsoft",
      "repo": "vscode"
    }
  }
}
```

#### Review Pull Request
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "github_review_pull_request",
    "arguments": {
      "owner": "microsoft",
      "repo": "vscode",
      "pullNumber": 123
    }
  }
}
```

#### Get Repository Contents
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "github_get_repository_contents",
    "arguments": {
      "owner": "microsoft",
      "repo": "vscode",
      "path": "src",
      "recursive": true,
      "includeContent": true,
      "maxFileSize": 50000
    }
  }
}
```

**Parameters for github_get_repository_contents:**
- `owner` (required): Repository owner
- `repo` (required): Repository name  
- `path` (optional): Path within repository (default: root)
- `recursive` (optional): Recursively fetch subdirectories (default: false)
- `includeContent` (optional): Include file contents (default: true)
- `maxFileSize` (optional): Max file size in bytes to include content (default: 100KB)

## Architecture

The project is structured for extensibility:

- `Models/McpModels.cs`: MCP protocol data models
- `Tools/GitHubTools.cs`: GitHub-specific tool implementations
- `Services/McpServer.cs`: Core MCP server logic
- `Program.cs`: Application entry point

### Adding New Tools

To add new GitHub tools:

1. Implement the `IGitHubTool` interface
2. Add the tool to the `_tools` dictionary in `McpServer.cs`
3. The tool will automatically be available via the MCP protocol

### Adding Non-GitHub Tools

For tools that don't use GitHub:

1. Create a new interface similar to `IGitHubTool`
2. Implement your tools using that interface
3. Modify `McpServer.cs` to include the new tools
4. Consider creating separate service classes for different APIs

## Dependencies

- **Octokit**: GitHub API client library
- **System.Text.Json**: JSON serialization
- **Newtonsoft.Json**: Additional JSON support

## Error Handling

The server includes comprehensive error handling:
- GitHub API errors are caught and returned as tool errors
- Invalid requests return appropriate JSON-RPC error responses
- Missing environment variables cause startup failures with clear messages
- **Fixed**: Argument type casting issues when JSON parameters contain JsonElement objects
- Robust parameter parsing for boolean, integer, and string arguments

## Security Notes

- Store your GitHub token securely
- Use environment variables, not hardcoded tokens
- Consider token rotation policies
- The token should have minimal required permissions
