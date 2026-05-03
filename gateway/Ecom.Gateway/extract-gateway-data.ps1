# Extract all readable data from .NET API Gateway project into gateway.txt

$outputFile = "gateway.txt"
$projectRoot = Get-Location

# File extensions to include
$includeExtensions = @('.cs', '.json', '.config', '.csproj', '.md', '.txt', '.xml', '.yml', '.yaml')

# Directories to exclude
$excludeDirs = @('bin', 'obj', '.git', '.vs', 'node_modules', 'packages')

# Clear output file if exists
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

Write-Host "Starting extraction from: $projectRoot" -ForegroundColor Green
Write-Host "Output file: $outputFile" -ForegroundColor Green
Write-Host ""

# Function to check if path should be excluded
function Should-Exclude {
    param($path)
    foreach ($excludeDir in $excludeDirs) {
        if ($path -like "*\$excludeDir\*" -or $path -like "*/$excludeDir/*") {
            return $true
        }
    }
    return $false
}

# Get all files recursively
$allFiles = Get-ChildItem -Path $projectRoot -Recurse -File -ErrorAction SilentlyContinue

$processedCount = 0
$skippedCount = 0

foreach ($file in $allFiles) {
    # Skip if in excluded directory
    if (Should-Exclude $file.FullName) {
        $skippedCount++
        continue
    }
    
    # Check if file extension is in include list
    if ($includeExtensions -contains $file.Extension) {
        try {
            # Get relative path
            $relativePath = $file.FullName.Substring($projectRoot.Path.Length + 1)
            
            Write-Host "Processing: $relativePath" -ForegroundColor Cyan
            
            # Create header
            $header = "`r`n===== File: $relativePath =====`r`n"
            Add-Content -Path $outputFile -Value $header -Encoding UTF8
            
            # Read and append file content
            $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
            Add-Content -Path $outputFile -Value $content -Encoding UTF8
            
            # Add separator
            Add-Content -Path $outputFile -Value "`r`n" -Encoding UTF8
            
            $processedCount++
        }
        catch {
            Write-Host "  Warning: Could not read $($file.Name) - $($_.Exception.Message)" -ForegroundColor Yellow
            $skippedCount++
        }
    }
    else {
        $skippedCount++
    }
}

Write-Host ""
Write-Host "Extraction complete!" -ForegroundColor Green
Write-Host "Processed files: $processedCount" -ForegroundColor Green
Write-Host "Skipped files: $skippedCount" -ForegroundColor Gray
Write-Host "Output saved to: $outputFile" -ForegroundColor Green
Write-Host ""
Write-Host "File size: $([math]::Round((Get-Item $outputFile).Length / 1KB, 2)) KB" -ForegroundColor Cyan
