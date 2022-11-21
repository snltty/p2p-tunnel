@echo off

SET sdkpath=%1
if "%sdkpath%"=="" (SET sdkpath="D:\\Android\\android-sdk")

rd /s /q public\\publish
rd /s /q public\\publish-zip
mkdir public\\publish-zip

rem set DOTNET_TieredPGO=1

rem 托盘程序
rem dotnet publish ./client/client.service.tray -c release -f net6.0-windows -o ./public/publish/tray-x64 -r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true
rem dotnet publish ./client/client.service.tray -c release -f net6.0-windows -o ./public/publish/tray-arm64 -r win-arm64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true
rem 客户端和服务端
for %%f in (client,server) do (
	for %%r in (win-x64,win-arm64,linux-x64,linux-arm64,linux-arm,osx-x64,osx-arm64) do (
		dotnet publish ./%%f/%%f.service -c release -f net6.0 -o ./public/publish/%%r-single/%%f  -r %%r  --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true
	)
	dotnet publish ./%%f/%%f.service -c release -f net6.0 -o ./public/publish/any/%%f 
)
rem app 改为自己的Android sdk地址, 可以在 工具->选项->Xamarin->Android设置 里查看sdk地址
dotnet publish ./client/client.service.app -c:Release -f:net6.0-android /p:AndroidSigningKeyPass=123321 /p:AndroidSigningStorePass=123321  /p:AndroidSdkDirectory=%sdkpath%
echo F|xcopy "client\\client.service.app\\bin\\Release\\net6.0-android\\publish\\p2p_tunnel.p2p_tunnel-Signed.apk" "public\\publish-zip\\p2p-tunnel.apk"  /s /f /h /y


for %%r in (x64,arm64,arm) do (
	for %%f in (tun2socks-linux) do (
		echo F|xcopy "client\\plugins\\client.service.vea\\%%f" "public\\publish\\linux-%%r-single\\client\\"  /f /h /y
		del  "public\\publish\\win-%%r-single\\client\\%%f"
		del  "public\\publish\\osx-%%r-single\\client\\%%f"
	)
	for %%f in (tun2socks-osx) do (
		echo F|xcopy "client\\plugins\\client.service.vea\\%%f" "public\\publish\\osx-%%r-single\\client\\"  /f /h /y
		del  "public\\publish\\win-%%r-single\\client\\%%f"
		del  "public\\publish\\linux-%%r-single\\client\\%%f"
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
		del  "public\\publish\\%%p-%%r-single\\server\\*.pac"
	)

	for %%p in (client,server) do (
		echo F|xcopy "%%p-install-windows service.bat" "public\\publish\\win-%%r-single\\%%p\\"  /f /h /y
		echo F|xcopy "%%p-uninstall-windows service.bat" "public\\publish\\win-%%r-single\\%%p\\"  /f /h /y
	)
	echo F|xcopy "client-install-windows service.bat" "public\\publish\\any\client\\"  /f /h /y
	echo F|xcopy "server-install-windows service.bat" "public\\publish\\any\server\\"  /f /h /y
	echo F|xcopy "client-uninstall-windows service.bat" "public\\publish\\any\client\\"  /f /h /y
	echo F|xcopy "server-uninstall-windows service.bat" "public\\publish\\any\server\\"  /f /h /y
	
)
del  "public\\publish\\any\\server\\*.pac"
rd /s /q "public\\publish\\osx-arm-single"
rd /s /q "public\\publish\\win-arm-single"

7z a -tzip ./public/publish-zip/any.zip ./public/publish/any/*
7z a -tzip ./public/publish-zip/win-x64-single.zip ./public/publish/win-x64-single/*
7z a -tzip ./public/publish-zip/win-arm64-single.zip ./public/publish/win-arm64-single/*
7z a -tzip ./public/publish-zip/linux-x64-single.zip ./public/publish/linux-x64-single/*
7z a -tzip ./public/publish-zip/linux-arm64-single.zip ./public/publish/linux-arm64-single/*
7z a -tzip ./public/publish-zip/linux-arm-single.zip ./public/publish/linux-arm-single/*
7z a -tzip ./public/publish-zip/osx-x64-single.zip ./public/publish/osx-x64-single/*
7z a -tzip ./public/publish-zip/osx-arm64-single.zip ./public/publish/osx-arm64-single/*

