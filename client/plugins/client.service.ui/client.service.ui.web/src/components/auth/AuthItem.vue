<template>
    <template v-if="services.indexOf(name) >= 0">
        <slot></slot>
    </template>
</template>

<script>
import { inject } from 'vue'
export default {
    props: ['name'],
    setup() {
        const services = inject('btn-auth-services');
        return {
            services
        }
    }
}
</script>
<style lang="stylus" scoped></style>