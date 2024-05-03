var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var HTMLClient;
(function (HTMLClient) {
    const Utility = WebsocketTSClient.Utility;
    const ErrCode = WebsocketTSClient.ErrCode;
    /// 页面负责交互逻辑的部分
    class MainPageController {
        constructor() {
            this.wsBackend = null;
            this.service = null;
            this.mainPage = null;
            /** 当前的masterId */
            this.masterClientId = null;
            /** service 启动步骤 */
            this.serviceStartupStep = -1;
            customElements.define('main-page', HTMLClient.MainPage);
            this.mainPage = document.querySelector('main-page');
            this.mainPage.OnMasterBtnClick.AddListener((master, btn) => {
                if (this.service != null) {
                    this.service.RegisterAsSlave(master);
                }
            });
            this.mainPage.OnBack2Main.AddListener(() => {
                if (this.masterClientId !== null) {
                    this.service.UnregisterFromSlave();
                }
            });
            this.mainPage.OnMenuItemClick.AddListener((topMost, id) => {
                const data = {
                    "topMost": topMost,
                    "id": id
                };
                this.service.Broadcast(data);
            });
            this.init();
        }
        init() {
            return __awaiter(this, void 0, void 0, function* () {
                //var backendUrl = 'ws://localhost:8080/ws';
                const hostname = window.location.hostname;
                const port = window.location.port ? `:${window.location.port}` : '';
                var backendUrl = `ws://${hostname}${port}/ws`;
                if (window.location.hostname == null || window.location.hostname == "") {
                    backendUrl = 'ws://shanxi.jeosun.cn/ws';
                }
                Utility.LogDebug("[MainPageController]", "backendUrl is " + backendUrl);
                Utility.LogDebug("[MainPageController]", "Create singleton backend start");
                if (WebsocketTSClient.WSBackend.CreateSingleton(backendUrl)) {
                    Utility.LogDebug("[MainPageController]", "Create singleton backend end");
                    this.wsBackend = WebsocketTSClient.WSBackend.singleton;
                    // await/async 异步等待服务器连接完成
                    Utility.LogDebug("[MainPageController]", "Connect to server start");
                    yield this.wsBackend.Connect2Server();
                    Utility.LogDebug("[MainPageController]", "Connect to server end");
                    // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                    this.service = new WebsocketTSClient.MasterSlavesGroupService();
                    // 设置事件，只设置几个用到的事件
                    this.service.OnRegisteredAsListener.AddListener((errCode) => this.handleRegisteredAsListener(errCode));
                    this.service.OnRegisteredAsSlave.AddListener((errCode, master, data) => this.handleRegisteredAsSlave(errCode, master, data));
                    this.service.OnUnregisteredFromSlave.AddListener((errCode) => this.handleUnregisteredFromSlave(errCode));
                    this.service.OnMasterCollectionChanged.AddListener((master) => this.handleMasterCollectionChanged(master));
                    this.service.OnGetAllMasters.AddListener((errCode, masters) => this.handleGetAllMasters(errCode, masters));
                    this.service.OnBroadcast.AddListener((errCode) => this.handleBroadcast(errCode));
                    this.service.OnRecievedBroadcast.AddListener((data) => this.handleReceivedBroadcast(data));
                    this.wsBackend.OnStateChanged.AddListener((state) => {
                        if (state === true) {
                            Utility.LogDebug("[MainPageController]", "backend reconnected and restart serviceStartup()");
                            this.serviceStartupStep = -1;
                            this.serviceStartup(0);
                        }
                    }); // 重新连接后，重新运行启动步骤
                    this.serviceStartup(0);
                }
                else {
                    Utility.LogDebug("[MainPageController]", "singleton backend already created");
                }
            });
        }
        serviceStartup(targetStep) {
            if (!this.wsBackend.IsConnected) {
                this.serviceStartupStep = -1;
                return;
            }
            if (this.serviceStartupStep === targetStep) {
                return;
            }
            this.serviceStartupStep = targetStep;
            if (this.serviceStartupStep === 0) {
                // 注册 Listener 用于监听
                this.service.RegisterAsListener();
                return;
            }
            if (this.serviceStartupStep === 1) {
                // 获取主界面按键信息
                this.service.GetAllMasters();
                return;
            }
        }
        handleRegisteredAsListener(errCode) {
            Utility.LogDebug("[MainPageController]", "RegisteredAsListener", errCode);
            var targetStep = this.serviceStartupStep;
            if (errCode === ErrCode.OK) {
                targetStep++;
            }
            this.serviceStartup(targetStep);
        }
        handleRegisteredAsSlave(errCode, master, data) {
            Utility.LogDebug("[MainPageController]", "RegisteredAsSlave", errCode);
            // if errCode is ok and data is not null and data contains menuCollection member
            if (errCode == ErrCode.OK) {
                this.masterClientId = master.clientId;
                if (data != null && 'menuCollection' in data) {
                    this.mainPage.SetupMenus(master.masterName, data.menuCollection);
                }
                else {
                    this.service.UnregisterFromSlave();
                }
            }
        }
        handleUnregisteredFromSlave(errCode) {
            Utility.LogDebug("[MainPageController]", "UnregisteredFromSlave", errCode);
            if (errCode == ErrCode.OK) {
                this.masterClientId = null;
                this.mainPage.Back2MainPage();
            }
        }
        handleMasterCollectionChanged(master) {
            Utility.LogDebug("[MainPageController]", "CollectionChanged", master.toString());
            if (master.isOnline) {
                this.mainPage.AddButton(master);
            }
            else {
                this.mainPage.RemoveBtnById(master.clientId);
                if (master.clientId === this.masterClientId) {
                    this.service.UnregisterFromSlave();
                }
            }
        }
        handleGetAllMasters(errCode, masters) {
            Utility.LogDebug("[MainPageController]", "GetAllMasters", errCode);
            masters.forEach((master) => {
                Utility.LogDebug("[MainPageController]", "---", master.toString());
            });
            this.mainPage.ResetMasterBtns(masters);
        }
        handleBroadcast(errCode) {
            Utility.LogDebug("[MainPageController]", "Broadcast", errCode);
        }
        handleReceivedBroadcast(data) {
            Utility.LogDebug("[MainPageController]", "ReceivedBroadcast", data);
        }
    }
    HTMLClient.MainPageController = MainPageController;
})(HTMLClient || (HTMLClient = {}));
