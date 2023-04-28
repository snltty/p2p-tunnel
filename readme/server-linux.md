
## linux下docker托管服务端
```
// 拉取运行
docker pull snltty/p2p-tunnel-server
docker run -it -d --name="p2p-tunnel-server" -p 5410:5410/udp -p 5410:5410/tcp snltty/p2p-tunnel-server

// 创建本地目录用来保存临时配置文件，用于修改配置信息
cd /usr/local
mkdir p2p-tunnel-server
cd p2p-tunnel-server

// 把配置文件从容器里复制出来
docker cp p2p-tunnel-server:/app/appsettings.json /usr/local/p2p-tunnel-server/appsettings.json
docker cp p2p-tunnel-server:/app/service-auth-groups.json /usr/local/p2p-tunnel-server/service-auth-groups.json
docker cp p2p-tunnel-server:/app/socks5-appsettings.json /usr/local/p2p-tunnel-server/socks5-appsettings.json
docker cp p2p-tunnel-server:/app/tcpforward-appsettings.json /usr/local/p2p-tunnel-server/tcpforward-appsettings.json
docker cp p2p-tunnel-server:/app/udpforward-appsettings.json /usr/local/p2p-tunnel-server/udpforward-appsettings.json

// 修改配置

// 把修改后配置文件复制进去
docker cp /usr/local/p2p-tunnel-server/appsettings.json  p2p-tunnel-server:/app/appsettings.json
docker cp /usr/local/p2p-tunnel-server/service-auth-groups.json  p2p-tunnel-server:/app/service-auth-groups.json
docker cp /usr/local/p2p-tunnel-server/socks5-appsettings.json  p2p-tunnel-server:/app/socks5-appsettings.json
docker cp /usr/local/p2p-tunnel-server/tcpforward-appsettings.json  p2p-tunnel-server:/app/tcpforward-appsettings.json
docker cp /usr/local/p2p-tunnel-server/udpforward-appsettings.json  p2p-tunnel-server:/app/udpforward-appsettings.json

// 重启容器
docker restart p2p-tunnel-server


//docker rm p2p-tunnel-server
//docker logs --since 360m c49c8c16acbd
```
