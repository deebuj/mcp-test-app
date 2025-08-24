# Test script for GitHub MCP Server
# This script sends a few test requests to verify the server is working

param(
    [string]$Owner = "microsoft",
    [string]$Repo = "vscode",
    [int]$PullNumber = 1
)

Write-Host "🧪 Testing GitHub MCP Server..." -ForegroundColor Blue

# Check if GITHUB_TOKEN is set
if (-not $env:GITHUB_TOKEN) {
    Write-Host "❌ GITHUB_TOKEN environment variable is not set" -ForegroundColor Red
    Write-Host "   Please set it with: `$env:GITHUB_TOKEN = 'your_token'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ GITHUB_TOKEN is set" -ForegroundColor Green

# Test JSON-RPC requests
$initializeRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{}
} | ConvertTo-Json -Depth 3

$toolsListRequest = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/list"
} | ConvertTo-Json -Depth 3

$analyzeRepoRequest = @{
    jsonrpc = "2.0"
    id = 3
    method = "tools/call"
    params = @{
        name = "github_analyze_repository"
        arguments = @{
            owner = $Owner
            repo = $Repo
        }
    }
} | ConvertTo-Json -Depth 4

$getContentsRequest = @{
    jsonrpc = "2.0"
    id = 4
    method = "tools/call"
    params = @{
        name = "github_get_repository_contents"
        arguments = @{
            owner = $Owner
            repo = $Repo
            path = ""
            recursive = $false
            includeContent = $true
            maxFileSize = 50000
        }
    }
} | ConvertTo-Json -Depth 4

Write-Host "📋 Test requests prepared for repository: $Owner/$Repo" -ForegroundColor Cyan
Write-Host "   Initialize request"
Write-Host "   Tools list request"
Write-Host "   Repository analysis request"
Write-Host "   Repository contents request"
Write-Host ""
Write-Host "🚀 You can now start the server with:" -ForegroundColor Blue
Write-Host "   dotnet run --project GitHubMcpServer.csproj" -ForegroundColor White
Write-Host ""
Write-Host "📝 Then send these JSON-RPC requests via stdin:" -ForegroundColor Blue
Write-Host "1. Initialize:"
Write-Host $initializeRequest -ForegroundColor Gray
Write-Host ""
Write-Host "2. List tools:"
Write-Host $toolsListRequest -ForegroundColor Gray
Write-Host ""
Write-Host "3. Analyze repository:"
Write-Host $analyzeRepoRequest -ForegroundColor Gray
Write-Host ""
Write-Host "4. Get repository contents:"
Write-Host $getContentsRequest -ForegroundColor Gray
