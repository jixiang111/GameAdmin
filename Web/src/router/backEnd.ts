import { RouteRecordRaw } from 'vue-router';
import pinia from '/@/stores/index';
import { useUserInfo } from '/@/stores/userInfo';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { Session } from '/@/utils/storage';
import { NextLoading } from '/@/utils/loading';
import { dynamicRoutes, notFoundAndNoPower, serverManageMenuRaw, gameServerItemAuditMenuRaw, gameServerItemAdjustMenuRaw, clientVersionMenuRaw } from '/@/router/route';
import { formatTwoStageRoutes, formatFlatteningRoutes, router } from '/@/router/index';
import { useRoutesList } from '/@/stores/routesList';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi } from '/@/api-services/api';
// import { ElMessage } from 'element-plus';

// 鍚庣鎺у埗璺敱

/**
 * 鑾峰彇鐩綍涓嬬殑 .vue銆?tsx 鍏ㄩ儴鏂囦欢
 * @method import.meta.glob
 * @link 鍙傝€冿細https://cn.vitejs.dev/guide/features.html#json
 */
const layouModules: any = import.meta.glob('../layout/routerView/*.{vue,tsx}');
const viewsModules: any = import.meta.glob('../views/**/*.{vue,tsx}');
const dynamicViewsModules: Record<string, Function> = Object.assign({}, { ...layouModules }, { ...viewsModules });

/**
 * 鍚庣鎺у埗璺敱锛氬垵濮嬪寲鏂规硶锛岄槻姝㈠埛鏂版椂璺敱涓㈠け
 * @method NextLoading 鐣岄潰 loading 鍔ㄧ敾寮€濮嬫墽琛?
 * @method useUserInfo().setUserInfos() 瑙﹀彂鍒濆鍖栫敤鎴蜂俊鎭?pinia
 * @method useRequestOldRoutes().setRequestOldRoutes() 瀛樺偍鎺ュ彛鍘熷璺敱锛堟湭澶勭悊component锛夛紝鏍规嵁闇€姹傞€夋嫨浣跨敤
 * @method setAddRoute 娣诲姞鍔ㄦ€佽矾鐢?
 * @method setFilterMenuAndCacheTagsViewRoutes 璁剧疆璺敱鍒?pinia routesList 涓紙宸插鐞嗘垚澶氱骇宓屽璺敱锛夊強缂撳瓨澶氱骇宓屽鏁扮粍澶勭悊鍚庣殑涓€缁存暟缁?
 */
export async function initBackEndControlRoutes() {

	// 鐣岄潰 loading 鍔ㄧ敾寮€濮嬫墽琛?

	if (window.nextLoading === undefined) NextLoading.start();

	// 鏃?token 鍋滄鎵ц涓嬩竴姝?

	if (!Session.get('token')) return false;

	// 瑙﹀彂鍒濆鍖栫敤鎴蜂俊鎭?pinia

	// https://gitee.com/lyt-top/vue-next-admin/issues/I5F1HP

	await useUserInfo().setUserInfos();

	await useUserInfo().setConstList();

	await useUserInfo().setDictList();

	// 鑾峰彇璺敱鑿滃崟鏁版嵁骞跺己鍒惰拷鍔犳湇鍔″櫒绠＄悊鍏ュ彛

	const rawRoutes = await getBackEndControlRoutes();

	const sanitizedRoutes = removeDisabledMenus(rawRoutes);
	const res = ensureBaseWorkbenchMenus(sanitizedRoutes);

	// 鏃犵櫥褰曟潈闄愭椂锛屾坊鍔犲垽鏂?

	// https://gitee.com/lyt-top/vue-next-admin/issues/I64HVO

	if (res == undefined || res.length <= 0) return Promise.resolve(true);

	// 瀛樺偍鎺ュ彛鍘熷璺敱锛堟湭澶勭悊component锛夛紝鏍规嵁闇€姹傞€夋嫨浣跨敤

	useRequestOldRoutes().setRequestOldRoutes(res as string[]);

	// 澶勭悊璺敱锛坈omponent锛夛紝鏇挎崲 dynamicRoutes锛?@/router/route锛夌涓€涓《绾?children 鐨勮矾鐢?

	dynamicRoutes[0].children = await backEndComponent(res);

	// 娣诲姞鍔ㄦ€佽矾鐢?

	await setAddRoute();

	// 璁剧疆璺敱鍒?pinia routesList 涓紙宸插鐞嗘垚澶氱骇宓屽璺敱锛夊強缂撳瓨澶氱骇宓屽鏁扮粍澶勭悊鍚庣殑涓€缁存暟鎹?

	setFilterMenuAndCacheTagsViewRoutes();

}



