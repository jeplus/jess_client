# Create a batch file (register.bat) to register the extension
@echo off
REM Run as administrator
%SystemRoot%\Microsoft\Windows\v1.0\RegAsm.exe /codebase "C:\dev\vs2017\shell_extension\bin\Release\net481\JessShellExtension.dll"