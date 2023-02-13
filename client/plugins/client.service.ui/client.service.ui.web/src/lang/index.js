import { createI18n } from 'vue-i18n'
import zhCn from './zh-cn'
import en from './en'
const getDefaultLanguage = () => {
    let lang = localStorage.getItem('lang') || navigator.language;
    return lang || 'zh-CN';
}

// 创建 i18n
const i18n = createI18n({
    legacy: false,
    globalInjection: true,
    locale: getDefaultLanguage(),
    messages: {
        'zh-CN': zhCn,
        'en': en
    }
})

export default i18n

