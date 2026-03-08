[CmdletBinding()]
param(
    [string]$Project = "",
    [string]$Configuration = "Release",
    [Parameter(Mandatory = $true)]
    [string]$Runtime,
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($Project)) {
    $Project = Join-Path $repoRoot "src/TimelineAnimations.App/TimelineAnimations.App.csproj"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "builds/artifacts"
}

$projectPath = (Resolve-Path $Project).Path
$outputRootPath = [System.IO.Path]::GetFullPath($OutputRoot)
$publishRoot = Join-Path $outputRootPath "publish"
$packageRoot = Join-Path $outputRootPath "package"
$distRoot = Join-Path $outputRootPath "dist"

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

$sourceBinaryBaseName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
$packageBinaryBaseName = "TimelineAnimations.Studio"
$packageBaseName = "$packageBinaryBaseName-$Version-$Runtime"
$publishDir = Join-Path $publishRoot $Runtime
$packageDir = Join-Path $packageRoot $packageBaseName

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

if (Test-Path $packageDir) {
    Remove-Item -Recurse -Force $packageDir
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

Write-Host "Publishing $packageBinaryBaseName for $Runtime ($Configuration) version $Version"

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishTrimmed=false `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    /p:Version=$Version `
    /p:InformationalVersion=$Version `
    /p:UseAppHost=true

$sourceBinaryName = if ($Runtime.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
    "$sourceBinaryBaseName.exe"
} else {
    $sourceBinaryBaseName
}

$packageBinaryName = if ($Runtime.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
    "$packageBinaryBaseName.exe"
} else {
    $packageBinaryBaseName
}

$binaryPath = Join-Path $publishDir $sourceBinaryName
if (-not (Test-Path $binaryPath)) {
    throw "Published binary not found: $binaryPath"
}

$packagedBinaryPath = Join-Path $packageDir $packageBinaryName
Copy-Item $binaryPath -Destination $packagedBinaryPath -Force

if (-not $Runtime.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
    & chmod +x $packagedBinaryPath
}

$licensePath = Join-Path $repoRoot "LICENSE"
if (Test-Path $licensePath) {
    Copy-Item $licensePath -Destination (Join-Path $packageDir "LICENSE.txt") -Force
}

$readmePath = Join-Path $repoRoot "README.md"
if (Test-Path $readmePath) {
    Copy-Item $readmePath -Destination (Join-Path $packageDir "README.md") -Force
}

$buildInfo = @(
    "Product: TimelineAnimations Studio"
    "Version: $Version"
    "Runtime: $Runtime"
    "Configuration: $Configuration"
    "BuiltUtc: $([DateTime]::UtcNow.ToString('u'))"
    "Commit: $($env:GITHUB_SHA)"
    "Repository: $($env:GITHUB_REPOSITORY)"
)

Set-Content -Path (Join-Path $packageDir "BUILD-INFO.txt") -Value $buildInfo -Encoding utf8

$archivePath = if ($Runtime.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
    $zipPath = Join-Path $distRoot "$packageBaseName.zip"
    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
    $zipPath
} else {
    $tarPath = Join-Path $distRoot "$packageBaseName.tar.gz"
    if (Test-Path $tarPath) {
        Remove-Item -Force $tarPath
    }

    & tar -C $packageRoot -czf $tarPath $packageBaseName
    if ($LASTEXITCODE -ne 0) {
        throw "tar packaging failed for $tarPath"
    }

    $tarPath
}

$hash = Get-FileHash -Path $archivePath -Algorithm SHA256
$hashFilePath = "$archivePath.sha256"
Set-Content -Path $hashFilePath -Value ("{0}  {1}" -f $hash.Hash.ToLowerInvariant(), [System.IO.Path]::GetFileName($archivePath)) -Encoding utf8

Write-Host "Created package: $archivePath"
Write-Host "Created checksum: $hashFilePath"
