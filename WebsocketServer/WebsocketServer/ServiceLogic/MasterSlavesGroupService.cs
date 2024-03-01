using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Xml.Linq;
using WebSocketServer.DataProvider;
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

    internal class MasterSlavesGroupService : AbstractServiceLogic
    {
        internal override string serviceName => "MasterSlavesGroupService";

        internal class MasterClient
        {
            public required string clientId;
            public required string masterName;

            public JToken? masterData;

            public override int GetHashCode()
            {
                return clientId.GetHashCode();
            }
        }

        internal class SlaveClient
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
        private MasterSlavesGroupData _dataProvider;

        internal MasterSlavesGroupService()
        {
            // 创建一个数据提供者
            _dataProvider = new MasterSlavesGroupData();

        }
        
        protected override async Task OnClientClose(string clientId)
        {
            // 当一个客户端 close 时，需要查看是否在 master collection 中，如果在，需要注销这个 master，并且发出通知
            var master = await _dataProvider.GetMasterByClientId(clientId);
            if (master != null)
            {
                await _dataProvider.UnregisterMaster(master);
                // 通知所有 slave 发生修改
                foreach (var slaveId in (await _dataProvider.GetAllSlaves()).Select(p => p.clientId))
                {
                    await CreateNotifyToClient(slaveId, serviceName, BackendOps.Notify_OnMasterCollectionChanged, null);
                }
            }
        }

        protected override async Task OnClientOpen(string clientId)
        {
            await Task.CompletedTask;
        }

        protected override async Task OnMessageRecieved(RequestPack pack)
        {
           
            // master 注册一个菜单提供者
            if (pack.cmd == BackendOps.Cmd_RegisterAsMaster)
            {
                await HandleRegisterAsMaster(pack);
            }
            // 注销 master
            else if (pack.cmd == BackendOps.Cmd_UnregisterFromMaster)
            {
                await HandleUnregisterFromMaster(pack);
            }
            // 获取所有master
            else if (pack.cmd == BackendOps.Cmd_GetAllMasters)
            {
                await HandleGetAllMasters(pack);
            }
            // 注册slave
            else if (pack.cmd == BackendOps.Cmd_RegisterAsSlave)
            {
                await HandleRegisterAsSlave(pack);
            }
            // 注销slave
            else if (pack.cmd == BackendOps.Cmd_UnregisterFromSlave)
            {
               await HandleUnregisterFromSlave(pack);
            }
            // 发送消息
            else if (pack.cmd == BackendOps.Cmd_Broadcast)
            {
                await HandleBroadcast(pack);
            }           
        }

        private async Task HandleRegisterAsMaster(RequestPack pack)
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
            var master = await _dataProvider.GetMasterByClientId(clientId);
            var masterData = JHelper.GetJsonObject(pack.data, "masterData");
            if (master != null)
            {
                master.masterName = masterName;
                master.masterData = masterData;
            }
            else
            {
                master = new MasterClient() { clientId = clientId, masterName = masterName, masterData = masterData };
                await _dataProvider.RegisterMaster(master);
            }
            await CreateResponseToClient(pack, null, ErrCode.OK);
        }

        private async Task HandleUnregisterFromMaster(RequestPack pack)
        {
            var clientId = pack.clientId;
            var master = await _dataProvider.GetMasterByClientId(clientId);
            await _dataProvider.UnregisterMaster(master);
            await CreateResponseToClient(pack, null, ErrCode.OK);
        }

        private async Task HandleGetAllMasters(RequestPack pack)
        {
            var masters = await _dataProvider.GetAllMasters();
            JObject jobj = new JObject();
            JArray jarr = new JArray();
            for (int i = 0; i < masters.Length; i++)
            {
                var jitem = new JObject();
                jitem.Add("clientId", masters[i].clientId);
                jitem.Add("masterName", masters[i].masterName);
                jarr.Add(jitem);
            }
            await CreateResponseToClient(pack, jobj, ErrCode.OK);
        }

        private async Task HandleRegisterAsSlave(RequestPack pack)
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

            var slaveClient = await _dataProvider.GetSlaveByClientId(clientId);
            if (slaveClient != null)
            {
                await CreateResponseToClient(pack, null, ErrCode.AlreadyRegistered);
                return;
            }

            slaveClient = new SlaveClient() { clientId = clientId, masterId = masterId };
            await _dataProvider.RegisterSlave(slaveClient);
            await CreateResponseToClient(pack, null, ErrCode.OK);
        }

        private async Task HandleUnregisterFromSlave(RequestPack pack)
        {
            var clientId = pack.clientId;
            var slaveClient = await _dataProvider.GetSlaveByClientId(clientId);
            await _dataProvider.UnregisterSlave(slaveClient);
            await CreateResponseToClient(pack, null, ErrCode.OK);
        }

        private async Task HandleBroadcast(RequestPack pack)
        {
            var clientId = pack.clientId;            
            var master = await _dataProvider.GetMasterByClientId(clientId);
            string? masterId = null;
            
            if (master != null)// 消息由 master 发送
            {
                masterId = master.clientId;
            }
            else// 消息由 slave 发送 此时需要向 master 广播消息
            {
                var slave = await _dataProvider.GetSlaveByClientId(clientId);
                if (slave != null)
                {
                    masterId = slave.masterId;
                    await CreateNotifyToClient(masterId, serviceName, BackendOps.Cmd_Broadcast, pack.data);
                }
            }

            if (!string.IsNullOrEmpty(masterId))
            {
                var slaves = await _dataProvider.GetSlavesOfOneMaster(masterId);
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
            }
            else
            {
                await CreateResponseToClient(pack, null, ErrCode.MasterIsOffline);
            }
        }
    }
}
