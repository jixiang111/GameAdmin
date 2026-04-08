import request from '/@/utils/request';

export enum GameServerItemAdjustApi {
	Submit = '/api/gameServerItemAdjust/submit',
}

export interface GameServerItemAdjustInput {
	serverId?: number;
	roleId?: string;
	itemId?: number;
	delta?: number;
	reasonCode?: string;
	remark?: string;
	traceId?: string;
}

export interface GameServerItemAdjustOutput {
	serverId: number;
	roleId: string | number;
	itemId: number;
	appliedDelta: string | number;
	beforeNum: string | number;
	afterNum: string | number;
	changeTimeTicks?: string | number;
	traceId?: string;
	reasonCode?: string;
	remark?: string;
	operatorId?: string | number;
	operatorName?: string;
}

export const submitGameServerItemAdjust = (data: GameServerItemAdjustInput) =>
	request({
		url: GameServerItemAdjustApi.Submit,
		method: 'post',
		data,
	});
