/*
 * @Author: snltty
 * @Date: 2023-02-10 21:31:49
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 21:41:19
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\directives\animation.js
 */

const fn = (app)=>{
    app.directive('ani-show', {
        mounted(el,binding) {
            //console.log(`mounted:${binding.value}`);
        },
        updated (el,binding){
            //console.log(`updated:${binding.value}`);
        }
    })
}
export default fn; 