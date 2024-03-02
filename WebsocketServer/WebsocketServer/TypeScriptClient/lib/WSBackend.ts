namespace WebsocketTSClient
{
    /**
     * 封装一层 backend
     */
    export class WSBackend {
       
        public static singleton: WSBackend;

        private _isInited: boolean = false;
        private _monitor: ConnMonitor;
        private _wsClient: WebSocketClient;

        private constructor(private _backendUrl: string)
        {
            var uid = Utility.GenerateUniqueId();
            this._wsClient = new WebSocketClient(this._backendUrl, uid);
            this._wsClient.AddStateChangedHandler(this.OnWSClientStateChanged);
            this._wsClient.AddMessageReceivedHandler(this.OnWSClientMsgRecieved);
        }

        public static CreateSingleton(backendUrl: string): boolean {
            if (!WSBackend.singleton) {
                WSBackend.singleton = new WSBackend(backendUrl);
                return true;
            }
            return false;
        }

        public IsConnected(): boolean {
            if (this._wsClient == null) {
                return false;
            }
            return this._wsClient.IsConnected();
        }

        public async Connect2Server(): Promise<void> {
            if (!this._isInited) {
                this._wsClient.Connect();
                const promise = new Promise<void>((resolve, reject) => {
                    const handler = (state: boolean) => {
                        if (state) {
                            Utility.LogDebug("[WSBackend]", "Connect2Server Promise resolved");
                            this._wsClient.RmStateChangedHandler(handler);
                            resolve();
                        }
                        // reject 不会使用，因为client会自动断线重连
                    };
                    this._wsClient.AddStateChangedHandler(handler);
                });

                this._isInited = true;
                return promise; // Return the promise to be awaited
            } else {
                Utility.LogDebug("[WSBackend]", "Already inited");
                return Promise.resolve(); // Return a resolved promise if already initialized
            }
        }     

        private OnWSClientStateChanged(state: boolean): void {
            this.OnStateChanged.forEach(handler => { handler(state) });
        }

        private OnWSClientMsgRecieved(msg: string): void {
            var jobj = JSON.parse(msg);
            if (jobj != null) {
                if (jobj.type == "response")
                {
                    // 收到response
                    this.OnResponse.forEach(handler => { handler(jobj) });
                }
                else if (jobj.type == "notify")
                {
                    // 收到notify
                    this.OnNotify.forEach(handler => { handler(jobj) });
                }
            }           
        }

        //public CreateBackendRequest(serviceName: string, cmd: string, data: any): WebSocketClientProxy.ProxyResult | null {
        //    if (!this._wsClientProxy || this._wsClientProxy.State !== WebSocketClientProxy.ClientState.open) {
        //        return null;
        //    }
        //    return this._wsClientProxy.SendRequest(serviceName, cmd, data);
        //}

        /**
          * ws 连接状态变化
          */
        private OnStateChanged: ((state: boolean) => void)[] = [];

        /**
         * ws 连接状态变化，添加事件
         */
        AddStateChangedHandler(handler: (state: boolean) => void): void {
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
         * ws 收到 Response
         */
        private OnResponse: ((message: ResponsePack) => void)[] = [];

        /**
         * add recieved Response event
         */
        AddResponseHandler(handler: (msg: ResponsePack) => void): void {
            this.OnResponse.push(handler);
        }

        /**
         * remove recieved Response event
         */
        RmResponseHandler(handler: (msg: ResponsePack) => void) {
            const index = this.OnResponse.indexOf(handler);
            if (index !== -1) {
                this.OnResponse.splice(index, 1);
            }
        }

        /**
        * ws 收到 Notify
        */
        private OnNotify: ((message: NotifyPack) => void)[] = [];

        /**
         * add recieved Notify event
         */
        AddNotifyHandler(handler: (msg: NotifyPack) => void): void {
            this.OnNotify.push(handler);
        }

        /**
         * remove recieved Notify event
         */
        RmNotifyHandler(handler: (msg: NotifyPack) => void) {
            const index = this.OnResponse.indexOf(handler);
            if (index !== -1) {
                this.OnNotify.splice(index, 1);
            }
        }
    }
}