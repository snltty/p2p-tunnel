<template>
    <div>
        <div class="absolute wrap">
            <div class="head">
                <span>FBI WARNING</span>
            </div>
            <div class="text">
                您未开启相应功能，或未加载相应插件，无法进入页面进行使用
            </div>
        </div>
    </div>
</template>

<script>
export default {
    setup() {


        return {}
    }
}
</script>

<style lang="stylus" scoped>
.wrap {
    box-sizing: border-box;
    background-color: #000;
    position: absolute;
    right: 1rem;
    bottom: 1rem;
    left: 1rem;
    top: 1rem;
    border-radius: 0.4rem;

    .head {
        padding: 5rem 2rem 2rem 2rem;
        text-align: center;

        span {
            padding: 1rem;
            background-color: #ff0000;
            color: #fff;
            font-size: 3rem;
            font-family: fantasy;
        }
    }

    .text {
        padding: 1rem;
        color: #fff;
        text-align: center;
        font-size: 1.4rem;
    }
}
</style>