# PowerShell Script to Extract All Readable Data from .NET Reporting Service Project
# Output: reporting.txt in the root directory

param(
    [string]$OutputFile = "reporting.txt",
    [string]$RootPath = "."
)

# Configuration
$IncludeFolders = @(
    "Ecom.Reporting.API",
    "Ecom.Reporting.Application",
    "Ecom.Reporting.Domain",
    "Ecom.Reporting.Infrastructure",
    "Ecom.Reporting.Tests"
)

$IncludeExtensions = @(
    "*.cs",
    "*.json",
    "*.config",
    "*.csproj",
    "*.md",
    "*.txt",
    "*.sln",
    "obj"
)

$ExcludeFolders = @(
    "bin",
    ".git",
    ".vs",
    "node_modules",
    "packages"
)

# Initialize output file
$OutputPath = Join-Path $RootPath $OutputFile
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Force
}

Write-Host "Starting data extraction..." -ForegroundColor Green
Write-Host "Output file: $OutputPath" -ForegroundColor Cyan

$fileCount = 0
$totalSize = 0

# Create output file with header
$header = @"
================================================================================
.NET REPORTING SERVICE PROJECT - COMPLETE DATA EXTRACTION
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
================================================================================

"@

Add-Content -Path $OutputPath -Value $header -Encoding UTF8

# Process each folder
foreach ($folder in $IncludeFolders) {
    $folderPath = Join-Path $RootPath $folder
    
    if (-not (Test-Path $folderPath)) {
        Write-Host "Folder not found: $folder" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "`nProcessing folder: $folder" -ForegroundColor Cyan
    
    # Get all files matching extensions
    $files = Get-ChildItem -Path $folderPath -Recurse -File | Where-Object {
        $file = $_
        $include = $false
        
        # Check if file extension matches
        foreach ($ext in $IncludeExtensions) {
            if ($file.Name -like $ext) {
                $include = $true
                break
            }
        }
        
        # Exclude files in excluded folders
        if ($include) {
            foreach ($excludeFolder in $ExcludeFolders) {
                if ($file.FullName -like "*\$excludeFolder\*") {
                    $include = $false
                    break
                }
            }
        }
        
        $include
    }
    
    # Process each file
    foreach ($file in $files) {
        try {
            $relativePath = $file.FullName.Substring($RootPath.Length + 1)
            $fileSize = $file.Length
            
            Write-Host "  Processing: $relativePath ($([math]::Round($fileSize/1KB, 2)) KB)" -ForegroundColor Gray
            
            # Create file header
            $fileHeader = @"

================================================================================
File: $relativePath
Size: $fileSize bytes
Last Modified: $($file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))
================================================================================

"@
            
            Add-Content -Path $OutputPath -Value $fileHeader -Encoding UTF8
            
            # Read and append file content
            # Use streaming for large files to avoid memory issues
            if ($fileSize -gt 1MB) {
                # Stream large files
                $reader = [System.IO.StreamReader]::new($file.FullName)
                $writer = [System.IO.StreamWriter]::new($OutputPath, $true, [System.Text.Encoding]::UTF8)
                
                while (-not $reader.EndOfStream) {
                    $line = $reader.ReadLine()
                    $writer.WriteLine($line)
                }
                
                $reader.Close()
                $writer.Close()
            } else {
                # Read small files directly
                $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
                Add-Content -Path $OutputPath -Value $content -Encoding UTF8
            }
            
            # Add separator
            Add-Content -Path $OutputPath -Value "`n`n" -Encoding UTF8
            
            $fileCount++
            $totalSize += $fileSize
            
        } catch {
            Write-Host "  ERROR processing $($file.Name): $_" -ForegroundColor Red
            $errorMsg = @"

[ERROR] Failed to process file: $relativePath
Error: $($_.Exception.Message)

"@
            Add-Content -Path $OutputPath -Value $errorMsg -Encoding UTF8
        }
    }
}

# Add footer
$footer = @"

================================================================================
EXTRACTION SUMMARY
================================================================================
Total Files Processed: $fileCount
Total Size: $([math]::Round($totalSize/1MB, 2)) MB
Completion Time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
================================================================================
"@

Add-Content -Path $OutputPath -Value $footer -Encoding UTF8

Write-Host "`n=================================================================================" -ForegroundColor Green
Write-Host "Extraction Complete!" -ForegroundColor Green
Write-Host "Files processed: $fileCount" -ForegroundColor Cyan
Write-Host "Total size: $([math]::Round($totalSize/1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "Output file: $OutputPath" -ForegroundColor Cyan
Write-Host "=================================================================================" -ForegroundColor Green
