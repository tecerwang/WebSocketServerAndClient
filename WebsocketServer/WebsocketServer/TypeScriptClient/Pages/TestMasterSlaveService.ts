namespace WebsocketTSClient
{
    var utility = WebsocketTSClient.Utility;
    var service: MasterSlavesGroupService = null;
    var wsBackend: WSBackend = null;
    async function init()
    {
        /// connect to server first
        const backendUrl = 'ws://localhost:8080/ws';

        // 创建一个 backend 的单例
        utility.LogDebug("[HTML]", "Create singleton backend start");
        if (WebsocketTSClient.WSBackend.CreateSingleton(backendUrl))
        {
            utility.LogDebug("[HTML]", "Create singleton backend end");

            wsBackend = WSBackend.singleton;
            // await/async 异步等待服务器连接完成
            utility.LogDebug("[HTML]", "Connect to server start");
            await wsBackend.Connect2Server();
            utility.LogDebug("[HTML]", "Connect to server end");

            // 创建一个 service 用于管理 MasterSlavesGroupService 通信服务
            service = new WebsocketTSClient.MasterSlavesGroupService();

            // Callback 后改变 UI
            service.OnRegisteredAsMaster.AddListener((errCode) => { if (errCode === ErrCode.OK) resetUiElements(); });
            service.OnUnregisteredFromMaster.AddListener((errCode) => { if (errCode === ErrCode.OK) resetUiElements(); });
            service.OnRegisteredAsSlave.AddListener((errCode) => { if (errCode === ErrCode.OK) resetUiElements(); });
            service.OnUnregisteredFromSlave.AddListener((errCode) => { if (errCode === ErrCode.OK) resetUiElements(); });


            resetUiElements();

            // UI 交互逻辑
            document.getElementById('registerMasterBtn').addEventListener('click', () =>
            {
                const masterName = (document.getElementById('inputMasterName') as HTMLInputElement).value;
                if (masterName)
                {
                    service.RegisterAsMaster(masterName, null);
                }
            });

            document.getElementById('unRegisterMasterBtn').addEventListener('click', () =>
            {
                service.UnRegisterFromMaster();
            });

            document.getElementById('registerSlaveBtn').addEventListener('click', () =>
            {
                const masterId = (document.getElementById('inputMasterId') as HTMLInputElement).value;
                if (masterId)
                {
                    service.RegisterAsSlave(masterId);
                }
            });

            document.getElementById('unRegisterSlaveBtn').addEventListener('click', () =>
            {
                service.UnregisterFromSlave();
            });

            document.getElementById('getAllMastersBtn').addEventListener('click', () =>
            {
                service.GetAllMasters();
            });

            document.getElementById('broadcastBtn').addEventListener('click', () =>
            {
                const message = (document.getElementById('inputBroadcast') as HTMLInputElement).value;
                if (message)
                {
                    const data =
                    {
                        msg: message
                    }
                    service.Broadcast(data);
                }
            });
        }
        else
        {
            utility.LogDebug("[HTML]", "singleton backend already created");
        }
    }

    /// 重置所有组件显示
    function resetUiElements()
    {
        var state = service.GetState();

        (document.getElementById('getAllMastersBtn') as HTMLInputElement).disabled = false;

        (document.getElementById('registerMasterBtn') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.Idle;
        (document.getElementById('unRegisterMasterBtn') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.IsMaster;
        (document.getElementById('registerSlaveBtn') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.Idle;
        (document.getElementById('unRegisterSlaveBtn') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.IsSlave;
       
      
        (document.getElementById('inputMasterName') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.Idle;
        (document.getElementById('inputMasterId') as HTMLInputElement).disabled = state != MasterSlavesGroupServiceState.Idle;
        (document.getElementById('broadcastBtn') as HTMLInputElement).disabled = state == MasterSlavesGroupServiceState.Idle;
        (document.getElementById('inputBroadcast') as HTMLInputElement).disabled = state == MasterSlavesGroupServiceState.Idle;
    }

    // Call the init function
    init();  
}
