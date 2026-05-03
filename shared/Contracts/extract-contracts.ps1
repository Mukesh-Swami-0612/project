# PowerShell script to extract all readable data from a directory into a single text file

$rootPath = $PSScriptRoot
$outputFile = Join-Path $rootPath "contracts.txt"

# File extensions to include
$includeExtensions = @('.cs', '.json', '.config', '.csproj', '.md', '.txt', '.cache', '.props', '.targets', '.editorconfig', '.dgspec')

# Folders to ignore (only .git and .vs)
$ignoreFolders = @('.git', '.vs')

# Clear output file if it exists
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

# Function to check if path contains ignored folders
function Should-IgnorePath {
    param([string]$path)
    
    foreach ($folder in $ignoreFolders) {
        if ($path -like "*\$folder\*" -or $path -like "*/$folder/*") {
            return $true
        }
    }
    return $false
}

# Get all files recursively
$files = Get-ChildItem -Path $rootPath -File -Recurse | Where-Object {
    $extension = $_.Extension
    $fullPath = $_.FullName
    
    # Exclude the output file itself and ignored folders
    ($fullPath -ne $outputFile) -and 
    (-not (Should-IgnorePath $fullPath)) -and
    (($includeExtensions -contains $extension) -or ($extension -eq ''))
}

# Process each file
foreach ($file in $files) {
    try {
        # Get relative path for header
        $relativePath = $file.FullName.Substring($rootPath.Length + 1)
        
        # Add header
        $header = "`r`n===== File: $relativePath =====`r`n"
        Add-Content -Path $outputFile -Value $header -Encoding UTF8
        
        # Read and append file content line by line
        Get-Content -Path $file.FullName -Encoding UTF8 | ForEach-Object {
            Add-Content -Path $outputFile -Value $_ -Encoding UTF8
        }
        
        # Add spacing after file content
        Add-Content -Path $outputFile -Value "`r`n" -Encoding UTF8
        
        Write-Host "Processed: $relativePath"
    }
    catch {
        Write-Warning "Failed to process file: $($file.FullName) - $_"
    }
}

Write-Host "`nAll files have been merged into: $outputFile"
