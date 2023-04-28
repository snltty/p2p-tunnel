
## linux下docker托管服务端
```

// 创建本地目录用来保存临时配置文件，用于修改配置信息
cd /usr/local
mkdir p2p-tunnel-server
cd p2p-tunnel-server


// 拉取镜像
docker pull snltty/p2p-tunnel-server

//运行镜像
docker run -it -d --name="p2p-tunnel-server" -p 5410:5410/udp -p 5410:5410/tcp snltty/p2p-tunnel-server \
--privileged=true \
-v /usr/local/p2p-tunnel-server/appsettings.json:/app/appsettings.json \
-v /usr/local/p2p-tunnel-server/orward-appsettings.json:/app/forward-appsettings.json \
-v /usr/local/p2p-tunnel-server/httpproxy-appsettings.json:/app/httpproxy-appsettings.json \
-v /usr/local/p2p-tunnel-server/socks5-appsettings.json:/app/socks5-appsettings.json \
-v /usr/local/p2p-tunnel-server/users.json:/app/users.json \
-v /usr/local/p2p-tunnel-server/users-appsettings.json:/app/users-appsettings.json 

// 启动容器
docker start p2p-tunnel-server
// 停止容器
docker stop p2p-tunnel-server
// 重启容器
docker restart p2p-tunnel-server


//docker自启动
//systemctl enable docker.service
//容器自启动
//docker  update --restart=always p2p-tunnel-server

//删除容易
//docker rm p2p-tunnel-server

//查看容器日志
//docker logs --since 360m c49c8c16acbd
```
