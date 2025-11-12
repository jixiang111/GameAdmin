<template>
	<div class="client-channel-version">
		<el-card shadow="hover" :body-style="{ padding: 12 }">
			<el-form :inline="true" :model="query">
				<el-form-item label="渠道">
					<el-select v-model="query.channelId" placeholder="全部" clearable style="width: 200px">
						<el-option v-for="item in channelOptions" :key="item.id" :label="item.name" :value="item.id" />
					</el-select>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Search" @click="handleQuery">查询</el-button>
					<el-button icon="ele-Refresh" @click="resetQuery">重置</el-button>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Plus" @click="openDrawer()">新增渠道版本</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 10px">
			<el-table :data="tableData" border v-loading="loading">
				<el-table-column prop="channelName" label="渠道" width="160" />
				<el-table-column prop="platform" label="平台" width="120" />
				<el-table-column label="白名单版本">
					<template #default="scope">
						<div>App：{{ scope.row.whiteAppVersion || '-' }}</div>
						<div>资源：{{ scope.row.whiteResourceVersion || '-' }}</div>
					</template>
				</el-table-column>
				<el-table-column label="正式版本">
					<template #default="scope">
						<div>App：{{ scope.row.releaseAppVersion || '-' }}</div>
						<div>资源：{{ scope.row.releaseResourceVersion || '-' }}</div>
					</template>
				</el-table-column>
				<el-table-column prop="resourceBucket" label="Bucket" />
				<el-table-column prop="updatedTime" label="更新时间" width="180" align="center" />
				<el-table-column label="操作" width="260" align="center">
					<template #default="scope">
						<el-button text type="primary" icon="ele-Edit" @click="openDrawer(scope.row)">编辑</el-button>
						<el-popconfirm title="将白名单版本发布为正式版本？" @confirm="handlePublish(scope.row)">
							<template #reference>
								<el-button text type="warning" icon="ele-Promotion">发布</el-button>
							</template>
						</el-popconfirm>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<el-drawer v-model="drawer.visible" :title="drawer.title" size="600px" @closed="resetDrawer">
			<el-form ref="drawerFormRef" :model="drawer.form" :rules="drawerRules" label-width="120px" class="drawer-form">
				<el-form-item label="渠道" prop="channelId">
					<el-select v-model="drawer.form.channelId" placeholder="请选择渠道" :disabled="!!drawer.form.id">
						<el-option v-for="item in channelOptions" :key="item.id" :label="item.name" :value="item.id" />
					</el-select>
				</el-form-item>
				<el-form-item label="运行平台" prop="platform">
					<el-select v-model="drawer.form.platform" placeholder="请选择平台" :disabled="!!drawer.form.id" @change="fetchVersionOptions">
						<el-option v-for="item in platformOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="Bucket">
					<el-input v-model="drawer.form.resourceBucket" placeholder="可选，默认使用 OSS 配置的 Bucket" />
				</el-form-item>
				<el-divider>白名单版本</el-divider>
				<el-form-item label="App 版本" prop="whiteAppVersion">
					<el-select v-model="drawer.form.whiteAppVersion" placeholder="请选择白名单 App 版本" @change="syncWhiteResourceOptions">
						<el-option v-for="item in versionOptions" :key="item.appVersion" :label="item.appVersion" :value="item.appVersion" />
					</el-select>
				</el-form-item>
				<el-form-item label="资源版本" prop="whiteResourceVersion">
					<el-select v-model="drawer.form.whiteResourceVersion" placeholder="请选择资源版本">
						<el-option v-for="item in currentWhiteResourceOptions" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="白名单用户" prop="whitelistIds">
					<el-select v-model="drawer.form.whitelistIds" multiple filterable placeholder="选择测试用户">
						<el-option v-for="item in whitelistOptions" :key="item.id" :label="item.machineCode" :value="item.id!" />
					</el-select>
				</el-form-item>
				<el-divider>正式版本</el-divider>
				<el-form-item label="App 版本" prop="releaseAppVersion">
					<el-select v-model="drawer.form.releaseAppVersion" placeholder="请选择正式 App 版本" @change="syncReleaseResourceOptions">
						<el-option v-for="item in versionOptions" :key="`release-${item.appVersion}`" :label="item.appVersion" :value="item.appVersion" />
					</el-select>
				</el-form-item>
				<el-form-item label="资源版本" prop="releaseResourceVersion">
					<el-select v-model="drawer.form.releaseResourceVersion" placeholder="请选择资源版本">
						<el-option v-for="item in currentReleaseResourceOptions" :key="`release-${item}`" :label="item" :value="item" />
					</el-select>
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
import { ElMessage } from 'element-plus';
import {
	getChannelOptions,
	getChannelVersionPage,
	getVersionOptions,
	getWhitelistPage,
	publishChannelVersion,
	saveChannelVersion,
	type ChannelOption,
	type ChannelVersionItem,
	type VersionResourceOption,
	type WhitelistItem,
} from '/@/api/clientVersion';

const platformOptions = [
	{ label: 'Android', value: 'Android' },
	{ label: 'iOS', value: 'iOS' },
	{ label: 'WebGL', value: 'WebGL' },
];

const query = reactive({
	channelId: undefined as number | undefined,
});

const loading = ref(false);
const tableData = ref<ChannelVersionItem[]>([]);
const channelOptions = ref<ChannelOption[]>([]);
const whitelistOptions = ref<WhitelistItem[]>([]);
const versionOptions = ref<VersionResourceOption[]>([]);

