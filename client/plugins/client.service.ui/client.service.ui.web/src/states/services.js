import { provide, inject, reactive } from "vue";
import { getServices } from '../apis/configure'
import { websocketState } from '../apis/request'

const provideServicesKey = Symbol();
export const provideServices = () => {

    const fn = () => {
        if (websocketState.connected) {
            getServices().then((res) => {
                state.services = res;
            }).catch(() => {
                setTimeout(fn, 1000);
            });
        } else {
            state.services = [];
            setTimeout(fn, 1000);
        }
    }

    const state = reactive({
        services: [],
        update: fn
    });
    provide(provideServicesKey, state);
    fn();
}
export const injectServices = () => {
    return inject(provideServicesKey);
}

export const accessService = (name, state = null) => {
    if (!state) {
        state = inject(provideServicesKey);
    }
    return state.services.length == 0 || state.services.indexOf(name) >= 0 || !name;
}
export const accessServiceOr = (names, state = null) => {
    if (!state) {
        state = inject(provideServicesKey);
    }
    if (!names || names.length == 0 || state.services.length == 0) return true;

    for (let i = 0; i < names.length; i++) {
        if (state.services.indexOf(names[i]) >= 0) {
            return true;
        }
    }
    return false;
}