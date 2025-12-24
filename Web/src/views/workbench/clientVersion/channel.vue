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
				<el-table-column label="对象存储" width="140" align="center">
					<template #default="scope">
						<el-tag type="info" size="small">{{ getProviderLabel(scope.row.provider) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="白名单测试版本" min-width="220">
					<template #default="scope">
						<div v-if="scope.row.whitelistVersionInfo">
							<div><strong>App:</strong> {{ scope.row.whitelistVersionInfo.appVersion }}</div>
							<div><strong>资源:</strong> {{ scope.row.whitelistVersionInfo.resVersion }}</div>
							<div class="text-secondary">
								资源创建时间：{{ formatVersionInfoTime(scope.row.whitelistVersionInfo) || '未知' }}
							</div>
							<div v-if="scope.row.whitelistMachineCodes && scope.row.whitelistMachineCodes.length">
								<el-tag size="small" type="info">{{ scope.row.whitelistMachineCodes.length }}个测试用户</el-tag>
							</div>
						</div>
						<span v-else class="text-secondary">未配置</span>
					</template>
				</el-table-column>
				<el-table-column label="正式版本" min-width="220">
					<template #default="scope">
						<div v-if="scope.row.generalVersionInfo">
							<div><strong>App:</strong> {{ scope.row.generalVersionInfo.appVersion }}</div>
							<div><strong>资源:</strong> {{ scope.row.generalVersionInfo.resVersion }}</div>
							<div class="text-secondary">
								资源创建时间：{{ formatVersionInfoTime(scope.row.generalVersionInfo) || '未知' }}
							</div>
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

		<el-drawer v-model="drawer.visible" :title="drawer.title" size="800px" @closed="resetDrawer">
			<el-form ref="drawerFormRef" :model="drawer.form" :rules="drawerRules" label-width="140px" class="drawer-form">
				<el-divider content-position="left">基础配置</el-divider>

				<el-form-item label="渠道ID" prop="channelId">
					<el-input
						v-model="drawer.form.channelId"
						placeholder="渠道唯一标识，如 official、taptap"
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
						<el-option label="小程序(WebGL)" :value="ClientPlatform.Mini" />
					</el-select>
					<el-text class="form-tip">选择后不可修改，每个渠道+平台仅允许存在一条规则</el-text>
				</el-form-item>

				<el-form-item label="对象存储参数" prop="ossConfigId">
					<el-select
						v-model="drawer.form.ossConfigId"
						placeholder="请选择对象存储配置"
						:loading="ossConfigLoading"
						:disabled="ossConfigOptions.length === 0"
						@change="onOssConfigChange"
					>
						<el-option
							v-for="item in ossConfigOptions"
							:key="item.id"
							:label="formatConfigLabel(item)"
							:value="item.id"
						/>
					</el-select>
					<el-text class="form-tip">选择后自动使用对应的 Bucket、域名、RootPath</el-text>
				</el-form-item>

				<el-form-item label="根路径 (RootPath)">
					<el-input v-model="drawer.form.rootPath" placeholder="默认 /" clearable />
				</el-form-item>

				<el-form-item label="自定义资源域名">
					<el-input v-model="drawer.form.resourceDomain" placeholder="可选，自定义 CDN 域名" clearable />
				</el-form-item>

				<el-divider content-position="left">白名单测试版本</el-divider>

				<el-form-item label="App 版本" prop="whitelistVersionInfo.appVersion">
					<el-select v-model="whitelistAppVersion" placeholder="请选择" :loading="appVersionsLoading" @change="onWhitelistAppVersionChange">
						<el-option v-for="item in appVersions" :key="item.appVersion" :label="item.appVersion" :value="item.appVersion" />
					</el-select>
					<el-button type="primary" link icon="ele-Refresh" @click="loadAppVersions">刷新</el-button>
				</el-form-item>

				<el-form-item label="资源版本" prop="whitelistVersionInfo.resVersion">
					<el-select
						v-model="whitelistResVersion"
						placeholder="请先选择 App 版本"
						:loading="whitelistResVersionsLoading"
						:disabled="!whitelistAppVersion"
					>
						<el-option
							v-for="item in whitelistResVersions"
							:key="item.fileName"
							class-name="res-option-item"
							:label="formatResVersionLabel(item)"
							:value="item.fileName"
						>
							<div class="res-option">
								<div class="res-option-header">
									<span class="res-option-name">{{ item.fileName }}</span>
									<el-tag v-if="item.isNewerThanOnline" type="success" size="small">新版本</el-tag>
								</div>
								<div class="option-meta">资源创建时间：{{ formatResVersionTime(item) || '未知' }}</div>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<el-form-item label="强制更新">
					<el-switch v-model="whitelistForceUpdate" />
				</el-form-item>

				<el-form-item label="更新地址">
					<el-input v-model="whitelistUpdateUrl" placeholder="可选，客户端下载地址" />
				</el-form-item>

				<el-form-item label="资源更新地址">
					<div class="res-url-editor">
						<div
							v-for="(url, index) in whitelistUpdateResUrls"
							:key="`whitelist-url-${index}`"
							class="res-url-row"
						>
							<el-input v-model="whitelistUpdateResUrls[index]" placeholder="https://cdn.example.com/res" />
							<el-button
								text
								type="danger"
								icon="ele-Delete"
								@click="removeUpdateResUrl('whitelist', index)"
							/>
						</div>
						<div class="res-url-actions">
							<el-button type="primary" link icon="ele-Plus" @click="addUpdateResUrl('whitelist')">
								新增地址
							</el-button>
							<el-text class="form-tip">按顺序尝试多个资源增量/热更地址，可选</el-text>
						</div>
					</div>
				</el-form-item>

				<el-form-item label="登录地址">
					<el-input v-model="whitelistLoginUrl" placeholder="可选，登录服务器地址" />
				</el-form-item>

				<el-form-item label="更新公告">
					<el-input v-model="whitelistUpdateNotice" type="textarea" :rows="2" placeholder="可选，更新内容说明" />
				</el-form-item>

				<el-form-item label="白名单测试用户" prop="whitelistTesterEntryIds">
					<el-select
						v-model="drawer.form.whitelistTesterEntryIds"
						multiple
						filterable
						placeholder="从白名单列表中选择测试用户"
						style="width: 100%"
					>
						<el-option
							v-for="item in whitelistOptions"
							:key="item.entryId"
							:label="`${item.machineCode} (${item.remark || '无备注'})`"
							:value="item.entryId"
						/>
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
						<el-option
							v-for="item in generalResVersions"
							:key="item.fileName"
							class-name="res-option-item"
							:label="formatResVersionLabel(item)"
							:value="item.fileName"
						>
							<div class="res-option">
								<div class="res-option-header">
									<span class="res-option-name">{{ item.fileName }}</span>
									<el-tag v-if="item.isNewerThanOnline" type="success" size="small">新版本</el-tag>
								</div>
								<div class="option-meta">资源创建时间：{{ formatResVersionTime(item) || '未知' }}</div>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<el-form-item label="强制更新">
					<el-switch v-model="generalForceUpdate" />
				</el-form-item>

				<el-form-item label="更新地址">
					<el-input v-model="generalUpdateUrl" placeholder="可选，客户端下载地址" />
				</el-form-item>

				<el-form-item label="资源更新地址">
					<div class="res-url-editor">
						<div v-for="(url, index) in generalUpdateResUrls" :key="`general-url-${index}`" class="res-url-row">
							<el-input v-model="generalUpdateResUrls[index]" placeholder="https://cdn.example.com/res" />
							<el-button
								text
								type="danger"
								icon="ele-Delete"
								@click="removeUpdateResUrl('general', index)"
							/>
						</div>
						<div class="res-url-actions">
							<el-button type="primary" link icon="ele-Plus" @click="addUpdateResUrl('general')">
								新增地址
							</el-button>
							<el-text class="form-tip">客户端会依次尝试多个资源更新地址，可选</el-text>
						</div>
					</div>
				</el-form-item>

				<el-form-item label="登录地址">
					<el-input v-model="generalLoginUrl" placeholder="可选，登录服务器地址" />
				</el-form-item>

				<el-form-item label="更新公告">
					<el-input v-model="generalUpdateNotice" type="textarea" :rows="2" placeholder="可选，更新内容说明" />
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
import { computed, onMounted, reactive, ref } from 'vue';
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
	getOssConfigList,
	type ClientVersionRuleOutput,
	type ClientVersionRuleSaveInput,
	type ChannelAppVersionItem,
	type ChannelResVersionItem,
	type ClientWhitelistOutput,
	type ClientAppVersionInfoDto,
	type ClientOssConfigSummaryOutput,
	ClientPlatform,
} from '/@/api/clientVersion';

interface DrawerForm extends ClientVersionRuleSaveInput {
	whitelistTesterEntryIds: Array<number | string>;
}

const providerNameMap: Record<string, string> = {
	aliyun: '阿里云 OSS',
	'tencent-cos': '腾讯云 COS',
};

const ossConfigOptions = ref<ClientOssConfigSummaryOutput[]>([]);
const ossConfigLoading = ref(false);

const query = reactive({
	channelId: '',
	platform: undefined as ClientPlatform | undefined,
});

const loading = ref(false);
const tableData = ref<ClientVersionRuleOutput[]>([]);
const defaultOssConfigId = computed<ClientOssConfigSummaryOutput['id'] | undefined>(() => {
	if (ossConfigOptions.value.length === 0) return undefined;
	return ossConfigOptions.value.find((item) => item.isDefault)?.id ?? ossConfigOptions.value[0].id;
});

const formatConfigLabel = (item: ClientOssConfigSummaryOutput) => {
	const name = providerNameMap[item.provider] || item.provider;
	const bucket = item.bucketName ? ` / ${item.bucketName}` : '';
	const remark = item.remark ? `（${item.remark}）` : '';
	const suffix = item.isDefault ? '（当前使用）' : '';
	return `${name}${bucket}${remark}${suffix}`;
};

const handleQuery = async () => {
	loading.value = true;
	try {
		const res: any = await getRuleList(query);
		tableData.value = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
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

const getProviderLabel = (value: string): string => {
	return providerNameMap[value] || value;
};

const drawerFormRef = ref<FormInstance>();
const drawer = reactive({
	visible: false,
	title: '新增版本规则',
	saving: false,
	form: {
		ruleId: undefined,
		channelId: '',
		channelName: '',
		platform: undefined,
		provider: '',
		ossConfigId: undefined,
		bucketName: '',
		rootPath: '/',
		resourceDomain: '',
		whitelistTesterEntryIds: [],
	} as DrawerForm,
});

const isEditing = computed(() => !!drawer.form.ruleId);

const drawerRules: FormRules<ClientVersionRuleSaveInput> = {
	channelId: [{ required: true, message: '请输入渠道ID', trigger: 'blur' }],
	channelName: [{ required: true, message: '请输入渠道名称', trigger: 'blur' }],
	platform: [{ required: true, message: '请选择平台', trigger: 'change' }],
	ossConfigId: [{ required: true, message: '请选择对象存储配置', trigger: 'change' }],
};

const applyConfigDefaults = (configId: ClientOssConfigSummaryOutput['id'], force = false) => {
	if (configId === undefined || configId === null) return;
	const config = ossConfigOptions.value.find((item) => item.id === configId);
	if (!config) return;
	drawer.form.provider = config.provider;
	if (!drawer.form.rootPath || force) drawer.form.rootPath = config.rootPath || '/';
	if (!drawer.form.resourceDomain || force) drawer.form.resourceDomain = config.resourceDomain || '';
};

const ensureOssConfigSelection = (force = false) => {
	if (ossConfigOptions.value.length === 0) {
		drawer.form.ossConfigId = undefined;
		drawer.form.provider = '';
		return;
	}
	if (!drawer.form.ossConfigId || force) {
		const next = defaultOssConfigId.value;
		if (next !== undefined && next !== null) {
			drawer.form.ossConfigId = next;
			applyConfigDefaults(next, true);
		}
	}
};

const loadOssConfigs = async () => {
	ossConfigLoading.value = true;
	try {
		const res: any = await getOssConfigList();
		ossConfigOptions.value = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
		ensureOssConfigSelection();
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载对象存储配置失败');
	} finally {
		ossConfigLoading.value = false;
	}
};

const onOssConfigChange = () => {
	if (drawer.form.ossConfigId === undefined || drawer.form.ossConfigId === null) return;
	applyConfigDefaults(drawer.form.ossConfigId, !isEditing.value);
};

const whitelistAppVersion = ref('');
const whitelistResVersion = ref('');
const whitelistForceUpdate = ref(false);
const whitelistUpdateUrl = ref('');
const whitelistUpdateResUrls = ref<string[]>([]);
const whitelistLoginUrl = ref('');
const whitelistUpdateNotice = ref('');

const generalAppVersion = ref('');
const generalResVersion = ref('');
const generalForceUpdate = ref(false);
const generalUpdateUrl = ref('');
const generalUpdateResUrls = ref<string[]>([]);
const generalLoginUrl = ref('');
const generalUpdateNotice = ref('');

const appVersions = ref<ChannelAppVersionItem[]>([]);
const appVersionsLoading = ref(false);
const whitelistResVersions = ref<ChannelResVersionItem[]>([]);
const whitelistResVersionsLoading = ref(false);
const generalResVersions = ref<ChannelResVersionItem[]>([]);
const generalResVersionsLoading = ref(false);
const whitelistOptions = ref<ClientWhitelistOutput[]>([]);

const syncChannelName = () => {
	if (!drawer.form.channelName && drawer.form.channelId) {
		drawer.form.channelName = drawer.form.channelId;
	}
};

const hasDuplicateChannelId = async () => {
	const channelId = drawer.form.channelId?.trim();
	if (!channelId || !drawer.form.platform) return false;
	const res: any = await getRuleList({ channelId, platform: drawer.form.platform });
	const list = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
	const currentRuleId = drawer.form.ruleId;
	return list.some((item: ClientVersionRuleOutput) => {
		if (currentRuleId === undefined || currentRuleId === null) return true;
		return String(item.ruleId) !== String(currentRuleId);
	});
};

const onPlatformChange = () => {
	whitelistAppVersion.value = '';
	whitelistResVersion.value = '';
	generalAppVersion.value = '';
	generalResVersion.value = '';
	appVersions.value = [];
	whitelistResVersions.value = [];
	generalResVersions.value = [];

	if (drawer.form.channelId && drawer.form.platform) {
		loadAppVersions();
	}
};

const loadAppVersions = async () => {
	if (!drawer.form.channelId || !drawer.form.platform) {
		ElMessage.warning('请先填写渠道ID并选择平台');
		return;
	}

	if (!drawer.form.ossConfigId) {
		ElMessage.warning('请先选择对象存储配置');
		return;
	}

	appVersionsLoading.value = true;
	try {
		const res: any = await getAppVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform,
			provider: drawer.form.provider,
			ossConfigId: drawer.form.ossConfigId,
			rootPath: drawer.form.rootPath || undefined,
		});
		appVersions.value = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];

		if (appVersions.value.length === 0) {
			ElMessage.warning('未找到任何 AppVersion，请确认对象存储目录中存在版本文件夹');
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载 AppVersion 失败');
	} finally {
		appVersionsLoading.value = false;
	}
};

const onWhitelistAppVersionChange = async (arg?: boolean | { preserveSelection?: boolean }) => {
	const preserveSelection =
		typeof arg === 'boolean' ? arg : typeof arg === 'object' && arg ? !!arg.preserveSelection : false;
	const previous = whitelistResVersion.value;
	if (!preserveSelection) {
		whitelistResVersion.value = '';
	}
	whitelistResVersions.value = [];

	if (!whitelistAppVersion.value) return;

	whitelistResVersionsLoading.value = true;
	try {
		if (!drawer.form.ossConfigId) {
			ElMessage.warning('请先选择对象存储配置');
			return;
		}

		const res: any = await getResVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform!,
			appVersion: whitelistAppVersion.value,
			provider: drawer.form.provider,
			ossConfigId: drawer.form.ossConfigId,
			rootPath: drawer.form.rootPath || undefined,
		});
		const list = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
		whitelistResVersions.value = list;

		if (preserveSelection && previous) {
			const exists = list.some((item: ChannelResVersionItem) => item.fileName === previous);
			if (!exists) {
				whitelistResVersions.value = [
					{
						fileName: previous,
						timestamp: undefined,
						fileSize: undefined,
						lastModified: undefined,
						isNewerThanOnline: false,
						downloadUrl: '',
					},
					...whitelistResVersions.value,
				];
			}
			whitelistResVersion.value = previous;
		} else if (!whitelistResVersion.value && whitelistResVersions.value.length > 0) {
			whitelistResVersion.value = whitelistResVersions.value[0].fileName;
		}
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载资源版本失败');
	} finally {
		whitelistResVersionsLoading.value = false;
	}
};

