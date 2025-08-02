# Run integration tests only
Write-Host "Running integration tests..." -ForegroundColor Yellow

# Run only integration tests
dotnet test --filter "Category=Integration" --logger "console;verbosity=normal" --verbosity normal

Write-Host "Integration tests completed!" -ForegroundColor Yellow 