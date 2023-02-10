/*
 * @Author: snltty
 * @Date: 2021-08-20 09:12:44
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 16:45:10
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\main.js
 */
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// import VConsole from 'vconsole';
// const vConsole = new VConsole();

const app = createApp(App);

import './assets/particles.min.js'

import './assets/style.css'
import './extends/index'
import auth from './components/auth'
app.use(auth);

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'


import { CircleClose, House, Link, Position, OfficeBuilding, SwitchButton, Loading, ArrowRightBold, Setting } from '@element-plus/icons'
app.component(CircleClose.name, CircleClose);
app.component(House.name, House);
app.component(Link.name, Link);
app.component(Position.name, Position);
app.component(OfficeBuilding.name, OfficeBuilding);
app.component(SwitchButton.name, SwitchButton);
app.component(Loading.name, Loading);
app.component(ArrowRightBold.name, ArrowRightBold);
app.component(Setting.name, Setting);

app.use(ElementPlus).use(router).mount('#app');
