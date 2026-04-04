@echo off
echo Building elara-macro for Windows 64-bit...
set GOARCH=amd64
set GOOS=windows
go build -ldflags "-H windowsgui -s -w" -o elara-macro.exe .
if %errorlevel% neq 0 (
    echo Build FAILED.
    exit /b %errorlevel%
)
echo Build SUCCESS: elara-macro.exe
