@echo off
echo Script Version 1

if not defined update_folder (
	echo Error: update_folder isn't specified!
	exit 1
)

if not defined app_path (
	echo Error: app_path isn't specified!
	exit 1
)

if not defined app_name (
	echo Error: app_name isn't specified!
	exit 1
)

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

if defined use_installer (
	echo Running Installer Update
	start "" /wait "%update_folder%\update.exe" /S
) else (
	echo Copy Update Files
	xcopy /s /y "%update_folder%" "%app_path%"
)

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
