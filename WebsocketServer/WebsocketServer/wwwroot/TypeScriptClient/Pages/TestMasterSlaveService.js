var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var WebsocketTSClient;
(function (WebsocketTSClient) {
    var Utility = WebsocketTSClient.Utility;
    var service = null;
    var wsBackend = null;
    function init() {
        return __awaiter(this, void 0, void 0, function* () {
            /// connect to server first
            const backendUrl = 'ws://localhost:5000/ws';
            // 创建一个 backend 的单例
            Utility.LogDebug("[HTML]", "Create singleton backend start");
            if (WebsocketTSClient.WSBackend.CreateSingleton(backendUrl)) {
                Utility.LogDebug("[HTML]", "Create singleton backend end");
                wsBackend = WebsocketTSClient.WSBackend.singleton;
                // await/async 异步等待服务器连接完成
                Utility.LogDebug("[HTML]", "Connect to server start");
                yield wsBackend.Connect2Server();
                Utility.LogDebug("[HTML]", "Connect to server end");
                // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                service = new WebsocketTSClient.MasterSlavesGroupService();
                service.RegisterAsListener();
                // Callback 后改变 UI
                wsBackend.OnStateChanged.AddListener((state) => { if (!state)
                    resetUiElements(); }); // 掉线后改变 UI
                service.OnRegisteredAsMaster.AddListener((errCode) => { if (errCode === WebsocketTSClient.ErrCode.OK)
                    resetUiElements(); });
                service.OnUnregisteredFromMaster.AddListener((errCode) => { if (errCode === WebsocketTSClient.ErrCode.OK)
                    resetUiElements(); });
                service.OnRegisteredAsSlave.AddListener((errCode) => { if (errCode === WebsocketTSClient.ErrCode.OK)
                    resetUiElements(); });
                service.OnUnregisteredFromSlave.AddListener((errCode) => { if (errCode === WebsocketTSClient.ErrCode.OK)
                    resetUiElements(); });
                resetUiElements();
                // UI 交互逻辑
                document.getElementById('registerMasterBtn').addEventListener('click', () => {
                    const masterName = document.getElementById('inputMasterName').value;
                    if (masterName) {
                        service.RegisterAsMaster(masterName, null);
                    }
                });
                document.getElementById('unRegisterMasterBtn').addEventListener('click', () => {
                    service.UnRegisterFromMaster();
                });
                document.getElementById('registerSlaveBtn').addEventListener('click', () => {
                    const masterId = document.getElementById('inputMasterId').value;
                    if (masterId) {
                        service.RegisterAsSlave(new WebsocketTSClient.MasterClient(masterId, -1, "tempName", true));
                    }
                });
                document.getElementById('unRegisterSlaveBtn').addEventListener('click', () => {
                    service.UnregisterFromSlave();
                });
                document.getElementById('getAllMastersBtn').addEventListener('click', () => {
                    service.GetAllMasters();
                });
                document.getElementById('broadcastBtn').addEventListener('click', () => {
                    const message = document.getElementById('inputBroadcast').value;
                    if (message) {
                        const data = {
                            msg: message
                        };
                        service.Broadcast(data);
                    }
                });
            }
            else {
                Utility.LogDebug("[HTML]", "singleton backend already created");
            }
        });
    }
    /// 重置所有组件显示
    function resetUiElements() {
        var state = service.GetState();
        document.getElementById('getAllMastersBtn').disabled = false;
        document.getElementById('registerMasterBtn').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
        document.getElementById('unRegisterMasterBtn').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.IsMaster;
        document.getElementById('registerSlaveBtn').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
        document.getElementById('unRegisterSlaveBtn').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.IsSlave;
        document.getElementById('inputMasterName').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
        document.getElementById('inputMasterId').disabled = state != WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
        document.getElementById('broadcastBtn').disabled = state == WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
        document.getElementById('inputBroadcast').disabled = state == WebsocketTSClient.MasterSlavesGroupServiceState.Idle;
    }
    // Call the init function
    init();
})(WebsocketTSClient || (WebsocketTSClient = {}));
