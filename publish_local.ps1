param(
    [string]$ApiKey
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "Please provide the API Key." -ForegroundColor Red
    Write-Host "Usage: .\publish_local.ps1 -ApiKey YOUR_KEY" -ForegroundColor Yellow
    exit 1
}

Write-Host "🚀 Starting Local Publish Process..." -ForegroundColor Cyan

# 1. Clean
Write-Host "Cleaning..." -ForegroundColor Gray
dotnet clean src/Papiro/Papiro.csproj -c Release

# 2. Pack
Write-Host "Packing..." -ForegroundColor Gray
dotnet pack src/Papiro/Papiro.csproj -c Release

# 3. Find Package
$packageFile = Get-ChildItem "src/Papiro/bin/Release/*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($null -eq $packageFile) {
    Write-Host "❌ Error: No package file found." -ForegroundColor Red
    exit 1
}

Write-Host "📦 Package Found: $($packageFile.Name)" -ForegroundColor Green

# 4. Push
Write-Host "Pushing to NuGet..." -ForegroundColor Cyan
dotnet nuget push $packageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json --skip-duplicate

Write-Host "✅ Done! Package published." -ForegroundColor Green
