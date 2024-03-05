var WebsocketTSClient;
(function (WebsocketTSClient) {
    class WebSocketClient {
        constructor(url, clientId) {
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
        Connect() {
            this.socket = new WebSocket(this.url + "?clientId=" + this.clientId);
            this.socket.onopen = (event) => {
                this._isConnectionOpen = true;
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connected');
                this.ClearReconnectTimer(); // 连接时清除计时器
                this.OnStateChanged.Trigger(true);
            };
            this.socket.onmessage = (event) => {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] Message received:', event.data);
                this.OnMessageReceived.Trigger(event.data);
            };
            this.socket.onclose = (event) => {
                this._isConnectionOpen = false;
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] connection broken, reconnect start');
                this.ScheduleReconnect(); // 连接断开，开启断线重连
                this.OnStateChanged.Trigger(false);
            };
            this.socket.onerror = (error) => {
                this._isConnectionOpen = true;
                console.error('[WebSocketClient] error:', error, "reconnect start");
                this.ScheduleReconnect(); // 连接错误，开启断线重连
                this.OnStateChanged.Trigger(false);
            };
        }
        ScheduleReconnect() {
            // Clear existing reconnect timer to avoid multiple timers
            this.ClearReconnectTimer();
            // Schedule reconnection after the specified interval
            this.reconnectTimer = setTimeout(() => {
                WebsocketTSClient.Utility.LogDebug('[WebSocketClient] reconnecting...');
                this.Connect();
            }, this.reconnectInterval);
        }
        ClearReconnectTimer() {
            // Clear the reconnect timer if it's set
            if (this.reconnectTimer) {
                clearTimeout(this.reconnectTimer);
                this.reconnectTimer = null;
            }
        }
        /**
         * ws 是否正在连接中
         */
        IsConnected() {
            return this._isConnectionOpen;
        }
        /**
        * 发送消息
        */
        SendMsg(message) {
            this.socket.send(message);
        }
        /**
         * 关闭连接
         */
        Close() {
            this.socket.close();
        }
    }
    WebsocketTSClient.WebSocketClient = WebSocketClient;
})(WebsocketTSClient || (WebsocketTSClient = {}));
