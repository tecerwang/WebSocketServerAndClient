using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Xml.Linq;
using WebSocketServer.DataService;
using WebSocketServer.ServerKernal;
using WebSocketServer.ServerKernal.MsgPack;
using WebSocketServer.Utilities;
using static WebSocketServer.ServiceLogic.ClientGroupBroadcastService;

namespace WebSocketServer.ServiceLogic
{
    /// <summary>
    /// 主从通讯服务;
    /// 一个 Master 可以对应多个 Slave;
    /// 组群消息以组群内广播形式通讯
    /// </summary>

    public class MasterSlavesGroupService : AbstractServiceLogic
    {
        public override string serviceName => "MasterSlavesGroupService";

        public class MasterClient
        {
            public required string clientId;
            public required string masterName;
            public required int displayIndex;
            public JToken? masterData;

            public override int GetHashCode()
            {
                return clientId.GetHashCode();
            }

            public JToken ToJsonWithoutData(bool isOnline)
            {
                return JHelper.MakeData(
                    "clientId", clientId,
                    "displayIndex", displayIndex,
                    "masterName", masterName,
                    "isOnline", isOnline);
            }

            public override string ToString()
            {
                return $"name:{masterName}, clientId:{clientId}";
            }
        }

        public class SlaveClient
        { 
            public required string clientId;
            public required string masterId;
            public override int GetHashCode()
            {
                return clientId.GetHashCode();
            }
        }

        /// <summary>
        /// 数据提供者，如果需要改变数据提供方式，比如 database 或者 reddit key-value 在这里解耦
        /// </summary>
        private MasterSlavesGroupData? _dataProvider;

        public MasterSlavesGroupService(IServiceProvider provider)
        {
            // 创建一个数据提供者
            _dataProvider = provider.GetService<MasterSlavesGroupData>();
        }
        
        protected override async Task OnClientClose(string clientId)
        {
            if (_dataProvider == null)
            {
                return;
            }

            await _dataProvider.UnregisterListener(clientId);

            var master = await _dataProvider.UnregisterMaster(clientId);
            // 当一个客户端 close 时，需要查看是否在 master collection 中，如果在，需要注销这个 master，并且发出通知
            if (master != null)
            {
                await Send_MasterChanged_2_Listenrs(_dataProvider, master, false);
            }
            else
            {
                var slave = await _dataProvider.GetSlaveByClientId(clientId);
                if (slave != null)
                {
                    await _dataProvider.UnregisterSlave(slave);
                }
            }
        }

        protected override async Task OnClientOpen(string clientId)
        {
            await Task.CompletedTask;
        }

        protected override async Task OnMessageRecieved(RequestPack pack)
        {
            if (_dataProvider == null)
            {
                return;
            }
            // 注册 listener，listener 接收特定的 Notify
            if (pack.cmd == BackendOps.Cmd_RegisterAsListener)
            {
                await HandleRegisterAsListener(_dataProvider, pack);
            }
            // 注销 listener
            else if (pack.cmd == BackendOps.Cmd_UnregisterFromListener)
            {
                await HandleUnegisterFromListener(_dataProvider, pack);
            }
            // 注册 master
            if (pack.cmd == BackendOps.Cmd_RegisterAsMaster)
            {
                await HandleRegisterAsMaster(_dataProvider, pack);
            }
            // 注销 master
            else if (pack.cmd == BackendOps.Cmd_UnregisterFromMaster)
            {
                await HandleUnregisterFromMaster(_dataProvider, pack);
            }
            // 获取所有 master
            else if (pack.cmd == BackendOps.Cmd_GetAllMasters)
            {
                await HandleGetAllMasters(_dataProvider, pack);
            }
            // 注册 slave
            else if (pack.cmd == BackendOps.Cmd_RegisterAsSlave)
            {
                await HandleRegisterAsSlave(_dataProvider, pack);
            }
            // 注销 slave
            else if (pack.cmd == BackendOps.Cmd_UnregisterFromSlave)
            {
               await HandleUnregisterFromSlave(_dataProvider, pack);
            }
            // 发送消息
            else if (pack.cmd == BackendOps.Cmd_Broadcast)
            {
                await HandleBroadcast(_dataProvider, pack);
            }           
        }

