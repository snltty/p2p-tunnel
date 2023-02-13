import { sendWebsocketMsg } from "./request";

export const getConfigures = () => {
    return sendWebsocketMsg(`configure/configures`);
}

export const getConfigure = (className) => {
    return sendWebsocketMsg(`configure/configure`, className);
}
export const saveConfigure = (className, content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: className, Content: content });
}

export const enableConfigure = (className, enable) => {
    return sendWebsocketMsg(`configure/enable`, { ClassName: className, Enable: enable });
}

export const getServices = () => {
    return sendWebsocketMsg(`configure/services`);
}

