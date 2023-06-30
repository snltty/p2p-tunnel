<template>
    <template v-if="show">
        <slot></slot>
    </template>
</template>

<script>
import { computed } from '@vue/reactivity';
import { injectServices, accessService } from '../../../states/services'
export default {
    props: ['names'],
    setup(props) {
        const servicesState = injectServices();
        const show = computed(() => {
            let names = props.names;
            for (let i = 0; i < names.length; i++) {
                if (accessService(names[i], servicesState)) {
                    return true;
                }
            }
            return false;
        });
        return {
            show
        }
    }
}
</script>
<style lang="stylus" scoped></style>