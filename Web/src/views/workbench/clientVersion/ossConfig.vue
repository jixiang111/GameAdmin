<template>
	<div class="client-oss-config">
		<el-card shadow="hover">
			<template #header>
				<span>OSS 参数设置</span>
			</template>
			<el-form ref="formRef" :model="form" :rules="rules" label-width="140px" class="oss-form">
				<el-form-item label="AccessKey ID" prop="accessKeyId">
					<el-input v-model="form.accessKeyId" placeholder="请输入 AccessKeyId" />
				</el-form-item>
				<el-form-item label="AccessKey Secret" prop="accessKeySecret">
					<el-input v-model="form.accessKeySecret" type="password" show-password placeholder="请输入 AccessKeySecret" />
				</el-form-item>
				<el-form-item label="Endpoint" prop="endpoint">
					<el-input v-model="form.endpoint" placeholder="例如：oss-cn-hangzhou.aliyuncs.com" />
				</el-form-item>
				<el-form-item label="Bucket" prop="bucket">
					<el-input v-model="form.bucket" placeholder="请输入 Bucket 名称" />
				</el-form-item>
				<el-form-item label="基础路径">
					<el-input v-model="form.basePath" placeholder="选填，如 client-assets" />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" :loading="saving" @click="handleSave">保存配置</el-button>
					<el-button @click="loadConfig">刷新</el-button>
				</el-form-item>
			</el-form>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="clientVersionOssConfig">
import { onMounted, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { getOssConfig, saveOssConfig, type OssConfig } from '/@/api/clientVersion';

const formRef = ref<FormInstance>();
const form = reactive<OssConfig>({
	accessKeyId: '',
	accessKeySecret: '',
	endpoint: '',
	bucket: '',
	basePath: '',
});
const saving = ref(false);

const rules: FormRules<OssConfig> = {
	accessKeyId: [{ required: true, message: '请输入 AccessKeyId', trigger: 'blur' }],
	accessKeySecret: [{ required: true, message: '请输入 AccessKeySecret', trigger: 'blur' }],
	endpoint: [{ required: true, message: '请输入 Endpoint', trigger: 'blur' }],
	bucket: [{ required: true, message: '请输入 Bucket', trigger: 'blur' }],
};

const assignForm = (data?: Partial<OssConfig>) => {
	form.accessKeyId = data?.accessKeyId ?? '';
	form.accessKeySecret = data?.accessKeySecret ?? '';
	form.endpoint = data?.endpoint ?? '';
	form.bucket = data?.bucket ?? '';
	form.basePath = data?.basePath ?? '';
};

const loadConfig = async () => {
	try {
		const res = await getOssConfig();
		assignForm(res.data?.result ?? res.data?.data ?? {});
	} catch (error) {
		assignForm();
	}
};

const handleSave = () => {
	formRef.value?.validate(async (valid) => {
		if (!valid) return;
		saving.value = true;
		try {
			await saveOssConfig(form);
			ElMessage.success('OSS 参数已保存');
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
	max-width: 720px;
	.oss-form {
		margin-top: 20px;
	}
}
</style>
