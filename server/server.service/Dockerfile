#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#docker run -it --entrypoint sh snltty/p2p-tunnel-server

# 拉取  运行
# docker pull snltty/p2p-tunnel-server
# docker run -it -d --name="p2p-tunnel-server" -p 5410:5410/udp -p 59410:59410/tcp snltty/p2p-tunnel-server

# 创建本地目录用来保存临时配置文件，用于修改配置信息
# cd /usr/local
# mkdir p2p-tunnel-server
# cd p2p-tunnel-server

# 把配置文件从容器里复制出来
# docker cp p2p-tunnel-server:/app/appsettings.json /usr/local/p2p-tunnel-server/appsettings.json
# docker cp p2p-tunnel-server:/app/socks5-appsettings.json /usr/local/p2p-tunnel-server/socks5-appsettings.json
# docker cp p2p-tunnel-server:/app/tcpforward-appsettings.json /usr/local/p2p-tunnel-server/tcpforward-appsettings.json
# docker cp p2p-tunnel-server:/app/udpforward-appsettings.json /usr/local/p2p-tunnel-server/udpforward-appsettings.json

# 修改配置
# vim  
# i 进入编辑修改
# esc 退出编辑
# :wq 保存退出

# 把配置文件复制进去
# docker cp /usr/local/p2p-tunnel-server/appsettings.json  p2p-tunnel-server:/app/appsettings.json
# docker cp /usr/local/p2p-tunnel-server/socks5-appsettings.json  p2p-tunnel-server:/app/socks5-appsettings.json
# docker cp /usr/local/p2p-tunnel-server/tcpforward-appsettings.json  p2p-tunnel-server:/app/tcpforward-appsettings.json
# docker cp /usr/local/p2p-tunnel-server/udpforward-appsettings.json  p2p-tunnel-server:/app/udpforward-appsettings.json

# 重启容器
# docker restart p2p-tunnel-server

FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

EXPOSE 5410/udp
EXPOSE 59410/tcp

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["server/server.service/server.service.csproj", "server/server.service/"]
COPY ["common/common.libs/common.libs.csproj", "common/common.libs/"]
COPY ["common/common.server/common.server.csproj", "common/common.server/"]
COPY ["server/plugins/server.service.manager/server.service.manager.csproj", "server/plugins/server.service.manager/"]
COPY ["server/server/server.csproj", "server/server/"]
COPY ["server/plugins/server.service.manager.models/server.service.manager.models.csproj", "server/plugins/server.service.manager.models/"]
COPY ["server/plugins/server.service.socks5/server.service.socks5.csproj", "server/plugins/server.service.socks5/"]
COPY ["common/common.socks5/common.socks5.csproj", "common/common.socks5/"]
COPY ["server/plugins/server.service.tcpforward/server.service.tcpforward.csproj", "server/plugins/server.service.tcpforward/"]
COPY ["common/common.tcpforward/common.tcpforward.csproj", "common/common.tcpforward/"]
COPY ["server/plugins/server.service.udpforward/server.service.udpforward.csproj", "server/plugins/server.service.udpforward/"]
COPY ["common/common.udpforward/common.udpforward.csproj", "common/common.udpforward/"]
RUN dotnet restore "server/server.service/server.service.csproj"
COPY . .
WORKDIR "/src/server/server.service"
FROM build AS publish
RUN dotnet publish "server.service.csproj" -c release -o /app/publish 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet","/app/server.service.dll"]