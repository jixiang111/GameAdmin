<template>
	<div class="game-server-item-adjust">
		<el-row :gutter="12">
			<el-col :xs="24" :lg="15">
				<el-card shadow="hover" :body-style="{ padding: 16 }">
					<template #header>
						<div class="card-title">玩家道具调整</div>
					</template>

					<el-alert
						title="正数表示发放，负数表示扣减，请确认服务器、玩家和道具信息后再提交。"
						type="warning"
						show-icon
						:closable="false"
						class="form-alert"
					/>

					<el-form :model="form" label-width="96px">
						<el-form-item label="服务器">
							<el-select
								v-model="form.serverId"
								filterable
								clearable
								placeholder="请选择服务器"
								style="width: 100%"
								:loading="serverLoading"
							>
								<el-option
									v-for="item in serverOptions"
									:key="item.serverId"
									:label="`${item.serverId} - ${item.serverName}`"
									:value="item.serverId"
								/>
							</el-select>
						</el-form-item>

						<el-row :gutter="12">
							<el-col :xs="24" :md="12">
								<el-form-item label="玩家ID">
									<el-input v-model="form.roleId" clearable placeholder="请输入 RoleId" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :md="12">
								<el-form-item label="物品ID">
									<el-input-number v-model="form.itemId" :controls="false" :min="1" style="width: 100%" />
								</el-form-item>
							</el-col>
						</el-row>

						<el-row :gutter="12">
							<el-col :xs="24" :md="12">
								<el-form-item label="调整数量">
									<el-input-number
										v-model="form.delta"
										:controls="false"
										:step="1"
										style="width: 100%"
										placeholder="正数发放，负数扣减"
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :md="12">
								<el-form-item label="原因码">
									<el-input v-model="form.reasonCode" maxlength="128" clearable placeholder="如 gm_compensate" />
								</el-form-item>
							</el-col>
						</el-row>

						<el-row :gutter="12">
							<el-col :xs="24" :md="12">
								<el-form-item label="TraceId">
									<el-input v-model="form.traceId" maxlength="128" clearable placeholder="可选，留空则后台生成" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :md="12">
								<el-form-item label="当前操作人">
									<div class="operator-box">
										<div class="operator-name">{{ operatorName }}</div>
										<div class="operator-id">{{ operatorIdText }}</div>
									</div>
								</el-form-item>
							</el-col>
						</el-row>

						<el-form-item label="备注">
							<el-input
								v-model="form.remark"
								type="textarea"
								:rows="3"
								maxlength="512"
								show-word-limit
								placeholder="请输入业务说明，例如补偿原因"
							/>
						</el-form-item>

						<el-form-item>
							<el-button type="primary" :loading="submitting" icon="ele-Position" @click="handleSubmit">
								提交调整
							</el-button>
							<el-button icon="ele-Refresh" @click="handleReset">重置</el-button>
							<el-button v-if="result" icon="ele-Histogram" @click="goToAudit">查看流水</el-button>
						</el-form-item>
					</el-form>
				</el-card>
			</el-col>

			<el-col :xs="24" :lg="9">
				<el-card shadow="hover" class="result-card" :body-style="{ padding: 16 }">
					<template #header>
						<div class="card-title">调整结果</div>
					</template>

					<template v-if="result">
						<div class="result-grid">
							<div class="result-item">
								<span class="label">服务器</span>
								<span class="value">{{ currentServerName || result.serverId }}</span>
							</div>
							<div class="result-item">
								<span class="label">玩家ID</span>
								<span class="value">{{ normalizeDisplayValue(result.roleId) }}</span>
							</div>
							<div class="result-item">
								<span class="label">物品ID</span>
								<span class="value">{{ result.itemId }}</span>
							</div>
							<div class="result-item">
								<span class="label">实际调整</span>
								<span class="value" :class="getDeltaClass(result.appliedDelta)">{{ formatSignedValue(result.appliedDelta) }}</span>
							</div>
							<div class="result-item">
								<span class="label">调整前</span>
								<span class="value">{{ normalizeDisplayValue(result.beforeNum) }}</span>
							</div>
							<div class="result-item">
								<span class="label">调整后</span>
								<span class="value">{{ normalizeDisplayValue(result.afterNum) }}</span>
							</div>
							<div class="result-item full">
								<span class="label">TraceId</span>
								<span class="value mono">{{ result.traceId || '-' }}</span>
							</div>
							<div class="result-item full">
								<span class="label">变更时间</span>
								<span class="value">{{ formatTicks(result.changeTimeTicks) }}</span>
							</div>
							<div class="result-item full">
								<span class="label">原因与备注</span>
								<span class="value">{{ result.reasonCode || '-' }} / {{ result.remark || '-' }}</span>
							</div>
						</div>
					</template>
					<el-empty v-else description="提交后会在这里展示服务端返回结果" />
				</el-card>
			</el-col>
		</el-row>
	</div>
