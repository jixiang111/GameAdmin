<template>
	<div class="game-server-item-audit">
		<el-card shadow="hover" :body-style="{ padding: 12 }">
			<el-form :inline="true" :model="query" class="query-form">
				<el-form-item label="服务器">
					<el-select
						v-model="query.serverId"
						filterable
						clearable
						placeholder="请选择服务器"
						style="width: 220px"
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
				<el-form-item label="玩家ID">
					<el-input v-model="query.roleId" clearable placeholder="请输入 RoleId" style="width: 220px" />
				</el-form-item>
				<el-form-item label="物品ID">
					<el-input-number
						v-model="query.itemId"
						:controls="false"
						:min="1"
						placeholder="全部"
						style="width: 160px"
					/>
				</el-form-item>
				<el-form-item label="来源">
					<el-select v-model="query.sourceType" clearable placeholder="全部" style="width: 140px">
						<el-option v-for="item in sourceOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="操作">
					<el-select v-model="query.operationType" clearable placeholder="全部" style="width: 160px">
						<el-option
							v-for="item in operationOptions"
							:key="item.value"
							:label="item.label"
							:value="item.value"
						/>
					</el-select>
				</el-form-item>
				<el-form-item label="操作人ID">
					<el-input v-model="query.operatorId" clearable placeholder="可选" style="width: 180px" />
				</el-form-item>
				<el-form-item label="原因">
					<el-input v-model="query.reasonCode" clearable placeholder="如 activity_reward" style="width: 220px" />
				</el-form-item>
				<el-form-item label="TraceId">
					<el-input v-model="query.traceId" clearable placeholder="可选链路号" style="width: 220px" />
				</el-form-item>
				<el-form-item label="时间范围">
					<el-date-picker
						v-model="timeRange"
						type="datetimerange"
						start-placeholder="开始时间"
						end-placeholder="结束时间"
						range-separator="至"
						style="width: 380px"
					/>
				</el-form-item>
				<el-form-item>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleSearch">查询</el-button>
						<el-button icon="ele-Refresh" @click="handleReset">重置</el-button>
					</el-button-group>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card shadow="hover" style="margin-top: 10px">
			<div class="summary-bar">
				<div class="summary-item">
					<span class="summary-label">当前服务器</span>
					<span class="summary-value">{{ pageInfo.serverName || currentServerName || '-' }}</span>
				</div>
				<div class="summary-item">
					<span class="summary-label">流水总数</span>
					<span class="summary-value">{{ pageInfo.total }}</span>
				</div>
				<div class="summary-item">
					<span class="summary-label">当前分页</span>
					<span class="summary-value">{{ pageInfo.page }}/{{ Math.max(pageInfo.totalPages, 1) }}</span>
				</div>
			</div>

			<el-table :data="tableData" v-loading="loading" border stripe>
				<el-table-column prop="changeTimeTicks" label="变更时间" width="180" align="center">
					<template #default="{ row }">
						<span>{{ formatTicks(row.changeTimeTicks) }}</span>
					</template>
				</el-table-column>
				<el-table-column prop="itemId" label="物品ID" width="120" align="center" />
				<el-table-column label="数量变化" width="120" align="center">
					<template #default="{ row }">
						<span :class="getDeltaClass(row.delta)">{{ formatSignedValue(row.delta) }}</span>
					</template>
				</el-table-column>
				<el-table-column label="变更前后" width="180" align="center">
					<template #default="{ row }">
						<span>{{ normalizeDisplayValue(row.beforeNum) }} -> {{ normalizeDisplayValue(row.afterNum) }}</span>
					</template>
				</el-table-column>
				<el-table-column label="来源" width="110" align="center">
					<template #default="{ row }">
						<el-tag size="small" :type="getSourceTagType(row.sourceType)">
							{{ getSourceLabel(row.sourceType) }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="操作" width="110" align="center">
					<template #default="{ row }">
						<el-tag size="small" effect="plain">{{ getOperationLabel(row.operationType) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="reasonCode" label="原因码" min-width="160" show-overflow-tooltip />
				<el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
				<el-table-column label="操作人" width="170" align="center" show-overflow-tooltip>
					<template #default="{ row }">
						<div>{{ row.operatorName || '-' }}</div>
						<div class="sub-text">{{ normalizeDisplayValue(row.operatorId) }}</div>
					</template>
				</el-table-column>
				<el-table-column prop="traceId" label="TraceId" min-width="220" show-overflow-tooltip />
				<el-table-column prop="requestUniId" label="RequestUniId" min-width="220" show-overflow-tooltip />
			</el-table>

			<el-pagination
				v-model:current-page="query.page"
				v-model:page-size="query.pageSize"
				:total="pageInfo.total"
				:page-sizes="[20, 50, 100]"
				layout="total, sizes, prev, pager, next, jumper"
				style="margin-top: 15px"
				@size-change="handlePageChange"
				@current-change="handlePageChange"
			/>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="gameServerItemAudit">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getGameServerAddressList, type GameServerAddressOutput } from '/@/api/gameServerAddress';
import {
	getGameServerItemAuditPage,
	ItemChangeOperationType,
	ItemChangeSourceType,
	type GameServerItemAuditOutput,
	type GameServerItemAuditPageOutput,
} from '/@/api/gameServerItemAudit';
import { formatDate } from '/@/utils/formatTime';

const DOTNET_EPOCH_TICKS = 621355968000000000n;
const TICKS_PER_MILLISECOND = 10000n;

const route = useRoute();
const loading = ref(false);
const serverLoading = ref(false);
const tableData = ref<GameServerItemAuditOutput[]>([]);
const serverOptions = ref<GameServerAddressOutput[]>([]);
const timeRange = ref<[Date, Date] | []>(createDefaultTimeRange());

const sourceOptions = [
	{ label: '客户端', value: ItemChangeSourceType.Client },
	{ label: 'GM', value: ItemChangeSourceType.Gm },
	{ label: '系统', value: ItemChangeSourceType.System },
];

const operationOptions = [
	{ label: '调整', value: ItemChangeOperationType.Adjust },
	{ label: '使用', value: ItemChangeOperationType.Use },
	{ label: '过期', value: ItemChangeOperationType.Expire },
	{ label: '出售', value: ItemChangeOperationType.Sell },
	{ label: 'GM调整', value: ItemChangeOperationType.GmAdjust },
];

const query = reactive({
	page: 1,
	pageSize: 50,
	serverId: undefined as number | undefined,
	roleId: '',
	itemId: undefined as number | undefined,
	sourceType: undefined as ItemChangeSourceType | undefined,
	operationType: undefined as ItemChangeOperationType | undefined,
	operatorId: '',
	traceId: '',
	reasonCode: '',
});

const pageInfo = reactive<GameServerItemAuditPageOutput>({
	serverId: 0,
	serverName: '',
	page: 1,
	pageSize: 50,
	total: 0,
	totalPages: 0,
	hasMore: false,
	hasNextPage: false,
	hasPrevPage: false,
	items: [],
});

const currentServerName = computed(() => {
	return serverOptions.value.find((item) => item.serverId === query.serverId)?.serverName ?? '';
});

function createDefaultTimeRange(): [Date, Date] {
	const end = new Date();
	const start = new Date(end.getTime() - 7 * 24 * 60 * 60 * 1000);
	return [start, end];
}

function initQueryFromRoute() {
	const serverId = Number(route.query.serverId);
	if (Number.isFinite(serverId) && serverId > 0) {
		query.serverId = serverId;
	}

	if (typeof route.query.roleId === 'string') {
		query.roleId = route.query.roleId.trim();
	}

	if (typeof route.query.itemId === 'string') {
		const itemId = Number(route.query.itemId);
		if (Number.isFinite(itemId) && itemId > 0) {
			query.itemId = itemId;
		}
	}

	if (typeof route.query.traceId === 'string') {
		query.traceId = route.query.traceId.trim();
	}

	if (typeof route.query.reasonCode === 'string') {
		query.reasonCode = route.query.reasonCode.trim();
	}
}

function toUtcTicksString(date: Date | undefined) {
	if (!date || Number.isNaN(date.getTime())) return undefined;
	return (BigInt(date.getTime()) * TICKS_PER_MILLISECOND + DOTNET_EPOCH_TICKS).toString();
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

function getSourceLabel(value: ItemChangeSourceType) {
	switch (value) {
		case ItemChangeSourceType.Client:
			return '客户端';
		case ItemChangeSourceType.Gm:
			return 'GM';
		case ItemChangeSourceType.System:
			return '系统';
		default:
			return '未知';
	}
}

function getSourceTagType(value: ItemChangeSourceType) {
	switch (value) {
		case ItemChangeSourceType.Client:
			return 'info';
		case ItemChangeSourceType.Gm:
			return 'warning';
		case ItemChangeSourceType.System:
			return 'success';
		default:
			return 'info';
	}
}

function getOperationLabel(value: ItemChangeOperationType) {
	switch (value) {
		case ItemChangeOperationType.Adjust:
			return '调整';
		case ItemChangeOperationType.Use:
			return '使用';
		case ItemChangeOperationType.Expire:
			return '过期';
		case ItemChangeOperationType.Sell:
			return '出售';
		case ItemChangeOperationType.GmAdjust:
			return 'GM调整';
		default:
			return '未知';
	}
}

async function fetchServerOptions() {
	serverLoading.value = true;
	try {
		const res = await getGameServerAddressList();
		const result = res.data?.result ?? res.data?.data ?? [];
		serverOptions.value = Array.isArray(result) ? result : [];
	} catch (error) {
		serverOptions.value = [];
		ElMessage.error('获取服务器列表失败');
	} finally {
		serverLoading.value = false;
	}
}

function buildQueryParams() {
	const [startTime, endTime] = Array.isArray(timeRange.value) ? timeRange.value : [];

	return {
		page: query.page,
		pageSize: query.pageSize,
		serverId: query.serverId,
		roleId: query.roleId.trim(),
		itemId: query.itemId,
		sourceType: query.sourceType,
		operationType: query.operationType,
		operatorId: query.operatorId.trim() || undefined,
		traceId: query.traceId.trim() || undefined,
		reasonCode: query.reasonCode.trim() || undefined,
		startTimeTicksUtc: toUtcTicksString(startTime),
		endTimeTicksUtc: toUtcTicksString(endTime),
	};
}

function validateQuery() {
	if (!query.serverId) {
		ElMessage.warning('请先选择服务器');
		return false;
	}

	if (!query.roleId.trim()) {
		ElMessage.warning('请输入玩家ID');
		return false;
	}

	return true;
}

async function fetchTableData() {
	if (!validateQuery()) return;

	loading.value = true;
	try {
		const res: any = await getGameServerItemAuditPage(buildQueryParams());
		const result: GameServerItemAuditPageOutput = res.data?.result ?? res.data?.data ?? res.data ?? pageInfo;
		tableData.value = Array.isArray(result.items) ? result.items : [];
		pageInfo.serverId = result.serverId ?? query.serverId ?? 0;
		pageInfo.serverName = result.serverName ?? '';
		pageInfo.page = result.page ?? query.page;
		pageInfo.pageSize = result.pageSize ?? query.pageSize;
		pageInfo.total = result.total ?? 0;
		pageInfo.totalPages = result.totalPages ?? 0;
		pageInfo.hasMore = Boolean(result.hasMore);
		pageInfo.hasNextPage = Boolean(result.hasNextPage);
		pageInfo.hasPrevPage = Boolean(result.hasPrevPage);
		pageInfo.items = tableData.value;
	} catch (error: any) {
		tableData.value = [];
		pageInfo.total = 0;
		pageInfo.totalPages = 0;
		pageInfo.serverName = '';
		ElMessage.error(error?.response?.data?.message || error?.message || '查询失败');
	} finally {
		loading.value = false;
	}
}

function handleSearch() {
	query.page = 1;
	fetchTableData();
}

function handlePageChange() {
	fetchTableData();
}

function handleReset() {
	query.page = 1;
	query.pageSize = 50;
	query.serverId = undefined;
	query.roleId = '';
	query.itemId = undefined;
	query.sourceType = undefined;
	query.operationType = undefined;
	query.operatorId = '';
	query.traceId = '';
	query.reasonCode = '';
	timeRange.value = createDefaultTimeRange();
	tableData.value = [];
	pageInfo.serverId = 0;
	pageInfo.serverName = '';
	pageInfo.total = 0;
	pageInfo.totalPages = 0;
	pageInfo.items = [];
}

onMounted(async () => {
	initQueryFromRoute();
	await fetchServerOptions();
	if (query.serverId && query.roleId) {
		await fetchTableData();
	}
});
</script>

<style scoped lang="scss">
.game-server-item-audit {
	.query-form {
		row-gap: 8px;
	}

	.summary-bar {
		display: flex;
		flex-wrap: wrap;
		gap: 12px;
		margin-bottom: 14px;
		padding: 12px 14px;
		border-radius: 10px;
		background: linear-gradient(135deg, #f6fbff 0%, #eef4ff 100%);
		border: 1px solid #dbeafe;
	}

	.summary-item {
		min-width: 180px;
	}

	.summary-label {
		display: block;
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-bottom: 4px;
	}

	.summary-value {
		font-size: 16px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.sub-text {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.delta-positive {
		color: var(--el-color-success);
		font-weight: 600;
	}

	.delta-negative {
		color: var(--el-color-danger);
		font-weight: 600;
	}
}
</style>


