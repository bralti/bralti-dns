@ECHO OFF
PUSHD "%~dp0.."
echo Compiling BraltiDNS.exe...
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /optimize+ /platform:anycpu /win32manifest:gui\app.manifest /win32icon:src\icon.ico /out:BraltiDNS.exe /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll gui\DPIShield.cs
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] BraltiDNS.exe compiled successfully!
) else (
    echo [ERROR] Compilation failed.
)
POPD
pause
