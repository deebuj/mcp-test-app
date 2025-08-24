# Simple test for the GitHub MCP Server executable

Write-Host "Testing GitHub MCP Server Executable..." -ForegroundColor Blue

$executablePath = ".\Executable\GitHubMcpServer.exe"

if (-not (Test-Path $executablePath)) {
    Write-Host "ERROR: Executable not found at $executablePath" -ForegroundColor Red
    exit 1
}

if (-not $env:GITHUB_TOKEN) {
    Write-Host "ERROR: GITHUB_TOKEN environment variable is not set" -ForegroundColor Red
    exit 1
}

Write-Host "SUCCESS: Executable found and GitHub token is set" -ForegroundColor Green

# Test initialize
Write-Host "Testing initialize..." -ForegroundColor Yellow
$response = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' | & $executablePath
if ($response -like "*GitHub MCP Server*") {
    Write-Host "SUCCESS: Initialize test passed" -ForegroundColor Green
} else {
    Write-Host "ERROR: Initialize test failed" -ForegroundColor Red
}

Write-Host "Executable is ready for MCP integration!" -ForegroundColor Green
