# PowerShell script to extract all readable data from .NET Workflow Service project
# Creates a single workflow.txt file with all relevant source code and configuration files

param(
    [string]$OutputFile = "workflow.txt",
    [string]$RootPath = "."
)

# Define file extensions to include
$IncludeExtensions = @('.cs', '.json', '.config', '.csproj', '.md', '.txt', '.xml', '.yml', '.yaml', '.sql')

# Define folders to exclude
$ExcludeFolders = @('bin', '.git', '.vs', 'node_modules', 'packages', '.nuget')

# Define project folders to process
$ProjectFolders = @(
    'Ecom.Workflow.API',
    'Ecom.Workflow.Application', 
    'Ecom.Workflow.Domain',
    'Ecom.Workflow.Infrastructure',
    'Ecom.Workflow.Tests'
)

Write-Host "Starting workflow data extraction..." -ForegroundColor Green
Write-Host "Output file: $OutputFile" -ForegroundColor Yellow

# Initialize output file
$OutputPath = Join-Path $RootPath $OutputFile
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Force
}

# Add header to output file
$Header = @"
===============================================================================
.NET WORKFLOW SERVICE PROJECT DATA EXTRACTION
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
===============================================================================

"@

Add-Content -Path $OutputPath -Value $Header -Encoding UTF8

# Function to check if folder should be excluded
function Should-ExcludeFolder {
    param([string]$FolderName)
    return $ExcludeFolders -contains $FolderName
}

# Function to check if file should be included
function Should-IncludeFile {
    param([string]$FilePath)
    $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
    return $IncludeExtensions -contains $extension
}

# Function to safely read file content
function Get-SafeFileContent {
    param([string]$FilePath)
    
    try {
        # Check file size (skip files larger than 10MB to avoid memory issues)
        $fileInfo = Get-Item $FilePath
        if ($fileInfo.Length -gt 10MB) {
            return "*** FILE TOO LARGE ($('{0:N2}' -f ($fileInfo.Length / 1MB)) MB) - CONTENT SKIPPED ***"
        }
        
        # Try to read as UTF8, fallback to default encoding
        $content = Get-Content -Path $FilePath -Raw -Encoding UTF8 -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($content)) {
            return "*** EMPTY FILE ***"
        }
        return $content
    }
    catch {
        return "*** ERROR READING FILE: $($_.Exception.Message) ***"
    }
}

# Function to process files recursively
function Process-Directory {
    param(
        [string]$DirectoryPath,
        [string]$RelativePath = ""
    )
    
    if (-not (Test-Path $DirectoryPath)) {
        Write-Warning "Directory not found: $DirectoryPath"
        return
    }
    
    # Get all items in directory
    $items = Get-ChildItem -Path $DirectoryPath -ErrorAction SilentlyContinue
    
    # Process files first
    $files = $items | Where-Object { -not $_.PSIsContainer }
    foreach ($file in $files) {
        if (Should-IncludeFile $file.FullName) {
            $relativeFilePath = if ($RelativePath) { "$RelativePath/$($file.Name)" } else { $file.Name }
            
            Write-Host "Processing: $relativeFilePath" -ForegroundColor Cyan
            
            # Create file header
            $fileHeader = @"

===============================================================================
File: $relativeFilePath
Size: $('{0:N0}' -f $file.Length) bytes
Modified: $($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
===============================================================================

"@
            
            Add-Content -Path $OutputPath -Value $fileHeader -Encoding UTF8
            
            # Get and add file content
            $content = Get-SafeFileContent $file.FullName
            Add-Content -Path $OutputPath -Value $content -Encoding UTF8
            
            # Add separator
            Add-Content -Path $OutputPath -Value "`n" -Encoding UTF8
        }
    }
    
    # Process subdirectories
    $directories = $items | Where-Object { $_.PSIsContainer }
    foreach ($directory in $directories) {
        if (-not (Should-ExcludeFolder $directory.Name)) {
            $newRelativePath = if ($RelativePath) { "$RelativePath/$($directory.Name)" } else { $directory.Name }
            Process-Directory -DirectoryPath $directory.FullName -RelativePath $newRelativePath
        }
        else {
            Write-Host "Skipping excluded folder: $($directory.Name)" -ForegroundColor DarkGray
        }
    }
}

# Process each project folder
$processedFolders = 0
foreach ($folder in $ProjectFolders) {
    $folderPath = Join-Path $RootPath $folder
    
    if (Test-Path $folderPath) {
        Write-Host "`nProcessing project folder: $folder" -ForegroundColor Green
        
        # Add project section header
        $projectHeader = @"

###############################################################################
PROJECT: $folder
###############################################################################

"@
        Add-Content -Path $OutputPath -Value $projectHeader -Encoding UTF8
        
        Process-Directory -DirectoryPath $folderPath -RelativePath $folder
        $processedFolders++
    }
    else {
        Write-Warning "Project folder not found: $folder"
    }
}

# Add footer
$Footer = @"

===============================================================================
EXTRACTION COMPLETE
Processed Folders: $processedFolders
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
===============================================================================
"@

Add-Content -Path $OutputPath -Value $Footer -Encoding UTF8

# Display summary
Write-Host "`nExtraction completed successfully!" -ForegroundColor Green
Write-Host "Output file: $OutputPath" -ForegroundColor Yellow

if (Test-Path $OutputPath) {
    $outputSize = (Get-Item $OutputPath).Length
    Write-Host "Output file size: $('{0:N2}' -f ($outputSize / 1KB)) KB" -ForegroundColor Yellow
}

Write-Host "`nTo run this script:" -ForegroundColor Cyan
Write-Host ".\Extract-WorkflowData.ps1" -ForegroundColor White
Write-Host "or with custom output:" -ForegroundColor Cyan
Write-Host ".\Extract-WorkflowData.ps1 -OutputFile 'my-workflow.txt'" -ForegroundColor White