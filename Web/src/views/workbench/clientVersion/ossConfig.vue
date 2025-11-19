<template>
	<div class="client-oss-config">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>阿里云 OSS 配置</span>
					<el-button type="primary" size="small" @click="loadConfig">
						<el-icon><ele-Refresh /></el-icon>
						刷新
					</el-button>
				</div>
			</template>
			<el-form ref="formRef" :model="form" :rules="rules" label-width="160px" class="oss-form">
				<el-form-item label="Provider">
					<el-input v-model="form.provider" disabled placeholder="aliyun" />
					<el-text class="form-tip">当前仅支持阿里云OSS，后续可扩展其他厂商</el-text>
				</el-form-item>
				<el-form-item label="AccessKey ID" prop="accessKeyId">
					<el-input v-model="form.accessKeyId" placeholder="请输入 AccessKeyId" clearable />
				</el-form-item>
				<el-form-item label="AccessKey Secret" prop="accessKeySecret">
					<el-input v-model="form.accessKeySecret" type="password" show-password placeholder="为空则保持原值不变" clearable />
					<el-text v-if="maskedSecret" class="form-tip">当前值：{{ maskedSecret }}</el-text>
				</el-form-item>
				<el-form-item label="Endpoint" prop="endpoint">
					<el-input v-model="form.endpoint" placeholder="例如：oss-cn-hangzhou.aliyuncs.com" clearable />
				</el-form-item>
				<el-form-item label="Bucket 名称" prop="bucketName">
					<el-input v-model="form.bucketName" placeholder="请输入 Bucket 名称" clearable />
				</el-form-item>
				<el-form-item label="根路径 (RootPath)">
					<el-input v-model="form.rootPath" placeholder="默认 /，可选填如 /client-assets" clearable />
					<el-text class="form-tip">用于OSS目录前缀，格式如 /client-assets</el-text>
				</el-form-item>
				<el-form-item label="自定义资源域名">
					<el-input v-model="form.resourceDomain" placeholder="可选，自定义CDN域名" clearable />
					<el-text class="form-tip">如已配置CDN，可填写自定义域名，如 https://cdn.example.com</el-text>
				</el-form-item>
				<el-form-item label="Region">
					<el-input v-model="form.region" placeholder="可选，如 cn-hangzhou" clearable />
				</el-form-item>
				<el-form-item label="设为默认配置">
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

const formRef = ref<FormInstance>();
const form = reactive<ClientOssConfigInput>({
	provider: 'aliyun',
	accessKeyId: '',
	accessKeySecret: '',
	endpoint: '',
	bucketName: '',
	rootPath: '/',
	resourceDomain: '',
	region: '',
	isDefault: true,
	remark: '',
});
const saving = ref(false);
const maskedSecret = ref('');

const rules: FormRules<ClientOssConfigInput> = {
	accessKeyId: [{ required: true, message: '请输入 AccessKeyId', trigger: 'blur' }],
	endpoint: [{ required: true, message: '请输入 Endpoint', trigger: 'blur' }],
	bucketName: [{ required: true, message: '请输入 Bucket 名称', trigger: 'blur' }],
};

const loadConfig = async () => {
	try {
		const res: any = await getOssConfig('aliyun');
		const data = res.data?.result ?? res.data?.data ?? res.data ?? {};

		form.provider = data.provider || 'aliyun';
		form.accessKeyId = data.accessKeyId || '';
		form.endpoint = data.endpoint || '';
		form.bucketName = data.bucketName || '';
		form.rootPath = data.rootPath || '/';
		form.resourceDomain = data.resourceDomain || '';
		form.region = data.region || '';
		form.isDefault = data.isDefault !== undefined ? data.isDefault : true;
		form.remark = data.remark || '';

		// 显示掩码后的密钥
		maskedSecret.value = data.accessKeySecretMasked || '';
		form.accessKeySecret = ''; // 清空输入框，避免显示掩码值

		ElMessage.success('OSS 配置已加载');
	} catch (error: any) {
		const errorMsg = error?.response?.data?.message || error?.message || 'OSS配置加载失败';
		ElMessage.warning(errorMsg);
		// 重置表单
		form.accessKeyId = '';
		form.endpoint = '';
		form.bucketName = '';
		form.rootPath = '/';
		form.resourceDomain = '';
		form.region = '';
		form.isDefault = true;
		form.remark = '';
		maskedSecret.value = '';
	}
};

const handleSave = () => {
	formRef.value?.validate(async (valid) => {
		if (!valid) return;

		saving.value = true;
		try {
			const saveData = { ...form };
			// 如果 AccessKeySecret 为空，后端会保持原值不变
			if (!saveData.accessKeySecret) {
				delete saveData.accessKeySecret;
			}

			await saveOssConfig(saveData);
			ElMessage.success('OSS 配置已保存');

			// 重新加载配置
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
