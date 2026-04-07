import request from '/@/utils/request';
import type { AxiosRequestConfig } from 'axios';

const requestWithFallback = async <T = any>(config: AxiosRequestConfig, fallbackUrls: string[] = []) => {
	try {
		return await request<T>(config);
	} catch (error: any) {
		if (error?.response?.status !== 404 || fallbackUrls.length === 0) throw error;

		for (const url of fallbackUrls) {
			try {
				return await request<T>({ ...config, url });
			} catch (err: any) {
				if (err?.response?.status !== 404) throw err;
			}
		}

		throw error;
	}
};

export enum GameServerItemAuditApi {
	Page = '/api/gameServerItemAudit/page',
}

export enum ItemChangeSourceType {
	Unknown = 0,
	Client = 1,
	Gm = 2,
	System = 3,
}

export enum ItemChangeOperationType {
	Unknown = 0,
	Adjust = 1,
	Use = 2,
	Expire = 3,
	Sell = 4,
	GmAdjust = 5,
}

export interface GameServerItemAuditQueryInput {
	page?: number;
	pageSize?: number;
	serverId?: number;
	roleId?: string;
	itemId?: number;
	sourceType?: ItemChangeSourceType;
	operationType?: ItemChangeOperationType;
	operatorId?: string;
	traceId?: string;
	reasonCode?: string;
	startTimeTicksUtc?: string;
	endTimeTicksUtc?: string;
}

export interface GameServerItemAuditOutput {
	logId: string | number;
	roleId: string | number;
	itemId: number;
	beforeNum: string | number;
	afterNum: string | number;
	delta: string | number;
	sourceType: ItemChangeSourceType;
	operationType: ItemChangeOperationType;
	reasonCode?: string;
	remark?: string;
	operatorId?: string | number;
	operatorName?: string;
	traceId?: string;
	requestUniId?: string;
	changeTimeTicks?: string | number;
}

export interface GameServerItemAuditPageOutput {
	serverId: number;
	serverName?: string;
	page: number;
	pageSize: number;
	total: number;
	totalPages: number;
	hasMore: boolean;
	hasNextPage: boolean;
	hasPrevPage: boolean;
	items: GameServerItemAuditOutput[];
}

export const getGameServerItemAuditPage = (params: GameServerItemAuditQueryInput) =>
	requestWithFallback<GameServerItemAuditPageOutput>(
		{
			url: GameServerItemAuditApi.Page,
			method: 'get',
			params,
		},
		['/api/gameServerItemAudit/getPage']
	);
