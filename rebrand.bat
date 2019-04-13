@echo off
setlocal enableextensions
call :find-files %1 %2
EXIT /B 0

:find-files
    set PATHS=
    set NAMES=
    for /r "%~dp0" %%P in ("*.cs","*.csproj") do (
		setlocal enabledelayedexpansion
        set PATHS=!PATHS! "%%~fP"
        set NAMES=!NAMES! "%%~nP%%~xP"
		set currentPath=%%~fP
		set currentPathNoObj=!currentPath:\obj\=!
		
		REM Ignore .vs and .git
		if "x!currentPath:\.git\=!"=="x!currentPath!" (
			if "x!currentPath:\.vs\=!"=="x!currentPath!" (
				if !currentPath!==!currentPathNoObj! (
					echo PROCESSING "%%~fP"
					call :replace-in-file "%~1" "%~2" "%%~fP"
				)
			)
		)
		endlocal
    )
EXIT /B 0

REM Arguments can be comma separated lists, or single items
:replace-in-file
	setlocal enabledelayedexpansion
	set arg1=%~1
	set arg2=%~2
	set textFile=%~3
	set tmpFile=tmp
	
	set /a index = 0
	for %%i in (!arg1!) do (
	   set toReplace[!index!]=%%i
	   set /a index += 1
	)
	
	set /a replaceSize=index-1
		
	set /a index = 0
	for %%i in (!arg2!) do (
	   set replaceWith[!index!]=%%i
	   set /a index += 1
	)
	
	set /a index = 0
	set replacements=
	for /l %%x in (0,1,!replaceSize!) do (	
		set replace=!toReplace[%%x]!
		set with=!replaceWith[%%x]!
		set replacements=!replacements! "!replace!=!with!"
		set /a index += 1
	)
	
	setlocal disabledelayedexpansion
	for /f "delims=" %%i in ('findstr /n "^" "%textFile%"') do (
		set "line=%%i"
		
		setlocal enabledelayedexpansion
		set "line=!line:*:=!"
		if ["!line!"]==[""] (
			>>"%tmpFile%" echo(!line!
		) else (
			for %%X in (!replacements!) do (
				for /f "tokens=1,2 delims==" %%Y in (%%X) do (
					set line=!line:%%Y=%%Z!
				)
			)
			>>"%tmpFile%" echo(!line!
		)
		endlocal
	)
	
	break>"%textFile%"
	
	type "%tmpFile%" >> "%textFile%"
	DEL "%tmpFile%"
EXIT /B 0