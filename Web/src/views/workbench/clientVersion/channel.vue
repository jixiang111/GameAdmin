<template>
	<div class="client-channel-version">
		<el-card shadow="hover" :body-style="{ padding: 12 }">
			<el-form :inline="true" :model="query">
				<el-form-item label="渠道ID">
					<el-input v-model="query.channelId" placeholder="渠道ID" clearable style="width: 200px" />
				</el-form-item>
				<el-form-item label="平台">
					<el-select v-model="query.platform" placeholder="全部" clearable style="width: 140px">
						<el-option label="Android" :value="ClientPlatform.Android" />
						<el-option label="iOS" :value="ClientPlatform.Ios" />
						<el-option label="小程序" :value="ClientPlatform.Mini" />
					</el-select>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Search" @click="handleQuery">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Plus" @click="openDrawer()">新增版本规则</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 10px">
			<el-table :data="tableData" border stripe v-loading="loading">
				<el-table-column type="index" label="序号" width="60" align="center" />
				<el-table-column prop="channelId" label="渠道ID" width="120" />
				<el-table-column prop="channelName" label="渠道名称" width="140" />
				<el-table-column label="平台" width="100" align="center">
					<template #default="scope">
						<el-tag :type="getPlatformTagType(scope.row.platform)" size="small">{{ getPlatformName(scope.row.platform) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="白名单测试版本" min-width="180">
					<template #default="scope">
						<div v-if="scope.row.whitelistVersionInfo">
							<div><strong>App:</strong> {{ scope.row.whitelistVersionInfo.appVersion }}</div>
							<div><strong>资源:</strong> {{ scope.row.whitelistVersionInfo.resVersion }}</div>
							<div v-if="scope.row.whitelistMachineCodes && scope.row.whitelistMachineCodes.length > 0">
								<el-tag size="small" type="info">{{ scope.row.whitelistMachineCodes.length }}个测试用户</el-tag>
							</div>
						</div>
						<span v-else class="text-secondary">未配置</span>
					</template>
				</el-table-column>
				<el-table-column label="正式版本" min-width="180">
					<template #default="scope">
						<div v-if="scope.row.generalVersionInfo">
							<div><strong>App:</strong> {{ scope.row.generalVersionInfo.appVersion }}</div>
							<div><strong>资源:</strong> {{ scope.row.generalVersionInfo.resVersion }}</div>
						</div>
						<span v-else class="text-secondary">未配置</span>
					</template>
				</el-table-column>
				<el-table-column prop="bucketName" label="Bucket" width="150" show-overflow-tooltip />
				<el-table-column label="最近发布" width="180">
					<template #default="scope">
						<div v-if="scope.row.latestPublishTime">
							<div>{{ scope.row.latestPublishUserName }}</div>
							<div class="text-secondary" style="font-size: 12px">{{ scope.row.latestPublishTime }}</div>
						</div>
						<span v-else class="text-secondary">未发布</span>
					</template>
				</el-table-column>
				<el-table-column label="操作" width="220" align="center" fixed="right">
					<template #default="scope">
						<el-button text type="primary" icon="ele-Edit" @click="openDrawer(scope.row)">编辑</el-button>
						<el-button
							text
							type="warning"
							icon="ele-Promotion"
							:disabled="!scope.row.whitelistVersionInfo"
							@click="handlePublish(scope.row)"
						>
							发布
						</el-button>
						<el-popconfirm title="确定删除该规则吗？" @confirm="handleDelete(scope.row)">
							<template #reference>
								<el-button text type="danger" icon="ele-Delete">删除</el-button>
							</template>
						</el-popconfirm>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<!-- 编辑抽屉 -->
		<el-drawer v-model="drawer.visible" :title="drawer.title" size="800px" @closed="resetDrawer">
			<el-form ref="drawerFormRef" :model="drawer.form" :rules="drawerRules" label-width="140px" class="drawer-form">
				<el-divider content-position="left">基础配置</el-divider>

				<el-form-item label="渠道ID" prop="channelId">
					<el-input
						v-model="drawer.form.channelId"
						placeholder="渠道唯一标识，如 official, taptap"
						:disabled="!!drawer.form.ruleId"
						@blur="syncChannelName"
					/>
				</el-form-item>

				<el-form-item label="渠道名称" prop="channelName">
					<el-input v-model="drawer.form.channelName" placeholder="渠道显示名称，如 官方渠道" />
				</el-form-item>

				<el-form-item label="运行平台" prop="platform">
					<el-select v-model="drawer.form.platform" placeholder="请选择平台" :disabled="!!drawer.form.ruleId" @change="onPlatformChange">
						<el-option label="Android" :value="ClientPlatform.Android" />
						<el-option label="iOS" :value="ClientPlatform.Ios" />
						<el-option label="小程序 (WebGL)" :value="ClientPlatform.Mini" />
					</el-select>
					<el-text class="form-tip">选择后不可修改，每个渠道+平台仅能有一条规则</el-text>
				</el-form-item>

				<el-form-item label="OSS Bucket">
					<el-input v-model="drawer.form.bucketName" placeholder="留空使用全局 OSS 配置的 Bucket" clearable />
				</el-form-item>

				<el-form-item label="根路径 (RootPath)">
					<el-input v-model="drawer.form.rootPath" placeholder="默认 /" clearable />
				</el-form-item>

				<el-form-item label="自定义资源域名">
					<el-input v-model="drawer.form.resourceDomain" placeholder="可选，自定义CDN域名" clearable />
				</el-form-item>

				<el-divider content-position="left">白名单测试版本</el-divider>

				<el-form-item label="App 版本" prop="whitelistVersionInfo.appVersion">
					<el-select v-model="whitelistAppVersion" placeholder="请选择" :loading="appVersionsLoading" @change="onWhitelistAppVersionChange">
						<el-option v-for="item in appVersions" :key="item.appVersion" :label="item.appVersion" :value="item.appVersion" />
					</el-select>
					<el-button type="primary" link icon="ele-Refresh" @click="loadAppVersions">刷新</el-button>
				</el-form-item>

				<el-form-item label="资源版本" prop="whitelistVersionInfo.resVersion">
					<el-select v-model="whitelistResVersion" placeholder="请先选择 App 版本" :loading="whitelistResVersionsLoading" :disabled="!whitelistAppVersion">
						<el-option v-for="item in whitelistResVersions" :key="item.fileName" :label="formatResVersionLabel(item)" :value="item.fileName">
							<div style="display: flex; justify-content: space-between">
								<span>{{ item.fileName }}</span>
								<el-tag v-if="item.isNewerThanOnline" type="success" size="small">新版本</el-tag>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<el-form-item label="强制更新">
					<el-switch v-model="whitelistForceUpdate" />
				</el-form-item>

				<el-form-item label="更新地址">
					<el-input v-model="whitelistUpdateUrl" placeholder="可选，客户端更新下载地址" />
				</el-form-item>

				<el-form-item label="登录地址">
					<el-input v-model="whitelistLoginUrl" placeholder="可选，登录服务器地址" />
				</el-form-item>

				<el-form-item label="更新公告">
					<el-input v-model="whitelistUpdateNotice" type="textarea" :rows="2" placeholder="可选，更新内容说明" />
				</el-form-item>

				<el-form-item label="白名单测试用户" prop="whitelistTesterEntryIds">
					<el-select v-model="drawer.form.whitelistTesterEntryIds" multiple filterable placeholder="从白名单列表中选择测试用户" style="width: 100%">
						<el-option v-for="item in whitelistOptions" :key="item.entryId" :label="`${item.machineCode} (${item.remark || '无备注'})`" :value="item.entryId" />
					</el-select>
					<el-button type="primary" link icon="ele-Refresh" @click="loadWhitelistOptions">刷新白名单</el-button>
				</el-form-item>

				<el-divider content-position="left">正式版本</el-divider>

				<el-form-item label="App 版本" prop="generalVersionInfo.appVersion">
					<el-select
						v-model="generalAppVersion"
						placeholder="请选择"
						:loading="appVersionsLoading"
						:disabled="isEditing"
						@change="onGeneralAppVersionChange"
					>
						<el-option v-for="item in appVersions" :key="item.appVersion" :label="item.appVersion" :value="item.appVersion" />
					</el-select>
					<el-button type="primary" link icon="ele-Refresh" :disabled="isEditing" @click="loadAppVersions">刷新</el-button>
					<el-text v-if="isEditing" class="form-tip">编辑模式下正式版由发布流程生成，无法手动修改</el-text>
				</el-form-item>

				<el-form-item label="资源版本" prop="generalVersionInfo.resVersion">
					<el-select
						v-model="generalResVersion"
						placeholder="请先选择 App 版本"
						:loading="generalResVersionsLoading"
						:disabled="!generalAppVersion || isEditing"
					>
						<el-option v-for="item in generalResVersions" :key="item.fileName" :label="formatResVersionLabel(item)" :value="item.fileName">
							<div style="display: flex; justify-content: space-between">
								<span>{{ item.fileName }}</span>
								<el-tag v-if="item.isNewerThanOnline" type="success" size="small">新版本</el-tag>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<el-form-item label="强制更新">
					<el-switch v-model="generalForceUpdate" :disabled="isEditing" />
				</el-form-item>

				<el-form-item label="更新地址">
					<el-input v-model="generalUpdateUrl" placeholder="可选，客户端更新下载地址" :disabled="isEditing" />
				</el-form-item>

				<el-form-item label="登录地址">
					<el-input v-model="generalLoginUrl" placeholder="可选，登录服务器地址" :disabled="isEditing" />
				</el-form-item>

				<el-form-item label="更新公告">
					<el-input v-model="generalUpdateNotice" type="textarea" :rows="2" placeholder="可选，更新内容说明" :disabled="isEditing" />
				</el-form-item>
			</el-form>

			<template #footer>
				<el-button @click="drawer.visible = false">取消</el-button>
				<el-button type="primary" :loading="drawer.saving" @click="submitDrawer">保存</el-button>
			</template>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="clientChannelVersion">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	getRuleList,
	saveRule,
	deleteRule,
	promoteWhitelist,
	getAppVersions,
	getResVersions,
	getWhitelistList,
	type ClientVersionRuleOutput,
	type ClientVersionRuleSaveInput,
	type ChannelAppVersionItem,
	type ChannelResVersionItem,
	type ClientWhitelistOutput,
	type ClientAppVersionInfoDto,
	ClientPlatform,
} from '/@/api/clientVersion';

// ==================== 查询表格 ====================

const query = reactive({
	channelId: '',
	platform: undefined as ClientPlatform | undefined,
});

const loading = ref(false);
const tableData = ref<ClientVersionRuleOutput[]>([]);

const handleQuery = async () => {
	loading.value = true;
	try {
		const res: any = await getRuleList(query);
		tableData.value = res.data?.result ?? res.data?.data ?? res.data ?? [];
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '查询失败');
	} finally {
		loading.value = false;
	}
};

