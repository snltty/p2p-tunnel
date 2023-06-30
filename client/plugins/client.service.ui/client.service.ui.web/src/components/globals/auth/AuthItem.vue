<template>
    <template v-if="accessService(name,servicesState)">
        <slot></slot>
    </template>
</template>

<script>
import { injectServices, accessService } from '../../../states/services'
export default {
    props: ['name'],
    setup() {
        const servicesState = injectServices();
        return {
            servicesState, accessService
        }
    }
}
</script>
<style lang="stylus" scoped></style>