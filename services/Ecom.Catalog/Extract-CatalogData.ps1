# Script to extract all readable data from .NET Catalog Service project
# Output: catalog.txt in the root directory

$ErrorActionPreference = "Continue"

# Define the output file
$outputFile = "catalog.txt"

# Define folders to include
$includeFolders = @(
    "Ecom.Catalog.API",
    "Ecom.Catalog.Application",
    "Ecom.Catalog.Domain",
    "Ecom.Catalog.Infrastructure",
    "Ecom.Catalog.Tests"
)

# Define file extensions to include
$includeExtensions = @("*.cs", "*.json", "*.config", "*.csproj", "*.md", "*.txt", "*.xml", "*.yml", "*.yaml","obj")

# Define folders to exclude
$excludeFolders = @("bin", ".git", ".vs", "node_modules", ".kiro")

# Initialize output file
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}

"=" * 80 | Out-File -FilePath $outputFile -Encoding UTF8
"CATALOG SERVICE - COMPLETE PROJECT EXTRACTION" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8
"`n`n" | Out-File -FilePath $outputFile -Append -Encoding UTF8

$fileCount = 0
$errorCount = 0

# Function to check if path should be excluded
function Should-Exclude {
    param($path)
    
    foreach ($exclude in $excludeFolders) {
        if ($path -like "*\$exclude\*" -or $path -like "*/$exclude/*") {
            return $true
        }
    }
    return $false
}

# Process each folder
foreach ($folder in $includeFolders) {
    if (-not (Test-Path $folder)) {
        Write-Host "Warning: Folder not found - $folder" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Processing folder: $folder" -ForegroundColor Cyan
    
    # Get all files matching the extensions
    foreach ($extension in $includeExtensions) {
        $files = Get-ChildItem -Path $folder -Filter $extension -Recurse -File -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            # Skip if in excluded folder
            if (Should-Exclude $file.FullName) {
                continue
            }
            
            try {
                $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
                
                Write-Host "  Adding: $relativePath" -ForegroundColor Green
                
                # Write file header
                "`n`n" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                "=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8
                "File: $relativePath" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                "Size: $([math]::Round($file.Length / 1KB, 2)) KB" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                "=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8
                "`n" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                
                # Read and write file content
                # Handle large files by reading in chunks
                if ($file.Length -gt 10MB) {
                    "[File too large - skipped content]" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                } else {
                    $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
                    $content | Out-File -FilePath $outputFile -Append -Encoding UTF8
                }
                
                $fileCount++
                
            } catch {
                Write-Host "  Error reading: $($file.FullName) - $($_.Exception.Message)" -ForegroundColor Red
                "`n[Error reading file: $($_.Exception.Message)]`n" | Out-File -FilePath $outputFile -Append -Encoding UTF8
                $errorCount++
            }
        }
    }
}

# Write summary
"`n`n" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8
"EXTRACTION SUMMARY" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8
"Total files processed: $fileCount" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"Errors encountered: $errorCount" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"Output file: $outputFile" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"Completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath $outputFile -Append -Encoding UTF8
"=" * 80 | Out-File -FilePath $outputFile -Append -Encoding UTF8

Write-Host "`n" -NoNewline
Write-Host "=" * 80 -ForegroundColor Green
Write-Host "Extraction Complete!" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor Green
Write-Host "Files processed: $fileCount" -ForegroundColor Cyan
Write-Host "Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Yellow" } else { "Cyan" })
Write-Host "Output saved to: $outputFile" -ForegroundColor Cyan
Write-Host "File size: $([math]::Round((Get-Item $outputFile).Length / 1MB, 2)) MB" -ForegroundColor Cyan
