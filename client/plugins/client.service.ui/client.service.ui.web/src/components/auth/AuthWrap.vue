<template>
    <slot></slot>
</template>
<script>
import { ref } from '@vue/reactivity';
import { getServices } from '../../apis/configure'
import { provide } from '@vue/runtime-core';
export default {
    setup() {
        const services = ref([]);
        getServices().then((res) => {
            services.value = res;
        });
        provide('btn-auth-services', services);
    }
}
</script>