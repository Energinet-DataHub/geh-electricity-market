$currentDir = Get-Location

try {
    Set-Location -Path $PSScriptRoot/../source/electricity-market/ElectricityMarket.IntegrationTests
    dotnet test --filter "Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios.ScenarioTests.Run_All_Scenarios_And_Verify_Results"
} finally {
    Set-Location -Path $currentDir
}

