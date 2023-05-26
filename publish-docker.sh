@echo off
SET target=$(cd $(dirname $0); pwd)

image=$1
if [ "${image}" = ""]; then
image="snltty/p2p-tunnel"
fi


fs=('client' 'server')
ps=('alpine' 'debian')
rs=('x64' 'arm64')

for f in ${fs[@]} 
do
	for p in ${ps[@]} 
	do
		for r in ${rs[@]} 
		do
			dotnet publish ./${f}/${f}.service -c release -f net7.0 -o ./public/publish/linux-${p}-${r}/${f}  -r ${p}-${r}  --self-contained true -p:TieredPGO=true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebuggerSupport=false -p:EnableUnsafeBinaryFormatterSerialization=false -p:EnableUnsafeUTF7Encoding=false -p:HttpActivityPropagationSupport=false -p:InvariantGlobalization=true  -p:MetadataUpdaterSupport=false  -p:UseSystemResourceKeys=true  -p:TrimMode=partial
			cp -rf public/publish/linux-${p}-${r}/${f}/${f}.service public/publish/linux-${p}-${r}/${f}/service.run
			rm -rf public/publish/linux-${p}-${r}/${f}/${f}.service
			cp -rf client/client.service/Dockerfile-${p}-${f} public/publish/linux-${p}-${r}/${f}/Dockerfile-${p}-${f}
			cp -rf client/plugins/client.service.vea/tun2socks-linux-${r} public/publish/linux-${p}-${r}/${f}/tun2socks-linux
			rm -rf public/publish/linux-${p}-${r}/${f}/tun2socks-osx
			rm -rf public/publish/linux-${p}-${r}/${f}/tun2socks-windows.exe
			rm -rf public/publish/linux-${p}-${r}/${f}/wintun.dll

			cp -rf client/plugins/client.service.ui/client.service.ui.api.service/public/web/ public/publish/${p}-${r}-single/client/public/web/
		done

		cd public/publish/linux-${p}-x64/${f}
		docker buildx build -f ${target}/public/publish/linux-${p}-x64/${f}/Dockerfile-${p}-${f} --platform="linux/x86_64"  --force-rm -t "${image}-${f}-${p}-x64" . --push
		cd ../../../../

		cd public/publish/linux-${p}-arm64/${f}
		docker buildx build -f ${target}/public/publish/linux-${p}-arm64/${f}/Dockerfile-${p}-${f} --platform="linux/arm64"  --force-rm -t "${image}-${f}-${p}-arm64" . --push
		cd ../../../../
	done
done