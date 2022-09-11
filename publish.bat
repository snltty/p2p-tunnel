@echo off

rem 24行发布安卓那里，改为自己的Android sdk地址, 可以在 工具->选项->Xamarin->Android设置 里查看sdk地址

rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip

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

dotnet publish ./client/client.service.app -c:Release -f:net6.0-android /p:AndroidSigningKeyPass=123321 /p:AndroidSigningStorePass=123321  /p:AndroidSdkDirectory=D:\\Android\\android-sdk
echo F|xcopy "client\\client.service.app\\bin\\Release\\net6.0-android\\publish\\p2p_tunnel.p2p_tunnel-Signed.apk" "public\\publish-zip\\p2p-tunnel.apk"  /s /f /h /y


for %%r in (x64,arm64) do (
	for %%f in (tun2socks-linux) do (
		echo F|xcopy "client\\plugins\\client.service.vea\\%%f" "public\\publish\\linux-%%r-single\\client\\"  /f /h /y
		del  "public\\publish\\win-%%r-single\\client\\%%f"
		del  "public\\publish\\osx-%%r-single\\client\\%%f"
	)	
	for %%f in (tun2socks-windows.exe,wintun.dll) do (
		echo F|xcopy "client\\plugins\\client.service.vea\\%%f" "public\\publish\\win-%%r-single\\client\\"  /f /h /y
		del  "public\\publish\\linux-%%r-single\\client\\%%f"
		del  "public\\publish\\osx-%%r-single\\client\\%%f"
	)
	for %%f in (nssm.exe) do (
		echo F|xcopy "client\\client.realize\\public\\%%f" "public\\publish\\win-%%r-single\\client\\public\\"  /f /h /y
		echo F|xcopy "client\\client.realize\\public\\%%f" "public\\publish\\win-%%r-single\\server\\public\\"  /f /h /y
		del  "public\\publish\\linux-%%r-single\\client\\public\\%%f"
		del  "public\\publish\\linux-%%r-single\\server\\public\\%%f"
		del  "public\\publish\\osx-%%r-single\\client\\public\\%%f"
		del  "public\\publish\\osx-%%r-single\\server\\public\\%%f"
	)
	for %%p in (win,linux,osx) do (
		echo D|xcopy "client\\plugins\\client.service.ui\\client.service.ui.api.service\\public\\web\\" "public\\publish\\%%p-%%r-single\\client\\public\\web\\" /s /f /h /y
	)
)

7z a -tzip ./public/publish-zip/any.zip ./public/publish/any/*
7z a -tzip ./public/publish-zip/win-x64-single.zip ./public/publish/win-x64-single/*
7z a -tzip ./public/publish-zip/win-arm64-single.zip ./public/publish/win-arm64-single/*
7z a -tzip ./public/publish-zip/linux-x64-single.zip ./public/publish/linux-x64-single/*
7z a -tzip ./public/publish-zip/linux-arm64-single.zip ./public/publish/linux-arm64-single/*
7z a -tzip ./public/publish-zip/osx-x64-single.zip ./public/publish/osx-x64-single/*
7z a -tzip ./public/publish-zip/osx-arm64-single.zip ./public/publish/osx-arm64-single/*
