import { createRouter, createWebHashHistory } from 'vue-router'

const files = require.context('../views/server/', true, /Index\.vue/);
const servers = files.keys().map(c => files(c).default).filter(c => c.plugin).sort((a, b) => {
    return (a.plugin.order || 0) - (b.plugin.order || 0);
});

const files1 = require.context('../views/nodes/', true, /Index\.vue/);
const nodes = files1.keys().map(c => files1(c).default).filter(c => c.plugin).sort((a, b) => {
    return (a.plugin.order || 0) - (b.plugin.order || 0);
});

const files2 = require.context('../views/doc/manager/', true, /Index\.vue/);
const managers = files2.keys().map(c => files1(c).default).filter(c => c.plugin).sort((a, b) => {
    return (a.plugin.order || 0) - (b.plugin.order || 0);
});

const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../views/home/Index.vue'),
        meta: { text: '首页' }
    },
    {
        path: '/nodes.html',
        name: 'Nodes',
        component: () => import('../views/nodes/Index.vue'),
        redirect: { name: 'NodesSettings' },
        children: nodes.map(c => {
            return {
                path: c.plugin.path,
                name: c.plugin.name,
                component: c,
                meta: { text: c.plugin.text, service: c.plugin.service, access: c.plugin.access }
            }
        })
    },
    {
        path: '/server.html',
        name: 'Servers',
        component: () => import('../views/server/Index.vue'),
        redirect: { name: 'ServerSettings' },
        children: servers.map(c => {
            return {
                path: c.plugin.path,
                name: c.plugin.name,
                component: c,
                meta: { text: c.plugin.text, service: c.plugin.service, access: c.plugin.access }
            }
        })
    },
    {
        path: '/manager.html',
        name: 'Manager',
        component: () => import('../views/doc/manager/Index.vue'),
        // redirect: { name: 'Manager' },
        children: managers.map(c => {
            return {
                path: c.plugin.path,
                name: c.plugin.name,
                component: c,
                meta: { text: c.plugin.text, service: c.plugin.service, access: c.plugin.access }
            }
        })
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
