var WebsocketTSClient;
(function (WebsocketTSClient) {
    var MasterSlavesGroupServiceState;
    (function (MasterSlavesGroupServiceState) {
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["Idle"] = 0] = "Idle";
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["IsMaster"] = 1] = "IsMaster";
        MasterSlavesGroupServiceState[MasterSlavesGroupServiceState["IsSlave"] = 2] = "IsSlave";
    })(MasterSlavesGroupServiceState = WebsocketTSClient.MasterSlavesGroupServiceState || (WebsocketTSClient.MasterSlavesGroupServiceState = {}));
    var MasterClient = /** @class */ (function () {
        function MasterClient(clientId, masterName, isOnline) {
            this.clientId = clientId;
            this.masterName = masterName;
            this.isOnline = isOnline;
        }
        MasterClient.parse = function (json) {
            if (!json || typeof json !== 'object') {
                return null;
            }
            var clientId = json.clientId;
            var masterName = json.masterName;
            var isOnline = json.isOnline;
            if (typeof clientId !== 'string' || typeof masterName !== 'string' || typeof isOnline !== 'boolean') {
                return null;
            }
            return new MasterClient(clientId, masterName, isOnline);
        };
        MasterClient.prototype.toString = function () {
            return "masterName: ".concat(this.masterName, ", isOnline: ").concat(this.isOnline, ", clientId: ").concat(this.clientId);
        };
        return MasterClient;
    }());
    WebsocketTSClient.MasterClient = MasterClient;
    var MasterSlavesGroupService = /** @class */ (function () {
        function MasterSlavesGroupService() {
            var _this = this;
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
            this.OnBackendStateChanged = function (state) {
                if (!state) {
                    _this.isQuarying = false;
                    _this.state = MasterSlavesGroupServiceState.Idle;
                }
            };
            this.OnBackendNotify = function (not) {
                if (not.serviceName === MasterSlavesGroupService.serviceName) {
                    // 收到服务器 master 集合变化的消息
                    if (not.cmd === WebsocketTSClient.BackendOps.Notify_OnMasterCollectionChanged) {
                        var master = MasterClient.parse(not.data);
                        WebsocketTSClient.Utility.LogDebug("MasterSlavesGroupService", "Notify Master Collection Changed", master.toString());
                        _this.OnMasterCollectionChanged.Trigger(master);
                    }
                    // 收到广播消息
                    if (not.cmd === WebsocketTSClient.BackendOps.Cmd_Broadcast) {
                        _this.OnRecievedBroadcast.Trigger(not.data);
                    }
                }
            };
            WebsocketTSClient.WSBackend.singleton.OnStateChanged.AddListener(this.OnBackendStateChanged);
            WebsocketTSClient.WSBackend.singleton.OnNotify.AddListener(this.OnBackendNotify);
        }
        MasterSlavesGroupService.prototype.GetState = function () {
            return this.state;
        };
        MasterSlavesGroupService.prototype.RegisterAsListener = function () {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsListener, null, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    _this.OnRegisteredAsListener.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.UnregisterFromListener = function () {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromListener, null, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    _this.OnUnregisteredFromListener.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.RegisterAsMaster = function (masterName, masterData) {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                var data = {
                    masterName: masterName,
                    masterData: masterData
                };
                this.state = MasterSlavesGroupServiceState.IsMaster;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsMaster, data, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                    _this.OnRegisteredAsMaster.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.UnRegisterFromMaster = function () {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromMaster, null, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsMaster;
                    _this.OnUnregisteredFromMaster.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.RegisterAsSlave = function (masterId) {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                var data = {
                    masterId: masterId
                };
                this.state = MasterSlavesGroupServiceState.IsSlave;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_RegisterAsSlave, data, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.IsSlave : MasterSlavesGroupServiceState.Idle;
                    _this.OnRegisteredAsSlave.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.UnregisterFromSlave = function () {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_UnregisterFromSlave, null, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.state = errCode === WebsocketTSClient.ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsSlave;
                    _this.OnUnregisteredFromSlave.Trigger(errCode);
                });
            }
        };
        MasterSlavesGroupService.prototype.GetAllMasters = function () {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_GetAllMasters, null, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    var result = [];
                    var jarr = data["masters"];
                    if (jarr !== null) {
                        jarr.forEach(function (jobj) {
                            var mc = MasterClient.parse(jobj);
                            if (mc !== null) {
                                result.push(mc);
                            }
                        });
                    }
                    _this.OnGetAllMasters.Trigger(errCode, result);
                });
            }
        };
        MasterSlavesGroupService.prototype.Broadcast = function (data) {
            var _this = this;
            if (!this.isQuarying && WebsocketTSClient.WSBackend.singleton.IsConnected) {
                this.isQuarying = true;
                WebsocketTSClient.BackendRequest.CreateRetry(MasterSlavesGroupService.serviceName, WebsocketTSClient.BackendOps.Cmd_Broadcast, data, null, 
                // resp
                function (errCode, data, context) {
                    _this.isQuarying = false;
                    _this.OnBroadcast.Trigger(errCode);
                });
            }
        };
        /** 服务名称 */
        MasterSlavesGroupService.serviceName = "MasterSlavesGroupService";
        return MasterSlavesGroupService;
    }());
    WebsocketTSClient.MasterSlavesGroupService = MasterSlavesGroupService;
})(WebsocketTSClient || (WebsocketTSClient = {}));
