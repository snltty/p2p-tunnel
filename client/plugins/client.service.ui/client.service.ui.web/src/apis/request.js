/*
 * @Author: snltty
 * @Date: 2021-08-19 23:04:50
 * @LastEditors: snltty
 * @LastEditTime: 2022-09-01 11:38:22
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\request.js
 */
import { ElMessage } from 'element-plus'

let requestId = 0, ws = null, wsUrl = '';
//请求缓存，等待回调
const requests = {};
const queues = [];
export const websocketState = { connected: false };

const sendQueueMsg = () => {
    if (queues.length > 0 && websocketState.connected) {
        ws.send(queues.shift());
    }
    setTimeout(sendQueueMsg, 1000 / 60);
}
sendQueueMsg();

//发布订阅
export const pushListener = {
    subs: {
    },
    add: function (type, callback) {
        if (typeof callback == 'function') {
            if (!this.subs[type]) {
                this.subs[type] = [];
            }
            this.subs[type].push(callback);
        }
    },
    remove (type, callback) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            if (funcs[i] == callback) {
                funcs.splice(i, 1);
            }
        }
    },
    push (type, data) {
        let funcs = this.subs[type] || [];
        for (let i = funcs.length - 1; i >= 0; i--) {
            funcs[i](data);
        }
    }
}

const websocketStateChangeKey = Symbol();
export const subWebsocketState = (callback) => {
    pushListener.add(websocketStateChangeKey, callback);
}
//消息处理
const onWebsocketOpen = () => {
    websocketState.connected = true;
    pushListener.push(websocketStateChangeKey, websocketState.connected);
}
const onWebsocketClose = (e) => {
    websocketState.connected = false;
    pushListener.push(websocketStateChangeKey, websocketState.connected);
    initWebsocket();
}

export const onWebsocketMsg = (msg) => {
    let json = JSON.parse(msg.data);
    let callback = requests[json.RequestId];
    if (callback) {
        if (json.Code == 0) {
            callback.resolve(json.Content);
        } else if (json.Code == 255) {
            callback.reject(json.Content);
            if (!callback.errHandle) {
                ElMessage.error(`${callback.path}:${json.Content}`);
            }
        } else {
            pushListener.push(json.Path, json.Content);
        }
        delete requests[json.RequestId];
    } else {
        pushListener.push(json.Path, json.Content);
    }
}
export const initWebsocket = (url = wsUrl) => {
    if (ws != null) {
        ws.close();
    }
    wsUrl = url;
    ws = new WebSocket(wsUrl);
    ws.onopen = onWebsocketOpen;
    ws.onclose = onWebsocketClose
    ws.onmessage = onWebsocketMsg
}

//发送消息
export const sendWebsocketMsg = (path, msg = {}, errHandle = false) => {
    return new Promise((resolve, reject) => {
        let id = ++requestId;
        try {
            requests[id] = { resolve, reject, errHandle, path };
            let str = JSON.stringify({
                Path: path,
                RequestId: id,
                Content: typeof msg == 'string' ? msg : JSON.stringify(msg)
            });
            if (websocketState.connected) {
                ws.send(str);
            } else {
                queues.push(str);
            }
        } catch (e) {
            reject('网络错误~');
            ElMessage.error('网络错误~');
            delete requests[id];
        }
    });
}

export const subNotifyMsg = (path, callback) => {
    pushListener.add(path, callback);
}
export const unsubNotifyMsg = (path, callback) => {
    pushListener.remove(path, callback);
}
