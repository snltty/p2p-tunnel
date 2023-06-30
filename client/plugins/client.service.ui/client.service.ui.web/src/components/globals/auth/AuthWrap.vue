<template>
    <slot></slot>
</template>
<script>
import { provideServices } from '../../../states/services'
export default {
    setup() {
        provideServices();
    }
}
</script>