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

FROM alpine:latest
ENV TZ=Asia/Shanghai DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
RUN echo "https://mirrors.tuna.tsinghua.edu.cn/alpine/latest-stable/main/" > /etc/apk/repositories \
    && apk add --no-cache libstdc++ libintl tzdata zeromq \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime \
    && echo $TZ > /etc/timezone

EXPOSE 5410/udp
EXPOSE 5410/tcp

WORKDIR /app
COPY . .
ENTRYPOINT ["./server.service"]