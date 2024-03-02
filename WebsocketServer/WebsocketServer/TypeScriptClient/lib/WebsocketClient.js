var WebsocketTSClient;
(function (WebsocketTSClient) {
    var WebSocketClient = /** @class */ (function () {
        function WebSocketClient(url, clientId) {
            this.url = url;
            this.clientId = clientId;
            this.reconnectInterval = 2000; // 断线重连时间间隔
            /**
             * ws 连接状态变化
             */
            this.OnStateChanged = [];
            /**
             * ws 收到信息
             */
            this.OnMessageReceived = [];
        }
        WebSocketClient.prototype.Connect = function () {
            var _this = this;
            this.socket = new WebSocket(this.url + "?clientId=" + this.clientId);
            this.socket.onopen = function (event) {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connected');
                _this.ClearReconnectTimer(); // 连接时清除计时器
                _this.OnStateChanged.forEach(function (handler) { handler(true); });
            };
            this.socket.onmessage = function (event) {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] Message received:', event.data);
                _this.OnMessageReceived.forEach(function (handler) { handler(event.data); });
            };
            this.socket.onclose = function (event) {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connection broken, reconnect start');
                _this.ScheduleReconnect(); // 连接断开，开启断线重连
                _this.OnStateChanged.forEach(function (handler) { handler(false); });
            };
            this.socket.onerror = function (error) {
                console.error('[WebSocketClient] error:', error, "reconnect start");
                _this.ScheduleReconnect(); // 连接错误，开启断线重连
                _this.OnStateChanged.forEach(function (handler) { handler(false); });
            };
        };
        WebSocketClient.prototype.ScheduleReconnect = function () {
            var _this = this;
            // Clear existing reconnect timer to avoid multiple timers
            this.ClearReconnectTimer();
            // Schedule reconnection after the specified interval
            this.reconnectTimer = setTimeout(function () {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] reconnecting...');
                _this.Connect();
            }, this.reconnectInterval);
        };
        WebSocketClient.prototype.ClearReconnectTimer = function () {
            // Clear the reconnect timer if it's set
            if (this.reconnectTimer) {
                clearTimeout(this.reconnectTimer);
                this.reconnectTimer = null;
            }
        };
        /**
         * ws 连接状态变化，添加事件
         */
        WebSocketClient.prototype.AddStateChangedHandler = function (handler) {
            this.OnStateChanged.push(handler);
        };
        /**
         * ws 连接状态变化，移除事件
         */
        WebSocketClient.prototype.RmStateChangedHandler = function (handler) {
            var index = this.OnStateChanged.indexOf(handler);
            if (index !== -1) {
                this.OnStateChanged.splice(index, 1);
            }
        };
        /**
         * 添加"ws 收到信息"事件
         */
        WebSocketClient.prototype.AddMessageReceivedHandler = function (handler) {
            this.OnMessageReceived.push(handler);
        };
        /**
         * 移除"ws 收到信息"事件
         */
        WebSocketClient.prototype.RmMessageReceivedHandler = function (handler) {
            var index = this.OnMessageReceived.indexOf(handler);
            if (index !== -1) {
                this.OnMessageReceived.splice(index, 1);
            }
        };
        /**
         * ws 是否正在连接中
         */
        WebSocketClient.prototype.IsConnected = function () {
            return this.socket.readyState == this.socket.OPEN;
        };
        /**
        * 发送消息
        */
        WebSocketClient.prototype.SendMsg = function (message) {
            this.socket.send(message);
        };
        /**
         * 关闭连接
         */
        WebSocketClient.prototype.Close = function () {
            this.socket.close();
        };
        return WebSocketClient;
    }());
    WebsocketTSClient.WebSocketClient = WebSocketClient;
})(WebsocketTSClient || (WebsocketTSClient = {}));
//# sourceMappingURL=WebsocketClient.js.map