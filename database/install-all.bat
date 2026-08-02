@echo off
chcp 65001 >nul
title CloudWarehouse 数据库安装
cd /d "%~dp0"

set "SERVER=%~1"
if "%SERVER%"=="" set "SERVER=localhost"

echo ========================================
echo   CloudWarehouse 数据库安装
echo   SQL Server: %SERVER%
echo ========================================
echo.
echo 前提：已安装 SQL Server，且本机可用 Windows 身份验证 (sqlcmd -E)
echo 若需 sa 登录，请先执行 setup-sql-authentication.sql
echo.

call :run schema.sql
if errorlevel 1 goto fail

call :run billing-schema.sql CloudWarehouse
if errorlevel 1 goto fail

call :run customer-quote-schema.sql CloudWarehouse
if errorlevel 1 goto fail

call :run seed-demo-93.sql CloudWarehouse
if errorlevel 1 goto fail

call :run fix-price-rules-index.sql CloudWarehouse
if errorlevel 1 goto fail

echo.
echo ========================================
echo   全部脚本执行完成
echo   数据库: CloudWarehouse
echo   下一步: 修改 appsettings.json 连接串，双击 启动云仓.bat
echo ========================================
pause
exit /b 0

:run
set "FILE=%~1"
set "DB=%~2"
echo.
echo --- %FILE% ---
if "%DB%"=="" (
    sqlcmd -S %SERVER% -E -C -i "%FILE%"
) else (
    sqlcmd -S %SERVER% -E -C -d %DB% -i "%FILE%"
)
if errorlevel 1 (
    echo [失败] %FILE%
    exit /b 1
)
exit /b 0

:fail
echo.
echo 安装失败，请用 SSMS 打开 database 目录下 SQL 手动执行。
pause
exit /b 1
