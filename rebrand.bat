@echo off
setlocal enableextensions enabledelayedexpansion
call :find-files %1 %2
EXIT /B 0

:find-files
    set PATHS=
    set NAMES=
    for /r "%~dp0" %%P in ("*.cs") do (
        set PATHS=!PATHS! "%%~fP"
        set NAMES=!NAMES! "%%~nP%%~xP"
		set currentPath=%%~fP
		set currentPathNoObj=!currentPath:\obj\=!
		
		if !currentPath!==!currentPathNoObj! (
			echo REPLACING IN "%%~fP"
			call :replace-in-file %~1 %~2 "%%~fP"
		)
    )
EXIT /B 0

:replace-in-file
	setlocal disabledelayedexpansion
	set search=%~1
    set replace=%~2
    set textFile=%~3
	set tmpFile=tmp
	
	for /f "delims=" %%i in ('findstr /n "^" "%textFile%"') do (
		set "line=%%i"
		
		setlocal enabledelayedexpansion
		set "line=!line:*:=!"
		if ["!line!"]==[""] (
			>>"%tmpFile%" echo(!line!
		) else (
			>>"%tmpFile%" echo(!line:%search%=%replace%!
		)
		endlocal
	)
		
	break>"%textFile%"
	
	type "%tmpFile%" >> "%textFile%"
	DEL "%tmpFile%"
EXIT /B 0