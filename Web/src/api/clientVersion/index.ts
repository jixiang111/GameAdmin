import request from '/@/utils/request';
import type { AxiosRequestConfig } from 'axios';

// ==================== 类型定义 ====================

export interface ClientOssConfigOutput {
	provider: string;
	accessKeyId: string;
	accessKeySecretMasked: string;
	endpoint: string;
	bucketName: string;
	rootPath: string;
	resourceDomain?: string;
	region?: string;
	isDefault: boolean;
	remark?: string;
}

export interface ClientOssConfigInput {
	provider?: string;
	accessKeyId: string;
	accessKeySecret?: string;
	endpoint: string;
	bucketName: string;
	rootPath?: string;
	resourceDomain?: string;
	region?: string;
	isDefault?: boolean;
	remark?: string;
}

export interface ClientWhitelistOutput {
	entryId: number | string;
	machineCode: string;
	remark?: string;
	tags?: string[];
	enabled: boolean;
	createdBy?: string;
	createTime?: string;
	updateTime?: string;
}

export interface ClientWhitelistUpsertInput {
	entryId?: number | string;
	machineCode: string;
	remark?: string;
	tags?: string[];
	enabled: boolean;
}

export interface ClientWhitelistPageInput {
	pageNo?: number;
	page?: number;
	pageSize: number;
	keyword?: string;
	tag?: string;
	onlyEnabled?: boolean;
}

export interface ClientAppVersionInfoDto {
	appVersion: string;
	resVersion: string;
	resVersionTimestamp?: number;
	resVersionFileSize?: number;
	updateUrl?: string;
	updateNotice?: string;
	loginUrl?: string;
	description?: string;
	forceUpdate: boolean;
}

export interface ClientVersionRuleOutput {
	ruleId: number | string;
	channelId: string;
	channelName: string;
	platform: ClientPlatform;
	bucketName: string;
	rootPath: string;
	resourceDomain?: string;
	generalVersionInfo?: ClientAppVersionInfoDto;
	whitelistVersionInfo?: ClientAppVersionInfoDto;
	whitelistTesterEntryIds: Array<number | string>;
	whitelistMachineCodes: string[];
	latestPublishTime?: string;
	latestPublishUserName?: string;
	latestPublishResVersion?: string;
	createTime?: string;
	updateTime?: string;
}

export interface ClientVersionRuleSaveInput {
	ruleId?: number | string;
	channelId: string;
	channelName: string;
	platform: ClientPlatform;
	bucketName: string;
	rootPath?: string;
	resourceDomain?: string;
	generalVersionInfo?: ClientAppVersionInfoDto;
	whitelistVersionInfo?: ClientAppVersionInfoDto;
	whitelistTesterEntryIds?: Array<number | string>;
}

export interface ChannelAppVersionItem {
	appVersion: string;
	folder: string;
	lastModified?: string;
}

export interface ChannelResVersionItem {
	fileName: string;
	timestamp?: number;
	fileSize?: number;
	lastModified?: string;
	isNewerThanOnline: boolean;
	downloadUrl: string;
}

// 与后端枚举保持一致（数字枚举：1=Android，2=iOS，3=MiniProgram）
export enum ClientPlatform {
	Android = 1,
	Ios = 2,
	Mini = 3,
}

export interface PromoteWhitelistVersionInput {
	ruleId: number | string;
	targetResVersion?: string;
	confirm: boolean;
}

// ==================== 辅助方法 ====================
// 部分接口在不同环境存在路径差异（有的使用中划线/多级路径，有的使用驼峰命名）。
// 404 时按顺序尝试后备路径，以兼容后台实际路由。
const requestWithFallback = async <T = any>(config: AxiosRequestConfig, fallbackUrls: string[] = []) => {
	try {
		return await request<T>(config);
	} catch (error: any) {
		if (error?.response?.status !== 404 || fallbackUrls.length === 0) throw error;

		for (const url of fallbackUrls) {
			try {
				return await request<T>({ ...config, url });
			} catch (err: any) {
				// 仅对 404 继续尝试下一个后备路径，其余错误直接抛出
				if (err?.response?.status !== 404) throw err;
			}
		}

		throw error;
	}
};

// ==================== API 接口 ====================

/**
 * 获取 OSS 配置
 */
export const getOssConfig = (provider = 'aliyun') =>
	requestWithFallback<ClientOssConfigOutput>(
		{
			url: '/api/clientVersion/ossConfig',
			method: 'get',
			params: { provider },
		},
		['/api/clientVersion/oss-config', '/api/clientVersion/getOssConfig']
	);

/**
 * 保存 OSS 配置
 */
