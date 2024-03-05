namespace HTMLClient
{
    const Utility = WebsocketTSClient.Utility; 
    const ErrCode = WebsocketTSClient.ErrCode;

    type MasterClient = WebsocketTSClient.MasterClient;
    type MainPage = HTMLClient.MainPage;

    /// 页面负责交互逻辑的部分
    export class MainPageController
    {
        private wsBackend: WebsocketTSClient.WSBackend | null = null;
        private service: WebsocketTSClient.MasterSlavesGroupService | null = null;
        private mainPage: MainPage | null = null;
        /** 当前的masterId */
        private masterClientId: string | null = null;

        constructor()
        {
            customElements.define('main-page', MainPage);
            this.mainPage = document.querySelector('main-page') as MainPage;
            this.mainPage.OnMasterBtnClick.AddListener((master, btn) =>
            {
                if (this.service != null)
                {
                    this.service.RegisterAsSlave(master);
                }
            });
            this.mainPage.OnBack2Main.AddListener(() =>
            {
                if (this.masterClientId !== null)
                {
                    this.service.UnregisterFromSlave();
                }
            });

            this.mainPage.ResetMasterBtns([
                new WebsocketTSClient.MasterClient("1", "name1", true),
                new WebsocketTSClient.MasterClient("2", "name2", true),
                new WebsocketTSClient.MasterClient("3", "name3", true),
                new WebsocketTSClient.MasterClient("4", "name4", true),
            ]);

            this.init();       
        }

        async init()
        {
            const backendUrl = 'ws://localhost:8080/ws';

            Utility.LogDebug("[MonitorManager]", "Create singleton backend start");
            if (WebsocketTSClient.WSBackend.CreateSingleton(backendUrl))
            {
                Utility.LogDebug("[MonitorManager]", "Create singleton backend end");

                this.wsBackend = WebsocketTSClient.WSBackend.singleton;
                // await/async 异步等待服务器连接完成
                Utility.LogDebug("[MonitorManager]", "Connect to server start");
                await this.wsBackend.Connect2Server();
                Utility.LogDebug("[MonitorManager]", "Connect to server end");


                // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
                this.service = new WebsocketTSClient.MasterSlavesGroupService();

                // 设置事件，只设置几个用到的事件
                this.service.OnRegisteredAsListener.AddListener((errCode) => this.handleRegisteredAsListener(errCode));
                this.service.OnRegisteredAsSlave.AddListener((errCode, master, data) => this.handleRegisteredAsSlave(errCode, master, data));
                this.service.OnUnregisteredFromSlave.AddListener((errCode) => this.handleUnregisteredFromSlave(errCode));
                this.service.OnMasterCollectionChanged.AddListener((master) => this.handleMasterCollectionChanged(master));
                this.service.OnGetAllMasters.AddListener((errCode, masters) => this.handleGetAllMasters(errCode, masters));
                this.service.OnBroadcast.AddListener((errCode) => this.handleBroadcast(errCode));
                this.service.OnRecievedBroadcast.AddListener((data) => this.handleReceivedBroadcast(data));

                this.serviceStartup(0);
               
            }
            else
            {
                Utility.LogDebug("[MonitorManager]", "singleton backend already created");
            }
        }

        /** service 启动步骤 */
        private serviceStartupStep: number = 0;

        private async serviceStartup(targetStep : number)
        {
            if (this.serviceStartupStep === targetStep)
            {
                await Utility.delay(2000);
            }
            this.serviceStartupStep = targetStep;

            if (this.serviceStartupStep === 0)
            {
                // 注册 Listener 用于监听
                this.service.RegisterAsListener();
                return;
            }

            if (this.serviceStartupStep === 1)
            {
                // 获取主界面按键信息
                this.service.GetAllMasters();
                return;
            }
        }

        private handleRegisteredAsListener(errCode: number) : void
        {
            Utility.LogDebug("[MonitorManager]", "RegisteredAsListener", errCode);
            var targetStep = this.serviceStartupStep;
            if (errCode === ErrCode.OK)
            {
                targetStep++;
            }
            this.serviceStartup(targetStep);           
        }

        private handleRegisteredAsSlave(errCode: number, master: MasterClient, data: any): void
        {
            Utility.LogDebug("[MonitorManager]", "RegisteredAsSlave", errCode);
            if (errCode === ErrCode.OK)
            {
                this.masterClientId = master.clientId;
                this.mainPage.DisplayMenuItems(master.masterName, data);
            }
        }

        private handleUnregisteredFromSlave(errCode: number): void
        {
            Utility.LogDebug("[MonitorManager]", "UnregisteredFromSlave", errCode);
            if (errCode == ErrCode.OK)
            {
                this.masterClientId = null;
                this.mainPage.Back2MainPage();
            }
        }

        private handleMasterCollectionChanged(master: MasterClient): void
        {
            Utility.LogDebug("[MonitorManager]", "CollectionChanged", master.toString());
            if (master.isOnline)
            {
                this.mainPage.AddButton(master);
            }
            else
            {
                this.mainPage.RemoveBtnById(master.clientId);
                if (master.clientId === this.masterClientId)
                {
                    this.service.UnregisterFromSlave();
                }
            }
        }

        private handleGetAllMasters(errCode : number, masters : MasterClient[]): void
        {
            Utility.LogDebug("[MonitorManager]", "GetAllMasters", errCode);
            masters.forEach((master) =>
            {
                Utility.LogDebug("[MonitorManager]", "---", master.toString());
            });
            this.mainPage.ResetMasterBtns(masters);
        }

        private handleBroadcast(errCode: number): void
        {
            Utility.LogDebug("[MonitorManager]", "Broadcast", errCode);
        }

        private handleReceivedBroadcast(data: any): void
        {
            Utility.LogDebug("[MonitorManager]", "ReceivedBroadcast", data);
        }     
    }
}