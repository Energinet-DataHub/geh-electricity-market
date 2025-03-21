$currentDir = Get-Location

try {
    Set-Location -Path $PSScriptRoot/../source/electricity-market/ElectricityMarket.IntegrationTests
    dotnet test --logger "console;verbosity=detailed" --filter "Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios.ScenarioTests.Test_Scenario"
} finally {
    Set-Location -Path $currentDir
}