/**
 * 璁剧疆璺敱鍒?pinia routesList 涓紙宸插鐞嗘垚澶氱骇宓屽璺敱锛夊強缂撳瓨澶氱骇宓屽鏁扮粍澶勭悊鍚庣殑涓€缁存暟缁?
 * @description 鐢ㄤ簬宸︿晶鑿滃崟銆佹í鍚戣彍鍗曠殑鏄剧ず
 * @description 鐢ㄤ簬 tagsView銆佽彍鍗曟悳绱腑锛氭湭杩囨护闅愯棌鐨?isHide)
 */
export async function setFilterMenuAndCacheTagsViewRoutes() {
	const storesRoutesList = useRoutesList(pinia);
	storesRoutesList.setRoutesList(dynamicRoutes[0].children as any);
	setCacheTagsViewRoutes();
}

/**
 * 缂撳瓨澶氱骇宓屽鏁扮粍澶勭悊鍚庣殑涓€缁存暟缁?
 * @description 鐢ㄤ簬 tagsView銆佽彍鍗曟悳绱腑锛氭湭杩囨护闅愯棌鐨?isHide)
 */
export function setCacheTagsViewRoutes() {
	const storesTagsView = useTagsViewRoutes(pinia);
	storesTagsView.setTagsViewRoutes(formatTwoStageRoutes(formatFlatteningRoutes(dynamicRoutes))[0].children);
}

/**
 * 澶勭悊璺敱鏍煎紡鍙婃坊鍔犳崟鑾锋墍鏈夎矾鐢辨垨 404 Not found 璺敱
 * @description 鏇挎崲 dynamicRoutes锛?@/router/route锛夌涓€涓《绾?children 鐨勮矾鐢?
 * @returns 杩斿洖鏇挎崲鍚庣殑璺敱鏁扮粍
 */
export function setFilterRouteEnd() {
	let filterRouteEnd: any = formatTwoStageRoutes(formatFlatteningRoutes(dynamicRoutes));
	// notFoundAndNoPower 闃叉 404銆?01 涓嶅湪 layout 甯冨眬涓紝涓嶈缃殑璇濓紝404銆?01 鐣岄潰灏嗗叏灞忔樉绀?
	// 鍏宠仈闂 No match found for location with path 'xxx'
	filterRouteEnd[0].children = [...filterRouteEnd[0].children, ...notFoundAndNoPower];
	return filterRouteEnd;
}

/**
 * 娣诲姞鍔ㄦ€佽矾鐢?
 * @method router.addRoute
 * @description 姝ゅ寰幆涓?dynamicRoutes锛?@/router/route锛夌涓€涓《绾?children 鐨勮矾鐢变竴缁存暟缁勶紝闈炲绾у祵濂?
 * @link 鍙傝€冿細https://next.router.vuejs.org/zh/api/#addroute
 */
export async function setAddRoute() {
	await setFilterRouteEnd().forEach((route: RouteRecordRaw) => {
		router.addRoute(route);
	});
}

/**
 * 璇锋眰鍚庣璺敱鑿滃崟鎺ュ彛
 * @description isRequestRoutes 涓?true锛屽垯寮€鍚悗绔帶鍒惰矾鐢?
 * @returns 杩斿洖鍚庣璺敱鑿滃崟鏁版嵁
 */
export async function getBackEndControlRoutes() {
	var res = await getAPI(SysMenuApi).apiSysMenuLoginMenuTreeGet();
	// if (res.data.result == undefined || res.data.result.length < 1) {
	// 	ElMessage.error('娌℃湁浠讳綍鑿滃崟鏉冮檺锛岃鑱旂郴绠＄悊鍛橈紒');
	// 	setTimeout(() => {
	// 		Session.removeToken();
	// 		window.location.reload();
	// 	}, 3000);
	// }
	return res.data.result;
}

