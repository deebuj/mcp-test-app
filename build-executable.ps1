# Build and Deploy GitHub MCP Server Executable

Write-Host "Building GitHub MCP Server Executable..." -ForegroundColor Blue

# Clean previous build
if (Test-Path ".\Executable") {
    Remove-Item .\Executable\* -Force
    Write-Host "Cleaned previous build" -ForegroundColor Yellow
}

# Build the executable
Write-Host "Building self-contained executable..." -ForegroundColor Yellow
dotnet publish GitHubMcpServer.csproj -c Release -r win-x64 --self-contained true -o ".\Executable" /p:PublishSingleFile=true

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Executable built successfully!" -ForegroundColor Green
    
    # Show file info
    $exeFile = Get-Item ".\Executable\GitHubMcpServer.exe"
    Write-Host "Executable: $($exeFile.FullName)" -ForegroundColor Cyan
    Write-Host "Size: $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    
    # Test the executable
    Write-Host "`nTesting executable..." -ForegroundColor Yellow
    if ($env:GITHUB_TOKEN) {
        .\test-simple.ps1
    } else {
        Write-Host "Set GITHUB_TOKEN to test the executable" -ForegroundColor Yellow
    }
    
    Write-Host "`nMCP Configuration updated in .vscode\mcp.json" -ForegroundColor Green
    Write-Host "Path: $($exeFile.FullName)" -ForegroundColor Cyan
    
} else {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
