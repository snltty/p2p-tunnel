<template>
    <div class="spinner" :style="style">
        <div class="spinner-container container1">
            <div class="circle1" :style="itemStyle"></div>
            <div class="circle2" :style="itemStyle"></div>
            <div class="circle3" :style="itemStyle"></div>
            <div class="circle4" :style="itemStyle"></div>
        </div>
        <div class="spinner-container container2">
            <div class="circle1" :style="itemStyle"></div>
            <div class="circle2" :style="itemStyle"></div>
            <div class="circle3" :style="itemStyle"></div>
            <div class="circle4" :style="itemStyle"></div>
        </div>
        <div class="spinner-container container3">
            <div class="circle1" :style="itemStyle"></div>
            <div class="circle2" :style="itemStyle"></div>
            <div class="circle3" :style="itemStyle"></div>
            <div class="circle4" :style="itemStyle"></div>
        </div>
    </div>
</template>

<script>
export default {
    props: ['size', 'color'],
    setup(props, { emit }) {
        const size = props.size || 30;
        const color = props.color || '#fff';
        const style = `width:${size}px;height:${size}px;`;
        const itemStyle = `background-color:${color};`
        return { style, itemStyle }
    }
}
</script>

<style lang="stylus" scoped>
.spinner {
    width: 30px;
    height: 30px;
    position: relative;
    display: inline-block;
}

.container1 > div, .container2 > div, .container3 > div {
    width: 30%;
    height: 30%;
    background-color: #fff;
    border-radius: 100%;
    position: absolute;
    -webkit-animation: bouncedelay 1.2s infinite ease-in-out;
    animation: bouncedelay 1.2s infinite ease-in-out;
    -webkit-animation-fill-mode: both;
    animation-fill-mode: both;
}

.spinner .spinner-container {
    position: absolute;
    width: 100%;
    height: 100%;
}

.container2 {
    -webkit-transform: rotateZ(45deg);
    transform: rotateZ(45deg);
}

.container3 {
    -webkit-transform: rotateZ(90deg);
    transform: rotateZ(90deg);
}

.circle1 {
    top: 0;
    left: 0;
}

.circle2 {
    top: 0;
    right: 0;
}

.circle3 {
    right: 0;
    bottom: 0;
}

.circle4 {
    left: 0;
    bottom: 0;
}

.container2 .circle1 {
    -webkit-animation-delay: -1.1s;
    animation-delay: -1.1s;
}

.container3 .circle1 {
    -webkit-animation-delay: -1s;
    animation-delay: -1s;
}

.container1 .circle2 {
    -webkit-animation-delay: -0.9s;
    animation-delay: -0.9s;
}

.container2 .circle2 {
    -webkit-animation-delay: -0.8s;
    animation-delay: -0.8s;
}

.container3 .circle2 {
    -webkit-animation-delay: -0.7s;
    animation-delay: -0.7s;
}

.container1 .circle3 {
    -webkit-animation-delay: -0.6s;
    animation-delay: -0.6s;
}

.container2 .circle3 {
    -webkit-animation-delay: -0.5s;
    animation-delay: -0.5s;
}

.container3 .circle3 {
    -webkit-animation-delay: -0.4s;
    animation-delay: -0.4s;
}

.container1 .circle4 {
    -webkit-animation-delay: -0.3s;
    animation-delay: -0.3s;
}

.container2 .circle4 {
    -webkit-animation-delay: -0.2s;
    animation-delay: -0.2s;
}

.container3 .circle4 {
    -webkit-animation-delay: -0.1s;
    animation-delay: -0.1s;
}

@keyframes bouncedelay {
    0%, 80%, 100% {
        -webkit-transform: scale(0);
    }

    40% {
        -webkit-transform: scale(1);
    }
}

@keyframes bouncedelay {
    0%, 80%, 100% {
        transform: scale(0);
        -webkit-transform: scale(0);
    }

    40% {
        transform: scale(1);
        -webkit-transform: scale(1);
    }
}
</style>