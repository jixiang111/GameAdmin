import request from '/@/utils/request';

export enum ClientVersionApi {
	OssConfig = '/api/client/version/oss-config',
	WhitelistPage = '/api/client/version/whitelist/page',
	WhitelistAdd = '/api/client/version/whitelist/add',
	WhitelistUpdate = '/api/client/version/whitelist/update',
	WhitelistDelete = '/api/client/version/whitelist/delete',
	ChannelList = '/api/client/version/channel/page',
	ChannelSave = '/api/client/version/channel/save',
	ChannelPublish = '/api/client/version/channel/publish',
	VersionOptions = '/api/client/version/options',
	ChannelOptions = '/api/client/version/channel/options',
}

export interface OssConfig {
	accessKeyId: string;
	accessKeySecret: string;
	endpoint: string;
	bucket: string;
	basePath?: string;
}

export interface WhitelistItem {
	id?: number;
	machineCode: string;
	remark?: string;
	updatedTime?: string;
}

export interface ChannelOption {
	id: number;
	name: string;
	code: string;
}

export interface VersionResourceOption {
	appVersion: string;
	resources: string[];
}

export interface ChannelVersionItem {
	id?: number;
	channelId: number | null;
	channelName?: string;
	platform: 'Android' | 'iOS' | 'WebGL';
	whiteAppVersion?: string;
	whiteResourceVersion?: string;
	releaseAppVersion?: string;
	releaseResourceVersion?: string;
	resourceBucket?: string;
	whitelistIds?: number[];
	updatedTime?: string;
}

export const getOssConfig = () =>
	request({
		url: ClientVersionApi.OssConfig,
		method: 'get',
	});

export const saveOssConfig = (data: OssConfig) =>
	request({
		url: ClientVersionApi.OssConfig,
		method: 'post',
		data,
	});

export const getWhitelistPage = (params?: { page?: number; pageSize?: number; keyword?: string }) =>
	request({
		url: ClientVersionApi.WhitelistPage,
		method: 'get',
		params,
	});

export const addWhitelistItem = (data: WhitelistItem) =>
	request({
		url: ClientVersionApi.WhitelistAdd,
		method: 'post',
		data,
	});

export const updateWhitelistItem = (data: WhitelistItem) =>
	request({
		url: ClientVersionApi.WhitelistUpdate,
		method: 'post',
		data,
	});

export const deleteWhitelistItem = (id: number) =>
	request({
		url: ClientVersionApi.WhitelistDelete,
		method: 'post',
		data: { id },
	});

export const getChannelVersionPage = (params?: { page?: number; pageSize?: number; channelId?: number }) =>
	request({
		url: ClientVersionApi.ChannelList,
		method: 'get',
		params,
	});

export const saveChannelVersion = (data: ChannelVersionItem) =>
	request({
		url: ClientVersionApi.ChannelSave,
		method: 'post',
		data,
	});

export const publishChannelVersion = (data: { id: number }) =>
	request({
		url: ClientVersionApi.ChannelPublish,
		method: 'post',
		data,
	});

export const getVersionOptions = (channelId: number, platform: string) =>
	request({
		url: ClientVersionApi.VersionOptions,
		method: 'get',
		params: { channelId, platform },
	});

export const getChannelOptions = () =>
	request({
		url: ClientVersionApi.ChannelOptions,
		method: 'get',
	});
