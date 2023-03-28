import { createRouter, createWebHashHistory } from 'vue-router'
import { shareData } from '../states/shareData'
const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../views/home/Index.vue'),
        meta: { name: '首页' }
    },
    {
        path: '/nodes.html',
        name: 'Nodes',
        component: () => import('../views/nodes/Index.vue'),
        redirect: { name: 'NodesSettings' },
        children: [
            {
                path: '/nodes/settings.html',
                name: 'NodesSettings',
                component: () => import('../views/nodes/settings/Index.vue'),
                meta: { name: '节点设置' }
            },
            {
                path: '/nodes/list.html',
                name: 'NodesList',
                component: () => import('../views/nodes/list/Index.vue'),
                meta: { name: '节点列表', service: 'ClientsClientService' }
            },
            {
                path: '/nodes/tcp-forward.html',
                name: 'NodesTcpForward',
                component: () => import('../views/nodes/tcpforward/Index.vue'),
                meta: { name: 'TCP转发', service: 'TcpForwardClientService' }
            },
            {
                path: '/nodes/udp-forward.html',
                name: 'NodesUdpForward',
                component: () => import('../views/nodes/udpforward/Index.vue'),
                meta: { name: 'UDP转发', service: 'UdpForwardClientService' }
            },
            {
                path: '/nodes/proxy.html',
                name: 'NodesProxy',
                component: () => import('../views/nodes/httpproxy/Index.vue'),
                meta: { name: 'http代理', service: 'HttpProxyClientService' }
            },
            {
                path: '/nodes/socks5.html',
                name: 'NodesSocks5',
                component: () => import('../views/nodes/socks5/Index.vue'),
                meta: { name: 'socks5代理', service: 'Socks5ClientService' }
            },
            {
                path: '/nodes/vea.html',
                name: 'NodesVea',
                component: () => import('../views/nodes/vea/Index.vue'),
                meta: { name: '虚拟网卡组网', service: 'VeaClientService' }
            },
            {
                path: '/nodes/logger.html',
                name: 'NodesLogger',
                component: () => import('../views/nodes/logger/Index.vue'),
                meta: { name: '日志信息', service: 'LoggerClientService' }
            },
            {
                path: '/nodes/configure.html',
                name: 'Nodesconfigure',
                component: () => import('../views/nodes/configure/Index.vue'),
                meta: { name: '配置文件', service: '' }
            }
        ]
    },
    {
        path: '/server.html',
        name: 'Servers',
        component: () => import('../views/server/Index.vue'),
        redirect: { name: 'ServerSettings' },
        children: [
            {
                path: '/server-settings.html',
                name: 'ServerSettings',
                component: () => import('../views/server/settings/Index.vue'),
                meta: { name: '服务器设置', service: 'ServerClientService', access: shareData.serverAccess.setting.value }
            },
            {
                path: '/server-tcp-forward.html',
                name: 'ServerTcpForward',
                component: () => import('../views/server/tcpforward/Index.vue'),
                meta: { name: 'tcp代理穿透', service: 'ServerTcpForwardClientService', access: shareData.serverAccess.tcpforward.value }
            },
            {
                path: '/server-udp-forward.html',
                name: 'ServerUdpForward',
                component: () => import('../views/server/udpforward/Index.vue'),
                meta: { name: 'udp代理穿透', service: 'ServerUdpForwardClientService', access: shareData.serverAccess.udpforward.value }
            },
            {
                path: '/server-users.html',
                name: 'ServerUsers',
                component: () => import('../views/server/users/Index.vue'),
                meta: { name: '账号管理', service: 'ServerUsersClientService', access: shareData.serverAccess.setting.value }
            },
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
