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

Run-Command "npm install --prefix ../source/electricity-market/InMemImporter.Application"
Run-Command "npm start --prefix ../source/electricity-market/InMemImporter.Application"