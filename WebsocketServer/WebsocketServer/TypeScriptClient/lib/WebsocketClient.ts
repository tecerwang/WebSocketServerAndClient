namespace WebsocketTSClient
{
    export class WebSocketClient
    {
        private socket: WebSocket;
        private reconnectInterval: number = 2000; // 断线重连时间间隔
        private reconnectTimer: any; // 断线重连及时器

        constructor(private url: string, private clientId: string)
        { 
        }

        public Connect(): void {
            this.socket = new WebSocket(this.url + "?clientId=" + this.clientId);

            this.socket.onopen = (event: Event) => {
                Utility.LogDebug('[WebSocketClient] connected');
                this.ClearReconnectTimer(); // 连接时清除计时器
                this.OnStateChanged.forEach(handler => { handler(true) });
            };

            this.socket.onmessage = (event: MessageEvent) => {
                Utility.LogDebug('[WebSocketClient] Message received:', event.data);
                this.OnMessageReceived.forEach(handler => { handler(event.data) });
            };

            this.socket.onclose = (event: CloseEvent) => {
                Utility.LogDebug('[WebSocketClient] connection broken, reconnect start');
                this.ScheduleReconnect(); // 连接断开，开启断线重连
                this.OnStateChanged.forEach(handler => { handler(false) });
            };

            this.socket.onerror = (error: Event) => {
                console.error('[WebSocketClient] error:', error, "reconnect start");
                this.ScheduleReconnect(); // 连接错误，开启断线重连
                this.OnStateChanged.forEach(handler => { handler(false) });
            };
        }

        private ScheduleReconnect(): void {
            // Clear existing reconnect timer to avoid multiple timers
            this.ClearReconnectTimer();
            // Schedule reconnection after the specified interval
            this.reconnectTimer = setTimeout(() => {
                Utility.LogDebug('[WebSocketClient] reconnecting...');
                this.Connect();
            }, this.reconnectInterval);
        }

        private ClearReconnectTimer(): void {
            // Clear the reconnect timer if it's set
            if (this.reconnectTimer) {
                clearTimeout(this.reconnectTimer);
                this.reconnectTimer = null;
            }
        }

        /**
         * ws 连接状态变化
         */
        private OnStateChanged: ((state: boolean) => void)[] = [];

        /**
         * ws 连接状态变化，添加事件
         */
        AddStateChangedHandler(handler : (state : boolean) => void): void
        {
            this.OnStateChanged.push(handler);
        }
        
        /**
         * ws 连接状态变化，移除事件
         */
        RmStateChangedHandler(handler: (state: boolean) => void) {
            const index = this.OnStateChanged.indexOf(handler);
            if (index !== -1) {
                this.OnStateChanged.splice(index, 1);
            }
        }

        /**
         * ws 收到信息
         */
        private OnMessageReceived: ((message: string) => void)[] = [];

        /**
         * 添加"ws 收到信息"事件
         */
        AddMessageReceivedHandler(handler: (msg: string) => void): void {
            this.OnMessageReceived.push(handler);
        }

        /**
         * 移除"ws 收到信息"事件
         */
        RmMessageReceivedHandler(handler: (msg: string) => void) {
            const index = this.OnMessageReceived.indexOf(handler);
            if (index !== -1) {
                this.OnMessageReceived.splice(index, 1);
            }
        }

        /**
         * ws 是否正在连接中
         */
        public IsConnected(): boolean
        {
            return this.socket.readyState == this.socket.OPEN;
        }        

        /**
        * 发送消息
        */
        SendMsg(message: string) {
            this.socket.send(message);
        }

        /**
         * 关闭连接
         */
        Close() {
            this.socket.close();        
        }
    }
}