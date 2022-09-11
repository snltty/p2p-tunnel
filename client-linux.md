## linux下supervisor托管客户端
```

//1、下载linux版本程序，放到 /usr/local/p2p-tunnel-client 文件夹

//2、安装supervisor
apt install -y supervisor

//3、写配置文件
vim /etc/supervisor/conf.d/p2p-tunnel-client.conf

[program:p2p-tunnel-client]
directory = /usr/local/p2p-tunnel-client
command = /usr/local/p2p-tunnel-client/client.service
autostart = true
startsec = 15
autorestart = true
startretries = 3
user = root
redirect_stderr = true
stdout_logfile_maxbytes = 50MB
stdout_logfile_backups = 20
stdout_logfile = /usr/local/p2p-tunnel-client/log/log.log
stderr_logfile = /usr/local/p2p-tunnel-client/log/log.err.log

//4、重新加载配置文件
supervisorctl update
//5、启动，或者重新启动
supervisorctl start p2p-tunnel-client
supervisorctl restart p2p-tunnel-client
```