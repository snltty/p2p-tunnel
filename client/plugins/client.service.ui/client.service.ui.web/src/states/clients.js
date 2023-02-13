import { provide, inject, reactive } from "vue";
import { websocketState } from '../apis/request'
import { getClients } from '../apis/clients'

const provideClientsKey = Symbol();
export const provideClients = () => {
    const state = reactive({
        clients: []
    });
    provide(provideClientsKey, state);

    setInterval(() => {
        if (websocketState.connected) {
            getClients().then((res) => {
                state.clients = res;
            })
        } else {
            state.clients = [];
        }
    }, 1000);
}
export const injectClients = () => {
    return inject(provideClientsKey);
}