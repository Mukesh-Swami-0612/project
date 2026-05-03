# PowerShell script to extract all readable data from Infrastructure directory

$sourceDirectory = $PSScriptRoot
$outputFile = Join-Path $sourceDirectory "infrastructure.txt"

# File extensions to include
$includeExtensions = @('.cs', '.json', '.config', '.csproj', '.md', '.txt')

# Directories to exclude
$excludeDirs = @('bin', 'obj', '.git', '.vs')

# Clear output file if it exists
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}

# Function to check if path contains excluded directory
function Should-ExcludePath {
    param([string]$path)
    
    foreach ($excludeDir in $excludeDirs) {
        if ($path -like "*\$excludeDir\*" -or $path -like "*/$excludeDir/*") {
            return $true
        }
    }
    return $false
}

# Get all files recursively
$allFiles = Get-ChildItem -Path $sourceDirectory -File -Recurse -ErrorAction SilentlyContinue

$processedCount = 0
$skippedCount = 0

foreach ($file in $allFiles) {
    # Skip if in excluded directory
    if (Should-ExcludePath -path $file.FullName) {
        $skippedCount++
        continue
    }
    
    # Skip if not in included extensions
    if ($includeExtensions -notcontains $file.Extension) {
        $skippedCount++
        continue
    }
    
    # Get relative path from source directory
    $relativePath = $file.FullName.Substring($sourceDirectory.Length + 1)
    
    # Create header
    $header = "`r`n===== File: $relativePath =====`r`n"
    
    try {
        # Read file content
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
        
        # Append to output file
        Add-Content -Path $outputFile -Value $header -NoNewline
        Add-Content -Path $outputFile -Value $content
        Add-Content -Path $outputFile -Value "`r`n"
        
        $processedCount++
        Write-Host "Processed: $relativePath" -ForegroundColor Green
    }
    catch {
        Write-Host "Error reading: $relativePath - $($_.Exception.Message)" -ForegroundColor Yellow
        $skippedCount++
    }
}

Write-Host "`r`nExtraction Complete!" -ForegroundColor Cyan
Write-Host "Files processed: $processedCount" -ForegroundColor Green
Write-Host "Files skipped: $skippedCount" -ForegroundColor Yellow
Write-Host "Output saved to: $outputFile" -ForegroundColor Cyan
