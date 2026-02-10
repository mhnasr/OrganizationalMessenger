@echo off
setlocal enabledelayedexpansion
 
set OUTPUT_FILE=all_cs_xaml_files.txt
set COUNT=0
 
REM حذف فایل خروجی قبلی
if exist "%OUTPUT_FILE%" del "%OUTPUT_FILE%"
 
echo ============================================= > "%OUTPUT_FILE%"
echo Merged Source Files - %date% %time% >> "%OUTPUT_FILE%"
echo Excluded: bin, obj, Migrations folders >> "%OUTPUT_FILE%"
echo ============================================= >> "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"
 
echo Starting to merge .cs, .xaml, .html, .htm, .css, .js files (excluding bin, obj, Migrations)...
echo.

REM پسوندهای مورد نظر
for %%E in (cs xaml html cshtml htm css js) do (
    for /R %%F in (*.%%E) do (
        set "FilePath=%%~dpF"
        set "ProcessFile=1"
        
        REM بررسی bin
        echo !FilePath! | find /I "\\bin\\" >nul
        if !ERRORLEVEL! EQU 0 set "ProcessFile=0"
        
        REM بررسی obj
        echo !FilePath! | find /I "\\obj\\" >nul
        if !ERRORLEVEL! EQU 0 set "ProcessFile=0"
        
        REM بررسی Migrations
        echo !FilePath! | find /I "\\Migrations\\" >nul
        if !ERRORLEVEL! EQU 0 set "ProcessFile=0"
        
        if !ProcessFile! EQU 1 (
            set /A COUNT+=1
            echo [!COUNT!] %%~nxF
            
            echo. >> "%OUTPUT_FILE%"
            echo ==================== %%~nxF ==================== >> "%OUTPUT_FILE%"
            echo Path: %%~dpF >> "%OUTPUT_FILE%"
            echo. >> "%OUTPUT_FILE%"
            type "%%F" >> "%OUTPUT_FILE%"
            echo. >> "%OUTPUT_FILE%"
        )
    )
)
 
echo. >> "%OUTPUT_FILE%"
echo Total Files: !COUNT! >> "%OUTPUT_FILE%"
 
echo.
echo Completed! %COUNT% files merged into %OUTPUT_FILE%
pause