const resetQuery = () => {
	query.channelId = '';
	query.platform = undefined;
	handleQuery();
};

const getPlatformName = (platform: ClientPlatform): string => {
	const map: Record<ClientPlatform, string> = {
		[ClientPlatform.Android]: 'Android',
		[ClientPlatform.Ios]: 'iOS',
		[ClientPlatform.Mini]: '小程序',
	};
	return map[platform] || '未知';
};

const getPlatformTagType = (platform: ClientPlatform): string => {
	const map: Record<ClientPlatform, string> = {
		[ClientPlatform.Android]: 'success',
		[ClientPlatform.Ios]: 'primary',
		[ClientPlatform.Mini]: 'warning',
	};
	return map[platform] || 'info';
};

// ==================== 抽屉表单 ====================

const drawerFormRef = ref<FormInstance>();
const drawer = reactive({
	visible: false,
	title: '新增版本规则',
	saving: false,
	form: {
		ruleId: undefined as string | undefined,
		channelId: '',
		channelName: '',
		platform: undefined as ClientPlatform | undefined,
		bucketName: '',
		rootPath: '/',
		resourceDomain: '',
		whitelistTesterEntryIds: [] as string[],
	} as ClientVersionRuleSaveInput,
});

const isEditing = computed(() => !!drawer.form.ruleId);

