@echo off
cd /d "F:\OrgMessenger\OrganizationalMessenger.Web"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
dotnet watch run --urls "https://localhost:7082;http://localhost:5133"
pause
