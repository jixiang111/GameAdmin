import request from '/@/utils/request';

export enum GameServerAddressApi {
	List = '/api/gameServerAddress/list',
	Add = '/api/gameServerAddress/add',
	Update = '/api/gameServerAddress/update',
	Delete = '/api/gameServerAddress/delete',
}

export interface GameServerAddressInput {
	serverId: number;
	serverName: string;
	ip: string;
	port: number;
	wsPort?: number | null;
	wsUrl?: string;
	grpcPort?: number | null;
	grpcUrl?: string;
	remark?: string;
	enabled?: boolean;
}

export interface GameServerAddressOutput extends GameServerAddressInput {
	wsPort: number | null;
	grpcPort: number | null;
	enabled: boolean;
}

export const getGameServerAddressList = (params?: Partial<Pick<GameServerAddressInput, 'serverId' | 'serverName'>>) =>
	request({
		url: GameServerAddressApi.List,
		method: 'get',
		params,
	});

export const addGameServerAddress = (data: GameServerAddressInput) =>
	request({
		url: GameServerAddressApi.Add,
		method: 'post',
		data,
	});

export const updateGameServerAddress = (data: GameServerAddressInput) =>
	request({
		url: GameServerAddressApi.Update,
		method: 'post',
		data,
	});

export const deleteGameServerAddress = (serverId: number) =>
	request({
		url: GameServerAddressApi.Delete,
		method: 'post',
		data: { serverId },
	});
