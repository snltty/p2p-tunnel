/*
 * @Author: snltty
 * @Date: 2021-09-26 19:09:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-14 21:00:07
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\ftp.js
 */
import { sendWebsocketMsg } from "./request";


export const getFtpInfo = () => {
    return sendWebsocketMsg(`ftp/Info`);
}

export const getLocalList = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalList`, path);
}
export const getLocalSpecialList = () => {
    return sendWebsocketMsg(`ftp/LocalSpecialList`);
}
export const sendLocalCreate = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalCreate`, path);
}
export const sendLocalDelete = (path = '') => {
    return sendWebsocketMsg(`ftp/LocalDelete`, path);
}
export const sendSetLocalPath = (path = '') => {
    return sendWebsocketMsg(`ftp/SetLocalPath`, path);
}
export const sendLocalCancel = (id, md5 = 0) => {
    return sendWebsocketMsg(`ftp/localCancel`, { Id: id, Md5: md5 });
}


export const geRemoteList = (id, path = '') => {
    return sendWebsocketMsg(`ftp/RemoteList`, { Id: id, Path: path });
}
export const sendRemoteCreate = (id, path = '') => {
    return sendWebsocketMsg(`ftp/RemoteCreate`, { Id: id, Path: path });
}
export const sendRemoteDelete = (id, path = '') => {
    return sendWebsocketMsg(`ftp/RemoteDelete`, { Id: id, Path: path });
}
export const sendRemoteUpload = (id, path = '') => {
    return sendWebsocketMsg(`ftp/Upload`, { Id: id, Path: path });
}
export const sendRemoteDownload = (id, path = '') => {
    return sendWebsocketMsg(`ftp/Download`, { Id: id, Path: path });
}
export const sendRemoteCancel = (id, md5 = 0) => {
    return sendWebsocketMsg(`ftp/remoteCancel`, { Id: id, Md5: md5 });
}