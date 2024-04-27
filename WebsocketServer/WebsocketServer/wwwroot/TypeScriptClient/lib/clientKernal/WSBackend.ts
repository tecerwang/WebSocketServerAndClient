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
        private _uid: string;

        /** ws 连接状态变化 */
        public OnStateChanged: EventHandler<[boolean]> = new EventHandler<[boolean]>();

        /** ws 收到 Response */
        public OnResponse: EventHandler<[ResponsePack]> = new EventHandler<[ResponsePack]>();

        /** ws 收到 Notify */
        public OnNotify: EventHandler<[NotifyPack]> = new EventHandler<[NotifyPack]>();

        private constructor(private _backendUrl: string)
        {
            this._uid = Utility.GenerateUniqueId();          
            this._wsClient = new WebSocketClient(this._backendUrl, this._uid);
            this._wsClient.OnStateChanged.AddListener(this.OnWSClientStateChanged);
            this._wsClient.OnMessageReceived.AddListener(this.OnWSClientMsgRecieved);            
        }

        public static CreateSingleton(backendUrl: string): boolean {
            if (!WSBackend.singleton) {
                WSBackend.singleton = new WSBackend(backendUrl);
                WSBackend.singleton._monitor = ConnMonitor.Create(WSBackend.singleton);
                WSBackend.singleton._monitor.Init();
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
            if (!this._isInited) 
            {
                this._wsClient.Connect();
                const promise = new Promise<void>((resolve, reject) => {
                    const handler = (state: boolean) => {
                        if (state) {
                            Utility.LogDebug("[WSBackend]", "Connect2Server Promise resolved");
                            this._wsClient.OnStateChanged.RmListener(handler);
                            resolve();
                        }
                        // reject 不会使用，因为client会自动断线重连
                    };
                    this._wsClient.OnStateChanged.AddListener(handler);
                });

                this._isInited = true;
                return promise; // Return the promise to be awaited
            } else {
                Utility.LogDebug("[WSBackend]", "Already inited");
                return Promise.resolve(); // Return a resolved promise if already initialized
            }
        }     

        public async RetryConnect(): Promise<void>
        {
            // disconnect first and reconnect to server again,
            this._wsClient.Close();
            await this.Connect2Server();
        }

        private OnWSClientStateChanged = (state: boolean): void => {
            this.OnStateChanged.Trigger(state);
        }

        private OnWSClientMsgRecieved = (msg: string): void => {
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
        }

        /**
         * 创建一个请求
         * @param serviceName
         * @param cmd
         * @param data
         * @returns
         */
        public CreateBackendRequest(serviceName: string, cmd: string, data: any): number {
            if (this._wsClient == null || !this._wsClient.IsConnected) {
                return -1;
            }
     
            var pack = new RequestPack(this._uid);
            pack.serviceName = serviceName;
            pack.cmd = cmd;
            pack.data = data;
            const sended = this._wsClient.SendMsg(JSON.stringify(pack).replace(/[\r\n\s]/g, ""));
            return sended ? pack.rid : -1;
        }             
    }
}