#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#docker run -it --entrypoint sh snltty/p2p-tunnel-client

# 拉取  运行
# docker pull snltty/p2p-tunnel-client
# docker run -it -d --name="p2p-tunnel-client" -p 5411:5411/tcp -p 59411:59411/tcp snltty/p2p-tunnel-client

# 创建本地目录用来保存临时配置文件，用于修改配置信息
# cd /usr/local
# mkdir p2p-tunnel-client
# cd p2p-tunnel-client

# 把配置文件从容器里复制出来
# docker cp p2p-tunnel-server:/app/appsettings.json /usr/local/p2p-tunnel-client/appsettings.json
# docker cp p2p-tunnel-server:/app/socks5-appsettings.json /usr/local/p2p-tunnel-client/socks5-appsettings.json
# docker cp p2p-tunnel-server:/app/tcpforward-appsettings.json /usr/local/p2p-tunnel-client/tcpforward-appsettings.json
# docker cp p2p-tunnel-server:/app/udpforward-appsettings.json /usr/local/p2p-tunnel-client/udpforward-appsettings.json

# 修改配置
# vim  
# i 进入编辑修改
# esc 退出编辑
# :wq 保存退出

# 把配置文件复制进去
# docker cp /usr/local/p2p-tunnel-client/appsettings.json  p2p-tunnel-server:/app/appsettings.json
# docker cp /usr/local/p2p-tunnel-client/socks5-appsettings.json  p2p-tunnel-server:/app/socks5-appsettings.json
# docker cp /usr/local/p2p-tunnel-client/tcpforward-appsettings.json  p2p-tunnel-server:/app/tcpforward-appsettings.json
# docker cp /usr/local/p2p-tunnel-client/udpforward-appsettings.json  p2p-tunnel-server:/app/udpforward-appsettings.json

# 重启容器
# docker restart p2p-tunnel-client

FROM alpine:latest
ENV TZ=Asia/Shanghai DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
RUN echo "http://mirrors.aliyun.com/alpine/latest-stable/main/" > /etc/apk/repositories \
    && apk add --no-cache libstdc++ libintl tzdata zeromq bash \
    && ln -snf /usr/share/zoneinfo/$clTZ /etc/localtime \
    && echo $TZ > /etc/timezone

EXPOSE 5411/tcp
EXPOSE 59411/tcp

WORKDIR /app
COPY . .
ENTRYPOINT ["./client.service"]