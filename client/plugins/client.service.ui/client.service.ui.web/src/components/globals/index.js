
import Auth from './auth/index'
import HighConfig from './HighConfig.vue'
export default {
    install: (app) => {
        app.use(Auth);
        app.component('HighConfig', HighConfig);
    }
}