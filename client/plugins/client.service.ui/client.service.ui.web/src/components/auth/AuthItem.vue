<template>
    <template v-if="servicesState.services.indexOf(name) >= 0">
        <slot></slot>
    </template>
</template>

<script>
import { injectServices } from '../../states/services'
export default {
    props: ['name'],
    setup() {
        const servicesState = injectServices();
        return {
            servicesState
        }
    }
}
</script>
<style lang="stylus" scoped></style>