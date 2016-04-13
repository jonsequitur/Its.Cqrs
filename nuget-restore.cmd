:: From https://github.com/appveyor/ci/blob/e262e1b01a1d15befd52c620f8d6e4c45e4ace37/scripts/nuget-restore.cmd

@echo off
rem initiate the retry number
set retryNumber=0
set maxRetries=3

:RESTORE
nuget restore %*

rem problem?
IF NOT ERRORLEVEL 1 GOTO :EOF
@echo Oops, nuget restore exited with code %ERRORLEVEL% - let us try again!
set /a retryNumber=%retryNumber%+1
IF %reTryNumber% LSS %maxRetries% (GOTO :RESTORE)
@echo Sorry, we tried restoring nuget packages for %maxRetries% times and all attempts were unsuccessful!
EXIT /B 1