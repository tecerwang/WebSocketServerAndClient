namespace WebsocketTSClient
{
    export enum MasterSlavesGroupServiceState
    {
        Idle,
        IsMaster,
        IsSlave
    }

    export class MasterClient
    {
        public clientId: string;
        public masterName: string;
        public isOnline: boolean;        

        constructor(clientId: string, masterName: string, isOnline: boolean)
        {
            this.clientId = clientId;
            this.masterName = masterName;
            this.isOnline = isOnline;
        }

        public static parse(json: any): MasterClient | null
        {
            if (!json || typeof json !== 'object')
            {
                return null;
            }

            const clientId = json.clientId;
            const masterName = json.masterName;
            const isOnline = json.isOnline;

            if (typeof clientId !== 'string' || typeof masterName !== 'string' || typeof isOnline !== 'boolean')
            {
                return null;
            }

            return new MasterClient(clientId, masterName, isOnline);
        }

        public toString(): string
        {
            return `masterName: ${this.masterName}, isOnline: ${this.isOnline}, clientId: ${this.clientId}`;
        }
    }

    export class MasterSlavesGroupService
    {
        /** 服务名称 */
        private static serviceName: string = "MasterSlavesGroupService";
        /** 是否在请求中 */
        private isQuarying: boolean = false;   
        /** 注册为 Listerner */
        public OnRegisteredAsListener: EventHandler<[number]> = new EventHandler<[number]>();
        /** 注销 Listerner */
        public OnUnregisteredFromListener: EventHandler<[number]> = new EventHandler<[number]>();
        /** 注册为 master */
        public OnRegisteredAsMaster: EventHandler<[number]> = new EventHandler<[number]>();
        /** 注销 master */
        public OnUnregisteredFromMaster: EventHandler<[number]> = new EventHandler<[number]>();
        /** 注册为 slave */
        public OnRegisteredAsSlave: EventHandler<[number, MasterClient, any]> = new EventHandler<[number, MasterClient, any]>();
        /** 注销 slave */
        public OnUnregisteredFromSlave: EventHandler<[number]> = new EventHandler<[number]>();
        /** 当服务器 master 集合发生变化 */
        public OnMasterCollectionChanged: EventHandler<[MasterClient]> = new EventHandler<[MasterClient]>();
        /** 获取所有 master */
        public OnGetAllMasters: EventHandler<[number, MasterClient[]]> = new EventHandler<[number, MasterClient[]]>();
        /** 广播消息 */
        public OnBroadcast: EventHandler<[number]> = new EventHandler<[number]>();
        /** 接收到广播消息 */
        public OnRecievedBroadcast: EventHandler<[any]> = new EventHandler<[any]>();

        private state: MasterSlavesGroupServiceState = MasterSlavesGroupServiceState.Idle;

        public GetState(): MasterSlavesGroupServiceState
        {
            return this.state;
        }

        constructor()
        {
            WSBackend.singleton.OnStateChanged.AddListener(this.OnBackendStateChanged);
            WSBackend.singleton.OnNotify.AddListener(this.OnBackendNotify);
        }

        private OnBackendStateChanged = (state: boolean): void =>
        {
            if (!state)
            {
                this.isQuarying = false;
                this.state = MasterSlavesGroupServiceState.Idle;
            }
        }

        private OnBackendNotify = (not: NotifyPack): void =>
        {
            if (not.serviceName === MasterSlavesGroupService.serviceName)
            {
                // 收到服务器 master 集合变化的消息
                if (not.cmd === BackendOps.Notify_OnMasterCollectionChanged)
                {
                    const master: MasterClient | null = MasterClient.parse(not.data);
                    Utility.LogDebug("MasterSlavesGroupService", "Notify Master Collection Changed", master.toString());
                    this.OnMasterCollectionChanged.Trigger(master);
                }
                // 收到广播消息
                if (not.cmd === BackendOps.Cmd_Broadcast)
                {
                    this.OnRecievedBroadcast.Trigger(not.data);
                }
            }
        }

        public RegisterAsListener(): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_RegisterAsListener,
                    null,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                        this.OnRegisteredAsListener.Trigger(errCode);
                    }
                );
            }
        }

        public UnregisterFromListener(): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_UnregisterFromListener,
                    null,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                        this.OnUnregisteredFromListener.Trigger(errCode);
                    }
                );
            }
        }

        public RegisterAsMaster(masterName: string, masterData: any | null): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                var data =
                {
                    masterName: masterName,
                    masterData: masterData
                };
                this.state = MasterSlavesGroupServiceState.IsMaster;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_RegisterAsMaster,
                    data,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.IsMaster : MasterSlavesGroupServiceState.Idle;
                        this.OnRegisteredAsMaster.Trigger(errCode);
                    }
                );
            }
        }

        public UnRegisterFromMaster(): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_UnregisterFromMaster,
                    null,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsMaster;
                        this.OnUnregisteredFromMaster.Trigger(errCode);
                    }
                );
            }
        }

        public RegisterAsSlave(master: MasterClient): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                var data = 
                {
                    masterId: master.clientId
                };
                this.state = MasterSlavesGroupServiceState.IsSlave;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_RegisterAsSlave,
                    data,
                    master,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.IsSlave : MasterSlavesGroupServiceState.Idle;
                        this.OnRegisteredAsSlave.Trigger(errCode, context, data);
                    }
                );
            }
        }

        public UnregisterFromSlave(): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_UnregisterFromSlave,
                    null,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.state = errCode === ErrCode.OK ? MasterSlavesGroupServiceState.Idle : MasterSlavesGroupServiceState.IsSlave;
                        this.OnUnregisteredFromSlave.Trigger(errCode);
                    }
                );
            }
        }

        public GetAllMasters(): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_GetAllMasters,
                    null,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;

                        const result: MasterClient[] = [];
                        const jarr: any[] | null = data["masters"];

                        if (jarr !== null)
                        {
                            jarr.forEach((jobj) =>
                            {
                                var mc = MasterClient.parse(jobj);
                                if (mc !== null)
                                {
                                    result.push(mc);
                                }
                            });
                        }
                        this.OnGetAllMasters.Trigger(errCode, result);
                    }
                );
            }
        }

        public Broadcast(data: any): void
        {
            if (!this.isQuarying && WSBackend.singleton.IsConnected)
            {
                this.isQuarying = true;
                BackendRequest.CreateRetry(
                    MasterSlavesGroupService.serviceName,
                    BackendOps.Cmd_Broadcast,
                    data,
                    null,
                    // resp
                    (errCode: number, data: any, context: any) =>
                    {
                        this.isQuarying = false;
                        this.OnBroadcast.Trigger(errCode);
                    }
                );
            }
        }
    }
}