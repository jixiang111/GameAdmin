<template>
	<div class="client-whitelist">
		<el-card shadow="hover" :body-style="{ padding: 12 }">
			<el-form :inline="true" :model="query">
				<el-form-item label="关键词">
					<el-input v-model="query.keyword" placeholder="机器码/备注/标签" clearable style="width: 200px" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="query.onlyEnabled" placeholder="全部" clearable style="width: 120px">
						<el-option label="全部" :value="undefined" />
						<el-option label="启用" :value="true" />
						<el-option label="禁用" :value="false" />
					</el-select>
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
			<el-table :data="tableData" v-loading="loading" border stripe>
				<el-table-column type="index" label="序号" width="60" align="center" />
				<el-table-column prop="machineCode" label="机器码" min-width="200" show-overflow-tooltip />
				<el-table-column label="标签" min-width="150" show-overflow-tooltip>
					<template #default="scope">
						<el-tag v-for="(tag, index) in scope.row.tags" :key="index" size="small" style="margin-right: 4px">
							{{ tag }}
						</el-tag>
						<span v-if="!scope.row.tags || scope.row.tags.length === 0" class="text-secondary">-</span>
					</template>
				</el-table-column>
				<el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
				<el-table-column label="状态" width="80" align="center">
					<template #default="scope">
						<el-tag v-if="scope.row.enabled" type="success" size="small">启用</el-tag>
						<el-tag v-else type="info" size="small">禁用</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="createdBy" label="创建人" width="100" align="center" show-overflow-tooltip />
				<el-table-column prop="updateTime" label="更新时间" width="160" align="center" show-overflow-tooltip />
				<el-table-column label="操作" width="160" align="center" fixed="right">
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
				v-model:current-page="query.pageNo"
				v-model:page-size="query.pageSize"
				:total="total"
				:page-sizes="[10, 20, 50, 100]"
				layout="total, sizes, prev, pager, next, jumper"
				style="margin-top: 15px"
				@size-change="handleQuery"
				@current-change="handleQuery"
			/>
		</el-card>

		<el-dialog v-model="dialog.visible" :title="dialog.title" width="600px" @closed="resetDialog">
			<el-form ref="dialogFormRef" :model="dialog.form" :rules="dialogRules" label-width="100px">
				<el-form-item label="机器码" prop="machineCode">
					<el-input v-model="dialog.form.machineCode" placeholder="16-128位字符，仅允许字母、数字、-" maxlength="128" show-word-limit />
					<el-text class="form-tip">机器码唯一标识客户端设备</el-text>
				</el-form-item>
				<el-form-item label="标签">
					<el-select v-model="dialog.form.tags" multiple filterable allow-create placeholder="输入标签后回车" style="width: 100%">
						<el-option v-for="tag in commonTags" :key="tag" :label="tag" :value="tag" />
					</el-select>
					<el-text class="form-tip">用于分组管理，可创建新标签</el-text>
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="dialog.form.remark" type="textarea" :rows="3" placeholder="可选填备注信息" maxlength="500" show-word-limit />
				</el-form-item>
				<el-form-item label="状态">
					<el-switch v-model="dialog.form.enabled" active-text="启用" inactive-text="禁用" />
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
import { getWhitelistPage, saveWhitelist, deleteWhitelist, type ClientWhitelistOutput, type ClientWhitelistUpsertInput } from '/@/api/clientVersion';

const query = reactive({
	pageNo: 1,
	pageSize: 10,
	keyword: '',
	onlyEnabled: undefined as boolean | undefined,
});

const loading = ref(false);
const tableData = ref<ClientWhitelistOutput[]>([]);
const total = ref(0);

// 常用标签
const commonTags = ref<string[]>(['测试', 'iOS', 'Android', '内部', 'VIP']);

const dialogFormRef = ref<FormInstance>();
const dialog = reactive({
	visible: false,
	title: '新增白名单',
	saving: false,
	form: {
		entryId: undefined as string | undefined,
		machineCode: '',
		remark: '',
		tags: [] as string[],
		enabled: true,
	},
});

const dialogRules: FormRules<ClientWhitelistUpsertInput> = {
	machineCode: [
		{ required: true, message: '请输入机器码', trigger: 'blur' },
		{
			pattern: /^[A-Za-z0-9-_]{16,128}$/,
			message: '机器码需16-128位，仅允许字母、数字、-、_',
			trigger: 'blur',
		},
	],
};

const handleQuery = async () => {
	loading.value = true;
	try {
		const res: any = await getWhitelistPage(query);
		const result = res.data?.result ?? res.data?.data ?? res.data ?? { items: [], total: 0 };
		tableData.value = result.items ?? result.records ?? [];
		total.value = result.total ?? result.count ?? 0;
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '查询失败');
	} finally {
		loading.value = false;
	}
};

const resetQuery = () => {
	query.keyword = '';
	query.onlyEnabled = undefined;
	query.pageNo = 1;
	handleQuery();
};

const openDialog = (row?: ClientWhitelistOutput) => {
	if (row?.entryId) {
		dialog.title = '编辑白名单';
		dialog.form.entryId = row.entryId;
		dialog.form.machineCode = row.machineCode;
		dialog.form.remark = row.remark ?? '';
		dialog.form.tags = row.tags ?? [];
		dialog.form.enabled = row.enabled;
	} else {
		dialog.title = '新增白名单';
		resetDialog();
	}
	dialog.visible = true;
};

const resetDialog = () => {
	dialog.form.entryId = undefined;
	dialog.form.machineCode = '';
	dialog.form.remark = '';
	dialog.form.tags = [];
	dialog.form.enabled = true;
	dialogFormRef.value?.clearValidate();
};

const submitDialog = () => {
	dialogFormRef.value?.validate(async (valid) => {
		if (!valid) return;
		dialog.saving = true;
		try {
			// 自动转大写并去除空格
			const saveData: ClientWhitelistUpsertInput = {
				...dialog.form,
				// 去除空格，保留用户大小写
				machineCode: dialog.form.machineCode.replace(/\s/g, ''),
			};

			await saveWhitelist(saveData);
			ElMessage.success(dialog.form.entryId ? '白名单已更新' : '白名单已新增');
			dialog.visible = false;
			handleQuery();
		} catch (error: any) {
			ElMessage.error(error?.response?.data?.message || error?.message || '保存失败');
		} finally {
			dialog.saving = false;
		}
	});
};

const handleDelete = async (row: ClientWhitelistOutput) => {
	if (!row.entryId) return;
	try {
		await deleteWhitelist(row.entryId);
		ElMessage.success('白名单已删除');
		handleQuery();
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '删除失败');
	}
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

	.text-secondary {
		color: var(--el-text-color-secondary);
	}

	.form-tip {
		display: block;
		margin-top: 4px;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}
</style>
