var WebsocketTSClient;
(function (WebsocketTSClient) {
    var BackendRequest = /** @class */ (function () {
        function BackendRequest() {
            var _this = this;
            /// <summary>
            /// 默认为 -1，代表未发起有效请求
            /// </summary>
            this._rid = -1;
            this._retryTimes = 0;
            this._retryCount = 0;
            this.Singleton_OnBackendStateChanged = function (state) {
                if (_this._rid < 0 && state) {
                    _this.Request(_this._requestContext);
                }
            };
            this.Singleton_OnBackendResponse = function (resp) {
                var _a, _b;
                if (_this._rid > 0 && resp.rid === _this._rid) {
                    (_a = _this._responseDel) === null || _a === void 0 ? void 0 : _a.call(_this, resp.errCode, resp.data, (_b = _this._requestContext) === null || _b === void 0 ? void 0 : _b.context);
                    _this.Release();
                }
            };
            WebsocketTSClient.WSBackend.singleton.OnStateChanged.AddListener(this.Singleton_OnBackendStateChanged);
            WebsocketTSClient.WSBackend.singleton.OnResponse.AddListener(this.Singleton_OnBackendResponse);
        }
        BackendRequest.prototype.Request = function (context) {
            var _a, _b;
            if (this._retryTimes < 0 || this._retryCount < this._retryTimes) {
                var rid = WebsocketTSClient.WSBackend.singleton.CreateBackendRequest(context.serviceName, context.cmd, context.data);
                this._retryCount++;
                this._rid = rid !== null && rid !== void 0 ? rid : -1;
            }
            else {
                (_a = this._responseDel) === null || _a === void 0 ? void 0 : _a.call(this, WebsocketTSClient.ErrCode.Internal_RetryTimesOut, null, (_b = this._requestContext) === null || _b === void 0 ? void 0 : _b.context);
                this.Release();
            }
        };
        BackendRequest.prototype.Release = function () {
            WebsocketTSClient.WSBackend.singleton.OnStateChanged.RmListener(this.Singleton_OnBackendStateChanged);
            WebsocketTSClient.WSBackend.singleton.OnResponse.RmListener(this.Singleton_OnBackendResponse);
        };
        BackendRequest.CreateRetry = function (serviceName, cmd, data, context, onResponse, retryTimes) {
            if (retryTimes === void 0) { retryTimes = -1; }
            if (WebsocketTSClient.WSBackend.singleton != null && WebsocketTSClient.WSBackend.singleton != undefined && WebsocketTSClient.WSBackend.singleton.IsConnected()) {
                var request = new BackendRequest();
                request._responseDel = onResponse;
                request._retryTimes = retryTimes;
                request._requestContext = {
                    serviceName: serviceName,
                    cmd: cmd,
                    data: data,
                    context: context
                };
                try {
                    request.Request(request._requestContext);
                    return true;
                }
                catch (ex) {
                    // 如果出错需要释放掉这个 request
                    request.Release();
                    WebsocketTSClient.Utility.LogDebug(ex);
                }
            }
            onResponse === null || onResponse === void 0 ? void 0 : onResponse(WebsocketTSClient.ErrCode.Internal_RetryTimesOut, null, context);
            return false;
        };
        return BackendRequest;
    }());
    WebsocketTSClient.BackendRequest = BackendRequest;
})(WebsocketTSClient || (WebsocketTSClient = {}));
