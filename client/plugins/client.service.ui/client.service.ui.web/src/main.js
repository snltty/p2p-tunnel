import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// import VConsole from 'vconsole';
// const vConsole = new VConsole();

const app = createApp(App);

// import i18n from './lang/index'
// app.use(i18n);

import './assets/particles.min.js'
import directives from './directives/index'
directives(app);

import './assets/style.css'
import './extends/index'
import globals from './components/globals/index'
app.use(globals);

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'


import "driver.js/dist/driver.min.css";

import {
    CircleCheck, CircleClose, HomeFilled, Link, Position, OfficeBuilding
    , SwitchButton, Loading, ArrowRightBold, Setting, ArrowDown
    , DArrowLeft, DArrowRight, Edit, Delete, Promotion, Share, Select, Warning, CirclePlus, List
} from '@element-plus/icons'
app.component(CircleCheck.name, CircleCheck);
app.component(CircleClose.name, CircleClose);
app.component(HomeFilled.name, HomeFilled);
app.component(Link.name, Link);
app.component(Position.name, Position);
app.component(OfficeBuilding.name, OfficeBuilding);
app.component(SwitchButton.name, SwitchButton);
app.component(Loading.name, Loading);
app.component(ArrowRightBold.name, ArrowRightBold);
app.component(Setting.name, Setting);
app.component(ArrowDown.name, ArrowDown);
app.component(DArrowLeft.name, DArrowLeft);
app.component(DArrowRight.name, DArrowRight);
app.component(Edit.name, Edit);
app.component(Delete.name, Delete);
app.component(Promotion.name, Promotion);
app.component(Share.name, Share);
app.component(Select.name, Select);
app.component(Warning.name, Warning);
app.component(CirclePlus.name, CirclePlus);
app.component(List.name, List);

app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
