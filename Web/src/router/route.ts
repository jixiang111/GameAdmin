import { RouteRecordRaw } from 'vue-router';

declare module 'vue-router' {
	interface RouteMeta {
		title?: string;
		isLink?: string;
		isHide?: boolean;
		isPublic?: boolean;
		isKeepAlive?: boolean;
		isAffix?: boolean;
		isIframe?: boolean;
		roles?: string[];
		icon?: string;
	}
}

export const serverManageRoute: RouteRecordRaw = {
	path: '/dashboard/server-manage',
	name: 'gameServerManager',
	component: () => import('/@/views/workbench/server/index.vue'),
	meta: {
		title: '服务器管理',
		isKeepAlive: true,
		icon: 'ele-DataLine',
	},
};

export const clientVersionRoute: RouteRecordRaw = {
	path: '/dashboard/client-version',
	name: 'clientVersion',
	component: () => import('/@/layout/routerView/parent.vue'),
	redirect: '/dashboard/client-version/oss',
	meta: {
		title: '客户端版本管理',
		icon: 'ele-Setting',
	},
	children: [
		{
			path: '/dashboard/client-version/oss',
			name: 'clientVersionOss',
			component: () => import('/@/views/workbench/clientVersion/ossConfig.vue'),
			meta: {
				title: 'OSS 参数设置',
				isKeepAlive: true,
			},
		},
		{
			path: '/dashboard/client-version/whitelist',
			name: 'clientVersionWhitelist',
			component: () => import('/@/views/workbench/clientVersion/whitelist.vue'),
			meta: {
				title: '白名单设置',
				isKeepAlive: true,
			},
		},
		{
			path: '/dashboard/client-version/channel',
			name: 'clientVersionChannel',
			component: () => import('/@/views/workbench/clientVersion/channel.vue'),
			meta: {
				title: '渠道版本管理',
				isKeepAlive: true,
			},
		},
	],
};

export const serverManageMenuRaw = {
	id: 0,
	pid: 0,
	type: 2,
	name: 'gameServerManager',
	path: '/dashboard/server-manage',
	component: '/workbench/server/index',
	permission: 'gameServerAddress:list',
	orderNo: 101,
	status: 1,
	meta: {
		title: '服务器管理',
		icon: 'ele-DataLine',
		isKeepAlive: true,
		isHide: false,
		isIframe: false,
	},
	children: [],
};

export const clientVersionMenuRaw = {
	id: 0,
	pid: 0,
	type: 1,
	name: 'clientVersion',
	path: '/dashboard/client-version',
	component: 'layout/routerView/parent',
	orderNo: 102,
	meta: {
		title: '客户端版本管理',
		icon: 'ele-Setting',
		isKeepAlive: true,
	},
	children: [
		{
			id: 0,
			pid: 0,
			type: 2,
			name: 'clientVersionOss',
			path: '/dashboard/client-version/oss',
			component: '/workbench/clientVersion/ossConfig',
			meta: {
				title: 'OSS 参数设置',
				icon: 'ele-Connection',
				isKeepAlive: true,
			},
		},
		{
			id: 0,
			pid: 0,
			type: 2,
			name: 'clientVersionWhitelist',
			path: '/dashboard/client-version/whitelist',
			component: '/workbench/clientVersion/whitelist',
			meta: {
				title: '白名单设置',
				icon: 'ele-List',
				isKeepAlive: true,
			},
		},
		{
			id: 0,
			pid: 0,
			type: 2,
			name: 'clientVersionChannel',
			path: '/dashboard/client-version/channel',
			component: '/workbench/clientVersion/channel',
			meta: {
				title: '渠道版本管理',
				icon: 'ele-Operation',
				isKeepAlive: true,
			},
		},
	],
};

export const dynamicRoutes: Array<RouteRecordRaw> = [
	{
		path: '/',
		name: '/',
		component: () => import('/@/layout/index.vue'),
		redirect: '/dashboard/home',
		meta: {
			isKeepAlive: true,
		},
		children: [serverManageRoute, clientVersionRoute],
	},
	{
		path: '/platform/job/dashboard',
		name: 'jobDashboard',
		component: () => import('/@/views/system/job/dashboard.vue'),
		meta: {
			title: '任务看板',
			isLink: window.__env__.VITE_API_URL + '/schedule',
			isHide: true,
			isKeepAlive: true,
			isAffix: false,
			isIframe: true,
			icon: 'ele-Clock',
		},
	},
	{
		path: '/develop/database/visual',
		name: 'databaseVisual',
		component: () => import('/@/views/system/database/component/visualTable.vue'),
		meta: {
			title: '库表可视化',
			isHide: true,
			isKeepAlive: true,
			isAffix: false,
			icon: 'ele-View',
		},
	},
];

export const notFoundAndNoPower = [
	{
		path: '/:path(.*)*',
		name: 'notFound',
		component: () => import('/@/views/error/404.vue'),
		meta: {
			title: '找不到此页面',
			isHide: true,
		},
	},
	{
		path: '/401',
		name: 'noPower',
		component: () => import('/@/views/error/401.vue'),
		meta: {
			title: '没有权限',
			isHide: true,
		},
	},
];

export const staticRoutes: Array<RouteRecordRaw> = [
	{
		path: '/login',
		name: 'login',
		component: () => import('/@/views/login/index.vue'),
		meta: {
			title: '登录',
			isPublic: true,
		},
	},
	{
		path: '/$callTel',
		name: '$callTel',
		component: () => import('/@/components/callTel/index.vue'),
		meta: {
			title: '拨号',
			isPublic: true,
		},
	},
];