const drawerRules: FormRules<ClientVersionRuleSaveInput> = {
	channelId: [{ required: true, message: '请输入渠道ID', trigger: 'blur' }],
	channelName: [{ required: true, message: '请输入渠道名称', trigger: 'blur' }],
	platform: [{ required: true, message: '请选择平台', trigger: 'change' }],
};

// 白名单版本信息
const whitelistAppVersion = ref('');
const whitelistResVersion = ref('');
const whitelistForceUpdate = ref(false);
const whitelistUpdateUrl = ref('');
const whitelistLoginUrl = ref('');
const whitelistUpdateNotice = ref('');

// 正式版本信息
const generalAppVersion = ref('');
const generalResVersion = ref('');
const generalForceUpdate = ref(false);
const generalUpdateUrl = ref('');
const generalLoginUrl = ref('');
const generalUpdateNotice = ref('');

// AppVersion 列表
const appVersions = ref<ChannelAppVersionItem[]>([]);
const appVersionsLoading = ref(false);

// 白名单资源版本列表
const whitelistResVersions = ref<ChannelResVersionItem[]>([]);
const whitelistResVersionsLoading = ref(false);

// 正式版资源版本列表
const generalResVersions = ref<ChannelResVersionItem[]>([]);
const generalResVersionsLoading = ref(false);

