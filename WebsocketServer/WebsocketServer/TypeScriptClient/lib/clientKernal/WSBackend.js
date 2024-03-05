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
    /**
     * 封装一层 backend
     */
    class WSBackend {
        constructor(_backendUrl) {
            this._backendUrl = _backendUrl;
            this._isInited = false;
            /** ws 连接状态变化 */
            this.OnStateChanged = new WebsocketTSClient.EventHandler();
            /** ws 收到 Response */
            this.OnResponse = new WebsocketTSClient.EventHandler();
            /** ws 收到 Notify */
            this.OnNotify = new WebsocketTSClient.EventHandler();
            this.OnWSClientStateChanged = (state) => {
                this.OnStateChanged.Trigger(state);
            };
            this.OnWSClientMsgRecieved = (msg) => {
                var jobj = JSON.parse(msg);
                if (jobj != null) {
                    if (jobj.type == "response") {
                        if (this.OnResponse != null) {
                            // 收到response
                            this.OnResponse.Trigger(jobj);
                        }
                    }
                    else if (jobj.type == "notify") {
                        if (this.OnNotify != null) {
                            // 收到notify
                            this.OnNotify.Trigger(jobj);
                        }
                    }
                }
            };
            this._uid = WebsocketTSClient.Utility.GenerateUniqueId();
            this._wsClient = new WebsocketTSClient.WebSocketClient(this._backendUrl, this._uid);
            this._wsClient.OnStateChanged.AddListener(this.OnWSClientStateChanged);
            this._wsClient.OnMessageReceived.AddListener(this.OnWSClientMsgRecieved);
        }
        static CreateSingleton(backendUrl) {
            if (!WSBackend.singleton) {
                WSBackend.singleton = new WSBackend(backendUrl);
                return true;
            }
            return false;
        }
        IsConnected() {
            if (this._wsClient == null) {
                return false;
            }
            return this._wsClient.IsConnected();
        }
        Connect2Server() {
            return __awaiter(this, void 0, void 0, function* () {
                if (!this._isInited) {
                    this._wsClient.Connect();
                    const promise = new Promise((resolve, reject) => {
                        const handler = (state) => {
                            if (state) {
                                WebsocketTSClient.Utility.LogDebug("[WSBackend]", "Connect2Server Promise resolved");
                                this._wsClient.OnStateChanged.RmListener(handler);
                                resolve();
                            }
                            // reject 不会使用，因为client会自动断线重连
                        };
                        this._wsClient.OnStateChanged.AddListener(handler);
                    });
                    this._isInited = true;
                    return promise; // Return the promise to be awaited
                }
                else {
                    WebsocketTSClient.Utility.LogDebug("[WSBackend]", "Already inited");
                    return Promise.resolve(); // Return a resolved promise if already initialized
                }
            });
        }
        /**
         * 创建一个请求
         * @param serviceName
         * @param cmd
         * @param data
         * @returns
         */
        CreateBackendRequest(serviceName, cmd, data) {
            if (this._wsClient == null || !this._wsClient.IsConnected) {
                return -1;
            }
            var pack = new WebsocketTSClient.RequestPack(this._uid);
            pack.serviceName = serviceName;
            pack.cmd = cmd;
            pack.data = data;
            this._wsClient.SendMsg(JSON.stringify(pack).replace(/[\r\n\s]/g, ""));
            return pack.rid;
        }
    }
    WebsocketTSClient.WSBackend = WSBackend;
})(WebsocketTSClient || (WebsocketTSClient = {}));
