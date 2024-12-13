@echo off
echo Stopping Explorer
taskkill /f /im explorer.exe

rem echo Unregistering Shell Extension
rem regsvr32 /u "C:\Program Files\jess_client\Jess_client\JessShellExtension.dll"

echo Clearing Shell Extension Caches
reg delete "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\MenuOrder" /f

rem echo Reregistering Shell Extension
rem regsvr32 "C:\Program Files\jess_client\Jess_client\JessShellExtension.dll"

echo Restarting Explorer
start explorer.exe