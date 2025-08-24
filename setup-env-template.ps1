# GitHub MCP Server Environment Setup
# Copy this file to setup-env.ps1 and update with your GitHub token

# Set your GitHub Personal Access Token here
# Get your token from: https://github.com/settings/tokens
$env:GITHUB_TOKEN = "your_github_token_here"

# Verify the token is set
if ($env:GITHUB_TOKEN -eq "your_github_token_here") {
    Write-Host "⚠️  Please update the GITHUB_TOKEN in this script with your actual token" -ForegroundColor Yellow
    Write-Host "   Get your token from: https://github.com/settings/tokens" -ForegroundColor Cyan
} else {
    Write-Host "✅ GITHUB_TOKEN environment variable is set" -ForegroundColor Green
    
    # Test the server
    Write-Host "🚀 Starting GitHub MCP Server..." -ForegroundColor Blue
    dotnet run --project GitHubMcpServer.csproj
}