        private async Task HandleRegisterAsListener(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;
            if (string.IsNullOrEmpty(clientId))
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
                return;
            }

            if (await dataProvider.RegisterListener(clientId))
            {
                await CreateResponseToClient(pack, null, ErrCode.OK);
            }
            else 
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
            }
        }

        private async Task HandleUnegisterFromListener(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;
            if (string.IsNullOrEmpty(clientId))
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
                return;
            }

            if (await dataProvider.UnregisterListener(clientId))
            {
                await CreateResponseToClient(pack, null, ErrCode.OK);
            }
            else
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
            }
        }

        private async Task HandleRegisterAsMaster(MasterSlavesGroupData dataProvider, RequestPack pack)
        {            
            var clientId = pack.clientId;
            if (pack.data == null)
            {
                await CreateResponseToClient(pack, null, ErrCode.DataIsNull);
                return;
            }
            var masterName = JHelper.GetJsonString(pack.data, "masterName");
            if (masterName == null)
            {
                await CreateResponseToClient(pack, null, ErrCode.MasterNameIsNull);
                return;
            }
            var displayIndex = JHelper.GetJsonInt(pack.data, "displayIndex", -1);

            var master = await dataProvider.GetMasterByClientId(clientId);
            if (master != null)
            {
                await CreateResponseToClient(pack, null, ErrCode.AlreadyRegistered);
                return;
            }
            else
            {
                var masterData = JHelper.GetJsonToken(pack.data, "masterData");
                master = new MasterClient() { clientId = clientId, masterName = masterName, displayIndex = displayIndex, masterData = masterData };
                if (await dataProvider.RegisterMaster(master))
                {
                    await Send_MasterChanged_2_Listenrs(dataProvider, master, true);
                    await CreateResponseToClient(pack, null, ErrCode.OK);
                    DebugLog.Print("MasterSlavesGroupService", "RegisterMaster", master.clientId);
                    return;
                }                
            }
            await CreateResponseToClient(pack, null, ErrCode.Unkown);
        }

        private async Task HandleUnregisterFromMaster(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;
            var master = await dataProvider.UnregisterMaster(clientId);
            if (master != null)
            {
                await Send_MasterChanged_2_Listenrs(dataProvider, master, false);
                await CreateResponseToClient(pack, null, ErrCode.OK);
                DebugLog.Print("MasterSlavesGroupService", "UnregisterMaster", master.clientId);
            }
            else
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
                DebugLog.Print("MasterSlavesGroupService", "HandleRegisterAsSlave", "Client is not in masters' collection");
            }
        }

        private async Task HandleGetAllMasters(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var masters = await dataProvider.GetAllMasters();
           
            JArray jarr = new JArray();
            foreach (var master in masters)
            {
                var jitem = master.ToJsonWithoutData(true);
                jarr.Add(jitem);
            }
            JObject jobj = JHelper.MakeData("masters", jarr);
            await CreateResponseToClient(pack, jobj, ErrCode.OK);
            DebugLog.Print("MasterSlavesGroupService", "HandleGetAllMasters", $"Master count : {masters.Count()}");
        }

        private async Task HandleRegisterAsSlave(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;
            if (pack.data == null)
            {
                await CreateResponseToClient(pack, null, ErrCode.DataIsNull);
                return;
            }

            var masterId = JHelper.GetJsonString(pack.data, "masterId");
            if (string.IsNullOrEmpty(masterId))
            {
                await CreateResponseToClient(pack, null, ErrCode.MasterIdIsNull);
                return;
            }

            var slaveClient = await dataProvider.GetSlaveByClientId(clientId);
            if (slaveClient != null)
            {
                await CreateResponseToClient(pack, null, ErrCode.AlreadyRegistered);
                return;
            }

            var masterClient = await dataProvider.GetMasterByClientId(masterId);
            if (masterClient == null)
            {
                await CreateResponseToClient(pack, null, ErrCode.MasterIsOffline);
                return;
            }

            slaveClient = new SlaveClient() { clientId = clientId, masterId = masterId };
            await dataProvider.RegisterSlave(slaveClient);
            await CreateResponseToClient(pack, masterClient.masterData, ErrCode.OK);
            DebugLog.Print("MasterSlavesGroupService", "HandleRegisterAsSlave", clientId);
        }

        private async Task HandleUnregisterFromSlave(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;
            var slaveClient = await dataProvider.GetSlaveByClientId(clientId);
            await dataProvider.UnregisterSlave(slaveClient);
          
            if (slaveClient != null)
            {
                await CreateResponseToClient(pack, null, ErrCode.OK);
                DebugLog.Print("MasterSlavesGroupService", "HandleUnregisterFromSlave", slaveClient.clientId);
            }
            else
            {
                await CreateResponseToClient(pack, null, ErrCode.Unkown);
                DebugLog.Print("MasterSlavesGroupService", "HandleUnregisterFromSlave", "Client is not in slaves' collection");
            }
        }

        private async Task HandleBroadcast(MasterSlavesGroupData dataProvider, RequestPack pack)
        {
            var clientId = pack.clientId;            
            var master = await dataProvider.GetMasterByClientId(clientId);
            string? masterId = null;
            
            if (master != null)// 消息由 master 发送
            {
                masterId = master.clientId;
            }
            else// 消息由 slave 发送 此时需要向 master 广播消息
            {
                var slave = await dataProvider.GetSlaveByClientId(clientId);
                if (slave != null)
                {
                    masterId = slave.masterId;
                    // 获取 master Id, 如果 masterId 对应的 client 为空，说明 Master client 已经掉线
                    master = await dataProvider.GetMasterByClientId(masterId);
                    if (master != null)
                    {
                        await CreateNotifyToClient(masterId, serviceName, BackendOps.Cmd_Broadcast, pack.data);
                    }
                    else
                    {
                        await CreateResponseToClient(pack, null, ErrCode.MasterIsOffline);
                        DebugLog.Print("MasterSlavesGroupService", "HandleBroadcast", $"Master is Offline");
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(masterId))
            {
                var slaves = await dataProvider.GetSlavesOfOneMaster(masterId);
                foreach (var slave in slaves)
                {
                    // 自己不用发 notify
                    if (clientId == slave.clientId)
                    {
                        continue;
                    }
                    /// 消息原样发出 notify
                    await CreateNotifyToClient(slave.clientId, serviceName, BackendOps.Cmd_Broadcast, pack.data);
                }
                await CreateResponseToClient(pack, null, ErrCode.OK);
                DebugLog.Print("MasterSlavesGroupService", "HandleBroadcast", $"Msg is {pack.data}");
            }
            else
            {
                await CreateResponseToClient(pack, null, ErrCode.MasterIsOffline);
                DebugLog.Print("MasterSlavesGroupService", "HandleBroadcast", $"Master is Offline");
            }
        }

        /// <summary>
        /// 需要向所有 listener 发送消息，告诉 Master 下线
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="master"></param>
        /// <returns></returns>
        private async Task Send_MasterChanged_2_Listenrs(MasterSlavesGroupData dataProvider, MasterClient master, bool isOnline)
        {
            JToken data = master.ToJsonWithoutData(isOnline);
            foreach (var clientId in (await dataProvider.GetAllListeners()))
            {
                await CreateNotifyToClient(clientId, serviceName, BackendOps.Notify_OnMasterCollectionChanged, data);
            }
            DebugLog.Print("MasterSlavesGroupService", master.ToString(), isOnline ? "Online" : "Offline");
        }
    }
}
