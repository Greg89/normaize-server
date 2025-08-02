# Run all tests with parallel execution
Write-Host "Running all tests with parallel execution..." -ForegroundColor Cyan

# Run all tests with parallel execution enabled
dotnet test --logger "console;verbosity=normal" --verbosity normal --configuration Release

Write-Host "All tests completed!" -ForegroundColor Cyan 