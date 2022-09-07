#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["client/client.service/client.service.csproj", "client/client.service/"]
COPY ["client/plugins/client.service.ftp/client.service.ftp.csproj", "client/plugins/client.service.ftp/"]
COPY ["client/plugins/client.service.ui/client.service.ui.api/client.service.ui.api.csproj", "client/plugins/client.service.ui/client.service.ui.api/"]
COPY ["client/client/client.csproj", "client/client/"]
COPY ["common/common.libs/common.libs.csproj", "common/common.libs/"]
COPY ["common/common.server/common.server.csproj", "common/common.server/"]
COPY ["client/plugins/client.service.ui/client.service.ui.api.service/client.service.ui.api.service.csproj", "client/plugins/client.service.ui/client.service.ui.api.service/"]
COPY ["client/plugins/client.service.ui/client.service.ui.api.webrtc/client.service.ui.api.webrtc.csproj", "client/plugins/client.service.ui/client.service.ui.api.webrtc/"]
COPY ["server/plugins/server.service.webrtc.models/server.service.webrtc.models.csproj", "server/plugins/server.service.webrtc.models/"]
COPY ["client/plugins/client.service.socks5/client.service.socks5.csproj", "client/plugins/client.service.socks5/"]
COPY ["common/common.socks5/common.socks5.csproj", "common/common.socks5/"]
COPY ["client/plugins/client.service.ui/client.service.ui.api.manager/client.service.ui.api.manager.csproj", "client/plugins/client.service.ui/client.service.ui.api.manager/"]
COPY ["server/plugins/server.service.manager.models/server.service.manager.models.csproj", "server/plugins/server.service.manager.models/"]
COPY ["client/plugins/client.service.logger/client.service.logger.csproj", "client/plugins/client.service.logger/"]
COPY ["client/plugins/client.service.tcpforward/client.service.tcpforward.csproj", "client/plugins/client.service.tcpforward/"]
COPY ["common/common.tcpforward/common.tcpforward.csproj", "common/common.tcpforward/"]
RUN dotnet restore "client/client.service/client.service.csproj"
COPY . .
WORKDIR "/src/client/client.service"
RUN dotnet build "client.service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "client.service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "client.service.dll"]