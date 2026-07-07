@echo off
chcp 65001 >nul
setlocal

echo ============================================
echo  RCK Is Takibi - Release Publish
echo ============================================
echo.
echo Uygulama aciksa once kapatın. Verileriniz publish\Data klasorunde kalir.
echo.

cd /d "%~dp0"

set "OUT=%~dp0bin\Release\publish"

dotnet publish "%~dp0RizaCanKilicIsTakibi.csproj" -c Release -o "%OUT%"
if errorlevel 1 (
    echo.
    echo HATA: Derleme basarisiz.
    pause
    exit /b 1
)

echo.
echo Basarili. Calistirilacak dosya:
echo %OUT%\RizaCanKilicIsTakibi.exe
echo.
pause
