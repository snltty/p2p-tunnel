import { provide, inject, reactive } from "vue";
import { websocketState } from '../apis/request'
import { getClients } from '../apis/clients'

const provideClientsKey = Symbol();
export const provideClients = () => {
    const state = reactive({
        clients: []
    });
    provide(provideClientsKey, state);

    const fn = () => {
        if (websocketState.connected) {
            getClients().then((res) => {
                state.clients = res;
                setTimeout(fn, 1000);
            }).catch(() => {
                setTimeout(fn, 1000);
            });
        } else {
            state.clients = [];
            setTimeout(fn, 1000);
        }
    }
    fn();
}
export const injectClients = () => {
    return inject(provideClientsKey);
}