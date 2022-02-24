@echo off
if [%2] == [] goto EOF

echo Parameters: %*

set SRC=%~p1
set NAME=%~2

set TARGET=..\..\..\Bin64\Plugins\Local
mkdir %TARGET% >NUL 2>&1

echo.
echo Deploying CLIENT plugin binary:
echo.
:RETRY
ping -n 2 127.0.0.1 >NUL 2>&1
echo From %1 to "%TARGET%\"
copy /y %1 "%TARGET%\"
IF %ERRORLEVEL% NEQ 0 GOTO :RETRY
echo Copying "%SRC%\0Harmony.dll" into "%TARGET%\"
copy /y "%SRC%\0Harmony.dll" "%TARGET%\"
echo Done
echo.
exit 0

:EOF