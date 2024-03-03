var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var WebsocketTSClient;
(function (WebsocketTSClient) {
    var utility = WebsocketTSClient.Utility;
    var service = null;
    var wsBackend = null;
    function init() {
        return __awaiter(this, void 0, void 0, function () {
            var backendUrl;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        backendUrl = 'ws://localhost:8080/ws';
                        // 创建一个 backend 的单例
                        utility.LogDebug("[HTML]", "Create singleton backend start");
                        if (!WebsocketTSClient.WSBackend.CreateSingleton(backendUrl)) return [3 /*break*/, 2];
                        utility.LogDebug("[HTML]", "Create singleton backend end");
                        wsBackend = WebsocketTSClient.WSBackend.singleton;
                        // await/async 异步等待服务器连接完成
                        utility.LogDebug("[HTML]", "Connect to server start");
                        return [4 /*yield*/, wsBackend.Connect2Server()];
                    case 1:
                        _a.sent();
                        utility.LogDebug("[HTML]", "Connect to server end");
                        // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                        service = new WebsocketTSClient.MasterSlavesGroupService();
                        // Callback 后改变 UI
                        service.OnRegisteredAsMaster.AddListener(function (errCode) { if (errCode === WebsocketTSClient.ErrCode.OK)
                            resetUiElements(); });
                        service.OnUnregisteredFromMaster.AddListener(function (errCode) { if (errCode === WebsocketTSClient.ErrCode.OK)
                            resetUiElements(); });
                        service.OnRegisteredAsSlave.AddListener(function (errCode) { if (errCode === WebsocketTSClient.ErrCode.OK)
                            resetUiElements(); });
                        service.OnUnregisteredFromSlave.AddListener(function (errCode) { if (errCode === WebsocketTSClient.ErrCode.OK)
                            resetUiElements(); });
                        resetUiElements();
                        // UI 交互逻辑
                        document.getElementById('registerMasterBtn').addEventListener('click', function () {
                            var masterName = document.getElementById('inputMasterName').value;
                            if (masterName) {
                                service.RegisterAsMaster(masterName, null);
                            }
                        });
                        document.getElementById('unRegisterMasterBtn').addEventListener('click', function () {
                            service.UnRegisterFromMaster();
                        });
                        document.getElementById('registerSlaveBtn').addEventListener('click', function () {
                            var masterId = document.getElementById('inputMasterId').value;
                            if (masterId) {
                                service.RegisterAsSlave(masterId);
                            }
                        });
                        document.getElementById('unRegisterSlaveBtn').addEventListener('click', function () {
                            service.UnregisterFromSlave();
                        });
                        document.getElementById('getAllMastersBtn').addEventListener('click', function () {
                            service.GetAllMasters();
                        });
                        document.getElementById('broadcastBtn').addEventListener('click', function () {
                            var message = document.getElementById('inputBroadcast').value;
                            if (message) {
                                var data = {
                                    msg: message
                                };
                                service.Broadcast(data);
                            }
                        });
                        return [3 /*break*/, 3];
                    case 2:
                        utility.LogDebug("[HTML]", "singleton backend already created");
                        _a.label = 3;
                    case 3: return [2 /*return*/];
                }
            });
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
