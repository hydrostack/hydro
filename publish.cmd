@ECHO OFF
del src\bin\Release\*.nupkg
dotnet pack src -c Release
nuget push src\bin\Release\*.nupkg -Source https://api.nuget.org/v3/index.json