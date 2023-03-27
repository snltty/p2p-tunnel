<template>
    <div class="menu-wrap flex">
        <div class="logo display">
            <router-link :to="{name:'Home'}">
                <img src="@/assets/logo.svg" alt="p2p-tunnel">
            </router-link>
        </div>
        <div class="flex-1"></div>
        <div class="navs">
            <router-link :to="{name:'Home'}">
                <el-icon>
                    <HomeFilled />
                </el-icon>
                <span>首页</span>
            </router-link>
            <router-link :to="{name:'Nodes'}">
                <el-icon>
                    <Share />
                </el-icon>
                <span>节点</span>
            </router-link>
            <auth-item-or :names="names">
                <router-link :to="{name:'Servers'}">
                    <el-icon>
                        <Promotion />
                    </el-icon>
                    <span>服务器</span>
                </router-link>
            </auth-item-or>
        </div>
        <div class="flex-1"></div>
        <div class="meta"></div>
    </div>
</template>
<script>
import AuthItem from './auth/AuthItem.vue';
import { useRouter } from 'vue-router'
import { injectSignIn } from '../states/signin'
import { accessService, injectServices } from '../states/services'
import { shareData } from '../states/shareData'
import { computed } from 'vue';
export default {
    components: { AuthItem },
    setup() {

        const router = useRouter();
        const servicesState = injectServices();
        const signinState = injectSignIn();
        const serviceAccess = computed(() => signinState.RemoteInfo.Access);

        const names = computed(() => {
            let menus = router.options.routes
                .filter(c => c.name == 'Servers')[0].children
                .filter(c => accessService(c.meta.service, servicesState) && shareData.serverAccessHas(serviceAccess.value, c.meta.access))
                .map(c => c.meta.service);
            return menus;
        });

        return {
            names
        }
    }
}
</script>
<style lang="stylus" scoped>
@media screen and (max-width: 400px) {
    .display {
        display: none;
    }

    .meta {
        width: 1rem !important;
    }
}

.el-dropdown {
    color: #fff;

    .el-dropdown-link {
        line-height: 5rem;

        .el-icon {
            vertical-align: middle;
        }
    }
}

.menu-wrap {
    position: relative;
    height: 5rem;
    padding-left: 1rem;
    background-color: #15602d;
    box-shadow: 1px 1px 0.6rem 0.6rem rgba(0, 0, 0, 0.1);
    border-bottom: 1px solid #205531;
    z-index: 9;
}

.logo {
    padding-top: 0.7rem;

    img {
        height: 3.6rem;
    }
}

.navs {
    padding-top: 1.1rem;

    a {
        display: inline-block;
        margin-left: 0.8rem;
        padding: 0.3rem 1rem 0.6rem 1rem;
        border-radius: 1rem;
        transition: 0.3s;
        color: #fff;
        font-size: 1.4rem;

        &.router-link-active, &:hover {
            color: #15602d;
            background-color: #fff;
        }

        .el-icon, span {
            vertical-align: middle;
        }

        .el-icon {
            // padding-top: 3px;
        }

        span {
            margin-left: 0.4rem;
        }
    }
}

.meta {
    width: 10rem;
}
</style>