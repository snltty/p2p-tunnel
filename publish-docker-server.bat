@echo off
SET target=%~dp0

SET image=%1
if "%image%"=="" (SET image="snltty/p2p-tunnel-server")

dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/linux-alpine-x64/server -r alpine-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

echo F|xcopy "server\\server.service\\Dockerfile" "public\\publish\\linux-alpine-x64\\server\\Dockerfile"  /s /f /h /y
del  "public\\publish\\linux-alpine-x64\\server\\*.pac"

cd public/publish/linux-alpine-x64/server
docker build -f "%target%\\public\\publish\\linux-alpine-x64\\server\\Dockerfile" --force-rm -t %image% .

cd ../../../../

docker push %image%
