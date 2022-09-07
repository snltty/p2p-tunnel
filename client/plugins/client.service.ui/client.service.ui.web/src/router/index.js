/*
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-09-01 16:50:48
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\router\index.js
 */
import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../views/home/Index.vue')
    },
    {
        path: '/register.html',
        name: 'Register',
        component: () => import('../views/Register.vue')
    },
    {
        path: '/services.html',
        name: 'Services',
        component: () => import('../views/service/Index.vue'),
        redirect: { name: 'ServiceConfigure' },
        children: [
            {
                path: '/vea.html',
                name: 'Vea',
                component: () => import('../views/service/vea/Index.vue'),
                meta: { name: '组网', service: 'VeaClientService' }
            },
            {
                path: '/service-tcp-forward.html',
                name: 'ServiceTcpForward',
                component: () => import('../views/service/tcpforward/Index.vue'),
                meta: { name: 'TCP转发', service: 'TcpForwardClientService' }
            },
            {
                path: '/service-http-proxy.html',
                name: 'ServiceHttpProxy',
                component: () => import('../views/service/httpproxy/Index.vue'),
                meta: { name: 'http代理', service: 'TcpForwardClientService' }
            },
            {
                path: '/service-udp-forward.html',
                name: 'ServiceUdpForward',
                component: () => import('../views/service/udpforward/Index.vue'),
                meta: { name: 'UDP转发', service: 'UdpForwardClientService' }
            },
            {
                path: '/service-socks5.html',
                name: 'ServiceSocks5',
                component: () => import('../views/service/socks5/Index.vue'),
                meta: { name: 'socks5代理', service: 'Socks5ClientService' }
            },
            {
                path: '/service-ftp.html',
                name: 'ServiceFtp',
                component: () => import('../views/service/ftp/Index.vue'),
                meta: { name: '文件服务', service: 'FtpClientService' }
            },
            {
                path: '/service-logger.html',
                name: 'ServiceLogger',
                component: () => import('../views/service/Logger.vue'),
                meta: { name: '日志信息', service: 'LoggerClientService' }
            }
        ]
    },
    {
        path: '/server.html',
        name: 'Server',
        component: () => import('../views/server/Index.vue'),
        redirect: { name: 'ServerTcpForward' },
        children: [
            {
                path: '/server-tcp-forward.html',
                name: 'ServerTcpForward',
                component: () => import('../views/server/tcpforward/Index.vue'),
                meta: { name: '服务器代理TCP转发', service: 'TcpForwardClientService' }
            },
            {
                path: '/server-udp-forward.html',
                name: 'ServerUdpForward',
                component: () => import('../views/server/udpforward/Index.vue'),
                meta: { name: '服务器代理UDP转发', service: 'UdpForwardClientService' }
            },
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
