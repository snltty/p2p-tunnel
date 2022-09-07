/*
 * @Author: snltty
 * @Date: 2021-09-24 11:24:42
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 23:18:41
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\axios.js
 */
import axios from 'axios'
import { ElMessage } from 'element-plus'
axios.defaults.adapter = require('axios/lib/adapters/http');

export default class Axios {
    instance = null
    constructor({
        timeout = 15000, baseURL = '', withCredentials = false
    }) {
        this.instance = axios.create({
            baseURL: baseURL,
            timeout: timeout
        })
        this.instance.defaults.withCredentials = withCredentials;
        this.instance.interceptors.response.use(res => {
            return res;
        }, err => {
            if (err.response && err.response.data) {
                ElMessage.error(err.response.data);
            } else {
                ElMessage.error(err + '');
            }
            return Promise.reject(err);
        });
    }
    get ({ url = '', params = {}, headers = {} }) {
        return this.instance.get(url, { headers: headers, params: params });
    }
    post ({ url = '', data = {}, params = {}, headers = {} }) {
        return this.instance.post(this._parseParams(url, params), data, { headers: headers });
    }
    _parseParams (url, params) {
        let [path, query] = url.split('?');
        let _params = [];
        for (let j in params) {
            _params.push(`${j}=${params[j]}`);
        }
        if (query) {
            _params.push(query);
        }
        return `${path}?${_params.join('&')}`;
    }
}