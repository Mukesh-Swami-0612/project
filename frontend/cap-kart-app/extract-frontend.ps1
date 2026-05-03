# # PowerShell script to extract ALL files from Angular frontend project
# # Creates a single frontend.txt file with EVERY file - NO EXCLUSIONS

# param(
#     [string]$ProjectPath = ".",
#     [string]$OutputFile = "frontend.txt"
# )

# Write-Host "Starting COMPLETE extraction from: $ProjectPath"
# Write-Host "Output file: $OutputFile"
# Write-Host "WARNING: This will include ALL files including node_modules, dist, etc."

# # Initialize output file
# $OutputPath = Join-Path $ProjectPath $OutputFile
# if (Test-Path $OutputPath) {
#     Remove-Item $OutputPath -Force
# }

# # Function to safely read file content
# function Get-SafeFileContent {
#     param([string]$FilePath)
    
#     try {
#         # Check file size
#         $fileInfo = Get-Item $FilePath
#         $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        
#         # For very large files, add a note but still try to read
#         if ($fileInfo.Length -gt 50MB) {
#             return "[LARGE FILE - $fileSizeMB MB - Content may be truncated by system]`n" + (Get-Content $FilePath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue)
#         }
        
#         # Try to read as UTF-8
#         $content = Get-Content $FilePath -Raw -Encoding UTF8 -ErrorAction Stop
#         if ([string]::IsNullOrEmpty($content)) {
#             return "[EMPTY FILE]"
#         }
#         return $content
#     }
#     catch {
#         # If UTF-8 fails, try reading as bytes and convert
#         try {
#             $bytes = [System.IO.File]::ReadAllBytes($FilePath)
#             if ($bytes.Length -eq 0) {
#                 return "[EMPTY FILE]"
#             }
#             # Try to detect if it's binary
#             $nullBytes = ($bytes | Where-Object { $_ -eq 0 }).Count
#             if ($nullBytes -gt ($bytes.Length * 0.1)) {
#                 return "[BINARY FILE - $($bytes.Length) bytes]"
#             }
#             # Try to convert to string
#             return [System.Text.Encoding]::UTF8.GetString($bytes)
#         }
#         catch {
#             return "[ERROR READING FILE: $($_.Exception.Message)]"
#         }
#     }
# }

# # Function to recursively process directory
# function Process-Directory {
#     param([string]$DirectoryPath, [string]$RelativePath = "")
    
#     try {
#         $items = Get-ChildItem $DirectoryPath -Force -ErrorAction Stop
        
#         foreach ($item in $items) {
#             # Skip only the output file itself to avoid infinite loop
#             if ($item.FullName -eq $OutputPath) {
#                 continue
#             }
            
#             $itemRelativePath = if ($RelativePath) { "$RelativePath/$($item.Name)" } else { $item.Name }
            
#             if ($item.PSIsContainer) {
#                 # It's a directory - process ALL directories
#                 Write-Host "Processing directory: $itemRelativePath"
#                 Process-Directory $item.FullName $itemRelativePath
#             }
#             else {
#                 # It's a file - include ALL files
#                 Write-Host "Processing file: $itemRelativePath"
                
#                 # Add file header
#                 $fileInfo = Get-Item $item.FullName
#                 $fileSizeKB = [math]::Round($fileInfo.Length / 1KB, 2)
#                 $header = @"

# ================================================================================
# File: $itemRelativePath
# Size: $fileSizeKB KB
# Last Modified: $($fileInfo.LastWriteTime)
# ================================================================================
# "@
#                 Add-Content -Path $OutputPath -Value $header -Encoding UTF8
                
#                 # Add file content
#                 $content = Get-SafeFileContent $item.FullName
#                 Add-Content -Path $OutputPath -Value $content -Encoding UTF8
                
#                 # Add separator
#                 Add-Content -Path $OutputPath -Value "`n" -Encoding UTF8
#             }
#         }
#     }
#     catch {
#         Write-Warning "Error processing directory ${DirectoryPath}: $($_.Exception.Message)"
#         # Log the error to output file
#         $errorMsg = "`n[ERROR accessing directory: $RelativePath - $($_.Exception.Message)]`n"
#         Add-Content -Path $OutputPath -Value $errorMsg -Encoding UTF8
#     }
# }

# # Main execution
# try {
#     # Validate project path
#     if (-not (Test-Path $ProjectPath)) {
#         throw "Project path does not exist: $ProjectPath"
#     }
    
#     # Add header to output file
#     $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
#     $headerText = @"
# ================================================================================
# COMPLETE ANGULAR FRONTEND PROJECT EXTRACTION
# ================================================================================
# Generated on: $timestamp
# Project Path: $ProjectPath
# Extraction Mode: COMPLETE - ALL FILES INCLUDED
# NO EXCLUSIONS - Every file and folder will be extracted
# ================================================================================

# "@
    
#     Add-Content -Path $OutputPath -Value $headerText -Encoding UTF8
    
