import request from '/@/utils/request';

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
	entryId: string;
	machineCode: string;
	remark?: string;
	tags?: string[];
	enabled: boolean;
	createdBy?: string;
	createTime?: string;
	updateTime?: string;
}

export interface ClientWhitelistUpsertInput {
	entryId?: string;
	machineCode: string;
	remark?: string;
	tags?: string[];
	enabled: boolean;
}

export interface ClientWhitelistPageInput {
	pageNo: number;
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
	ruleId: string;
	channelId: string;
	channelName: string;
	platform: ClientPlatform;
	bucketName: string;
	rootPath: string;
	resourceDomain?: string;
	generalVersionInfo?: ClientAppVersionInfoDto;
	whitelistVersionInfo?: ClientAppVersionInfoDto;
	whitelistTesterEntryIds: string[];
	whitelistMachineCodes: string[];
	latestPublishTime?: string;
	latestPublishUserName?: string;
	latestPublishResVersion?: string;
	createTime?: string;
	updateTime?: string;
}

export interface ClientVersionRuleSaveInput {
	ruleId?: string;
	channelId: string;
	channelName: string;
	platform: ClientPlatform;
	bucketName: string;
	rootPath?: string;
	resourceDomain?: string;
	generalVersionInfo?: ClientAppVersionInfoDto;
	whitelistVersionInfo?: ClientAppVersionInfoDto;
	whitelistTesterEntryIds?: string[];
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

export enum ClientPlatform {
	Android = 'android',
	Ios = 'ios',
	Mini = 'mini',
}

export interface PromoteWhitelistVersionInput {
	ruleId: string;
	targetResVersion?: string;
	confirm: boolean;
}

// ==================== API 接口 ====================

/**
 * 获取 OSS 配置
 */
export const getOssConfig = (provider = 'aliyun') =>
	request<ClientOssConfigOutput>({
		url: '/api/clientVersion/oss-config',
		method: 'get',
		params: { provider },
	});

/**
 * 保存 OSS 配置
 */
export const saveOssConfig = (data: ClientOssConfigInput) =>
	request({
		url: '/api/clientVersion/oss-config',
		method: 'post',
		data,
	});

/**
 * 获取白名单分页
 */
export const getWhitelistPage = (params: ClientWhitelistPageInput) =>
	request<{ items: ClientWhitelistOutput[]; total: number }>({
		url: '/api/clientVersion/whitelist/page',
		method: 'get',
		params,
	});

/**
 * 获取白名单列表（用于下拉选择）
 */
export const getWhitelistList = (params?: { entryIds?: string[]; onlyEnabled?: boolean }) =>
	request<ClientWhitelistOutput[]>({
		url: '/api/clientVersion/whitelist/list',
		method: 'get',
		params,
	});

/**
 * 保存白名单（新增/编辑）
 */
export const saveWhitelist = (data: ClientWhitelistUpsertInput) =>
	request<ClientWhitelistOutput>({
		url: '/api/clientVersion/whitelist/save',
		method: 'post',
		data,
	});

/**
 * 删除白名单
 */
export const deleteWhitelist = (entryId: string) =>
	request({
		url: '/api/clientVersion/whitelist/delete',
		method: 'post',
		data: { entryId },
	});

/**
 * 获取版本规则列表
 */
export const getRuleList = (params?: { channelId?: string; platform?: ClientPlatform }) =>
	request<ClientVersionRuleOutput[]>({
		url: '/api/clientVersion/rule/list',
		method: 'get',
		params,
	});

/**
 * 获取版本规则详情
 */
export const getRuleDetail = (ruleId: string) =>
	request<ClientVersionRuleOutput>({
		url: '/api/clientVersion/rule/detail',
		method: 'get',
		params: { ruleId },
	});

/**
 * 保存版本规则
 */
export const saveRule = (data: ClientVersionRuleSaveInput) =>
	request<ClientVersionRuleOutput>({
		url: '/api/clientVersion/rule/save',
		method: 'post',
		data,
	});

/**
 * 删除版本规则
 */
export const deleteRule = (ruleId: string) =>
	request({
		url: '/api/clientVersion/rule/delete',
		method: 'post',
		data: { ruleId },
	});

/**
 * 获取可选的 AppVersion 列表
 */
export const getAppVersions = (params: {
	channelId: string;
	platform: ClientPlatform;
	bucketName?: string;
	rootPath?: string;
}) =>
	request<ChannelAppVersionItem[]>({
		url: '/api/clientVersion/rule/appVersions',
		method: 'get',
		params,
	});

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
	request<ChannelResVersionItem[]>({
		url: '/api/clientVersion/rule/resVersions',
		method: 'get',
		params,
	});

/**
 * 白名单版本一键发布为正式版
 */
export const promoteWhitelist = (data: PromoteWhitelistVersionInput) =>
	request<ClientVersionRuleOutput>({
		url: '/api/clientVersion/rule/promote',
		method: 'post',
		data,
	});
