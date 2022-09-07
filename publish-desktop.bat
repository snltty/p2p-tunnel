@echo off
SET target=%~dp0

cd %target%/client/client.service.command
dotnet publish -c release -f net6.0 -o d:/free/p2p-tunnel -r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o d:/free/p2p-tunnel/ -r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
cd %target%/client/client.service.tray
dotnet publish -c release -o d:/free/p2p-tunnel -r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

cd %target%
pause