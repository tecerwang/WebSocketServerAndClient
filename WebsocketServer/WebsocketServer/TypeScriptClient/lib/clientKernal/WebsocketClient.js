var WebsocketTSClient;
(function (WebsocketTSClient) {
    var WebSocketClient = /** @class */ (function () {
        function WebSocketClient(url, clientId) {
            this.url = url;
            this.clientId = clientId;
            this.reconnectInterval = 2000; // 断线重连时间间隔
            this._isConnectionOpen = false;
            /**
          * ws 连接状态变化
          */
            this.OnStateChanged = new WebsocketTSClient.EventHandler();
            /**
             * ws 收到信息
             */
            this.OnMessageReceived = new WebsocketTSClient.EventHandler();
        }
        WebSocketClient.prototype.Connect = function () {
            var _this = this;
            this.socket = new WebSocket(this.url + "?clientId=" + this.clientId);
            this.socket.onopen = function (event) {
                _this._isConnectionOpen = true;
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connected');
                _this.ClearReconnectTimer(); // 连接时清除计时器
                _this.OnStateChanged.Trigger(true);
            };
            this.socket.onmessage = function (event) {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] Message received:', event.data);
                _this.OnMessageReceived.Trigger(event.data);
            };
            this.socket.onclose = function (event) {
                _this._isConnectionOpen = false;
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connection broken, reconnect start');
                _this.ScheduleReconnect(); // 连接断开，开启断线重连
                _this.OnStateChanged.Trigger(false);
            };
            this.socket.onerror = function (error) {
                _this._isConnectionOpen = true;
                console.error('[WebSocketClient] error:', error, "reconnect start");
                _this.ScheduleReconnect(); // 连接错误，开启断线重连
                _this.OnStateChanged.Trigger(false);
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
         * ws 是否正在连接中
         */
        WebSocketClient.prototype.IsConnected = function () {
            return this._isConnectionOpen;
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
