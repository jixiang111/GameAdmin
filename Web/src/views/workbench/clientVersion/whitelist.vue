<template>
	<div class="client-whitelist">
		<el-card shadow="hover" :body-style="{ padding: 12 }">
			<el-form :inline="true" :model="query">
				<el-form-item label="机器码">
					<el-input v-model="query.keyword" placeholder="输入机器码" clearable />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Search" @click="handleQuery">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Plus" @click="openDialog()">新增白名单</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 10px">
			<el-table :data="tableData" v-loading="loading" border>
				<el-table-column prop="machineCode" label="机器码" />
				<el-table-column prop="remark" label="备注" show-overflow-tooltip />
				<el-table-column prop="updatedTime" label="更新时间" width="180" align="center" />
				<el-table-column label="操作" width="200" align="center">
					<template #default="scope">
						<el-button text type="primary" icon="ele-Edit" @click="openDialog(scope.row)">编辑</el-button>
						<el-popconfirm title="确定删除该白名单吗？" @confirm="handleDelete(scope.row)">
							<template #reference>
								<el-button text type="danger" icon="ele-Delete">删除</el-button>
							</template>
						</el-popconfirm>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:current-page="query.page"
				v-model:page-size="query.pageSize"
				:total="total"
				:page-sizes="[10, 20, 50]"
				layout="total, sizes, prev, pager, next"
				style="margin-top: 15px; text-align: right"
				@size-change="handleQuery"
				@current-change="handleQuery"
			/>
		</el-card>

		<el-dialog v-model="dialog.visible" :title="dialog.title" width="500px" @closed="resetDialog">
			<el-form ref="dialogFormRef" :model="dialog.form" :rules="dialogRules" label-width="90px">
				<el-form-item label="机器码" prop="machineCode">
					<el-input v-model="dialog.form.machineCode" placeholder="请输入机器码" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="dialog.form.remark" placeholder="选填" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="dialog.visible = false">取消</el-button>
				<el-button type="primary" :loading="dialog.saving" @click="submitDialog">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="clientWhitelist">
import { onMounted, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import {
	addWhitelistItem,
	deleteWhitelistItem,
	getWhitelistPage,
	updateWhitelistItem,
	type WhitelistItem,
} from '/@/api/clientVersion';

const query = reactive({
	page: 1,
	pageSize: 10,
	keyword: '',
});

const loading = ref(false);
const tableData = ref<WhitelistItem[]>([]);
const total = ref(0);

const dialogFormRef = ref<FormInstance>();
const dialog = reactive({
	visible: false,
	title: '新增白名单',
	saving: false,
	form: {
		id: undefined as number | undefined,
		machineCode: '',
		remark: '',
	},
});

const dialogRules: FormRules<typeof dialog.form> = {
	machineCode: [{ required: true, message: '请输入机器码', trigger: 'blur' }],
};

const handleQuery = async () => {
	loading.value = true;
	try {
		const res = await getWhitelistPage(query);
		const result = res.data?.result ?? res.data?.data ?? { items: [], total: 0 };
		tableData.value = result.items ?? result.records ?? [];
		total.value = result.total ?? result.count ?? 0;
	} finally {
		loading.value = false;
	}
};

const resetQuery = () => {
	query.keyword = '';
	query.page = 1;
	handleQuery();
};

const openDialog = (row?: WhitelistItem) => {
	if (row?.id) {
		dialog.title = '编辑白名单';
		dialog.form.id = row.id;
		dialog.form.machineCode = row.machineCode;
		dialog.form.remark = row.remark ?? '';
	} else {
		dialog.title = '新增白名单';
		resetDialog();
	}
	dialog.visible = true;
};

const resetDialog = () => {
	dialog.form.id = undefined;
	dialog.form.machineCode = '';
	dialog.form.remark = '';
	dialogFormRef.value?.clearValidate();
};

const submitDialog = () => {
	dialogFormRef.value?.validate(async (valid) => {
		if (!valid) return;
		dialog.saving = true;
		try {
			if (dialog.form.id) {
				await updateWhitelistItem(dialog.form);
				ElMessage.success('白名单已更新');
			} else {
				await addWhitelistItem(dialog.form);
				ElMessage.success('白名单已新增');
			}
			dialog.visible = false;
			handleQuery();
		} finally {
			dialog.saving = false;
		}
	});
};

const handleDelete = async (row: WhitelistItem) => {
	if (!row.id) return;
	await deleteWhitelistItem(row.id);
	ElMessage.success('已删除白名单');
	handleQuery();
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped lang="scss">
.client-whitelist {
	.full-table {
		margin-top: 10px;
	}
}
</style>
