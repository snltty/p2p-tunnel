import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../views/home/Index.vue')
    },
    {
        path: '/settings.html',
        name: 'Settings',
        component: () => import('../views/settings/Index.vue')
    },
    {
        path: '/nodes.html',
        name: 'Nodes',
        component: () => import('../views/nodes/Index.vue'),
        redirect: { name: 'NodesList' },
        children: [
            {
                path: '/nodes/list.html',
                name: 'NodesList',
                component: () => import('../views/nodes/list/Index.vue'),
                meta: { name: '节点', service: 'ClientsClientService' }
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
                path: '/nodes/wakeup.html',
                name: 'NodesWakeUp',
                component: () => import('../views/nodes/wakeup/Index.vue'),
                meta: { name: '远程唤醒', service: 'WakeUpClientService' }
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
        redirect: { name: 'ServerTcpForward' },
        children: [
            {
                path: '/server-tcp-forward.html',
                name: 'ServerTcpForward',
                component: () => import('../views/server/tcpforward/Index.vue'),
                meta: { name: 'tcp转发', service: 'ServerTcpForwardClientService' }
            },
            {
                path: '/server-udp-forward.html',
                name: 'ServerUdpForward',
                component: () => import('../views/server/udpforward/Index.vue'),
                meta: { name: 'udp转发', service: 'ServerUdpForwardClientService' }
            },
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