const onGeneralAppVersionChange = async (arg?: boolean | { preserveSelection?: boolean }) => {
	const preserveSelection =
		typeof arg === 'boolean' ? arg : typeof arg === 'object' && arg ? !!arg.preserveSelection : false;
	const previous = generalResVersion.value;
	if (!preserveSelection) {
		generalResVersion.value = '';
	}
	generalResVersions.value = [];

	if (!generalAppVersion.value) return;

	generalResVersionsLoading.value = true;
	try {
		if (!drawer.form.ossConfigId) {
			ElMessage.warning('请先选择对象存储配置');
			return;
		}

		const res: any = await getResVersions({
			channelId: drawer.form.channelId,
			platform: drawer.form.platform!,
			appVersion: generalAppVersion.value,
			provider: drawer.form.provider,
			ossConfigId: drawer.form.ossConfigId,
			rootPath: drawer.form.rootPath || undefined,
		});
		const list = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
		generalResVersions.value = list;

		if (preserveSelection && previous) {
			const exists = list.some((item: ChannelResVersionItem) => item.fileName === previous);
			if (!exists) {
				generalResVersions.value = [
					{
						fileName: previous,
						timestamp: undefined,
						fileSize: undefined,
						lastModified: undefined,
						isNewerThanOnline: false,
						downloadUrl: '',
					},
					...generalResVersions.value,
				];
			}
			generalResVersion.value = previous;
		} else if (!generalResVersion.value && generalResVersions.value.length > 0) {
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
		whitelistOptions.value = res?.data?.result ?? res?.data?.data ?? res?.data ?? [];
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '加载白名单失败');
	}
};