// 白名单选项
const whitelistOptions = ref<ClientWhitelistOutput[]>([]);

const syncChannelName = () => {
	if (!drawer.form.channelName && drawer.form.channelId) {
		drawer.form.channelName = drawer.form.channelId;
	}
};

const onPlatformChange = () => {
	// 清空版本选择
	whitelistAppVersion.value = '';
	whitelistResVersion.value = '';
	generalAppVersion.value = '';
	generalResVersion.value = '';
	appVersions.value = [];
	whitelistResVersions.value = [];
	generalResVersions.value = [];

	// 加载 AppVersion 列表
	if (drawer.form.channelId && drawer.form.platform) {
		loadAppVersions();
	}
};

const loadAppVersions = async () => {
	if (!drawer.form.channelId || !drawer.form.platform) {
		ElMessage.warning('请先填写渠道ID和选择平台');
		return;
	}

	appVersionsLoading.value = true;
	try {
		const res: any = await getAppVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform,
			bucketName: drawer.form.bucketName || undefined,
			rootPath: drawer.form.rootPath || undefined,
		});
		appVersions.value = res.data?.result ?? res.data?.data ?? res.data ?? [];

		if (appVersions.value.length === 0) {
			ElMessage.warning('未找到任何 AppVersion，请确认 OSS 目录下存在版本文件夹');
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载 AppVersion 失败');
	} finally {
		appVersionsLoading.value = false;
	}
};

const onWhitelistAppVersionChange = async () => {
	whitelistResVersion.value = '';
	whitelistResVersions.value = [];

	if (!whitelistAppVersion.value) return;

	whitelistResVersionsLoading.value = true;
	try {
		const res: any = await getResVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform!,
			appVersion: whitelistAppVersion.value,
			bucketName: drawer.form.bucketName || undefined,
			rootPath: drawer.form.rootPath || undefined,
		});
		whitelistResVersions.value = res.data?.result ?? res.data?.data ?? res.data ?? [];

		if (whitelistResVersions.value.length > 0) {
			// 默认选择第一个（最新的）
			whitelistResVersion.value = whitelistResVersions.value[0].fileName;
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载资源版本失败');
	} finally {
		whitelistResVersionsLoading.value = false;
	}
};

