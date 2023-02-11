/*
 * @Author: snltty
 * @Date: 2021-08-20 16:33:56
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 01:18:55
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\lang\index.js
 */

import { createI18n } from 'vue-i18n'
import zhCn from './zh-cn'
import en from './en'
const getDefaultLanguage = ()=>{
    let lang = localStorage.getItem('lang') || navigator.language;
    return lang || 'zh-CN';
}

// 创建 i18n
const i18n = createI18n({
    legacy: false,
    globalInjection: true,
    locale: getDefaultLanguage(),
    messages: {
      'zh-CN':zhCn,
      'en':en
    }
})
  
export default i18n

