namespace WebsocketTSClient
{
    /// send heart beat to server
    export class ConnMonitor
    {
        private static serviceName: string = "ConnMonitorService";
        private _backend: WSBackend;
        private static intervalMS: number = 5000;
        private _heartBeatCTS: any;

        public Init(): void
        {
            this._backend.OnStateChanged.AddListener(this.Singleton_OnBackendStateChanged);
            //this._backend.OnResponse.AddListener(this.Backend_OnBackendResponse);
            //this._backend.OnNotify.AddListener(this.Backend_OnBackendNotify);
        }

        public Release(): void
        {
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

        private StartNewHeartBeatTick(): void
        {
            if (!this._backend.IsConnected)
            {
                return;
            }
            Utility.LogDebug("[ConnMonitor]", "send WSPing");
            BackendRequest.CreateRetry(ConnMonitor.serviceName, "WSPing", null, null, (errCode: number, data: any, context: any) =>
            {
                if (errCode == ErrCode.OK)
                {
                    this._heartBeatCTS = setTimeout(() =>
                    {
                        this.StartNewHeartBeatTick();
                    }, ConnMonitor.intervalMS);
                }
                else
                {
                    // print error message
                    Utility.LogDebug("[ConnMonitor]", "send WSPing error " + errCode);
                }
            });
        }

        private Singleton_OnBackendStateChanged = (): void =>
        {
            if (this._backend.IsConnected)
            {          
                clearTimeout(this._heartBeatCTS);
                Utility.LogDebug("[ConnMonitor]", "heart beat monitor start");
                this.StartNewHeartBeatTick();
            }
            else
            {
                clearTimeout(this._heartBeatCTS);
                this._heartBeatCTS = null;
                Utility.LogDebug("[ConnMonitor]", "heart beat monitor cancel by ws close");
            }
        }

        public static Create(backend: WSBackend): ConnMonitor
        {
            const instance: ConnMonitor = new ConnMonitor();
            instance._backend = backend;
            return instance;
        }
    }
}