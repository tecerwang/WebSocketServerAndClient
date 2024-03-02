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
    /**
     * 封装一层 backend
     */
    var WSBackend = /** @class */ (function () {
        function WSBackend(_backendUrl) {
            this._backendUrl = _backendUrl;
            this._isInited = false;
            //public CreateBackendRequest(serviceName: string, cmd: string, data: any): WebSocketClientProxy.ProxyResult | null {
            //    if (!this._wsClientProxy || this._wsClientProxy.State !== WebSocketClientProxy.ClientState.open) {
            //        return null;
            //    }
            //    return this._wsClientProxy.SendRequest(serviceName, cmd, data);
            //}
            /**
              * ws 连接状态变化
              */
            this.OnStateChanged = [];
            /**
             * ws 收到 Response
             */
            this.OnResponse = [];
            /**
            * ws 收到 Notify
            */
            this.OnNotify = [];
            var uid = WebsocketTSClient.Utility.GenerateUniqueId();
            this._wsClient = new WebsocketTSClient.WebSocketClient(this._backendUrl, uid);
            this._wsClient.AddStateChangedHandler(this.OnWSClientStateChanged);
            this._wsClient.AddMessageReceivedHandler(this.OnWSClientMsgRecieved);
        }
        WSBackend.CreateSingleton = function (backendUrl) {
            if (!WSBackend.singleton) {
                WSBackend.singleton = new WSBackend(backendUrl);
                return true;
            }
            return false;
        };
        WSBackend.prototype.IsConnected = function () {
            if (this._wsClient == null) {
                return false;
            }
            return this._wsClient.IsConnected();
        };
        WSBackend.prototype.Connect2Server = function () {
            return __awaiter(this, void 0, void 0, function () {
                var promise;
                var _this = this;
                return __generator(this, function (_a) {
                    if (!this._isInited) {
                        this._wsClient.Connect();
                        promise = new Promise(function (resolve, reject) {
                            var handler = function (state) {
                                if (state) {
                                    WebsocketTSClient.Utility.LogDebug("[WSBackend]", "Connect2Server Promise resolved");
                                    _this._wsClient.RmStateChangedHandler(handler);
                                    resolve();
                                }
                                // reject 不会使用，因为client会自动断线重连
                            };
                            _this._wsClient.AddStateChangedHandler(handler);
                        });
                        this._isInited = true;
                        return [2 /*return*/, promise]; // Return the promise to be awaited
                    }
                    else {
                        WebsocketTSClient.Utility.LogDebug("[WSBackend]", "Already inited");
                        return [2 /*return*/, Promise.resolve()]; // Return a resolved promise if already initialized
                    }
                    return [2 /*return*/];
                });
            });
        };
        WSBackend.prototype.OnWSClientStateChanged = function (state) {
            this.OnStateChanged.forEach(function (handler) { handler(state); });
        };
        WSBackend.prototype.OnWSClientMsgRecieved = function (msg) {
            var jobj = JSON.parse(msg);
            if (jobj != null) {
                if (jobj.type == "response") {
                    // 收到response
                    this.OnResponse.forEach(function (handler) { handler(jobj); });
                }
                else if (jobj.type == "notify") {
                    // 收到notify
                    this.OnNotify.forEach(function (handler) { handler(jobj); });
                }
            }
        };
        /**
         * ws 连接状态变化，添加事件
         */
        WSBackend.prototype.AddStateChangedHandler = function (handler) {
            this.OnStateChanged.push(handler);
        };
        /**
         * ws 连接状态变化，移除事件
         */
        WSBackend.prototype.RmStateChangedHandler = function (handler) {
            var index = this.OnStateChanged.indexOf(handler);
            if (index !== -1) {
                this.OnStateChanged.splice(index, 1);
            }
        };
        /**
         * add recieved Response event
         */
        WSBackend.prototype.AddResponseHandler = function (handler) {
            this.OnResponse.push(handler);
        };
        /**
         * remove recieved Response event
         */
        WSBackend.prototype.RmResponseHandler = function (handler) {
            var index = this.OnResponse.indexOf(handler);
            if (index !== -1) {
                this.OnResponse.splice(index, 1);
            }
        };
        /**
         * add recieved Notify event
         */
        WSBackend.prototype.AddNotifyHandler = function (handler) {
            this.OnNotify.push(handler);
        };
        /**
         * remove recieved Notify event
         */
        WSBackend.prototype.RmNotifyHandler = function (handler) {
            var index = this.OnResponse.indexOf(handler);
            if (index !== -1) {
                this.OnNotify.splice(index, 1);
            }
        };
        return WSBackend;
    }());
    WebsocketTSClient.WSBackend = WSBackend;
})(WebsocketTSClient || (WebsocketTSClient = {}));
//# sourceMappingURL=WSBackend.js.map