const drawerFormRef = ref<FormInstance>();
const drawer = reactive({
	visible: false,
	title: '新增渠道版本',
	saving: false,
	form: {
		id: undefined as number | undefined,
		channelId: undefined as number | undefined,
		platform: undefined as 'Android' | 'iOS' | 'WebGL' | undefined,
		whiteAppVersion: '',
		whiteResourceVersion: '',
		releaseAppVersion: '',
		releaseResourceVersion: '',
		resourceBucket: '',
		whitelistIds: [] as number[],
	},
});

const drawerRules: FormRules<typeof drawer.form> = {
	channelId: [{ required: true, message: '请选择渠道', trigger: 'change' }],
	platform: [{ required: true, message: '请选择平台', trigger: 'change' }],
	whiteAppVersion: [{ required: true, message: '请选择白名单版本', trigger: 'change' }],
	whiteResourceVersion: [{ required: true, message: '请选择白名单资源版本', trigger: 'change' }],
	releaseAppVersion: [{ required: true, message: '请选择正式版本', trigger: 'change' }],
	releaseResourceVersion: [{ required: true, message: '请选择正式资源版本', trigger: 'change' }],
};

const currentWhiteResourceOptions = computed(() => {
	const version = versionOptions.value.find((item) => item.appVersion === drawer.form.whiteAppVersion);
	return version?.resources ?? [];
});

const currentReleaseResourceOptions = computed(() => {
	const version = versionOptions.value.find((item) => item.appVersion === drawer.form.releaseAppVersion);
	return version?.resources ?? [];
});

const handleQuery = async () => {
	loading.value = true;
	try {
		const res = await getChannelVersionPage({ channelId: query.channelId });
		const result = res.data?.result ?? res.data?.data ?? { items: [] };
		tableData.value = result.items ?? result.records ?? [];
	} finally {
		loading.value = false;
	}
};

const resetQuery = () => {
	query.channelId = undefined;
	handleQuery();
};

const openDrawer = (row?: ChannelVersionItem) => {
	if (row?.id) {
		drawer.title = '编辑渠道版本';
		Object.assign(drawer.form, {
			id: row.id,
			channelId: row.channelId,
			platform: row.platform,
			whiteAppVersion: row.whiteAppVersion,
			whiteResourceVersion: row.whiteResourceVersion,
			releaseAppVersion: row.releaseAppVersion,
			releaseResourceVersion: row.releaseResourceVersion,
			resourceBucket: row.resourceBucket,
			whitelistIds: row.whitelistIds ?? [],
		});
	} else {
		drawer.title = '新增渠道版本';
		resetDrawer();
	}
	drawer.visible = true;
	fetchVersionOptions();
};

const resetDrawer = () => {
	Object.assign(drawer.form, {
		id: undefined,
		channelId: undefined,
		platform: undefined,
		whiteAppVersion: '',
		whiteResourceVersion: '',
		releaseAppVersion: '',
		releaseResourceVersion: '',
		resourceBucket: '',
		whitelistIds: [],
	});
	versionOptions.value = [];
	drawerFormRef.value?.clearValidate();
};

const fetchChannels = async () => {
	const res = await getChannelOptions();
	channelOptions.value = res.data?.result ?? res.data?.data ?? [];
};

const fetchWhitelistOptions = async () => {
	const res = await getWhitelistPage({ page: 1, pageSize: 200 });
	const result = res.data?.result ?? res.data?.data ?? { items: [] };
	whitelistOptions.value = result.items ?? result.records ?? [];
};

const fetchVersionOptions = async () => {
	if (!drawer.form.channelId || !drawer.form.platform) {
		versionOptions.value = [];
		return;
	}
	const res = await getVersionOptions(drawer.form.channelId, drawer.form.platform);
	versionOptions.value = res.data?.result ?? res.data?.data ?? [];
	syncWhiteResourceOptions();
	syncReleaseResourceOptions();
};

const syncWhiteResourceOptions = () => {
	if (!currentWhiteResourceOptions.value.includes(drawer.form.whiteResourceVersion)) {
		drawer.form.whiteResourceVersion = currentWhiteResourceOptions.value[0] ?? '';
	}
};

const syncReleaseResourceOptions = () => {
	if (!currentReleaseResourceOptions.value.includes(drawer.form.releaseResourceVersion)) {
		drawer.form.releaseResourceVersion = currentReleaseResourceOptions.value[0] ?? '';
	}
};

const submitDrawer = () => {
	drawerFormRef.value?.validate(async (valid) => {
		if (!valid) return;
		drawer.saving = true;
		try {
			await saveChannelVersion(drawer.form);
			ElMessage.success('渠道版本已保存');
			drawer.visible = false;
			handleQuery();
		} finally {
			drawer.saving = false;
		}
	});
};

const handlePublish = async (row: ChannelVersionItem) => {
	if (!row.id) return;
	await publishChannelVersion({ id: row.id });
	ElMessage.success('白名单版本已发布为正式版本');
	handleQuery();
};

watch(
	() => drawer.visible,
	(visible) => {
		if (!visible) return;
		if (drawer.form.channelId && drawer.form.platform) {
			fetchVersionOptions();
		}
	}
);

onMounted(() => {
	handleQuery();
	fetchChannels();
	fetchWhitelistOptions();
});
</script>

<style scoped lang="scss">
.client-channel-version {
	.drawer-form {
		.el-select,
		.el-input {
			width: 100%;
		}
	}
}
</style>
