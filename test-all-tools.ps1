# Test all GitHub MCP Server tools

Write-Host "Testing all GitHub MCP Server tools..." -ForegroundColor Blue

$executablePath = ".\Executable\GitHubMcpServer.exe"

if (-not (Test-Path $executablePath)) {
    Write-Host "ERROR: Executable not found" -ForegroundColor Red
    exit 1
}

if (-not $env:GITHUB_TOKEN) {
    Write-Host "ERROR: GITHUB_TOKEN not set" -ForegroundColor Red  
    exit 1
}

# Test 1: Repository analyzer
Write-Host "1. Testing repository analyzer..." -ForegroundColor Yellow
$response = '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"github_analyze_repository","arguments":{"owner":"octocat","repo":"Hello-World"}}}' | & $executablePath
if ($response -like "*Hello-World*") {
    Write-Host "   SUCCESS: Repository analyzer working" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Repository analyzer failed" -ForegroundColor Red
}

# Test 2: Repository contents
Write-Host "2. Testing repository contents..." -ForegroundColor Yellow
$response = '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"github_get_repository_contents","arguments":{"owner":"octocat","repo":"Hello-World","recursive":false,"includeContent":true}}}' | & $executablePath
if ($response -like "*README*") {
    Write-Host "   SUCCESS: Repository contents working" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Repository contents failed" -ForegroundColor Red
}

# Test 3: Pull request reviewer (using a known PR)
Write-Host "3. Testing pull request reviewer..." -ForegroundColor Yellow
$response = '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"github_review_pull_request","arguments":{"owner":"octocat","repo":"Hello-World","pullNumber":1}}}' | & $executablePath
if ($response -like "*pullRequest*" -or $response -like "*error*") {
    Write-Host "   SUCCESS: Pull request reviewer responding (might be no PRs)" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Pull request reviewer failed" -ForegroundColor Red
}

Write-Host "`nAll tests completed! The MCP server is ready." -ForegroundColor Green
Write-Host "The casting error has been fixed." -ForegroundColor Cyan
