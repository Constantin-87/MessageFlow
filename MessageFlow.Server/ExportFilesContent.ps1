# Get the current working directory name
$currentDir = (Get-Item -Path ".").Name

# Define output file name based on the directory name
$outputDir = "C:\Temp"
$outputFile = "$outputDir\$currentDir-Contents.txt"

# Ensure output directory exists
if (!(Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory | Out-Null
}

# Remove existing output file if it exists
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}

Write-Host " Output file set to: $outputFile"

# Define file extensions to include (without dots)
$extensions = @("cs", "razor", "config", "csproj")

# DEBUG: List all files before filtering
Write-Host "`n DEBUG: Listing all files before filtering..."
$allFiles = Get-ChildItem -Path . -Recurse -File
foreach ($file in $allFiles) {
    $fileExt = $file.Extension
    if (-not $fileExt) { $fileExt = "No Extension" }  # Ensure no null values
    Write-Host ("Found file: {0} | Extension: {1}" -f $file.FullName, $fileExt)
}

# Get all files matching the extensions recursively, excluding 'bin' and 'obj' folders
$files = $allFiles | Where-Object {
    ($_.Extension -ne "") -and  # Ensure the file has an extension
    ($extensions -contains $_.Extension.TrimStart('.')) -and  # Match extensions
    ($_.FullName -notmatch "\\bin\\") -and 
    ($_.FullName -notmatch "\\obj\\")
}

# DEBUG: Show filtered files
Write-Host "`nDEBUG: Listing filtered files..."
foreach ($file in $files) {
    $fileExt = $file.Extension
    if (-not $fileExt) { $fileExt = "No Extension" }  # Ensure no null values
    Write-Host ("Matched file: {0} | Extension: {1}" -f $file.FullName, $fileExt)
}

# Ensure we have files to write
if ($files.Count -eq 0) {
    Write-Host "No matching files found. Exiting."
    exit
}

# Loop through each file and write its content to the output file
foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)"

    Add-Content -Path $outputFile -Value "===== FILE: $($file.FullName) ====="
    Add-Content -Path $outputFile -Value (Get-Content -Path $file.FullName -Raw)
    Add-Content -Path $outputFile -Value "`r`n"

}

# Verify if the output file was created
if (Test-Path $outputFile) {
    Write-Host "All file contents have been written to: $outputFile"
} else {
    Write-Host 'ERROR: Output file was not created. Check permissions or file paths.'
}


