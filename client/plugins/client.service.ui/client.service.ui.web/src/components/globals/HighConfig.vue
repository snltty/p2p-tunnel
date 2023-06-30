<template>
    <slot></slot>
</template>
<script>
import { injectSignIn } from '../../states/signin'
export default {
    setup() {
        const signState = injectSignIn();
        return {
            signState
        }
    }
}
</script>
<style lang="stylus" scoped></style>