@echo off
SET target=%~dp0

SET image=%1
if "%image%"=="" (SET image="snltty/p2p-tunnel-server")

docker build -f "%target%\server\server.service\Dockerfile" --force-rm -t %image% "%target%\"

docker push %image%
