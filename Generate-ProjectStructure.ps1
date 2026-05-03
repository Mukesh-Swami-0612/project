#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates a complete project structure tree and saves it to structure.txt
.DESCRIPTION
    Recursively scans the entire project directory and creates a tree-like representation
    of all folders and files, including hidden/system folders.
#>

param(
    [string]$RootPath = ".",
    [string]$OutputFile = "structure.txt"
)

# Directories to exclude
$ExcludeDirs = @(
    '.dotnet-home',
    '.nuget',
    'node_modules',
    'bin',
    'obj',
    '.vs',
    '.git',
    'packages',
    'TestResults',
    'coverage',
    '.angular',
    'dist',
    'build',
    'out',
    '.next',
    '.cache'
)

function Get-TreeStructure {
    param(
        [string]$Path,
        [string]$Prefix = "",
        [bool]$IsLast = $true,
        [int]$Depth = 0
    )
    
    $items = @()
    
    try {
        # Get all items (files and folders) including hidden ones, excluding specified directories
        $allItems = Get-ChildItem -Path $Path -Force | 
            Where-Object { -not ($_.PSIsContainer -and $ExcludeDirs -contains $_.Name) } |
            Sort-Object @{Expression={$_.PSIsContainer}; Descending=$true}, Name
        
        for ($i = 0; $i -lt $allItems.Count; $i++) {
            $item = $allItems[$i]
            $isLastItem = ($i -eq ($allItems.Count - 1))
            
            # Determine the tree symbols
            if ($isLastItem) {
                $currentPrefix = $Prefix + "+-- "
                $nextPrefix = $Prefix + "    "
            } else {
                $currentPrefix = $Prefix + "|-- "
                $nextPrefix = $Prefix + "|   "
            }
            
            # Add current item to output
            if ($item.PSIsContainer) {
                $items += "$currentPrefix$($item.Name)/"
                
                # Recursively process subdirectories
                try {
                    $subItems = Get-TreeStructure -Path $item.FullName -Prefix $nextPrefix -IsLast $isLastItem -Depth ($Depth + 1)
                    $items += $subItems
                } catch {
                    # Handle access denied or other errors
                    $items += "$nextPrefix[Access Denied]"
                }
            } else {
                $items += "$currentPrefix$($item.Name)"
            }
        }
    } catch {
        Write-Warning "Error accessing path: $Path - $($_.Exception.Message)"
    }
    
    return $items
}

# Main execution
Write-Host "Generating project structure tree..." -ForegroundColor Green

# Get the absolute path and project name
$absolutePath = Resolve-Path $RootPath
$projectName = Split-Path $absolutePath -Leaf

# Start building the structure
$structure = @()
$structure += "$projectName/"

# Get the tree structure
Write-Host "Scanning directories and files..." -ForegroundColor Yellow
$treeItems = Get-TreeStructure -Path $absolutePath

$structure += $treeItems

# Write to output file
$outputPath = Join-Path $absolutePath $OutputFile
Write-Host "Writing structure to: $outputPath" -ForegroundColor Yellow

try {
    $structure | Out-File -FilePath $outputPath -Encoding UTF8
    Write-Host "Project structure successfully saved to $OutputFile" -ForegroundColor Green
    Write-Host "Total items processed: $($structure.Count - 1)" -ForegroundColor Cyan
} catch {
    Write-Error "Failed to write output file: $($_.Exception.Message)"
    exit 1
}

# Display summary
Write-Host "`nStructure generation completed!" -ForegroundColor Green
Write-Host "Output file: $OutputFile" -ForegroundColor Cyan
Write-Host "You can view the structure with: Get-Content $OutputFile" -ForegroundColor Gray