const getUpdateResUrlList = (type: 'general' | 'whitelist') =>
	type === 'general' ? generalUpdateResUrls.value : whitelistUpdateResUrls.value;

const addUpdateResUrl = (type: 'general' | 'whitelist') => {
	getUpdateResUrlList(type).push('');
};

const removeUpdateResUrl = (type: 'general' | 'whitelist', index: number) => {
	const list = getUpdateResUrlList(type);
	list.splice(index, 1);
};

const sanitizeUpdateResUrls = (urls: string[]): string[] | undefined => {
	const normalized = urls
		.map((item) => (item || '').trim())
		.filter((item) => !!item);

	if (normalized.length === 0) return undefined;

	const seen = new Set<string>();
	const result: string[] = [];
	normalized.forEach((item) => {
		const key = item.toLowerCase();
		if (!seen.has(key)) {
			seen.add(key);
			result.push(item);
		}
	});

	return result.length > 0 ? result : undefined;
};

const formatResVersionLabel = (item: ChannelResVersionItem): string => {
	const size = item.fileSize ? `${(item.fileSize / 1024).toFixed(2)} KB` : '';
	const time = formatResVersionTime(item) || '未知';
	const result: string[] = [item.fileName];
	if (size) result.push(`[${size}]`);
	result.push(`资源创建时间：${time}`);
	return result.join(' ');
};

