@echo off

rd /s /q public\\publish
rd /s /q public\\publish-zip

dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/any/client 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/any/server 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/win-x64-single/client	-r win-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/win-x64-single/server	-r win-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/win-arm64-single/client	-r win-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/win-arm64-single/server	-r win-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/linux-x64-single/client	-r linux-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/linux-x64-single/server	-r linux-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/linux-arm64-single/client	-r linux-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/linux-arm64-single/server	-r linux-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/osx-x64-single/client	-r osx-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/osx-x64-single/server	-r osx-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./client/client.service -c release -f net6.0 -o ./public/publish/osx-arm64-single/client	-r osx-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 
dotnet publish ./server/server.service -c release -f net6.0 -o ./public/publish/osx-arm64-single/server	-r osx-arm64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true 


mkdir public\\publish-zip
cd public/publish
winrar a -r ../publish-zip/any.zip any
winrar a -r ../publish-zip/win-x64-single.zip win-x64-single
winrar a -r ../publish-zip/win-arm64-single.zip win-arm64-single
winrar a -r ../publish-zip/linux-x64-single.zip linux-x64-single
winrar a -r ../publish-zip/linux-arm64-single.zip linux-arm64-single
winrar a -r ../publish-zip/osx-x64-single.zip osx-x64-single
winrar a -r ../publish-zip/osx-arm64-single.zip osx-arm64-single

cd ../../