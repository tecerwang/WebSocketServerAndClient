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
            this.mainPage.OnMenuItemClick.AddListener((topMost: boolean, id: number) =>
            {
                const data = {
                    "topMost": topMost,
                    "id": id
                };
                this.service.Broadcast(data);
            });
            this.init();
        }

        async init()
        {
            //const backendUrl = 'ws://localhost:8080/ws';
            var backendUrl = 'ws://' + window.location.hostname + ':8080/ws';
            if (window.location.hostname == null || window.location.hostname == "")
            {
                backendUrl = 'ws://shanxi.jeosun.cn:8080/ws';
            }
            Utility.LogDebug("[MainPageController]", "Create singleton backend start");
            if (WebsocketTSClient.WSBackend.CreateSingleton(backendUrl))
            {
                Utility.LogDebug("[MainPageController]", "Create singleton backend end");

                this.wsBackend = WebsocketTSClient.WSBackend.singleton;
                // await/async 异步等待服务器连接完成
                Utility.LogDebug("[MainPageController]", "Connect to server start");
                await this.wsBackend.Connect2Server();
                Utility.LogDebug("[MainPageController]", "Connect to server end");


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
                this.wsBackend.OnStateChanged.AddListener((state) =>
                {
                    if (state === true)
                    {
                        Utility.LogDebug("[MainPageController]", "backend reconnected and restart serviceStartup()");
                        this.serviceStartupStep = -1;
                        this.serviceStartup(0);
                    }
                }); // 重新连接后，重新运行启动步骤

                this.serviceStartup(0);
               
            }
            else
            {
                Utility.LogDebug("[MainPageController]", "singleton backend already created");
            }
        }

        /** service 启动步骤 */
        private serviceStartupStep: number = -1;

        private serviceStartup(targetStep : number)
        {
            if (!this.wsBackend.IsConnected)
            {
                this.serviceStartupStep = -1;
                return;
            }

            if (this.serviceStartupStep === targetStep)
            {
                return;
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
            Utility.LogDebug("[MainPageController]", "RegisteredAsListener", errCode);
            var targetStep = this.serviceStartupStep;
            if (errCode === ErrCode.OK)
            {
                targetStep++;
            }
            this.serviceStartup(targetStep);           
        }

        private handleRegisteredAsSlave(errCode: number, master: MasterClient, data: any): void
        {
            Utility.LogDebug("[MainPageController]", "RegisteredAsSlave", errCode);
            if (errCode === ErrCode.OK)
            {
                this.masterClientId = master.clientId;
                this.mainPage.SetupMenus(master.masterName, data.menuCollection);
            }
        }

        private handleUnregisteredFromSlave(errCode: number): void
        {
            Utility.LogDebug("[MainPageController]", "UnregisteredFromSlave", errCode);
            if (errCode == ErrCode.OK)
            {
                this.masterClientId = null;
                this.mainPage.Back2MainPage();
            }
        }

        private handleMasterCollectionChanged(master: MasterClient): void
        {
            Utility.LogDebug("[MainPageController]", "CollectionChanged", master.toString());
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
            Utility.LogDebug("[MainPageController]", "GetAllMasters", errCode);
            masters.forEach((master) =>
            {
                Utility.LogDebug("[MainPageController]", "---", master.toString());
            });
            this.mainPage.ResetMasterBtns(masters);
        }

        private handleBroadcast(errCode: number): void
        {
            Utility.LogDebug("[MainPageController]", "Broadcast", errCode);
        }

        private handleReceivedBroadcast(data: any): void
        {
            Utility.LogDebug("[MainPageController]", "ReceivedBroadcast", data);
        } 
    }
}