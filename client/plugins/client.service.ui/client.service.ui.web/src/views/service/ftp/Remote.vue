<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:51:49
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-07 18:44:30
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\ftp\Remote.vue
-->
<template>
    <div class="flex flex-column h-100">
        <div class="head flex flex-nowrap">
            <el-select v-model="listShareData.clientId" placeholder="请选择已连接的目标客户端" @change="handleClientChange" size="small">
                <template v-for="client in clients" :key="client.Id">
                    <el-option :label="client.Name" :value="client.Id">
                    </el-option>
                </template>
            </el-select>
            <span class="split"></span>
            <el-button size="small" :loading="loading" @click="getFiles('')">刷新列表</el-button>
            <span class="split"></span>
            <ConfigureModal className="FtpClientConfigure">
                <el-button size="small">配置插件</el-button>
            </ConfigureModal>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table :data="data" size="small" height="100%" @selection-change="handleSelectionChange" @row-dblclick="handleRowDblClick" @row-contextmenu="handleContextMenu">
                    <el-table-column type="selection" width="45" />
                    <el-table-column prop="Label" label="文件名（远程）"></el-table-column>
                    <el-table-column prop="Length" label="大小" width="100">
                        <template #default="scope">
                            <span v-if="scope.row.Type !=0">{{scope.row.Length.sizeFormat().join('')}} </span>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
    <ContextMenu ref="contextMenu"></ContextMenu>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { geRemoteList, sendRemoteCreate, sendRemoteDelete, sendRemoteDownload } from '../../../apis/ftp'
import { onMounted, onUnmounted } from '@vue/runtime-core';
import FileTree from './FileTree.vue'
import ContextMenu from './ContextMenu.vue'
import { ElMessageBox } from 'element-plus'
import { injectClients } from '../../../states/clients'
import ConfigureModal from '../configure/ConfigureModal.vue'
import { injectFilesData } from './list-share-data'
import { pushListener } from '../../../apis/request'
import AuthItem from '../../../components/auth/AuthItem.vue';
export default {
    components: { FileTree, ContextMenu, ConfigureModal, AuthItem },
    setup () {
        const listShareData = injectFilesData();
        const clientState = injectClients();
        const state = reactive({
            data: [],
            multipleSelection: [],
            loading: false,
        });

        const getFiles = (path = '') => {
            state.loading = true;
            geRemoteList(listShareData.clientId || 0, path).then((res) => {
                state.loading = false;
                listShareData.remotes = state.data = [{ Name: '..', Label: '.. 上一级', Length: 0, Type: 0 }].concat(res.Data.map(c => {
                    c.Label = c.Name;
                    return c;
                }));
            }).catch((err) => {
                state.loading = false;
            });
        }

        const onUploadChange = () => {
            getFiles();
        }
        onMounted(() => {
            getFiles();
            pushListener.add('ftp.progress.upload', onUploadChange);
        });
        onUnmounted(() => {
            pushListener.remove('ftp.progress.upload', onUploadChange);
        });

        const handleClientChange = () => {
            getFiles();
        }
        const handleRowDblClick = (row) => {
            if (!state.loading && row.Type == 0) {
                getFiles(row.Name);
            }
        }
        const contextMenu = ref(null);
        const handleContextMenu = (row, column, event) => {
            if (!state.loading && row.Name != '..') {
                contextMenu.value.show(event, [
                    {
                        text: '下载', handle: () => {
                            if (listShareData.locals.filter(c => c.Name == row.Name).length) {
                                ElMessageBox.confirm(`同名文件已存在，是否确定下载覆盖，【${row.Name}】`, '下载', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendRemoteDownload(listShareData.clientId || 0, row.Name).then(() => {
                                        state.loading = false;
                                    }).catch(() => {
                                        state.loading = false;
                                    });
                                });
                            } else {
                                state.loading = true;
                                sendRemoteDownload(listShareData.clientId || 0, row.Name).then(() => {
                                    state.loading = false;
                                }).catch(() => {
                                    state.loading = false;
                                });
                            }
                        }
                    },
                    {
                        text: '下载选中', handle: () => {
                            if (state.multipleSelection.length > 0) {
                                ElMessageBox.confirm(`如果存在同名文件，则直接替换，不再提示`, '下载', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendRemoteDownload(listShareData.clientId || 0, state.multipleSelection.map(c => c.Name).join(',')).then(() => {
                                        getFiles();
                                    }).catch(() => {
                                        state.loading = false;
                                    });
                                });
                            }
                        }
                    },
                    {
                        text: '创建文件夹', handle: () => {
                            ElMessageBox.prompt('输入文件夹名称', '创建文件夹', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                inputValue: '新建文件夹'
                            }).then(({ value }) => {
                                state.loading = true;
                                sendRemoteCreate(listShareData.clientId || 0, value).then(() => {
                                    getFiles();
                                }).catch(() => {
                                    state.loading = false;
                                });
                            });
                        }
                    },
                    {
                        text: '删除', handle: () => {
                            ElMessageBox.confirm(`删除【${row.Name}】`, '删除', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                type: 'warning'
                            }).then(() => {
                                state.loading = true;
                                sendRemoteDelete(listShareData.clientId || 0, row.Name).then(() => {
                                    getFiles();
                                }).catch(() => {
                                    state.loading = false;
                                });
                            });
                        }
                    },
                    {
                        text: '删除选中', handle: () => {
                            if (state.multipleSelection.length > 0) {
                                ElMessageBox.confirm(`删除多个选中文件，是否确认`, '下载', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendRemoteDelete(listShareData.clientId || 0, state.multipleSelection.map(c => c.Name).join(',')).then(() => {
                                        getFiles();
                                    }).catch(() => {
                                        state.loading = false;
                                    });
                                });
                            }
                        }
                    },
                ]);
            }
            event.preventDefault();
        }
        const handleSelectionChange = (value) => {
            state.multipleSelection = value.filter(c => c.Name != '..');
        }

        return {
            ...toRefs(state), ...toRefs(clientState), listShareData, getFiles, contextMenu,
            handleSelectionChange, handleRowDblClick, handleContextMenu, handleClientChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-table::before
    height: 0;

.head
    padding-bottom: 0.4rem;

    .split
        width: 0.2rem;

.body
    border: 1px solid #ddd;
</style>