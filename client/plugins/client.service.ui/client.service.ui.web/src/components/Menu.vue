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
                    <House />
                </el-icon>
                <span>首页</span>
            </router-link>
            <router-link :to="{name:'Nodes'}">
                <el-icon>
                    <Link />
                </el-icon>
                <span>节点</span>
            </router-link>
            <auth-item-or :names="['ServerTcpForwardClientService','ServerUdpForwardClientService']">
                <router-link :to="{name:'Servers'}">
                    <el-icon>
                        <Position />
                    </el-icon>
                    <span>代理穿透</span>
                </router-link>
            </auth-item-or>
            <router-link :to="{name:'Settings'}">
                <el-icon>
                    <Setting />
                </el-icon>
                <span>设置</span>
            </router-link>
        </div>
        <div class="flex-1"></div>
        <div class="meta"></div>
    </div>
</template>
<script>
import AuthItem from './auth/AuthItem.vue';
export default {
    components: { AuthItem },
    setup() {

        return {}
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