@echo off
SET target=%~dp0

SET image=%1
if "%image%"=="" (SET image="snltty/p2p-tunnel-server")

dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/linux-alpine-x64 -r alpine-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true

echo F|xcopy "server\\server.service\\Dockerfile" "public\\publish\\linux-alpine-x64\\Dockerfile"  /s /f /h /y

cd public/publish/linux-alpine-x64
docker build -f "%target%\\public\\publish\\linux-alpine-x64\\Dockerfile" --force-rm -t %image% .

cd ../../../

docker push %image%