const formatResVersionTime = (item: ChannelResVersionItem): string => {
	if (item.lastModified) {
		const date = new Date(item.lastModified);
		if (!isNaN(date.getTime())) return formatDateTime(date);
	}

	if (item.timestamp) {
		const ts = item.timestamp;
		const date = new Date(ts > 1e12 ? ts : ts * 1000);
		if (!isNaN(date.getTime())) return formatDateTime(date);
	}

	const fromName = extractTimestamp(item.fileName);
	if (fromName) {
		const date = new Date(fromName.length === 14 ? parseYyyyMMddHHmmss(fromName) : Number(fromName));
		if (!isNaN(date.getTime())) return formatDateTime(date);
	}

	return '';
};

const extractTimestamp = (fileName: string): string | null => {
	const name = fileName.split('/').pop() || fileName;

	const specific = /versions_(\d{8,})/i.exec(name);
	if (specific) return specific[1];

	const underscore = /_(\d{10,14})(?=\.[^.]+$)/.exec(name);
	if (underscore) return underscore[1];

	const trailingDigits = /(\d{10,14})(?=\.[^.]+$)/.exec(name);
	if (trailingDigits) return trailingDigits[1];

	return null;
};

const formatVersionInfoTime = (info?: ClientAppVersionInfoDto): string => {
	if (!info) return '';

	if (info.resVersionTimestamp) {
		const ts = info.resVersionTimestamp;
		const date = new Date(ts > 1e12 ? ts : ts * 1000);
		if (!isNaN(date.getTime())) return formatDateTime(date);
	}

	if (info.resVersion) {
		const fromName = extractTimestamp(info.resVersion);
		if (fromName) {
			const date = new Date(fromName.length === 14 ? parseYyyyMMddHHmmss(fromName) : Number(fromName));
			if (!isNaN(date.getTime())) return formatDateTime(date);
		}
	}

	return '';
};