const onGeneralAppVersionChange = async () => {
	generalResVersion.value = '';
	generalResVersions.value = [];

	if (!generalAppVersion.value) return;

	generalResVersionsLoading.value = true;
	try {
		const res: any = await getResVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform!,
			appVersion: generalAppVersion.value,
			bucketName: drawer.form.bucketName || undefined,
			rootPath: drawer.form.rootPath || undefined,
		});
		generalResVersions.value = res.data?.result ?? res.data?.data ?? res.data ?? [];

		if (generalResVersions.value.length > 0) {
			// 默认选择第一个（最新的）
			generalResVersion.value = generalResVersions.value[0].fileName;
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载资源版本失败');
	} finally {
		generalResVersionsLoading.value = false;
	}
};

const loadWhitelistOptions = async () => {
	try {
		const res: any = await getWhitelistList({ onlyEnabled: true });
		whitelistOptions.value = res.data?.result ?? res.data?.data ?? res.data ?? [];
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载白名单失败');
	}
};

const formatResVersionLabel = (item: ChannelResVersionItem): string => {
	const size = item.fileSize ? `${(item.fileSize / 1024).toFixed(2)} KB` : '';
	const time = item.timestamp ? new Date(item.timestamp).toLocaleString() : '';
	return `${item.fileName} ${size ? `[${size}]` : ''} ${time ? `[${time}]` : ''}`;
};

const openDrawer = async (row?: ClientVersionRuleOutput) => {
	resetDrawer();

	if (row?.ruleId) {
		drawer.title = '编辑版本规则';
		drawer.form.ruleId = row.ruleId;
		drawer.form.channelId = row.channelId;
		drawer.form.channelName = row.channelName;
		drawer.form.platform = row.platform;
		drawer.form.bucketName = row.bucketName;
		drawer.form.rootPath = row.rootPath;
		drawer.form.resourceDomain = row.resourceDomain || '';
		drawer.form.whitelistTesterEntryIds = row.whitelistTesterEntryIds || [];

		// 白名单版本信息
		if (row.whitelistVersionInfo) {
			whitelistAppVersion.value = row.whitelistVersionInfo.appVersion;
			whitelistResVersion.value = row.whitelistVersionInfo.resVersion;
			whitelistForceUpdate.value = row.whitelistVersionInfo.forceUpdate;
			whitelistUpdateUrl.value = row.whitelistVersionInfo.updateUrl || '';
			whitelistLoginUrl.value = row.whitelistVersionInfo.loginUrl || '';
			whitelistUpdateNotice.value = row.whitelistVersionInfo.updateNotice || '';
		}

		// 正式版本信息
		if (row.generalVersionInfo) {
			generalAppVersion.value = row.generalVersionInfo.appVersion;
			generalResVersion.value = row.generalVersionInfo.resVersion;
			generalForceUpdate.value = row.generalVersionInfo.forceUpdate;
			generalUpdateUrl.value = row.generalVersionInfo.updateUrl || '';
			generalLoginUrl.value = row.generalVersionInfo.loginUrl || '';
			generalUpdateNotice.value = row.generalVersionInfo.updateNotice || '';
		}
	} else {
		drawer.title = '新增版本规则';
	}

	drawer.visible = true;

	// 加载 AppVersion 和白名单选项
	if (drawer.form.channelId && drawer.form.platform) {
		await loadAppVersions();
		await loadWhitelistOptions();

		// 如果是编辑模式，加载资源版本
		if (row?.ruleId) {
			if (whitelistAppVersion.value) {
				await onWhitelistAppVersionChange();
			}
			if (generalAppVersion.value) {
				await onGeneralAppVersionChange();
			}
		}
	}
};

