$currentDir = Get-Location

try {
    Set-Location -Path $PSScriptRoot/../source/electricity-market/InMemImporter.Application
    npm install
    npm start
} finally {
    Set-Location -Path $currentDir
}
