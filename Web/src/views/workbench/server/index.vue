<template>
	<div class="game-server-manage">
		<el-card shadow="hover" :body-style="{ padding: 8 }">
			<el-form :model="state.queryParams" :inline="true">
				<el-form-item label="服务器 ID">
					<el-input-number
						v-model="state.queryParams.serverId"
						:controls="false"
						:min="1"
						placeholder="全部"
						class="w150"
					/>
				</el-form-item>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.keyword" placeholder="名称 / IP" clearable class="w200" />
				</el-form-item>
				<el-form-item label="状态">
					<el-select v-model="state.queryParams.enabled" clearable placeholder="全部" class="w120">
						<el-option label="启用" :value="true" />
						<el-option label="停用" :value="false" />
					</el-select>
				</el-form-item>
				<el-form-item>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
					</el-button-group>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Plus" @click="openAddDialog"> 新增服务器 </el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 10px">
			<el-table :data="filteredServers" v-loading="state.loading" border>
				<el-table-column prop="serverId" label="服务器 ID" width="120" align="center" />
				<el-table-column prop="serverName" label="名称" width="160" show-overflow-tooltip />
				<el-table-column label="IP / 端口" min-width="220">
					<template #default="scope">
						<div class="ip-item">
							<span>{{ scope.row.ip }}:{{ scope.row.port }}</span>
						</div>
						<div class="ip-item text-secondary" v-if="scope.row.wsPort">
							<span>WS: {{ scope.row.wsUrl || `ws://${scope.row.ip}:${scope.row.wsPort}` }}</span>
						</div>
						<div class="ip-item text-secondary" v-if="scope.row.grpcPort">
							<span>gRPC: {{ scope.row.grpcUrl || `http://${scope.row.ip}:${scope.row.grpcPort}` }}</span>
						</div>
					</template>
				</el-table-column>
				<el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
				<el-table-column label="状态" width="120" align="center">
					<template #default="scope">
						<el-switch
							v-model="scope.row.enabled"
							size="small"
							active-text="启用"
							inactive-text="停用"
							:inline-prompt="true"
							@change="handleEnabledChange(scope.row)"
						/>
					</template>
				</el-table-column>
				<el-table-column label="操作" width="220" fixed="right" align="center">
					<template #default="scope">
						<el-button icon="ele-Edit" text type="primary" @click="openEditDialog(scope.row)"> 编辑 </el-button>
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(scope.row)"> 删除 </el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<el-dialog v-model="state.dialog.visible" :title="state.dialog.title" width="580px" @closed="resetDialogForm">
			<el-form ref="dialogFormRef" :model="dialogForm" :rules="dialogRules" label-width="110px">
				<el-row :gutter="12">
					<el-col :span="12">
						<el-form-item label="服务器 ID" prop="serverId">
							<el-input-number v-model="dialogForm.serverId" :controls="false" :min="1" placeholder="唯一编号" class="w100p" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="服务器名称" prop="serverName">
							<el-input v-model="dialogForm.serverName" maxlength="64" placeholder="示例：一区双线" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="12">
					<el-col :span="12">
						<el-form-item label="IP" prop="ip">
							<el-input v-model="dialogForm.ip" placeholder="10.0.0.1" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="端口" prop="port">
							<el-input-number v-model="dialogForm.port" :controls="false" :min="1" placeholder="8700" class="w100p" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="12">
					<el-col :span="12">
						<el-form-item label="WS 端口">
							<el-input-number v-model="dialogForm.wsPort" :controls="false" :min="0" placeholder="8800" class="w100p" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="gRPC 端口">
							<el-input-number v-model="dialogForm.grpcPort" :controls="false" :min="0" placeholder="30000" class="w100p" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="12">
					<el-col :span="12">
						<el-form-item label="WS 地址">
							<el-input v-model="dialogForm.wsUrl" placeholder="自定义 ws url" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="gRPC 地址">
							<el-input v-model="dialogForm.grpcUrl" placeholder="https://xx:30000" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-form-item label="备注">
					<el-input v-model="dialogForm.remark" type="textarea" :rows="2" maxlength="256" show-word-limit />
				</el-form-item>
				<el-form-item label="状态">
					<el-switch v-model="dialogForm.enabled" inline-prompt active-text="启用" inactive-text="停用" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialog.visible = false"> 取消 </el-button>
				<el-button type="primary" :loading="state.dialog.submitting" @click="submitDialog"> 保存 </el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="gameServerManager">
import { computed, onMounted, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	addGameServerAddress,
	deleteGameServerAddress,
	getGameServerAddressList,
	updateGameServerAddress,
} from '/@/api/gameServerAddress';
import type { GameServerAddressInput, GameServerAddressOutput } from '/@/api/gameServerAddress';

interface GameServerQuery {
	serverId?: number | null;
	keyword?: string;
	enabled?: boolean;
}

interface GameServerAddressForm {
	serverId?: number;
	serverName: string;
	ip: string;
	port?: number;
	wsPort?: number | null;
	wsUrl?: string;
	grpcPort?: number | null;
	grpcUrl?: string;
	remark?: string;
	enabled: boolean;
}

const dialogFormRef = ref<FormInstance>();

const state = reactive({
	loading: false,
	list: [] as GameServerAddressOutput[],
	queryParams: {
		serverId: undefined as number | undefined,
		keyword: '',
		enabled: undefined as undefined | boolean,
	} as GameServerQuery,
	dialog: {
		visible: false,
		title: '新增服务器',
		submitting: false,
		isEdit: false,
	},
});

const dialogForm = reactive<GameServerAddressForm>(getDefaultForm());