const parseYyyyMMddHHmmss = (value: string): number => {
	const y = Number(value.slice(0, 4));
	const m = Number(value.slice(4, 6)) - 1;
	const d = Number(value.slice(6, 8));
	const hh = Number(value.slice(8, 10));
	const mm = Number(value.slice(10, 12));
	const ss = Number(value.slice(12, 14));
	return new Date(y, m, d, hh, mm, ss).getTime();
};

const formatDateTime = (date: Date): string => {
	const pad = (num: number) => num.toString().padStart(2, '0');
	const month = date.getMonth() + 1;
	const day = date.getDate();
	return `${date.getFullYear()}年${month}月${day}日 ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(
		date.getSeconds()
	)}`;
};

const openDrawer = async (row?: ClientVersionRuleOutput) => {
	if (!ossConfigOptions.value.length) {
		await loadOssConfigs();
		if (!ossConfigOptions.value.length) {
			ElMessage.warning('请先配置对象存储参数');
			return;
		}
	}

	resetDrawer();

	if (row?.ruleId) {
		drawer.title = '编辑版本规则';
		drawer.form.ruleId = row.ruleId;
		drawer.form.channelId = row.channelId;
		drawer.form.channelName = row.channelName;
		drawer.form.platform = row.platform;
		drawer.form.provider = row.provider || '';
		drawer.form.ossConfigId = row.ossConfigId ?? drawer.form.ossConfigId;
		if (!drawer.form.ossConfigId && row.provider) {
			const matched = ossConfigOptions.value.find((item) => item.provider === row.provider);
			if (matched) {
				drawer.form.ossConfigId = matched.id;
			}
		}
		if (drawer.form.ossConfigId !== undefined && drawer.form.ossConfigId !== null) {
			applyConfigDefaults(drawer.form.ossConfigId, false);
		}
		drawer.form.bucketName = row.bucketName;
		drawer.form.rootPath = row.rootPath;
		drawer.form.resourceDomain = row.resourceDomain || '';
		drawer.form.whitelistTesterEntryIds = row.whitelistTesterEntryIds || [];

		if (row.whitelistVersionInfo) {
			whitelistAppVersion.value = row.whitelistVersionInfo.appVersion;
			whitelistResVersion.value = row.whitelistVersionInfo.resVersion;
			whitelistForceUpdate.value = row.whitelistVersionInfo.forceUpdate;
			whitelistUpdateUrl.value = row.whitelistVersionInfo.updateUrl || '';
			whitelistUpdateResUrls.value = row.whitelistVersionInfo.updateResUrls
				? [...row.whitelistVersionInfo.updateResUrls]
				: [];
			whitelistLoginUrl.value = row.whitelistVersionInfo.loginUrl || '';
			whitelistUpdateNotice.value = row.whitelistVersionInfo.updateNotice || '';
		}

		if (row.generalVersionInfo) {
			generalAppVersion.value = row.generalVersionInfo.appVersion;
			generalResVersion.value = row.generalVersionInfo.resVersion;
			generalForceUpdate.value = row.generalVersionInfo.forceUpdate;
			generalUpdateUrl.value = row.generalVersionInfo.updateUrl || '';
			generalUpdateResUrls.value = row.generalVersionInfo.updateResUrls
				? [...row.generalVersionInfo.updateResUrls]
				: [];
			generalLoginUrl.value = row.generalVersionInfo.loginUrl || '';
			generalUpdateNotice.value = row.generalVersionInfo.updateNotice || '';
		}
	} else {
		drawer.title = '新增版本规则';
		if (drawer.form.ossConfigId !== undefined && drawer.form.ossConfigId !== null) {
			applyConfigDefaults(drawer.form.ossConfigId, true);
		}
	}

	drawer.visible = true;

	if (drawer.form.channelId && drawer.form.platform) {
		await loadAppVersions();
	}
	await loadWhitelistOptions();

	if (row?.ruleId) {
		if (whitelistAppVersion.value) await onWhitelistAppVersionChange(true);
		if (generalAppVersion.value) await onGeneralAppVersionChange(true);
	}
};

