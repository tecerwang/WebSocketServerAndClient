namespace HTMLClient
{
    var Utility = WebsocketTSClient.Utility; 
    var WSBackend = WebsocketTSClient.WSBackend;

    export class MonitorPage
    {
        private wsBackend: WebsocketTSClient.WSBackend = null;
        private service: WebsocketTSClient.MasterSlavesGroupService = null;

        constructor()
        {
            this.init();
        }

        async init()
        {
            const backendUrl = 'ws://localhost:8080/ws';

            Utility.LogDebug("[HTML]", "Create singleton backend start");
            if (WSBackend.CreateSingleton(backendUrl))
            {
                Utility.LogDebug("[HTML]", "Create singleton backend end");

                this.wsBackend = WebsocketTSClient.WSBackend.singleton;
                // await/async 异步等待服务器连接完成
                Utility.LogDebug("[HTML]", "Connect to server start");
                await this.wsBackend.Connect2Server();
                Utility.LogDebug("[HTML]", "Connect to server end");


                // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                this.service = new WebsocketTSClient.MasterSlavesGroupService();
                this.service.RegisterAsMaster("name", null);
            }
            else
            {
                Utility.LogDebug("[HTML]", "singleton backend already created");
            }
        }
    }
}