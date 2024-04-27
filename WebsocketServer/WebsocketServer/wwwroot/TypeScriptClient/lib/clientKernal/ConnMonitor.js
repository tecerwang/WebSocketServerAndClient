var WebsocketTSClient;
(function (WebsocketTSClient) {
    /// send heart beat to server
    class ConnMonitor {
        constructor() {
            this.Singleton_OnBackendStateChanged = () => {
                if (this._backend.IsConnected) {
                    clearTimeout(this._heartBeatCTS);
                    WebsocketTSClient.Utility.LogDebug("[ConnMonitor]", "heart beat monitor start");
                    this.StartNewHeartBeatTick();
                }
                else {
                    clearTimeout(this._heartBeatCTS);
                    this._heartBeatCTS = null;
                    WebsocketTSClient.Utility.LogDebug("[ConnMonitor]", "heart beat monitor cancel by ws close");
                }
            };
        }
        Init() {
            this._backend.OnStateChanged.AddListener(this.Singleton_OnBackendStateChanged);
            //this._backend.OnResponse.AddListener(this.Backend_OnBackendResponse);
            //this._backend.OnNotify.AddListener(this.Backend_OnBackendNotify);
        }
        Release() {
            this._backend.OnStateChanged.RmListener(this.Singleton_OnBackendStateChanged);
            //this._backend.OnResponse.RmListener(this.Backend_OnBackendResponse);
            //this._backend.OnNotify.RmListener(this.Backend_OnBackendNotify);
        }
        //private Backend_OnBackendResponse = (resp: ResponsePack): void =>
        //{
        //    this.StartNewHeartBeatTick();
        //}
        //private Backend_OnBackendNotify = (not: NotifyPack): void =>
        //{
        //    this.StartNewHeartBeatTick();
        //}
        StartNewHeartBeatTick() {
            if (!this._backend.IsConnected) {
                return;
            }
            WebsocketTSClient.Utility.LogDebug("[ConnMonitor]", "send WSPing");
            WebsocketTSClient.BackendRequest.CreateRetry(ConnMonitor.serviceName, "WSPing", null, null, (errCode, data, context) => {
                if (errCode == WebsocketTSClient.ErrCode.OK) {
                    this._heartBeatCTS = setTimeout(() => {
                        this.StartNewHeartBeatTick();
                    }, ConnMonitor.intervalMS);
                }
                else {
                    // print error message
                    WebsocketTSClient.Utility.LogDebug("[ConnMonitor]", "send WSPing error " + errCode);
                }
            });
        }
        static Create(backend) {
            const instance = new ConnMonitor();
            instance._backend = backend;
            return instance;
        }
    }
    ConnMonitor.serviceName = "ConnMonitorService";
    ConnMonitor.intervalMS = 5000;
    WebsocketTSClient.ConnMonitor = ConnMonitor;
})(WebsocketTSClient || (WebsocketTSClient = {}));
