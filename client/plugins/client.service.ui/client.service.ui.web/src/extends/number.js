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
    return this.replace(/\s/g, '').split(/,|\n/).filter(c => c.length > 0).map(c => c.replace(/\s/g, ''));
}