# Extract all readable data from .NET Auth Service project
# Output: auth.txt in root directory

$outputFile = "auth.txt"
$rootPath = Get-Location

# Folders to scan
$targetFolders = @(
    "Ecom.Auth.API",
    "Ecom.Auth.Application",
    "Ecom.Auth.Domain",
    "Ecom.Auth.Infrastructure",
    "Ecom.Auth.Tests"
)

# File extensions to include
$includeExtensions = @("*.cs", "*.json", "*.config", "*.csproj", "*.md", "*.txt","obj")

# Folders to exclude
$excludeFolders = @("bin",  ".git", ".vs", "node_modules")

# Clear output file if exists
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

Write-Host "Starting extraction..." -ForegroundColor Green

$fileCount = 0

foreach ($folder in $targetFolders) {
    $folderPath = Join-Path $rootPath $folder
    
    if (-not (Test-Path $folderPath)) {
        Write-Host "Skipping $folder (not found)" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "Processing $folder..." -ForegroundColor Cyan
    
    # Get all files recursively
    foreach ($extension in $includeExtensions) {
        $files = Get-ChildItem -Path $folderPath -Filter $extension -Recurse -File -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            # Check if file is in excluded folder
            $shouldExclude = $false
            foreach ($excludeFolder in $excludeFolders) {
                if ($file.FullName -like "*\$excludeFolder\*") {
                    $shouldExclude = $true
                    break
                }
            }
            
            if ($shouldExclude) {
                continue
            }
            
            try {
                # Get relative path
                $relativePath = $file.FullName.Substring($rootPath.Path.Length + 1)
                
                # Write header
                $header = "`n`n===== File: $relativePath =====`n"
                Add-Content -Path $outputFile -Value $header -Encoding UTF8
                
                # Read and write file content
                $content = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
                Add-Content -Path $outputFile -Value $content -Encoding UTF8
                
                $fileCount++
                Write-Host "  Added: $relativePath" -ForegroundColor Gray
            }
            catch {
                Write-Host "  Error reading: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

Write-Host "`nExtraction complete!" -ForegroundColor Green
Write-Host "Total files processed: $fileCount" -ForegroundColor Green
Write-Host "Output file: $outputFile" -ForegroundColor Green
