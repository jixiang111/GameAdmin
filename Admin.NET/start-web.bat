@echo off
setlocal

set "ROOT=%~dp0"
cd /d "%ROOT%"

set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5005"

dotnet run --project "Admin.NET.Web.Entry\Admin.NET.Web.Entry.csproj"
