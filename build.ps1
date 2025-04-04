param (
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Publish,
    [string]$NuGetApiKey = "",
    [switch]$BuildExamples
)

# Set working directory to the script location
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Update version in csproj
$csprojPath = Join-Path $scriptPath "Tus.Client.csproj"
$csprojContent = Get-Content $csprojPath -Raw
$csprojContent = $csprojContent -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
$csprojContent | Set-Content $csprojPath

# Build the library project
Write-Host "Building Tus.Client library..."
dotnet build -c $Configuration

# Build the examples project if requested
if ($BuildExamples) {
    Write-Host "Building Tus.Client.Examples..."
    $examplesPath = Join-Path $scriptPath "Examples"
    dotnet build -c $Configuration $examplesPath
}

# Create the package
Write-Host "Creating NuGet package..."
dotnet pack -c $Configuration --no-build

# Publish the package if requested
if ($Publish) {
    if ([string]::IsNullOrEmpty($NuGetApiKey)) {
        Write-Host "Error: NuGet API key is required for publishing. Use -NuGetApiKey parameter." -ForegroundColor Red
        exit 1
    }

    $packagePath = Join-Path $scriptPath "bin\$Configuration\Tus.Client.$Version.nupkg"
    if (Test-Path $packagePath) {
        Write-Host "Publishing package to NuGet..."
        dotnet nuget push $packagePath --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json
    } else {
        Write-Host "Error: Package not found at $packagePath" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Done!" -ForegroundColor Green 