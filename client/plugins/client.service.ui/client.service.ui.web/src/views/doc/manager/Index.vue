<template>
    <div class="absolute flex flex-column">
        <div class="head">
            <el-select v-model="state.key" filterable placeholder="Select" @change="handleChange">
                <el-option v-for="(item,index) in state.docs" :key="index" :label="item.key" :value="item.key" />
            </el-select>
        </div>
        <div class="body flex-1 relative">
            <dir class="wrap absolute scrollbar">
                <div class="inner flex-1 flex flex-nowrap">
                    <div class="menu">
                        <ul>
                            <template v-for="(item,index) in state.doc" :key="index">
                                <li :class="{current:index==state.docItem}" @click="handleChangeIndex(index)">
                                    <p>{{item.text}}</p>
                                </li>
                            </template>
                        </ul>
                    </div>
                    <div class="content flex-1">
                        <div class="path">{{state.doc[state.docItem].text}} - {{state.doc[state.docItem].path}}</div>
                        <h3>参数</h3>
                        <div class="params">
                            <pre>{{state.doc[state.docItem].params}}</pre>
                        </div>
                        <h3>返回</h3>
                        <div class="response">
                            <pre>{{state.doc[state.docItem].response}}</pre>
                        </div>
                    </div>
                </div>
            </dir>
        </div>
    </div>
</template>

<script>
import { reactive } from 'vue';
const files = require.context('./docs/', true, /.*\.js/);
const docs = files.keys().map(c => {
    let key = c.split('/');
    return {
        key: key[key.length - 1].split('.')[0],
        value: files(c).default
    }
});

export default {
    setup() {
        const state = reactive({
            key: docs[0].key,
            docs: docs,
            doc: docs[0].value,
            docItem: 0
        });
        const handleChange = () => {
            state.doc = state.docs.filter(c => c.key == state.key)[0].value;
            state.docItem = 0;
        }
        const handleChangeIndex = (index) => {
            state.docItem = index;
        }

        return { state, handleChange, handleChangeIndex }
    }
}
</script>

<style lang="stylus" scoped>
.head {
    text-align: center;
    border-bottom: 1px solid #ddd;
    padding: 1rem 0;
    background-color: #fff;
}

.body {
    background-color: #fafafa;
    font-size: 1.4rem;

    pre {
        background-color: #fafafa;
        border: 1px solid #eee;
        padding: 1rem;
        border-radius: 0.4rem;
        white-space: pre-wrap;
    }

    .wrap {
        padding: 2rem;

        .inner {
            border: 1px solid #ddd;
            background-color: #fff;
            border-radius: 4px;

            .menu {
                border-right: 1px solid #ddd;
                width: 14rem;
                padding: 1rem;

                li {
                    padding: 1rem;
                    cursor: pointer;
                    border-width: 1px;
                    border-style: solid;
                    border-color: transparent;
                    margin-bottom: 0.6rem;
                    border-radius: 4px;
                    transition: 0.3s;

                    &.current, &:hover {
                        background-color: #f2f2f2;
                        font-weight: bold;
                        border-color: #ddd;
                    }
                }
            }

            .content {
                padding: 1rem;

                h3 {
                    padding: 1rem;
                    padding-bottom: 0;
                    font-size: 1.4rem;
                }

                div.path {
                    font-size: 1.6rem;
                    font-weight: bold;
                }

                div {
                    padding: 1rem;

                    &.params {
                        padding-bottom: 0;
                    }
                }
            }
        }
    }
}
</style>