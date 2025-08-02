@echo off
echo 🔍 Checking for outdated packages...

REM Get outdated packages
dotnet list package --outdated

echo.
echo 🚀 Starting package updates...

REM Update packages for each project
echo 📁 Updating Normaize.API...
dotnet add Normaize.API/Normaize.API.csproj package CsvHelper --version 33.1.0
dotnet add Normaize.API/Normaize.API.csproj package DotNetEnv --version 3.1.1
dotnet add Normaize.API/Normaize.API.csproj package EPPlus --version 8.0.8
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.AspNetCore.Cors --version 2.3.0
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.AspNetCore.OpenApi --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.EntityFrameworkCore.InMemory --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.EntityFrameworkCore.Tools --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Serilog.AspNetCore --version 9.0.0
dotnet add Normaize.API/Normaize.API.csproj package Serilog.Enrichers.Environment --version 3.0.1
dotnet add Normaize.API/Normaize.API.csproj package Serilog.Enrichers.Process --version 3.0.0
dotnet add Normaize.API/Normaize.API.csproj package Serilog.Enrichers.Thread --version 4.0.0
dotnet add Normaize.API/Normaize.API.csproj package Serilog.Sinks.Console --version 6.0.0
dotnet add Normaize.API/Normaize.API.csproj package Serilog.Sinks.Seq --version 9.0.0
dotnet add Normaize.API/Normaize.API.csproj package Microsoft.Extensions.Logging.Abstractions --version 9.0.7
dotnet add Normaize.API/Normaize.API.csproj package Serilog --version 4.3.0
dotnet add Normaize.API/Normaize.API.csproj package System.Text.Json --version 9.0.7

echo 📁 Updating Normaize.Core...
dotnet add Normaize.Core/Normaize.Core.csproj package AutoMapper --version 15.0.1
dotnet add Normaize.Core/Normaize.Core.csproj package CsvHelper --version 33.1.0
dotnet add Normaize.Core/Normaize.Core.csproj package EPPlus --version 8.0.8
dotnet add Normaize.Core/Normaize.Core.csproj package FluentValidation --version 12.0.0
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.AspNetCore.Http.Abstractions --version 2.3.0
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.Extensions.Caching.Memory --version 9.0.7
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.Extensions.Configuration --version 9.0.7
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.Extensions.Configuration.Abstractions --version 9.0.7
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.Extensions.Configuration.Binder --version 9.0.7
dotnet add Normaize.Core/Normaize.Core.csproj package Microsoft.Extensions.Logging.Abstractions --version 9.0.7
dotnet add Normaize.Core/Normaize.Core.csproj package Serilog --version 4.3.0
dotnet add Normaize.Core/Normaize.Core.csproj package System.Text.Json --version 9.0.7

echo 📁 Updating Normaize.Data...
dotnet add Normaize.Data/Normaize.Data.csproj package AWSSDK.S3 --version 4.0.6.2
dotnet add Normaize.Data/Normaize.Data.csproj package DotNetEnv --version 3.1.1
dotnet add Normaize.Data/Normaize.Data.csproj package Microsoft.EntityFrameworkCore --version 9.0.7
dotnet add Normaize.Data/Normaize.Data.csproj package Microsoft.EntityFrameworkCore.Tools --version 9.0.7
dotnet add Normaize.Data/Normaize.Data.csproj package Serilog --version 4.3.0

echo 📁 Updating Normaize.Tests...
dotnet add Normaize.Tests/Normaize.Tests.csproj package coverlet.collector --version 6.0.4
dotnet add Normaize.Tests/Normaize.Tests.csproj package FluentAssertions --version 8.5.0
dotnet add Normaize.Tests/Normaize.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 9.0.7
dotnet add Normaize.Tests/Normaize.Tests.csproj package Microsoft.NET.Test.Sdk --version 17.14.1
dotnet add Normaize.Tests/Normaize.Tests.csproj package Moq --version 4.20.72
dotnet add Normaize.Tests/Normaize.Tests.csproj package xunit --version 2.9.3
dotnet add Normaize.Tests/Normaize.Tests.csproj package xunit.runner.visualstudio --version 3.1.3

echo.
echo 🔄 Restoring packages...
dotnet restore

echo.
echo 🔨 Building solution...
dotnet build

echo.
echo 🧪 Running tests...
dotnet test --verbosity normal

echo.
echo 🎉 Package update process completed!
pause 