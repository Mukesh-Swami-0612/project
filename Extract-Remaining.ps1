# ============================================
# REMAINING CRITICAL FILES EXTRACTION SCRIPT
# Output: remain.txt
# ============================================

param(
    [string]$RootPath = ".",
    [string]$OutputFile = "remain.txt"
)

Write-Host "Starting REMAINING CRITICAL FILES extraction..." -ForegroundColor Green
Write-Host "Root Path: $RootPath" -ForegroundColor Cyan

$OutputPath = Join-Path $RootPath $OutputFile

# Remove old file
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Force
    Write-Host "Removed existing output file" -ForegroundColor Yellow
}

# Excluded folders
$excludeFolders = @(
    "bin", "obj", ".git", ".vs", "node_modules",
    "dist", ".angular", "packages", ".nuget", "TestResults"
)

# Root-level important files
$rootFilePatterns = @(
    "*.sln",
    "*.slnx",
    "Directory.Build.props",
    "Directory.Packages.props",
    "global.json",
    "nuget.config",
    ".editorconfig"
)

# Important config files
$configFiles = @(
    "ocelot.json",
    "ocelot.staging.json",
    "appsettings.json",
    "appsettings.Development.json",
    "appsettings.Production.json",
    "appsettings.Staging.json",
    "docker-compose.yml",
    "docker-compose.override.yml",
    "Dockerfile",
    ".dockerignore",
    ".env",
    ".env.example",
    "launchSettings.json"
)

# Database patterns
$dbFilePatterns = @(
    "*DbContext.cs",
    "*DbContextModelSnapshot.cs"
)

# Function to check if path should be excluded
function Should-Exclude($path) {
    foreach ($folder in $excludeFolders) {
        if ($path -like "*\$folder\*" -or $path -like "*/$folder/*") {
            return $true
        }
    }
    return $false
}

# Function to safely read content
function Get-ContentSafe($filePath) {
    try {
        $fileInfo = Get-Item $filePath
        
        if ($fileInfo.Length -gt 10MB) {
            return "[SKIPPED LARGE FILE: $([math]::Round($fileInfo.Length / 1MB, 2)) MB]"
        }
        
        $binaryExtensions = @(".dll", ".exe", ".pdb", ".cache", ".png", ".jpg", ".ico", ".gif")
        if ($binaryExtensions -contains $fileInfo.Extension.ToLower()) {
            return "[BINARY FILE - SKIPPED]"
        }
        
        return Get-Content $filePath -Raw -Encoding UTF8 -ErrorAction Stop
    }
    catch {
        return "[ERROR READING FILE: $($_.Exception.Message)]"
    }
}

# Function to write file content
function Write-FileContent($file) {
    $relative = $file.FullName.Substring((Resolve-Path $RootPath).Path.Length + 1)
    Write-Host "  + Adding: $relative" -ForegroundColor Cyan
    
    $fileSizeKB = [math]::Round($file.Length / 1KB, 2)
    
    $separator = "=" * 80
    Add-Content -Path $OutputPath -Value "" -Encoding UTF8
    Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
    Add-Content -Path $OutputPath -Value "File: $relative" -Encoding UTF8
    Add-Content -Path $OutputPath -Value "Size: $fileSizeKB KB" -Encoding UTF8
    Add-Content -Path $OutputPath -Value "Last Modified: $($file.LastWriteTime)" -Encoding UTF8
    Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
    
    $content = Get-ContentSafe $file.FullName
    Add-Content -Path $OutputPath -Value $content -Encoding UTF8
    Add-Content -Path $OutputPath -Value "" -Encoding UTF8
    
    return 1
}

# Write header
$separator = "=" * 80
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
Add-Content -Path $OutputPath -Value "REMAINING CRITICAL PROJECT DATA EXTRACTION" -Encoding UTF8
Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
Add-Content -Path $OutputPath -Value "Generated: $timestamp" -Encoding UTF8
Add-Content -Path $OutputPath -Value "Root Path: $RootPath" -Encoding UTF8
Add-Content -Path $OutputPath -Value "" -Encoding UTF8
Add-Content -Path $OutputPath -Value "INCLUDES:" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Root solution files (.sln, .slnx, Directory.Build.props, etc.)" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Gateway config (ocelot.json)" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - All appsettings.json files" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Docker / Environment files" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Database contexts and migrations" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Launch settings" -Encoding UTF8
Add-Content -Path $OutputPath -Value "" -Encoding UTF8
Add-Content -Path $OutputPath -Value "EXCLUDES:" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - bin, obj, node_modules, dist folders" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Binary files (.dll, .exe, .pdb)" -Encoding UTF8
Add-Content -Path $OutputPath -Value "  - Large files (>10MB)" -Encoding UTF8
Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
Add-Content -Path $OutputPath -Value "" -Encoding UTF8

