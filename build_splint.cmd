@echo off
setlocal EnableExtensions
set "HERE=%~dp0"
set "OUT=%SPLINT_OUTPUT_DIR%"
if not defined OUT set "OUT=%HERE%dist"
set "TMP=%TEMP%\splint-managed-build-%RANDOM%-%RANDOM%"
set "HASH_SOURCE=%SPLINT_HASH_SOURCE%"
if not defined HASH_SOURCE set "HASH_SOURCE=%HERE%..\hash.txt"

if not exist "%HERE%free\Splint.cs" (
  echo FREE source files were not found.
  exit /b 1
)
if not exist "%HERE%pro\Splint.cs" (
  echo PRO source files were not found.
  exit /b 1
)
if not exist "%HASH_SOURCE%" (
  echo hash.txt was not found beside the SPLINT project.
  exit /b 1
)

set "CSCDLL="
set "SDK_VERSION="
set "DOTNET_EXE="
for /f "usebackq tokens=*" %%I in (`dotnet --list-sdks`) do (
  for /f "tokens=1 delims= " %%V in ("%%I") do set "SDK_VERSION=%%V"
)
if not defined SDK_VERSION (
  echo The .NET SDK was not found.
  exit /b 1
)
for %%D in ("%ProgramFiles%\dotnet" "%ProgramFiles(x86)%\dotnet") do (
  if not defined DOTNET_EXE if exist "%%~D\dotnet.exe" set "DOTNET_EXE=%%~D\dotnet.exe"
  if not defined CSCDLL if exist "%%~D\sdk\%SDK_VERSION%\Roslyn\bincore\csc.dll" set "CSCDLL=%%~D\sdk\%SDK_VERSION%\Roslyn\bincore\csc.dll"
)
if not defined DOTNET_EXE set "DOTNET_EXE=dotnet"
if not defined CSCDLL (
  echo Roslyn csc.dll was not found in the installed .NET SDK.
  exit /b 1
)

set "FXDIR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FXDIR%\System.Windows.Forms.dll" set "FXDIR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
if not exist "%FXDIR%\System.Windows.Forms.dll" (
  echo .NET Framework reference assemblies were not found.
  exit /b 1
)

mkdir "%TMP%" >nul 2>&1
if errorlevel 1 (
  echo Could not create the temporary build directory.
  exit /b 1
)
set "SPLINT_ROOT=%HERE%"
set "SPLINT_HASH_SOURCE=%HASH_SOURCE%"
set "SPLINT_TEMP=%TMP%"
set "SPLINT_OUTPUT=%OUT%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $root=$env:SPLINT_ROOT; $temp=$env:SPLINT_TEMP; $hashSource=$env:SPLINT_HASH_SOURCE; $proHash=$env:SPLINT_PRO_KEY_HASH; if($proHash -and $proHash -notmatch '^[0-9A-Fa-f]{64}$'){throw 'SPLINT_PRO_KEY_HASH must contain exactly 64 hexadecimal characters.'}; if(!$proHash){$proHash='0000000000000000000000000000000000000000000000000000000000000000'}; python (Join-Path $root 'pack_hashes.py') $hashSource (Join-Path $temp 'hashes.bin.gz'); if($LASTEXITCODE -ne 0){throw 'Could not pack hash.txt.'}; foreach($tier in @('free','pro')){$source=[IO.File]::ReadAllText((Join-Path $root ($tier+'\Splint.cs'))); $hash=if($tier -eq 'pro'){$proHash}else{'0000000000000000000000000000000000000000000000000000000000000000'}; $source=$source.Replace('@@PLAN_HASH@@',$hash); [IO.File]::WriteAllText((Join-Path $temp ($tier+'.cs')),$source,[Text.UTF8Encoding]::new($false))}"
if errorlevel 1 goto :fail

for %%T in (free pro) do (
  call :build_tier %%T
  if errorlevel 1 goto :fail
)

echo FREE and PRO builds created successfully in "%OUT%".
if not defined SPLINT_PRO_KEY_HASH echo PRO was compiled with an inactive development hash. Set SPLINT_PRO_KEY_HASH to a valid 64 character SHA 256 value for an activated release.
rd /s /q "%TMP%"
exit /b 0

:build_tier
set "TIER=%~1"
set "TIER_UPPER=%TIER%"
if /I "%TIER%"=="free" set "TIER_UPPER=FREE"
if /I "%TIER%"=="pro" set "TIER_UPPER=PRO"
if not exist "%OUT%\%TIER_UPPER%" mkdir "%OUT%\%TIER_UPPER%"
if /I "%TIER%"=="free" (
  "%DOTNET_EXE%" exec "%CSCDLL%" /nostdlib+ /lib:"%FXDIR%" /reference:"%FXDIR%\mscorlib.dll" /reference:"%FXDIR%\System.dll" /reference:"%FXDIR%\System.Core.dll" /nologo /codepage:65001 /optimize+ /debug- /o+ /deterministic /platform:x64 /highEntropyVA+ /langversion:latest /filealign:512 /target:winexe /win32manifest:"%HERE%app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /resource:"%TMP%\hashes.bin.gz",SPLINT.HASH.bin.gz /out:"%OUT%\%TIER_UPPER%\SPLINT.exe" "%TMP%\%TIER%.cs" "%HERE%AppSetup.cs" "%HERE%free\SplintWizard.cs" "%HERE%Heuristics.cs" "%HERE%HashDatabase.cs" "%HERE%AuthenticodeTrust.cs"
) else (
  "%DOTNET_EXE%" exec "%CSCDLL%" /nostdlib+ /lib:"%FXDIR%" /reference:"%FXDIR%\mscorlib.dll" /reference:"%FXDIR%\System.dll" /reference:"%FXDIR%\System.Core.dll" /nologo /codepage:65001 /optimize+ /debug- /o+ /deterministic /platform:x64 /highEntropyVA+ /langversion:latest /filealign:512 /target:winexe /win32manifest:"%HERE%app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /resource:"%TMP%\hashes.bin.gz",SPLINT.HASH.bin.gz /out:"%OUT%\%TIER_UPPER%\SPLINT.exe" "%TMP%\%TIER%.cs" "%HERE%AppSetup.cs" "%HERE%pro\SplintWizard.cs" "%HERE%pro\Detection.cs" "%HERE%pro\Ransom.cs" "%HERE%Heuristics.cs" "%HERE%HashDatabase.cs" "%HERE%AuthenticodeTrust.cs" "%HERE%FileMetrics.cs"
)
if errorlevel 1 exit /b 1
if /I "%TIER%"=="free" copy /y "%HERE%free\LICENSE" "%OUT%\%TIER_UPPER%\LICENSE" >nul
if /I "%TIER%"=="pro" copy /y "%HERE%pro\LICENSE" "%OUT%\%TIER_UPPER%\LICENSE" >nul
if errorlevel 1 exit /b 1
exit /b 0

:fail
echo Build failed. Check the error output above.
echo Temporary files: %TMP%
exit /b 1
