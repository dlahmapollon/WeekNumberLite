@echo off
setlocal
set "MSBUILD="
if not exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" goto missing_msbuild
for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD goto missing_msbuild
"%MSBUILD%" "%~dp0WeekNumberLite.csproj" -t:Rebuild -p:Configuration=Release -nologo -v:minimal
exit /b %errorlevel%

:missing_msbuild
echo ERROR: MSBuild was not found. Install Visual Studio Build Tools with MSBuild and the .NET Framework 4.8 Developer Pack. 1>&2
exit /b 1
