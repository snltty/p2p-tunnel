import AuthItem from './AuthItem.vue';
import AuthItemOr from './AuthItemOr.vue';
import AuthWrap from './AuthWrap.vue';
export default {
    install: (app) => {
        app.component('AuthWrap', AuthWrap);
        app.component('AuthItem', AuthItem);
        app.component('AuthItemOr', AuthItemOr);
    }
}