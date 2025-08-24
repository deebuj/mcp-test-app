# Test script for the GitHub MCP Server executable

param(
    [string]$Owner = "microsoft",
    [string]$Repo = "vscode"
)

$executablePath = ".\Executable\GitHubMcpServer.exe"

Write-Host "🧪 Testing GitHub MCP Server Executable..." -ForegroundColor Blue
Write-Host "📍 Executable: $executablePath" -ForegroundColor Cyan

# Check if executable exists
if (-not (Test-Path $executablePath)) {
    Write-Host "❌ Executable not found at $executablePath" -ForegroundColor Red
    Write-Host "   Run: dotnet publish to build the executable first" -ForegroundColor Yellow
    exit 1
}

# Check if GITHUB_TOKEN is set
if (-not $env:GITHUB_TOKEN) {
    Write-Host "❌ GITHUB_TOKEN environment variable is not set" -ForegroundColor Red
    Write-Host "   Please set it with: `$env:GITHUB_TOKEN = 'your_token'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ GITHUB_TOKEN is set" -ForegroundColor Green
Write-Host "✅ Executable found" -ForegroundColor Green
Write-Host ""

# Test 1: Initialize
Write-Host "1️⃣ Testing initialize..." -ForegroundColor Yellow
$initRequest = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}'
$response = Write-Output $initRequest | & $executablePath
Write-Host "Response: $response" -ForegroundColor Gray
Write-Host ""

# Test 2: List tools
Write-Host "2️⃣ Testing tools list..." -ForegroundColor Yellow
$toolsRequest = '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
$response = Write-Output $toolsRequest | & $executablePath
Write-Host "Response: $response" -ForegroundColor Gray
Write-Host ""

# Test 3: Analyze repository (quick test)
Write-Host "3️⃣ Testing repository analysis for $Owner/$Repo..." -ForegroundColor Yellow
$analyzeRequest = '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"github_analyze_repository","arguments":{"owner":"' + $Owner + '","repo":"' + $Repo + '"}}}'
Write-Host "Sending request..." -ForegroundColor Cyan
$response = Write-Output $analyzeRequest | & $executablePath
Write-Host "Response length: $($response.Length) characters" -ForegroundColor Gray
if ($response.Length -gt 500) {
    Write-Host "Response (first 500 chars): $($response.Substring(0, 500))..." -ForegroundColor Gray
} else {
    Write-Host "Response: $response" -ForegroundColor Gray
}
Write-Host ""

Write-Host "🎉 Executable testing complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 MCP Configuration:" -ForegroundColor Blue
Write-Host "   File: .vscode/mcp.json" -ForegroundColor White
Write-Host "   Command: $((Get-Item $executablePath).FullName)" -ForegroundColor White
Write-Host ""
Write-Host "🚀 The executable is ready for use with VS Code MCP integration!" -ForegroundColor Green