const resetDrawer = () => {
	drawer.form = {
		ruleId: undefined,
		channelId: '',
		channelName: '',
		platform: undefined,
		bucketName: '',
		rootPath: '/',
		resourceDomain: '',
		whitelistTesterEntryIds: [],
	};

	whitelistAppVersion.value = '';
	whitelistResVersion.value = '';
	whitelistForceUpdate.value = false;
	whitelistUpdateUrl.value = '';
	whitelistLoginUrl.value = '';
	whitelistUpdateNotice.value = '';

	generalAppVersion.value = '';
	generalResVersion.value = '';
	generalForceUpdate.value = false;
	generalUpdateUrl.value = '';
	generalLoginUrl.value = '';
	generalUpdateNotice.value = '';

	appVersions.value = [];
	whitelistResVersions.value = [];
	generalResVersions.value = [];

	drawerFormRef.value?.clearValidate();
};

const submitDrawer = () => {
	drawerFormRef.value?.validate(async (valid) => {
		if (!valid) return;

		// 构建白名单版本信息
		let whitelistVersionInfo: ClientAppVersionInfoDto | undefined;
		if (whitelistAppVersion.value && whitelistResVersion.value) {
			whitelistVersionInfo = {
				appVersion: whitelistAppVersion.value,
				resVersion: whitelistResVersion.value,
				forceUpdate: whitelistForceUpdate.value,
				updateUrl: whitelistUpdateUrl.value || undefined,
				loginUrl: whitelistLoginUrl.value || undefined,
				updateNotice: whitelistUpdateNotice.value || undefined,
			};
		}

		// 构建正式版本信息
		let generalVersionInfo: ClientAppVersionInfoDto | undefined;
		if (generalAppVersion.value && generalResVersion.value) {
			generalVersionInfo = {
				appVersion: generalAppVersion.value,
				resVersion: generalResVersion.value,
				forceUpdate: generalForceUpdate.value,
				updateUrl: generalUpdateUrl.value || undefined,
				loginUrl: generalLoginUrl.value || undefined,
				updateNotice: generalUpdateNotice.value || undefined,
			};
		}

		const saveData: ClientVersionRuleSaveInput = {
			...drawer.form,
			whitelistVersionInfo,
			generalVersionInfo,
		};

		drawer.saving = true;
		try {
			await saveRule(saveData);
			ElMessage.success('版本规则已保存');
			drawer.visible = false;
			handleQuery();
		} catch (error: any) {
			ElMessage.error(error?.response?.data?.message || error?.message || '保存失败');
		} finally {
			drawer.saving = false;
		}
	});
};

const handlePublish = async (row: ClientVersionRuleOutput) => {
	if (!row.whitelistVersionInfo) {
		ElMessage.warning('请先配置白名单测试版本');
		return;
	}

	try {
		await ElMessageBox.confirm(
			`确定要将白名单版本发布为正式版本吗？
			<br><br>
			<strong>白名单版本：</strong><br>
			App: ${row.whitelistVersionInfo.appVersion}<br>
			资源: ${row.whitelistVersionInfo.resVersion}<br><br>
			<strong>当前正式版本：</strong><br>
			${row.generalVersionInfo ? `App: ${row.generalVersionInfo.appVersion}<br>资源: ${row.generalVersionInfo.resVersion}` : '未配置'}
			`,
			'确认发布',
			{
				confirmButtonText: '确定发布',
				cancelButtonText: '取消',
				type: 'warning',
				dangerouslyUseHTMLString: true,
			}
		);

		await promoteWhitelist({
			ruleId: row.ruleId,
			confirm: true,
		});

		ElMessage.success('白名单版本已发布为正式版本');
		handleQuery();
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || error?.message || '发布失败');
		}
	}
};

const handleDelete = async (row: ClientVersionRuleOutput) => {
	try {
		await deleteRule(row.ruleId);
		ElMessage.success('版本规则已删除');
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
.client-channel-version {
	.drawer-form {
		:deep(.el-select),
		:deep(.el-input) {
			width: 100%;
		}

		.form-tip {
			display: block;
			margin-top: 4px;
			font-size: 12px;
			color: var(--el-text-color-secondary);
		}
	}

	.text-secondary {
		color: var(--el-text-color-secondary);
		font-size: 12px;
	}
}
</style>
