@echo off
set GITHUB_REF=/main
set GITHUB_REF_NAME=99.0.0
dotnet build --configuration Release /p:Version=%GITHUB_REF_NAME% /p:AssemblyVersion=%GITHUB_REF_NAME%.0 /p:FileVersion=%GITHUB_REF_NAME%.0
