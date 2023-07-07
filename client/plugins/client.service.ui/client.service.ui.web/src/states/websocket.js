import { provide, inject, reactive } from "vue";
import { subWebsocketState } from '../apis/request'

const provideWebsocketKey = Symbol();
export const provideWebsocket = () => {
    const state = reactive({
        connected: false,
        connecting: false,
        url: ''
    });
    provide(provideWebsocketKey, state);

    subWebsocketState((connected) => {
        state.connected = connected;
    });
}
export const injectWebsocket = () => {
    return inject(provideWebsocketKey);
}