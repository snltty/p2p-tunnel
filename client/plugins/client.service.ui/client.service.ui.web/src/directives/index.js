const files = require.context('.', false, /\.js$/);
const fn = (app) => {
    files.keys().forEach(key => {
        if (key == './index.js') return;
        files(key).default(app);
    });
}
export default fn;