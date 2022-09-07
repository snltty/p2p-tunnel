<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:51:49
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-19 13:53:25
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\ftp\Local.vue
-->
<template>
    <div class="flex flex-column h-100">
        <div class="head flex flex-nowrap">
            <el-dropdown size="small" trigger="click" @command="handleSpecialFolderCommand" class=" flex-1">
                <el-input size="small" :title="specialFolderModel" :value="specialFolderModel" suffix-icon="el-icon-arrow-down"></el-input>
                <template #dropdown>
                    <FileTree :childs="specialFolder"></FileTree>
                </template>
            </el-dropdown>
            <span class="split"></span>
            <el-button size="small" :loading="loading" @click="getFiles('')">刷新列表</el-button>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table :data="data" size="small" height="100%" @selection-change="handleSelectionChange" @row-dblclick="handleRowDblClick" @row-contextmenu="handleContextMenu">
                    <el-table-column type="selection" width="45" />
                    <el-table-column prop="Label" label="文件名（本地）"></el-table-column>
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
import { getLocalSpecialList, getLocalList, sendLocalCreate, sendLocalDelete, sendSetLocalPath, sendRemoteUpload } from '../../../apis/ftp'
import { onMounted, onUnmounted } from '@vue/runtime-core';
import FileTree from './FileTree.vue'
import ContextMenu from './ContextMenu.vue'
import { ElMessageBox } from 'element-plus'
import { injectFilesData } from './list-share-data'
import { pushListener } from '../../../apis/request'
export default {
    components: { FileTree, ContextMenu },
    setup () {

        const listShareData = injectFilesData();
        const state = reactive({
            data: [],
            multipleSelection: [],
            loading: false,
            specialFolder: [],
            specialFolderModel: '特殊文件夹'
        });
        const getSpecial = () => {
            getLocalSpecialList().then((res) => {
                state.specialFolder = [res];
            });
        }
        const getFiles = (path = '') => {
            state.loading = true;
            getLocalList(path).then((res) => {
                state.loading = false;
                state.specialFolderModel = res.Current;
                listShareData.locals = state.data = [{ Name: '..', Label: '.. 上一级', Length: 0, Type: 0 }].concat(res.Data.Data.map(c => {
                    c.Label = c.Name;
                    return c;
                }));
            }).catch(() => {
                state.loading = false;
            });
        }

        const onDownloadChange = () => {
            getFiles();
        }
        onMounted(() => {
            getSpecial();
            getFiles();
            pushListener.add('ftp.progress.download', onDownloadChange);
        });
        onUnmounted(() => {
            pushListener.remove('ftp.progress.download', onDownloadChange);
        });

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
                        text: '上传', handle: () => {
                            if (listShareData.remotes.filter(c => c.Name == row.Name).length > 0) {
                                ElMessageBox.confirm(`同名文件已存在，是否确定上传覆盖，【${row.Name}】`, '上传', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendRemoteUpload(listShareData.clientId || 0, row.Name).then((res) => {
                                        state.loading = false;
                                    }).catch((err) => {
                                        state.loading = false;
                                    });
                                });
                            } else {
                                state.loading = true;
                                sendRemoteUpload(listShareData.clientId || 0, row.Name).then((res) => {
                                    state.loading = false;
                                }).catch((err) => {
                                    state.loading = false;
                                });
                            }
                        }
                    },
                    {
                        text: '上传选中', handle: () => {
                            if (state.multipleSelection.length > 0) {
                                ElMessageBox.confirm(`如果存在同名文件，则直接替换，不再提示`, '上传', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendRemoteUpload(listShareData.clientId || 0, state.multipleSelection.map(c => c.Name).join(','))
                                        .then(() => {
                                            state.loading = false;
                                        }).catch((e) => {
                                            state.loading = false;
                                        });;
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
                                sendLocalCreate(value).then(() => {
                                    getFiles();
                                }).catch(() => {
                                    state.loading = false;
                                });;
                            });
                        }
                    },
                    {
                        text: '删除', handle: () => {
                            ElMessageBox.confirm(`删除,【${row.Name}】`, '删除', {
                                confirmButtonText: '确定',
                                cancelButtonText: '取消',
                                type: 'warning'
                            }).then(() => {
                                state.loading = true;
                                sendLocalDelete(row.Name).then(() => {
                                    getFiles();
                                }).catch(() => {
                                    state.loading = false;
                                });;
                            });
                        }
                    },
                    {
                        text: '删除选中', handle: () => {
                            if (state.multipleSelection.length > 0) {
                                ElMessageBox.confirm(`删除多个选中文件，是否确认？`, '删除', {
                                    confirmButtonText: '确定',
                                    cancelButtonText: '取消',
                                    type: 'warning'
                                }).then(() => {
                                    state.loading = true;
                                    sendLocalDelete(state.multipleSelection.map(c => c.Name).join(',')).then(() => {
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
        const handleSpecialFolderCommand = (item) => {
            if (!state.loading && item.FullName) {
                sendSetLocalPath(item.FullName).then(() => {
                    getFiles();
                });
            }
        }

        return {
            ...toRefs(state), getFiles, contextMenu, handleSelectionChange, handleRowDblClick, handleContextMenu, handleSpecialFolderCommand
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