const dialogRules: FormRules<GameServerAddressForm> = {
	serverId: [{ required: true, message: '请输入唯一编号', trigger: 'blur' }],
	serverName: [{ required: true, message: '请输入服务器名称', trigger: 'blur' }],
	ip: [{ required: true, message: '请输入 IP 地址', trigger: 'blur' }],
	port: [{ required: true, message: '请输入端口', trigger: 'blur' }],
};

const filteredServers = computed(() => {
	return state.list.filter((item) => {
		const hasServerId = typeof state.queryParams.serverId === 'number' && !Number.isNaN(state.queryParams.serverId);
		const matchesId = hasServerId ? item.serverId === Number(state.queryParams.serverId) : true;
		const keyword = state.queryParams.keyword?.trim().toLowerCase();
		const matchesKeyword = keyword
			? [item.serverName, item.ip, item.remark ?? '', item.wsUrl ?? '', item.grpcUrl ?? ''].some((field) =>
					String(field ?? '').toLowerCase().includes(keyword)
			  )
			: true;
		const matchesStatus =
			typeof state.queryParams.enabled === 'boolean' ? item.enabled === state.queryParams.enabled : true;
		return matchesId && matchesKeyword && matchesStatus;
	});
});

function getDefaultForm(): GameServerAddressForm {
	return {
		serverId: undefined,
		serverName: '',
		ip: '',
		port: undefined,
		wsPort: undefined,
		wsUrl: '',
		grpcPort: undefined,
		grpcUrl: '',
		remark: '',
		enabled: true,
	};
}

const fetchList = async () => {
	state.loading = true;
	try {
		const res = await getGameServerAddressList();
		const result = res.data?.result ?? res.data?.data ?? [];
		state.list = Array.isArray(result) ? result : [];
	} catch (error) {
		state.list = [];
		ElMessage.error('获取服务器列表失败');
	} finally {
		state.loading = false;
	}
};

const handleQuery = () => {
	fetchList();
};

const resetQuery = () => {
	state.queryParams.serverId = undefined;
	state.queryParams.keyword = '';
	state.queryParams.enabled = undefined;
	fetchList();
};

const openAddDialog = () => {
	state.dialog.visible = true;
	state.dialog.title = '新增服务器';
	state.dialog.isEdit = false;
	resetDialogForm();
};

const openEditDialog = (row: GameServerAddressOutput) => {
	state.dialog.visible = true;
	state.dialog.title = `编辑服务器 #${row.serverId}`;
	state.dialog.isEdit = true;
	Object.assign(dialogForm, {
		serverId: row.serverId,
		serverName: row.serverName,
		ip: row.ip,
		port: row.port,
		wsPort: row.wsPort,
		wsUrl: row.wsUrl,
		grpcPort: row.grpcPort,
		grpcUrl: row.grpcUrl,
		remark: row.remark,
		enabled: row.enabled,
	});
};

const resetDialogForm = () => {
	Object.assign(dialogForm, getDefaultForm());
	dialogFormRef.value?.clearValidate();
};

const buildSubmitPayload = (): GameServerAddressInput => {
	return {
		serverId: Number(dialogForm.serverId),
		serverName: dialogForm.serverName?.trim(),
		ip: dialogForm.ip?.trim(),
		port: Number(dialogForm.port),
		wsPort: typeof dialogForm.wsPort === 'number' ? dialogForm.wsPort : undefined,
		wsUrl: dialogForm.wsUrl?.trim() || undefined,
		grpcPort: typeof dialogForm.grpcPort === 'number' ? dialogForm.grpcPort : undefined,
		grpcUrl: dialogForm.grpcUrl?.trim() || undefined,
		remark: dialogForm.remark?.trim() || undefined,
		enabled: dialogForm.enabled,
	};
};

const submitDialog = () => {
	dialogFormRef.value?.validate(async (valid) => {
		if (!valid) return;
		const payload = buildSubmitPayload();
		state.dialog.submitting = true;
		try {
			if (state.dialog.isEdit) {
				await updateGameServerAddress(payload);
				ElMessage.success('服务器已更新');
			} else {
				await addGameServerAddress(payload);
				ElMessage.success('服务器已新增');
			}
			state.dialog.visible = false;
			await fetchList();
		} finally {
			state.dialog.submitting = false;
		}
	});
};

const handleEnabledChange = async (row: GameServerAddressOutput) => {
	try {
		await updateGameServerAddress({
			serverId: row.serverId,
			serverName: row.serverName,
			ip: row.ip,
			port: row.port,
			wsPort: row.wsPort,
			wsUrl: row.wsUrl,
			grpcPort: row.grpcPort,
			grpcUrl: row.grpcUrl,
			remark: row.remark,
			enabled: row.enabled,
		});
		ElMessage.success(row.enabled ? '已启用服务器' : '已停用服务器');
	} catch (error) {
		row.enabled = !row.enabled;
		ElMessage.error('状态切换失败');
	}
};

const handleDelete = (row: GameServerAddressOutput) => {
	ElMessageBox.confirm(`确定删除服务器「${row.serverName}」?`, '提示', {
		type: 'warning',
	})
		.then(async () => {
			await deleteGameServerAddress(row.serverId);
			ElMessage.success('删除成功');
			fetchList();
		})
		.catch(() => undefined);
};

onMounted(() => {
	fetchList();
});
</script>

<style lang="scss" scoped>
.game-server-manage {
	.full-table {
		min-height: 400px;
	}

	.w150 {
		width: 150px;
	}

	.w200 {
		width: 200px;
	}

	.w120 {
		width: 120px;
	}

	.w100p {
		width: 100%;
	}

	.ip-item + .ip-item {
		margin-top: 4px;
	}

	.text-secondary {
		color: var(--el-text-color-secondary);
		font-size: 12px;
	}
}
</style>
