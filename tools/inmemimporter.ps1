function Run-Command {
    param (
        [string]$Command
    )
    
    Invoke-Expression $Command
    
    if ($LASTEXITCODE -ne 0) {
        Read-Host "Press Enter to exit..."
        exit 1
    }
}

$currentDir = Get-Location

try {
    Set-Location -Path $PSScriptRoot/../source/electricity-market/InMemImporter.Application
    Run-Command "npm install"
    Run-Command "npm start"
} finally {
    Set-Location -Path $currentDir
}
