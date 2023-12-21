@echo off
echo Script Version 1
echo Update Files Path: "%update_folder%"
echo Knet Path: "%app_path%"
echo Knet Exec Name: "%app_name%"

echo Waiting for Knet to close
set /a time=0
:retry
timeout /t 1
set /a time=time+1
IF %time%==30 goto cancel
2>nul (
	>>"%app_path%\%app_name%" (call )
) && (echo Ready) || (goto retry)

echo Copy Update Files
xcopy /s /y "%update_folder%" "%app_path%"

echo Launching Knet
echo "%app_path%\%app_name%"
start "" "%app_path%\%app_name%"

echo Cleanup
echo Deleting: "%update_folder%"
rmdir /s /q "%update_folder%"
exit 0

:cancel
echo Time limit reached, canceling...
exit 1