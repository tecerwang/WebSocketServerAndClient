namespace HTMLClient
{
    var Utility = WebsocketTSClient.Utility; 
    var WSBackend = WebsocketTSClient.WSBackend;

    /// 页面负责交互逻辑的部分
    export class MonitorBackend
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

            Utility.LogDebug("[MonitorBackend]", "Create singleton backend start");
            if (WSBackend.CreateSingleton(backendUrl))
            {
                Utility.LogDebug("[MonitorBackend]", "Create singleton backend end");

                this.wsBackend = WebsocketTSClient.WSBackend.singleton;
                // await/async 异步等待服务器连接完成
                Utility.LogDebug("[MonitorBackend]", "Connect to server start");
                await this.wsBackend.Connect2Server();
                Utility.LogDebug("[MonitorBackend]", "Connect to server end");


                // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                this.service = new WebsocketTSClient.MasterSlavesGroupService();

                // 设置事件

                // 注册 Listener 用于监听
                this.service.RegisterAsListener();

               
            }
            else
            {
                Utility.LogDebug("[MonitorBackend]", "singleton backend already created");
            }
        }
    }
}