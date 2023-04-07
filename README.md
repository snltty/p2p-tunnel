<!--
 * @Author: snltty
 * @Date: 2021-08-22 14:09:03
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-21 16:36:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.webd:\desktop\p2p-tunnel\README.md
-->
<div align="center">

# p2p-tunnel
## Visual Studio 2022 LTSC 17.4.1
#### 使用前请确保你已知其中风险，本软件仅供学习交流，请勿用于违法犯罪

![GitHub Repo stars](https://img.shields.io/github/stars/snltty/p2p-tunnel?style=social)
![GitHub Repo forks](https://img.shields.io/github/forks/snltty/p2p-tunnel?style=social)
[![star](https://gitee.com/snltty/p2p-tunnel/badge/star.svg?theme=dark)](https://gitee.com/snltty/p2p-tunnel/stargazers)
[![fork](https://gitee.com/snltty/p2p-tunnel/badge/fork.svg?theme=dark)](https://gitee.com/snltty/p2p-tunnel/members)

#### 有任何想法，皆可进群(**1121552990**)了解
</div>

这是一个内网穿透项目，包括p2p打洞穿透，服务器代理穿透，还包含了一些有趣的功能

除了rudp(<a href="https://github.com/RevenantX/LiteNetLib" target="_blank">LiteNetLib</a>)，其它代码都是撸出来的，所以代码量，内存占用率，都比较小，速度也比较快。通信速度能达到 800MB/s+

## 几种通信线路
```mermaid
    flowchart LR
    id9((访问端)) <-->|交换数据| id3([客户端A]) <-->|交换数据| id5([客户端B]) <--> id13[(内网服务)]
```
```mermaid
    flowchart LR
    id6((访问端)) <--> |交换数据| id12[(服务器)] <-->|交换数据| id5([客户端B]) <--> id13[(内网服务)]
```
```mermaid
    flowchart LR
    id7((访问端)) <-->|交换数据| id3([客户端A])<-->|交换数据| id11[(服务器)] <-->|服务器中继| id5([客户端B]) <--> id13[(内网服务)]
```
```mermaid
    flowchart LR
    id8((访问端)) <-->|交换数据| id3([客户端A])<-->|交换数据| id10[(客户端XX)] <-->|节点中继| id5([客户端B]) <--> id13[(内网服务)]
```
```mermaid
    flowchart LR
    id7((访问端)) <-->|交换数据| id3([客户端A])<-->|交换数据| id11[(服务器)] <--> id13[(外网服务)]
```

## 其它描述
- [x] **【p2p打洞】** 打洞支持tcp、udp(<a href="https://github.com/RevenantX/LiteNetLib" target="_blank">LiteNetLib rudp</a>)
- [x] **【.NET7】** 跨平台，小尺寸，小内存
- [x] **【UI界面】** 简单易用的web管理页面
- [x] **【加密】** 支持通信数据加密(预配置密钥或自动交换密钥)
- [x] **【插件式】** 可扩展的插件式
- [x] **【高效】** 高效的打包解包，作死的全手写序列化，通信速度 **800MB/s+**
- [x] **【节点中继】** 如果你有某个节点比较牛逼，可以允许某个节点作为中继节点，节省服务器带宽，节点中继可以任意节点数，中继过程不参与打包解包，仅网络消耗
- [x] **【自建服务器】** 自建服务器则可开启 服务器代理穿透，服务器中继
- [x] 免费的打洞服务器
- [x] android app

## 一些写好的插件
- [x] **【账号管理】** 简单的权限配置
- [x] **【tcp转发】** 转发tcp协议
- [x] **【udp转发】** 转发udp协议
- [x] **【http代理】**  以节点或者服务端作为代理目标
- [x] **【socks5代理】** 以节点或者服务端作为代理目标
- [x] **【虚拟网卡组网】** <a href="https://github.com/xjasonlyu/tun2socks" target="_blank">tun2socks</a>虚拟网卡组网，让你的多个不同内网客户端组成一个网络，通过其ip访问，更有局域网网段绑定，访问目标局域网任意设备(**暂时仅支持windows、linux、osx**)，如果无法运行虚拟网卡软件，你可能得自行下载对应系统及cpu版本的软件进行同名替换 <a href="https://github.com/xjasonlyu/tun2socks/releases" target="_blank">tun2socks下载</a>

## 录了些个使用介绍视频
- 基础介绍 <a href="https://www.bilibili.com/video/BV1Pa411R79U/">https://www.bilibili.com/video/BV1Pa411R79U/</a>
- 联机cs1.6 <a href="https://www.bilibili.com/video/BV18d4y1u7DN/">https://www.bilibili.com/video/BV18d4y1u7DN/</a>

## 部署和运行
### windows 可使用托盘程序
> client.service.tray.exe    //客户端

>server.service.tray.exe    //服务端

### linux 按你喜欢的方式进行托管
- <a href="./readme/server-linux.md">服务端 linux docker托管</a>
- <a href="./readme/client-linux.md">客户端 linux supervisor托管</a>
- 服务端docker镜像  **snltty/p2p-tunnel-server**
- 客户端端docker镜像  **snltty/p2p-tunnel-client**