</template>

<script setup lang="ts" name="gameServerItemAdjust">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useUserInfo } from '/@/stores/userInfo';
import { getGameServerAddressList, type GameServerAddressOutput } from '/@/api/gameServerAddress';
import {
	submitGameServerItemAdjust,
	type GameServerItemAdjustInput,
	type GameServerItemAdjustOutput,
} from '/@/api/gameServerItemAdjust';
import { formatDate } from '/@/utils/formatTime';

const DOTNET_EPOCH_TICKS = 621355968000000000n;
const TICKS_PER_MILLISECOND = 10000n;

const route = useRoute();
const router = useRouter();
const userStore = useUserInfo();
const serverLoading = ref(false);
const submitting = ref(false);
const result = ref<GameServerItemAdjustOutput | null>(null);
const serverOptions = ref<GameServerAddressOutput[]>([]);

const form = reactive({
	serverId: undefined as number | undefined,
	roleId: '',
	itemId: undefined as number | undefined,
	delta: undefined as number | undefined,
	reasonCode: '',
	remark: '',
	traceId: '',
});

const operatorName = computed(() => userStore.userInfos.realName || userStore.userInfos.account || '-');
const operatorIdText = computed(() => userStore.userInfos.id || '-');
const currentServerName = computed(() => {
	return serverOptions.value.find((item) => item.serverId === form.serverId)?.serverName ?? '';
});

watch(
	() => [form.serverId, form.roleId, form.itemId, form.delta, form.reasonCode, form.remark, form.traceId],
	() => {
		result.value = null;
	}
);

function initFormFromRoute() {
	const serverId = Number(route.query.serverId);
	if (Number.isFinite(serverId) && serverId > 0) {
		form.serverId = serverId;
	}

	if (typeof route.query.roleId === 'string') {
		form.roleId = route.query.roleId.trim();
	}

	if (typeof route.query.itemId === 'string') {
		const itemId = Number(route.query.itemId);
		if (Number.isFinite(itemId) && itemId > 0) {
			form.itemId = itemId;
		}
	}

	if (typeof route.query.reasonCode === 'string') {
		form.reasonCode = route.query.reasonCode.trim();
	}

	if (typeof route.query.traceId === 'string') {
		form.traceId = route.query.traceId.trim();
	}
}

async function fetchServerOptions() {
	serverLoading.value = true;
	try {
		const res = await getGameServerAddressList();
		const result = res.data?.result ?? res.data?.data ?? [];
		serverOptions.value = Array.isArray(result) ? result : [];
	} catch {
		serverOptions.value = [];
		ElMessage.error('获取服务器列表失败');
	} finally {
		serverLoading.value = false;
	}
}

function buildSubmitPayload(): GameServerItemAdjustInput {
	return {
		serverId: form.serverId,
		roleId: form.roleId.trim(),
		itemId: form.itemId,
		delta: form.delta,
		reasonCode: form.reasonCode.trim(),
		remark: form.remark.trim() || undefined,
		traceId: form.traceId.trim() || undefined,
	};
}

function validateForm() {
	if (!form.serverId) {
		ElMessage.warning('请先选择服务器');
		return false;
	}

	if (!form.roleId.trim()) {
		ElMessage.warning('请输入玩家ID');
		return false;
	}

	if (!form.itemId) {
		ElMessage.warning('请输入物品ID');
		return false;
	}

	if (!Number.isFinite(form.delta) || !form.delta) {
		ElMessage.warning('请输入非 0 的调整数量');
		return false;
	}

	if (!form.reasonCode.trim()) {
		ElMessage.warning('请输入原因码');
		return false;
	}

	return true;
}