#     # Start processing
#     Write-Host "`nStarting recursive file processing..."
#     Write-Host "This may take a while for large projects...`n"
#     Process-Directory $ProjectPath
    
#     # Add footer
#     $footerText = @"

# ================================================================================
# EXTRACTION COMPLETE
# ================================================================================
# All files have been processed and included in this output.
# Check above for any errors or warnings during extraction.
# ================================================================================
# "@
    
#     Add-Content -Path $OutputPath -Value $footerText -Encoding UTF8
    
#     Write-Host "`n================================"
#     Write-Host "Extraction completed successfully!"
#     Write-Host "Output saved to: $OutputPath"
    
#     # Show file size
#     $outputSize = (Get-Item $OutputPath).Length
#     $outputSizeMB = [math]::Round($outputSize / 1MB, 2)
#     Write-Host "Output file size: $outputSizeMB MB"
#     Write-Host "================================`n"
# }
# catch {
#     Write-Error "Script execution failed: $($_.Exception.Message)"
#     exit 1
# }


# PowerShell script to extract Angular frontend project (OPTIMIZED)

param(
    [string]$ProjectPath = ".",
    [string]$OutputFile = "frontend.txt"
)

Write-Host "Starting optimized extraction from: $ProjectPath"
Write-Host "Output file: $OutputFile"

# Output path
$OutputPath = Join-Path $ProjectPath $OutputFile
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Force
}

# 🚫 Folders to exclude
$excludeFolders = @(
    "node_modules",
    "dist",
    ".angular",
    ".git",
    ".vscode"
)

# 🚫 File extensions to exclude (optional)
$excludeExtensions = @(
    ".png", ".jpg", ".jpeg", ".gif",
    ".ico", ".log", ".zip", ".map"
)

# Function to safely read file
function Get-SafeFileContent {
    param([string]$FilePath)

    try {
        $fileInfo = Get-Item $FilePath

        # Skip very large files (>10MB)
        if ($fileInfo.Length -gt 10MB) {
            return "[SKIPPED LARGE FILE: $($fileInfo.Length / 1MB) MB]"
        }

        return Get-Content $FilePath -Raw -Encoding UTF8 -ErrorAction Stop
    }
    catch {
        return "[ERROR READING FILE: $($_.Exception.Message)]"
    }
}

# Main processing function
function Process-Directory {
    param([string]$DirectoryPath, [string]$RelativePath = "")

    try {
        $items = Get-ChildItem $DirectoryPath -Force -ErrorAction Stop

        foreach ($item in $items) {

            # Skip output file
            if ($item.FullName -eq $OutputPath) {
                continue
            }

            # 🚫 Skip excluded folders
            if ($item.PSIsContainer -and $excludeFolders -contains $item.Name) {
                Write-Host "Skipping folder: $($item.Name)" -ForegroundColor Yellow
                continue
            }

            $itemRelativePath = if ($RelativePath) {
                "$RelativePath/$($item.Name)"
            } else {
                $item.Name
            }

            if ($item.PSIsContainer) {
                Write-Host "Processing directory: $itemRelativePath"
                Process-Directory $item.FullName $itemRelativePath
            }
            else {
                # 🚫 Skip unwanted file types
                if ($excludeExtensions -contains $item.Extension) {
                    continue
                }

                Write-Host "Processing file: $itemRelativePath"

                $fileInfo = Get-Item $item.FullName
                $fileSizeKB = [math]::Round($fileInfo.Length / 1KB, 2)

                # File header
                $header = @"

================================================================================
File: $itemRelativePath
Size: $fileSizeKB KB
Last Modified: $($fileInfo.LastWriteTime)
================================================================================
"@

                Add-Content -Path $OutputPath -Value $header -Encoding UTF8

                # File content
                $content = Get-SafeFileContent $item.FullName
                Add-Content -Path $OutputPath -Value $content -Encoding UTF8

                Add-Content -Path $OutputPath -Value "`n" -Encoding UTF8
            }
        }
    }
    catch {
        Write-Warning "Error processing directory ${DirectoryPath}: $($_.Exception.Message)"
    }
}

# Header
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$headerText = @"
================================================================================
ANGULAR FRONTEND PROJECT EXTRACTION (OPTIMIZED)
================================================================================
Generated on: $timestamp
Project Path: $ProjectPath

EXCLUDED:
- node_modules
- dist
- .angular
- .git
- .vscode
- Large/binary files

================================================================================

"@

Add-Content -Path $OutputPath -Value $headerText -Encoding UTF8

# Start processing
Write-Host "`nStarting extraction..."
Process-Directory $ProjectPath

# Footer
$footerText = @"

================================================================================
EXTRACTION COMPLETE
================================================================================
"@

Add-Content -Path $OutputPath -Value $footerText -Encoding UTF8

Write-Host "`n================================"
Write-Host "Extraction completed successfully!"
Write-Host "Output saved to: $OutputPath"

$outputSize = (Get-Item $OutputPath).Length
$outputSizeMB = [math]::Round($outputSize / 1MB, 2)

Write-Host "Output file size: $outputSizeMB MB"
Write-Host "================================`n"