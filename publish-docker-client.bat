@echo off
SET target=%~dp0

SET image=%1
if "%image%"=="" (SET image="snltty/p2p-tunnel-client")

dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/linux-alpine-x64/client -r alpine-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

echo F|xcopy "client\\client.service\\Dockerfile" "public\\publish\\linux-alpine-x64\\client\\Dockerfile"  /s /f /h /y
echo F|xcopy "client\\plugins\\client.service.vea\\tun2socks-linux" "public\\publish\\linux-alpine-x64\\client\\"  /f /h /y
del  "public\\publish\\linux-alpine-x64\\client\\tun2socks-windows.exe"
del  "public\\publish\\linux-alpine-x64\\client\\wintun.dll"


cd public/publish/linux-alpine-x64\\client
docker build -f "%target%\\public\\publish\\linux-alpine-x64\\client\\Dockerfile" --force-rm -t %image% .

cd ../../../../

docker push %image%
