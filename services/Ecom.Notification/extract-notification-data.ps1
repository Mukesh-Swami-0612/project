# PowerShell script to extract all readable data from .NET Notification Service project
# Output: notification.txt in the root directory

$outputFile = "notification.txt"
$rootPath = Get-Location

# Define folders to include
$includeFolders = @(
    "Ecom.Notification.API",
    "Ecom.Notification.Application",
    "Ecom.Notification.Domain",
    "Ecom.Notification.Infrastructure",
    "Ecom.Notification.Tests",
    "Ecom.Notification.Worker"
)

# Define file extensions to include
$includeExtensions = @("*.cs", "*.json", "*.config", "*.csproj", "*.md", "*.txt", "*.sln", "*.slnx", "*.user","obj")

# Define folders to exclude
$excludeFolders = @("bin", ".git", ".vs", "node_modules")

# Initialize output file
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}

New-Item -Path $outputFile -ItemType File -Force | Out-Null

Write-Host "Starting extraction process..." -ForegroundColor Green

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

# Function to append file content safely
function Append-FileContent {
    param($filePath, $relativePath)
    
    try {
        $header = "`r`n===== File: $relativePath =====`r`n"
        Add-Content -Path $outputFile -Value $header -Encoding UTF8
        
        # Read file in chunks to handle large files
        $content = Get-Content -Path $filePath -Raw -ErrorAction Stop
        Add-Content -Path $outputFile -Value $content -Encoding UTF8
        Add-Content -Path $outputFile -Value "`r`n" -Encoding UTF8
        
        Write-Host "Processed: $relativePath" -ForegroundColor Cyan
    }
    catch {
        $errorMsg = "ERROR reading file: $relativePath - $($_.Exception.Message)"
        Add-Content -Path $outputFile -Value $errorMsg -Encoding UTF8
        Write-Host $errorMsg -ForegroundColor Yellow
    }
}

# Process root-level files (.sln, .slnx, .user)
Write-Host "`nProcessing root-level files..." -ForegroundColor Green
$rootFiles = Get-ChildItem -Path $rootPath -File | Where-Object {
    $_.Extension -in @(".sln", ".slnx", ".user")
}

foreach ($file in $rootFiles) {
    $relativePath = $file.Name
    Append-FileContent -filePath $file.FullName -relativePath $relativePath
}

# Process each included folder
foreach ($folder in $includeFolders) {
    $folderPath = Join-Path -Path $rootPath -ChildPath $folder
    
    if (-not (Test-Path $folderPath)) {
        Write-Host "Folder not found: $folder" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "`nProcessing folder: $folder" -ForegroundColor Green
    
    # Get all files recursively with included extensions
    foreach ($extension in $includeExtensions) {
        $files = Get-ChildItem -Path $folderPath -Filter $extension -Recurse -File -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            # Skip excluded folders
            if (Should-Exclude -path $file.FullName) {
                continue
            }
            
            # Get relative path from root
            $relativePath = $file.FullName.Substring($rootPath.Path.Length + 1)
            
            Append-FileContent -filePath $file.FullName -relativePath $relativePath
        }
    }
}

Write-Host "`nExtraction complete! Output saved to: $outputFile" -ForegroundColor Green
Write-Host "File size: $((Get-Item $outputFile).Length / 1MB) MB" -ForegroundColor Green