/**

 * 杩藉姞鏈嶅姟鍣ㄧ鐞嗚彍鍗曢」锛岀‘淇濆墠绔缁堝彲瑙?

 */

const DISABLED_MENU_NAMES = ['develop', 'doc', 'clientVersion'];
const DISABLED_MENU_TITLES = ['开发工具', '帮助文档', '客户端版本管理'];
const BASE_WORKBENCH_MENUS = [serverManageMenuRaw, gameServerItemAuditMenuRaw, gameServerItemAdjustMenuRaw, clientVersionMenuRaw];

function ensureBaseWorkbenchMenus(routes: any) {
	const list = Array.isArray(routes) ? [...routes] : [];
	const workbenchIndex = list.findIndex((route) => route?.name === 'dashboard' || route?.path === '/dashboard');
	let insertIndex = workbenchIndex;

	BASE_WORKBENCH_MENUS.forEach((menu) => {
		const existsIndex = list.findIndex((route) => route?.path === menu.path || route?.name === menu.name);
		if (existsIndex > -1) {
			if (existsIndex > insertIndex) insertIndex = existsIndex;
			return;
		}
		const cloneMenu = JSON.parse(JSON.stringify(menu));
		if (insertIndex > -1) {
			list.splice(insertIndex + 1, 0, cloneMenu);
			insertIndex += 1;
		} else {
			list.push(cloneMenu);
			insertIndex = list.length - 1;
		}
	});

	return list;
}

function removeDisabledMenus(routes: any) {
	if (!Array.isArray(routes)) return [];
	const deepFilter = (items: any[]) => {
		return (items || [])
			.filter((item) => {
				const blockByName = DISABLED_MENU_NAMES.includes(item?.name);
				const blockByTitle = DISABLED_MENU_TITLES.includes(item?.title);
				return !(blockByName || blockByTitle);
			})
			.map((item) => {
				if (Array.isArray(item.children) && item.children.length > 0) {
					item.children = deepFilter(item.children);
				}
				return item;
			});
	};
	return deepFilter(routes);
}



/**
 * 閲嶆柊璇锋眰鍚庣璺敱鑿滃崟鎺ュ彛
 * @description 鐢ㄤ簬鑿滃崟绠＄悊鐣岄潰鍒锋柊鑿滃崟锛堟湭杩涜娴嬭瘯锛?
 * @description 璺緞锛?src/views/system/menu/component/addMenu.vue
 */
export async function setBackEndControlRefreshRoutes() {
	await getBackEndControlRoutes();
}

/**
 * 鍚庣璺敱 component 杞崲
 * @param routes 鍚庣杩斿洖鐨勮矾鐢辫〃鏁扮粍
 * @returns 杩斿洖澶勭悊鎴愬嚱鏁板悗鐨?component
 */
export function backEndComponent(routes: any) {
	if (!routes) return;
	return routes.map((item: any) => {
		if (!item.path) item.path = ''; // 闃叉鍚庣杩斿洖鐨勮矾鐢辨病鏈塸ath灞炴€э紝瀵艰嚧璺敱鎶ラ敊
		if (item.component) item.component = dynamicImport(dynamicViewsModules, item.component as string);
		item.children && backEndComponent(item.children);
		return item;
	});
}

/**
 * 鍚庣璺敱 component 杞崲鍑芥暟
 * @param dynamicViewsModules 鑾峰彇鐩綍涓嬬殑 .vue銆?tsx 鍏ㄩ儴鏂囦欢
 * @param component 褰撳墠瑕佸鐞嗛」 component
 * @returns 杩斿洖澶勭悊鎴愬嚱鏁板悗鐨?component
 */
export function dynamicImport(dynamicViewsModules: Record<string, Function>, component: string) {
	const keys = Object.keys(dynamicViewsModules);
	const matchKeys = keys.filter((key) => {
		const k = key.replace(/..\/views|../, '');
		return k.startsWith(`${component}`) || k.startsWith(`/${component}`);
	});
	if (matchKeys?.length === 1) {
		const matchKey = matchKeys[0];
		return dynamicViewsModules[matchKey];
	}
	if (matchKeys?.length > 1) {
		return false;
	}
}