$totalFiles = 0

# 1. ROOT-LEVEL FILES
Write-Host ""
Write-Host "[1/5] Extracting ROOT-LEVEL files..." -ForegroundColor Yellow

foreach ($pattern in $rootFilePatterns) {
    Get-ChildItem -Path $RootPath -Filter $pattern -File -ErrorAction SilentlyContinue | 
        Where-Object { -not (Should-Exclude $_.FullName) } | 
        ForEach-Object { 
            $totalFiles += Write-FileContent $_
        }
}

# 2. IMPORTANT CONFIG FILES
Write-Host ""
Write-Host "[2/5] Extracting CONFIG files..." -ForegroundColor Yellow

foreach ($configFile in $configFiles) {
    Get-ChildItem -Path $RootPath -Recurse -File -ErrorAction SilentlyContinue | 
        Where-Object { 
            $_.Name -eq $configFile -and -not (Should-Exclude $_.FullName)
        } | 
        ForEach-Object { 
            $totalFiles += Write-FileContent $_
        }
}

# 3. DATABASE CONTEXT FILES
Write-Host ""
Write-Host "[3/5] Extracting DATABASE CONTEXT files..." -ForegroundColor Yellow

foreach ($pattern in $dbFilePatterns) {
    Get-ChildItem -Path $RootPath -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | 
        Where-Object { -not (Should-Exclude $_.FullName) } | 
        ForEach-Object { 
            $totalFiles += Write-FileContent $_
        }
}

# 4. MIGRATION FILES
Write-Host ""
Write-Host "[4/5] Extracting MIGRATION files..." -ForegroundColor Yellow

Get-ChildItem -Path $RootPath -Recurse -Directory -Filter "Migrations" -ErrorAction SilentlyContinue | 
    Where-Object { -not (Should-Exclude $_.FullName) } | 
    ForEach-Object {
        $migrationsFolder = $_
        Write-Host "  Found Migrations folder: $($migrationsFolder.FullName)" -ForegroundColor Gray
        
        Get-ChildItem -Path $migrationsFolder.FullName -File -Filter "*.cs" -ErrorAction SilentlyContinue | 
            ForEach-Object {
                $totalFiles += Write-FileContent $_
            }
    }

# 5. SQL FILES
Write-Host ""
Write-Host "[5/5] Extracting SQL files..." -ForegroundColor Yellow

Get-ChildItem -Path $RootPath -Recurse -File -Filter "*.sql" -ErrorAction SilentlyContinue | 
    Where-Object { -not (Should-Exclude $_.FullName) } | 
    ForEach-Object { 
        $totalFiles += Write-FileContent $_
    }

# Write footer
Add-Content -Path $OutputPath -Value "" -Encoding UTF8
Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
Add-Content -Path $OutputPath -Value "EXTRACTION COMPLETE" -Encoding UTF8
Add-Content -Path $OutputPath -Value $separator -Encoding UTF8
Add-Content -Path $OutputPath -Value "Total files extracted: $totalFiles" -Encoding UTF8
Add-Content -Path $OutputPath -Value "Check above for any errors or warnings during extraction." -Encoding UTF8
Add-Content -Path $OutputPath -Value $separator -Encoding UTF8

# Summary
Write-Host ""
Write-Host "====================================" -ForegroundColor Green
Write-Host "EXTRACTION COMPLETE!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host "Output file: $OutputPath" -ForegroundColor Cyan
Write-Host "Total files: $totalFiles" -ForegroundColor Cyan

if (Test-Path $OutputPath) {
    $fileSize = (Get-Item $OutputPath).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
    Write-Host "File size: $fileSizeMB MB" -ForegroundColor Cyan
}

Write-Host "====================================" -ForegroundColor Green
