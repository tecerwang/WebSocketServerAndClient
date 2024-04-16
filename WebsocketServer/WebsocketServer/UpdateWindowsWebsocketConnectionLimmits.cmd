@echo off
set /p max_connections=Enter the maximum number of WebSocket connections per server (between 2 and 128): 

if %max_connections% LSS 2 (
    echo Error: The value must be at least 2.
    exit /b 1
) 

if %max_connections% GTR 128 (
    echo Error: The value cannot exceed 128.
    exit /b 1
)

reg add "HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_WEBSOCKET_MAXCONNECTIONSPERSERVER" /v iexplore.exe /t REG_DWORD /d %max_connections% /f
echo Successfully set the maximum number of WebSocket connections per server to %max_connections%.

pause