export const saveOssConfig = (data: ClientOssConfigInput) =>
	requestWithFallback(
		{
			url: '/api/clientVersion/saveOssConfig',
			method: 'post',
			data,
		},
		['/api/clientVersion/oss-config', '/api/clientVersion/ossConfig']
	);

/**
 * 获取白名单分页
 */
export const getWhitelistPage = (params: ClientWhitelistPageInput) => {
	const query: any = { ...params };
	if (query.pageNo !== undefined) {
		query.page = query.pageNo;
		delete query.pageNo;
	}
	return requestWithFallback<{ items: ClientWhitelistOutput[]; total: number }>(
		{
			url: '/api/clientVersion/whitelistPage',
			method: 'get',
			params: query,
		},
		['/api/clientVersion/getWhitelistPage', '/api/clientVersion/whitelist/page']
	);
};

/**
 * 获取白名单列表（用于下拉选择）
 */
export const getWhitelistList = (params?: { entryIds?: Array<number | string>; onlyEnabled?: boolean }) =>
	requestWithFallback<ClientWhitelistOutput[]>(
		{
			url: '/api/clientVersion/whitelistList',
			method: 'get',
			params,
		},
		['/api/clientVersion/getWhitelistList', '/api/clientVersion/whitelist/list']
	);

/**
 * 保存白名单（新增/编辑）
 */
export const saveWhitelist = (data: ClientWhitelistUpsertInput) =>
	requestWithFallback<ClientWhitelistOutput>(
		{
			url: '/api/clientVersion/saveWhitelist',
			method: 'post',
			data,
		},
		['/api/clientVersion/whitelist/save']
	);

/**
 * 删除白名单
 */
export const deleteWhitelist = (entryId: number | string) =>
	requestWithFallback(
		{
			url: '/api/clientVersion/deleteWhitelist',
			method: 'post',
			data: { entryId },
		},
		['/api/clientVersion/whitelist/delete']
	);

/**
 * 获取版本规则列表
 */
export const getRuleList = (params?: { channelId?: string; platform?: ClientPlatform }) =>
	requestWithFallback<ClientVersionRuleOutput[]>(
		{
			url: '/api/clientVersion/ruleList',
			method: 'get',
			params,
		},
		['/api/clientVersion/getRuleList', '/api/clientVersion/rule/list']
	);

/**
 * 获取版本规则详情
 */
export const getRuleDetail = (ruleId: number | string) =>
	requestWithFallback<ClientVersionRuleOutput>(
		{
			url: '/api/clientVersion/ruleDetail',
			method: 'get',
			params: { ruleId },
		},
		['/api/clientVersion/getRuleDetail', '/api/clientVersion/rule/detail']
	);

/**
 * 保存版本规则
 */
export const saveRule = (data: ClientVersionRuleSaveInput) =>
	requestWithFallback<ClientVersionRuleOutput>(
		{
			url: '/api/clientVersion/saveRule',
			method: 'post',
			data,
		},
		['/api/clientVersion/rule/save']
	);

/**
 * 删除版本规则
 */
export const deleteRule = (ruleId: number | string) =>
	requestWithFallback(
		{
			url: '/api/clientVersion/deleteRule',
			method: 'post',
			data: { ruleId },
		},
		['/api/clientVersion/rule/delete']
	);

/**
 * 获取可选的 AppVersion 列表
 */
export const getAppVersions = (params: {
	channelId: string;
	platform: ClientPlatform;
	bucketName?: string;
	rootPath?: string;
}) =>
	requestWithFallback<ChannelAppVersionItem[]>(
		{
			url: '/api/clientVersion/appVersions',
			method: 'get',
			params,
		},
		['/api/clientVersion/getAppVersions', '/api/clientVersion/rule/appVersions']
	);

/**
 * 获取可选的资源版本列表
 */
export const getResVersions = (params: {
	channelId: string;
	platform: ClientPlatform;
	appVersion: string;
	bucketName?: string;
	rootPath?: string;
}) =>
	requestWithFallback<ChannelResVersionItem[]>(
		{
			url: '/api/clientVersion/resVersions',
			method: 'get',
			params,
		},
		['/api/clientVersion/getResVersions', '/api/clientVersion/rule/resVersions']
	);

/**
 * 白名单版本一键发布为正式版
 */
export const promoteWhitelist = (data: PromoteWhitelistVersionInput) =>
	requestWithFallback<ClientVersionRuleOutput>(
		{
			url: '/api/clientVersion/promoteWhitelist',
			method: 'post',
			data,
		},
		['/api/clientVersion/rule/promote']
	);
