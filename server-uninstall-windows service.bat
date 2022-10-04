@echo off

SET exepath=%~dp0

cd %exepath%public

%~d0

echo 停止服务==========================
nssm stop p2p-tunnel-server
echo.
echo.
echo 删除服务=======================
nssm remove p2p-tunnel-server confirm
echo =================================
echo.
echo.
echo 已完成


pause