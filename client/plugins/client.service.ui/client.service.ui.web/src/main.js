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
import auth from './components/auth'
app.use(auth);

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/display.css'
import 'element-plus/theme-chalk/dark/css-vars.css'


import { CircleClose, House, Link, Position, OfficeBuilding, SwitchButton, Loading, ArrowRightBold, Setting, ArrowDown } from '@element-plus/icons'
app.component(CircleClose.name, CircleClose);
app.component(House.name, House);
app.component(Link.name, Link);
app.component(Position.name, Position);
app.component(OfficeBuilding.name, OfficeBuilding);
app.component(SwitchButton.name, SwitchButton);
app.component(Loading.name, Loading);
app.component(ArrowRightBold.name, ArrowRightBold);
app.component(Setting.name, Setting);
app.component(ArrowDown.name, ArrowDown);

app.use(ElementPlus, { size: 'default' }).use(router).mount('#app');