const resetDrawer = () => {
	drawer.form = {
		ruleId: undefined,
		channelId: '',
		channelName: '',
		platform: undefined,
		provider: '',
		ossConfigId: undefined,
		bucketName: '',
		rootPath: '/',
		resourceDomain: '',
		whitelistTesterEntryIds: [],
	} as DrawerForm;

	whitelistAppVersion.value = '';
	whitelistResVersion.value = '';
	whitelistForceUpdate.value = false;
	whitelistUpdateUrl.value = '';
	whitelistUpdateResUrls.value = [];
	whitelistLoginUrl.value = '';
	whitelistUpdateNotice.value = '';

	generalAppVersion.value = '';
	generalResVersion.value = '';
	generalForceUpdate.value = false;
	generalUpdateUrl.value = '';
	generalUpdateResUrls.value = [];
	generalLoginUrl.value = '';
	generalUpdateNotice.value = '';

	appVersions.value = [];
	whitelistResVersions.value = [];
	generalResVersions.value = [];

	drawerFormRef.value?.clearValidate();
	ensureOssConfigSelection(true);
};

const submitDrawer = () => {
	drawerFormRef.value?.validate(async (valid) => {
		if (!valid) return;

		if (drawer.form.channelId) {
			drawer.form.channelId = drawer.form.channelId.trim();
		}

		try {
			if (await hasDuplicateChannelId()) {
				ElMessage.error('渠道ID已存在，无法保存');
				return;
			}
		} catch (error: any) {
			ElMessage.error(error?.response?.data?.message || error?.message || '渠道ID校验失败');
			return;
		}

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
			const urls = sanitizeUpdateResUrls(whitelistUpdateResUrls.value);
			if (urls) {
				whitelistVersionInfo.updateResUrls = urls;
			}
		}

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
			const urls = sanitizeUpdateResUrls(generalUpdateResUrls.value);
			if (urls) {
				generalVersionInfo.updateResUrls = urls;
			}
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
			`确定将白名单版本发布为正式版本吗？
			<br/><br/>
			<strong>白名单版本：</strong><br/>
			App：${row.whitelistVersionInfo.appVersion}<br/>
			资源：${row.whitelistVersionInfo.resVersion}<br/><br/>
			<strong>当前正式版本：</strong><br/>
			${
				row.generalVersionInfo
					? `App：${row.generalVersionInfo.appVersion}<br/>资源：${row.generalVersionInfo.resVersion}`
					: '未配置'
			}`,
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
	loadOssConfigs();
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

	.res-option {
		display: flex;
		flex-direction: column;
		gap: 4px;
	}

	.res-option-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		width: 100%;
	}

	.res-option-name {
		flex: 1;
		margin-right: 8px;
	}

	.option-meta {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	:deep(.res-option-item) {
		height: auto;
		line-height: 1.4;
		padding-top: 6px;
		padding-bottom: 6px;
		align-items: flex-start;
	}

	.res-url-editor {
		display: flex;
		flex-direction: column;
		gap: 6px;
		width: 100%;
	}

	.res-url-row {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	.res-url-row :deep(.el-input) {
		flex: 1;
	}

	.res-url-actions {
		display: flex;
		align-items: center;
		gap: 12px;
	}
}
</style>
