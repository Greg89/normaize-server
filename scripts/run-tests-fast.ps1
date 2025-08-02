# Run fast unit tests only for quick feedback during development
Write-Host "Running fast unit tests only..." -ForegroundColor Green

# Run only unit tests (excluding integration, slow, and external tests)
dotnet test --filter "Category!=Integration&Category!=Slow&Category!=External&Category!=Database" --logger "console;verbosity=minimal" --verbosity minimal

Write-Host "Fast tests completed!" -ForegroundColor Green 