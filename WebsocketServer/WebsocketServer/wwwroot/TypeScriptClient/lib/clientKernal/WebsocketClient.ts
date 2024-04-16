namespace WebsocketTSClient
{
    export class WebSocketClient
    {
        private socket: WebSocket;
        private reconnectInterval: number = 2000; // 断线重连时间间隔
        private reconnectTimer: any; // 断线重连及时器
        private _isConnectionOpen: boolean = false;

        /**
      * ws 连接状态变化
      */
        public OnStateChanged: EventHandler<[boolean]> = new EventHandler<[boolean]>();

        /**
         * ws 收到信息
         */
        public OnMessageReceived: EventHandler<[string]> = new EventHandler<[string]>();

        constructor(private url: string, private clientId: string)
        {
        }

        public Connect(): void
        {
            this.socket = new WebSocket(this.url + "?clientId=" + this.clientId);

            this.socket.onopen = (event: Event) =>
            {
                this._isConnectionOpen = true;
                Utility.LogDebug('[WebSocketClient] connected');
                this.ClearReconnectTimer(); // 连接时清除计时器
                this.OnStateChanged.Trigger(true);
            };

            this.socket.onmessage = (event: MessageEvent) =>
            {
                Utility.LogDebug('[WebSocketClient] Message received:', event.data);
                this.OnMessageReceived.Trigger(event.data);
            };

            this.socket.onclose = (event: CloseEvent) =>
            {
                this._isConnectionOpen = false;
                Utility.LogDebug('[WebSocketClient] connection broken, reconnect start');
                this.ScheduleReconnect(); // 连接断开，开启断线重连
                this.OnStateChanged.Trigger(false);
            };

            this.socket.onerror = (error: Event) =>
            {
                this._isConnectionOpen = true;
                console.error('[WebSocketClient] error:', error, "reconnect start");
                this.ScheduleReconnect(); // 连接错误，开启断线重连
                this.OnStateChanged.Trigger(false);
            };
        }

        private ScheduleReconnect(): void
        {
            // Clear existing reconnect timer to avoid multiple timers
            this.ClearReconnectTimer();
            // Schedule reconnection after the specified interval
            this.reconnectTimer = setTimeout(() =>
            {
                Utility.LogDebug('[WebSocketClient] reconnecting...');
                this.Connect();
            }, this.reconnectInterval);
        }

        private ClearReconnectTimer(): void
        {
            // Clear the reconnect timer if it's set
            if (this.reconnectTimer)
            {
                clearTimeout(this.reconnectTimer);
                this.reconnectTimer = null;
            }
        }

        /**
         * ws 是否正在连接中
         */
        public IsConnected(): boolean
        {
            return this._isConnectionOpen;
        }

        /**
        * 发送消息
        */
        SendMsg(message: string)
        {
            this.socket.send(message);
        }

        /**
         * 关闭连接
         */
        Close()
        {
            this.socket.close();
        }
    }
}