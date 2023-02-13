<template>
    <el-config-provider :locale="locale" size="default">
        <auth-wrap>
            <Background></Background>
            <div class="body absolute">
                <div class="wrap absolute flex flex-column flex-nowrap h-100">
                    <div class="menu">
                        <Menu></Menu>
                    </div>
                    <div class="content flex-1 relative scrollbar-10">
                        <router-view />
                    </div>
                    <div class="status-bar">
                        <StatusBar></StatusBar>
                    </div>
                </div>
            </div>
        </auth-wrap>
    </el-config-provider>
</template>
<script>
import Menu from "./components/Menu.vue";
import Background from "./components/Background.vue";
import StatusBar from "./components/statusBar/Index.vue";
import { provideRegister } from "./states/register";
import { provideWebsocket } from "./states/websocket";
import { provideClients } from "./states/clients";
import { provideShareData } from "./states/shareData";
import { ElConfigProvider } from "element-plus";
import zhCn from "element-plus/lib/locale/lang/zh-cn";
export default {
    components: { Menu, Background, StatusBar, ElConfigProvider },
    setup() {
        provideRegister();
        provideWebsocket();
        provideClients();
        provideShareData();

        return {
            locale: zhCn,
        };
    },
};
</script>
<style lang="stylus" scoped>
.body {
    z-index: 9;
}

.wrap {
    width: 100%;
    height: 60rem;
    max-width: 80rem;
    max-height: 100%;
    background-color: #fff;
    left: 50%;
    top: 50%;
    transform: translateX(-50%) translateY(-50%);
    box-shadow: 0 0 10px 6px #ffffff08;
    border-radius: 4px;
    border: 1px solid #297c5c;
    overflow: hidden;
}

@media screen and (max-width: 800px) {
    .wrap {
        height: 100%;
    }
}
</style>
