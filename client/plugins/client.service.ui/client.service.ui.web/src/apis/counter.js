import { sendWebsocketMsg } from "./request";

export const getCounter = () => {
    return sendWebsocketMsg(`counter/info`, {}, true);
}