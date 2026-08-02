@echo off
chcp 65001 >nul
title CloudWarehouse 云仓
cd /d "%~dp0"
rem 监听所有网卡，局域网可访问；访问地址为师傅电脑 IP
set ASPNETCORE_URLS=http://0.0.0.0:5001
echo ========================================
echo   云仓已启动
echo   本机访问: http://localhost:5001
echo   局域网访问: http://本机IP:5001  （cmd 输入 ipconfig 查看）
echo   按 Ctrl+C 停止服务
echo ========================================
CloudWarehouse.Backend.exe
