@echo off

SET exepath=%~dp0

cd %exepath%public

%~d0

echo 删除服务=======================
nssm remove p2p-tunnel-server confirm
echo =================================
echo.
echo.
echo 安装服务==========================
nssm install p2p-tunnel-client	 %exepath%server.service.exe
nssm set p2p-tunnel-server AppDirectory %exepath%
echo =================================
echo.
echo.
echo 设置自动延迟启动===================
sc config p2p-tunnel-server start=delayed-auto
echo =================================
echo.
echo.
echo 启动服务==========================
nssm start p2p-tunnel-server
echo.
echo.
echo 已完成


pause