async function handleSubmit() {
	if (!validateForm()) return;

	try {
		await ElMessageBox.confirm(
			`确认对玩家 ${form.roleId.trim()} 的物品 ${form.itemId} 执行 ${formatSignedValue(form.delta)} 调整吗？`,
			'确认调整',
			{
				type: 'warning',
				confirmButtonText: '确认提交',
				cancelButtonText: '取消',
			}
		);
	} catch {
		return;
	}

	submitting.value = true;
	try {
		const res: any = await submitGameServerItemAdjust(buildSubmitPayload());
		result.value = res.data?.result ?? res.data?.data ?? res.data;
		ElMessage.success('调整成功');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '调整失败');
	} finally {
		submitting.value = false;
	}
}

function handleReset() {
	form.serverId = undefined;
	form.roleId = '';
	form.itemId = undefined;
	form.delta = undefined;
	form.reasonCode = '';
	form.remark = '';
	form.traceId = '';
	result.value = null;
}

function goToAudit() {
	const target = result.value;
	if (!target) return;

	router.push({
		path: '/dashboard/server-item-audit',
		query: {
			serverId: String(target.serverId || form.serverId || ''),
			roleId: String(target.roleId || form.roleId || ''),
			itemId: String(target.itemId || form.itemId || ''),
			traceId: target.traceId || form.traceId.trim() || undefined,
			reasonCode: target.reasonCode || form.reasonCode.trim() || undefined,
		},
	});
}

function parseTicksToDate(value: string | number | bigint | null | undefined) {
	if (value === undefined || value === null || value === '') return null;

	try {
		const ticks = BigInt(String(value));
		const milliseconds = Number((ticks - DOTNET_EPOCH_TICKS) / TICKS_PER_MILLISECOND);
		const date = new Date(milliseconds);
		return Number.isNaN(date.getTime()) ? null : date;
	} catch {
		if (typeof value === 'number') {
			const milliseconds = (value - Number(DOTNET_EPOCH_TICKS)) / Number(TICKS_PER_MILLISECOND);
			const date = new Date(milliseconds);
			return Number.isNaN(date.getTime()) ? null : date;
		}
		return null;
	}
}

function formatTicks(value: string | number | bigint | null | undefined) {
	const date = parseTicksToDate(value);
	return date ? formatDate(date, 'YYYY-mm-dd HH:MM:SS') : '-';
}

function normalizeDisplayValue(value: unknown) {
	if (value === undefined || value === null || value === '') return '-';
	return String(value);
}

function formatSignedValue(value: unknown) {
	const text = normalizeDisplayValue(value);
	if (text === '-') return text;
	return text.startsWith('-') || text.startsWith('+') ? text : `+${text}`;
}

function getDeltaClass(value: unknown) {
	const text = normalizeDisplayValue(value);
	if (text.startsWith('-')) return 'delta-negative';
	if (text !== '-') return 'delta-positive';
	return '';
}

onMounted(async () => {
	initFormFromRoute();
	if (!userStore.userInfos.id) {
		await userStore.setUserInfos();
	}
	await fetchServerOptions();
});
</script>

<style scoped lang="scss">
.game-server-item-adjust {
	.card-title {
		font-size: 15px;
		font-weight: 600;
	}

	.form-alert {
		margin-bottom: 16px;
	}

	.operator-box {
		width: 100%;
		padding: 10px 12px;
		border-radius: 8px;
		background: linear-gradient(135deg, #f7f8fb 0%, #eef2ff 100%);
		border: 1px solid #d8e1ff;
	}

	.operator-name {
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.operator-id {
		margin-top: 4px;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.result-card {
		min-height: 100%;
	}

	.result-grid {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 12px;
	}

	.result-item {
		padding: 12px;
		border-radius: 10px;
		background: #f8fafc;
		border: 1px solid #e5e7eb;
	}

	.result-item.full {
		grid-column: 1 / -1;
	}

	.label {
		display: block;
		margin-bottom: 6px;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.value {
		font-size: 14px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		word-break: break-all;
	}

	.value.mono {
		font-family: Consolas, Monaco, monospace;
		font-size: 13px;
	}

	.delta-positive {
		color: var(--el-color-success);
	}

	.delta-negative {
		color: var(--el-color-danger);
	}
}
</style>
