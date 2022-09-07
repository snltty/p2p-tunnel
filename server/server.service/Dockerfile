#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 5410
EXPOSE 59410

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["server/server.service/server.service.csproj", "server/server.service/"]
COPY ["common/common.libs/common.libs.csproj", "common/common.libs/"]
COPY ["server/plugins/server.service.webrtc/server.service.webrtc.csproj", "server/plugins/server.service.webrtc/"]
COPY ["server/plugins/server.service.webrtc.models/server.service.webrtc.models.csproj", "server/plugins/server.service.webrtc.models/"]
COPY ["server/server/server.csproj", "server/server/"]
COPY ["common/common.server/common.server.csproj", "common/common.server/"]
COPY ["server/plugins/server.service.tcpforward/server.service.tcpforward.csproj", "server/plugins/server.service.tcpforward/"]
COPY ["common/common.tcpforward/common.tcpforward.csproj", "common/common.tcpforward/"]
COPY ["server/plugins/server.service.socks5/server.service.socks5.csproj", "server/plugins/server.service.socks5/"]
COPY ["common/common.socks5/common.socks5.csproj", "common/common.socks5/"]
COPY ["server/plugins/server.service.manager/server.service.manager.csproj", "server/plugins/server.service.manager/"]
COPY ["server/plugins/server.service.manager.models/server.service.manager.models.csproj", "server/plugins/server.service.manager.models/"]
RUN dotnet restore "server/server.service/server.service.csproj"
COPY . .
WORKDIR "/src/server/server.service"
RUN dotnet build "server.service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "server.service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "server.service.dll"]