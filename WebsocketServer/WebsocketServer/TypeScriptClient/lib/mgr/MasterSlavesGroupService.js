var WebsocketTSClient;
(function (WebsocketTSClient) {
    let MasterSlavesGroupServiceState;
    (function (MasterSlavesGroupServiceState) {
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["Idle"] = 0] = "Idle";
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["IsMaster"] = 1] = "IsMaster";
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["IsSlave"] = 2] = "IsSlave";
    })(MasterSlavesGroupServiceState = WebsocketTSClient.MasterSlavesGroupServiceState || (WebsocketTSClient.MasterSlavesGroupServiceState = {}));
    class MasterClient {
        constructor(clientId, masterName, isOnline) {
            this.clientId = clientId;
            this.masterName = masterName;
            this.isOnline = isOnline;
        }
        static parse(json) {
            if (!json || typeof json !== 'object') {
                return null;
            }
            const clientId = json.clientId;
            const masterName = json.masterName;
            const isOnline = json.isOnline;
            if (typeof clientId !== 'string' || typeof masterName !== 'string' || typeof isOnline !== 'boolean') {
                return null;
            }
            return new MasterClient(clientId, masterName, isOnline);
        }
        toString() {
            return `masterName: ${this.masterName}, isOnline: ${this.isOnline}, clientId: ${this.clientId}`;
        }
    }
    WebsocketTSClient.MasterClient = MasterClient;
    class MasterSlavesGroupService {
        GetState() {
            return this.state;
        }
        constructor() {
            /** 是否在请求中 */
            this.isQuarying = false;
            /** 注册为 Listerner */
            this.OnRegisteredAsListener = new WebsocketTSClient.EventHandler();
            /** 注销 Listerner */
            this.OnUnregisteredFromListener = new WebsocketTSClient.EventHandler();
            /** 注册为 master */
            this.OnRegisteredAsMaster = new WebsocketTSClient.EventHandler();
            /** 注销 master */
            this.OnUnregisteredFromMaster = new WebsocketTSClient.EventHandler();
            /** 注册为 slave */
            this.OnRegisteredAsSlave = new WebsocketTSClient.EventHandler();
            /** 注销 slave */
            this.OnUnregisteredFromSlave = new WebsocketTSClient.EventHandler();
            /** 当服务器 master 集合发生变化 */
            this.OnMasterCollectionChanged = new WebsocketTSClient.EventHandler();
            /** 获取所有 master */
            this.OnGetAllMasters = new WebsocketTSClient.EventHandler();
            /** 广播消息 */
            this.OnBroadcast = new WebsocketTSClient.EventHandler();
            /** 接收到广播消息 */
            this.OnRecievedBroadcast = new WebsocketTSClient.EventHandler();
            this.state = MasterSlavesGroupServiceState.Idle;
            this.OnBackendStateChanged = (state) => {
                if (!state) {
                    this.isQuarying = false;
                    this.state = MasterSlavesGroupServiceState.Idle;
                }
            };
            this.OnBackendNotify = (not) => {
                if (not.serviceName === MasterSlavesGroupService.serviceName) {
                    // 收到服务器 master 集合变化的消息
                    if (not.cmd === WebsocketTSClient.BackendOps.Notify_OnMasterCollectionChanged) {
                        const master = MasterClient.parse(not.data);
                        WebsocketTSClient.Utility.LogDebug("MasterSlavesGroupService", "Notify Master Collection Changed", master.toString());
                        this.OnMasterCollectionChanged.Trigger(master);
                    }
                    // 收到广播消息
                    if (not.cmd === WebsocketTSClient.BackendOps.Cmd_Broadcast) {
                        this.OnRecievedBroadcast.Trigger(not.data);
                    }
                }
            };
            WebsocketTSClient.WSBackend.singleton.OnStateChanged.AddListener(this.OnBackendStateChanged);
            WebsocketTSClient.WSBackend.singleton.OnNotify.AddListener(this.OnBackendNotify);
        }
        RegisterAsListener() {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsListener, null, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    this.OnRegisteredAsListener.Trigger(errCode);
                });
            }
        }
        UnregisterFromListener() {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromListener, null, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    this.OnUnregisteredFromListener.Trigger(errCode);
                });
            }
        }
        RegisterAsMaster(masterName, masterData) {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                var data = {
                    masterName: masterName,
                    masterData: masterData
                };
                this.state = MasterSlavesGroupServiceState.IsMaster;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsMaster, data, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    this.OnRegisteredAsMaster.Trigger(errCode);
                });
            }
        }
        UnRegisterFromMaster() {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromMaster, null, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsMaster;
                    this.OnUnregisteredFromMaster.Trigger(errCode);
                });
            }
        }
        RegisterAsSlave(master) {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                var data = {
                    masterId: master.clientId
                };
                this.state = MasterSlavesGroupServiceState.IsSlave;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsSlave, data, master, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsSlave : MasterSlavesGroupServiceState.Idle;
                    this.OnRegisteredAsSlave.Trigger(errCode, context, data);
                });
            }
        }
        UnregisterFromSlave() {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromSlave, null, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsSlave;
                    this.OnUnregisteredFromSlave.Trigger(errCode);
                });
            }
        }
        GetAllMasters() {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_GetAllMasters, null, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    const result = [];
                    const jarr = data["masters"];
                    if (jarr !== null) {
                        jarr.forEach((jobj) => {
                            var mc = MasterClient.parse(jobj);
                            if (mc !== null) {
                                result.push(mc);
                            }
                        });
                    }
                    this.OnGetAllMasters.Trigger(errCode, result);
                });
            }
        }
        Broadcast(data) {
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_Broadcast, data, null, 
                // resp
                (errCode, data, context) => {
                    this.isQuarying = false;
                    this.OnBroadcast.Trigger(errCode);
                });
            }
        }
    }
    /** 服务名称 */
    MasterSlavesGroupService.serviceName = "MasterSlavesGroupService";
    WebsocketTSClient.MasterSlavesGroupService = MasterSlavesGroupService;
})(WebsocketTSClient || (WebsocketTSClient = {}));
