Number.prototype.sizeFormat = function () {
    let unites = ['B', 'KB', 'MB', 'GB', 'TB'];
    let unit = unites[0], size = this;
    while ((unit = unites.shift()) && size.toFixed(2) >= 1024) {
        size /= 1024;
    }
    return unit == 'B' ? [size, unit] : [size.toFixed(2), unit];
}
const add0 = (num) => {
    return num < 10 ? '0' + num : num;
}
Number.prototype.timeFormat = function () {
    let num = this;
    return [
        add0(Math.floor(num / 60 / 60 / 24)),
        add0(Math.floor(num / 60 / 60 % 24)),
        add0(Math.floor(num / 60 % 60)),
        add0(Math.floor(num % 60)),
    ];
}
Number.prototype.splitStr = function () {
    return this.toString().split(/,|\n/).map(c => c.replace(/\s/g, '')).filter(c => c.length > 0);
}
String.prototype.splitStr = function () {
    return this.split(/,|\n/).map(c => c.replace(/\s/g, '')).filter(c => c.length > 0);
}



Number.prototype.toIpv4Str = function () {
    if (this.toString().length > 32) {
        return '';
    }

    const num = this;
    const pow24 = Math.pow(2, 24);
    const num1 = parseInt(num / pow24) >>> 0;
    const num2 = parseInt(((num << 8) >>> 0) / pow24) >>> 0;
    const num3 = parseInt(((num << 16) >>> 0) / pow24) >>> 0;
    const num4 = parseInt(((num << 24) >>> 0) / pow24) >>> 0;
    return `${num1}.${num2}.${num3}.${num4}`;
}
String.prototype.ipv42number = function () {
    return this.split('.').reduce((value, current, index) => {
        value += (+current) << (32 - ((index + 1) * 8));
        return value >>> 0;
    }, 0);
}
Number.prototype.maskLength2value = function () {
    return (0xffffffff << (32 - this)) >>> 0;
}
Number.prototype.ipv42network = function (maskvalue) {
    return (this & maskvalue) >>> 0;
}
Number.prototype.ipv42broadcast = function (maskvalue) {
    return (this | ((~maskvalue) >>> 0)) >>> 0;
}
