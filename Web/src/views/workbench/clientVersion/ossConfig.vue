<template>
	<div class="client-oss-config">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>对象存储参数设置</span>
					<el-tag type="success" v-if="form.provider">{{ currentProviderLabel }}</el-tag>
					<el-button type="primary" size="small" @click="loadConfig">
						<el-icon><ele-Refresh /></el-icon>
						刷新
					</el-button>
				</div>
			</template>
			<el-form ref="formRef" :model="form" :rules="rules" label-width="160px" class="oss-form">
				<el-form-item label="对象存储厂商" prop="provider">
					<el-select v-model="form.provider" placeholder="请选择厂商" @change="handleProviderChange">
						<el-option v-for="item in providerOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
					<el-text class="form-tip">当前支持阿里云 OSS、腾讯云 COS</el-text>
				</el-form-item>

				<el-form-item label="AccessKey ID" prop="accessKeyId">
					<el-input v-model="form.accessKeyId" placeholder="请输入 AccessKeyId" clearable />
				</el-form-item>
				<el-form-item label="AccessKey Secret" prop="accessKeySecret">
					<el-input v-model="form.accessKeySecret" type="password" show-password placeholder="为空则保持原值不变" clearable />
					<el-text v-if="maskedSecret" class="form-tip">当前值：{{ maskedSecret }}</el-text>
				</el-form-item>
				<el-form-item label="Endpoint" prop="endpoint">
					<el-input v-model="form.endpoint" placeholder="例如：oss-cn-hangzhou.aliyuncs.com 或 cos.ap-shanghai.myqcloud.com" clearable />
				</el-form-item>
				<el-form-item label="Bucket 名称" prop="bucketName">
					<el-input v-model="form.bucketName" placeholder="请输入 Bucket 名称" clearable />
				</el-form-item>
				<el-form-item label="设为当前使用">
					<el-switch v-model="form.isDefault" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="form.remark" type="textarea" :rows="3" placeholder="可选备注信息" />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" :loading="saving" @click="handleSave">
						<el-icon><ele-Check /></el-icon>
						保存配置
					</el-button>
					<el-button @click="loadConfig">
						<el-icon><ele-Refresh /></el-icon>
						重新加载
					</el-button>
				</el-form-item>
			</el-form>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="clientVersionOssConfig">
import { onMounted, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { getOssConfig, saveOssConfig, type ClientOssConfigInput } from '/@/api/clientVersion';

const providerOptions = [
	{ label: '阿里云 OSS', value: 'aliyun' },
	{ label: '腾讯云 COS', value: 'tencent-cos' },
];

const formRef = ref<FormInstance>();
const form = reactive<ClientOssConfigInput>({
	provider: '',
	accessKeyId: '',
	accessKeySecret: '',
	endpoint: '',
	bucketName: '',
	resourceDomain: '',
	region: '',
	isDefault: true,
	remark: '',
});
const saving = ref(false);
const maskedSecret = ref('');
const currentProviderLabel = ref('');

const rules: FormRules<ClientOssConfigInput> = {
	accessKeyId: [{ required: true, message: '请输入 AccessKeyId', trigger: 'blur' }],
	endpoint: [{ required: true, message: '请输入 Endpoint', trigger: 'blur' }],
	bucketName: [{ required: true, message: '请输入 Bucket 名称', trigger: 'blur' }],
};

const loadConfig = async () => {
	const provider = form.provider || undefined;
	try {
		const res: any = await getOssConfig(provider);
		const data = res.data?.result ?? res.data?.data ?? res.data ?? {};

		form.provider = data.provider || form.provider || 'aliyun';
		form.accessKeyId = data.accessKeyId || '';
		form.endpoint = data.endpoint || '';
		form.bucketName = data.bucketName || '';
		form.isDefault = data.isDefault !== undefined ? data.isDefault : true;
		form.remark = data.remark || '';

		maskedSecret.value = data.accessKeySecretMasked || '';
		form.accessKeySecret = '';

		const current = providerOptions.find((opt) => opt.value === form.provider);
		currentProviderLabel.value = current?.label || '';

		ElMessage.success('对象存储配置已加载');
	} catch (error: any) {
		const errorMsg = error?.response?.data?.message || error?.message || '对象存储配置加载失败';
		ElMessage.warning(errorMsg);
		form.accessKeyId = '';
		form.endpoint = '';
		form.bucketName = '';
		form.resourceDomain = '';
		form.region = '';
		form.isDefault = true;
		form.remark = '';
		currentProviderLabel.value = '';
		maskedSecret.value = '';
	}
};

const handleProviderChange = () => {
	loadConfig();
};

const handleSave = () => {
	formRef.value?.validate(async (valid) => {
		if (!valid) return;

		saving.value = true;
		try {
			const saveData = { ...form };
			if (!saveData.accessKeySecret) {
				delete saveData.accessKeySecret;
			}
			delete (saveData as any).rootPath;
			delete (saveData as any).resourceDomain;
			delete (saveData as any).region;

			await saveOssConfig(saveData);
			ElMessage.success('对象存储配置已保存');
			await loadConfig();
		} catch (error: any) {
			const errorMsg = error?.response?.data?.message || error?.message || '保存失败';
			ElMessage.error(errorMsg);
		} finally {
			saving.value = false;
		}
	});
};

onMounted(() => {
	loadConfig();
});
</script>

<style scoped lang="scss">
.client-oss-config {
	max-width: 900px;

	.card-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.oss-form {
		margin-top: 20px;

		.form-tip {
			display: block;
			margin-top: 4px;
			font-size: 12px;
			color: var(--el-text-color-secondary);
		}
	}
}
</style>
