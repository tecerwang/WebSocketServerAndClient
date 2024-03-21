using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;
using WebSocketClient;

namespace WebSocketClient
{
    public class MasterSlavesGroupService : BackendServiceManagerBase
    {
        public class MasterClient
        {
            public string clientId;          
            public string masterName;
            public bool isOnline;

            public static MasterClient Parse(JToken token)
            {
                if (token == null)
                {
                    return null;
                }
                var cid = JHelper.GetJsonString(token, "clientId");
                var masterName = JHelper.GetJsonString(token, "masterName");
                var isOnline = JHelper.GetJsonBool(token, "isOnline");

                if (string.IsNullOrEmpty(cid))
                {
                    return null;
                }
                return new MasterClient()
                {
                    clientId = cid,
                    masterName = masterName,
                    isOnline = isOnline
                };
            }

            public override string ToString()
            {
                return $"masterName:{masterName}, {isOnline}, clientId:{clientId}";
            }
        }

        public enum ClientState
        {
            Idle,           
            IsMaster,
            IsSlave
        }

        public ClientState clientState { get; private set; } = ClientState.Idle;

        private bool _isQuarying = false;
        private const string serviceName = "MasterSlavesGroupService";

        /// <summary>
        /// 注册成为 master
        /// </summary>
        public event Action<int> OnRegisterAsListener;

        /// <summary>
        /// 注销 master
        /// </summary>
        public event Action<int> OnUnregisteredFromListener;


        /// <summary>
        /// 注册成为 master
        /// </summary>
        public event Action<int> OnRegisteredAsMaster;

        /// <summary>
        /// 注销 master
        /// </summary>
        public event Action<int> OnUnregisteredFromMaster;

        /// <summary>
        /// 注册成为 slave
        /// </summary>
        public event Action<int, JToken> OnRegisteredAsSlave;

        /// <summary>
        /// 注销 slave
        /// </summary>
        public event Action<int> OnUnregisteredFromSlave;

        /// <summary>
        /// 服务器上的 master 集合发生变化
        /// </summary>
        public event Action<MasterClient> OnMasterCollectionChanged;

        /// <summary>
        /// 获取所有的 master
        /// </summary>
        public event Action<int, IEnumerable<MasterClient>> OnGetAllMasters;

        /// <summary>
        /// 广播消息完成
        /// </summary>
        public event Action<int> OnBroadcast;

        /// <summary>
        /// 收到别人发出的广播消息
        /// </summary>
        public event Action<JToken> OnRecievedBroadcast;

        public override async Task Init()
        {
            WSBackend.singleton.OnBackendNotify += Singleton_OnBackendNotify;
            WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;
            await Task.CompletedTask;
        }

        public override async Task Release()
        {
            WSBackend.singleton.OnBackendNotify -= Singleton_OnBackendNotify;
            WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
            await Task.CompletedTask;
        }

        private void Singleton_OnBackendStateChanged()
        {
            if (WSBackend.singleton.State == WSBackend.WSBackendState.Close)
            {
                // 断线后状态为空闲
                clientState = ClientState.Idle;
                _isQuarying = false;
                Utility.LogDebug("MasterSlavesGroupService", "Connect Closed,become idle state");
            }
        }

        private void Singleton_OnBackendNotify(NotifyPack not)
        {
            if (not.serviceName == MasterSlavesGroupService.serviceName)
            {
                // master collectoion on server changed
                if (not.cmd == BackendOps.Notify_OnMasterCollectionChanged)
                {
                    var masterClient=  MasterClient.Parse(not.data);
                    if (masterClient != null)
                    {
                        Utility.LogDebug("MasterSlavesGroupService", "Notify Master Collection Changed", masterClient);
                        OnMasterCollectionChanged?.Invoke(masterClient);
                    }
                }
                // 收到别人发送的消息
                else if (not.cmd == BackendOps.Cmd_Broadcast)
                {
                    Utility.LogDebug("MasterSlavesGroupService", $"Recieved Broadcast msg {not.data}");
                    OnRecievedBroadcast?.Invoke(not.data);
                }
            }
        }

        /// <summary>
        /// 注册本机作为 Listenr
        /// </summary>
        /// <param name="masterName"></param>
        public async void RegisterAsListener()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsListener Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_RegisterAsListener, null))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    OnRegisterAsListener?.Invoke(resp.errCode);
                    Utility.LogDebug("MasterSlavesGroupService", "RegisterAsListener End", resp.errCode);                    
                }
            }
        }

        /// <summary>
        /// 注销 Listenr
        /// </summary>
        /// <param name="masterName"></param>
        public async void UnregisterFromListener()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromListener Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_UnregisterFromListener, null))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    OnUnregisteredFromListener?.Invoke(resp.errCode);
                    Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromListener End", resp.errCode);
                }
            }
        }

        /// <summary>
        /// 注册本机作为 master
        /// </summary>
        /// <param name="masterName"></param>
        public async void RegisterAsMaster(string masterName, int displayIndex, JToken masterData)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                clientState = ClientState.IsMaster;
                JObject data = new JObject();
                data.Add("masterName", masterName);
                data.Add("displayIndex", displayIndex);
                data.Add("masterData", masterData);
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsMaster Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_RegisterAsMaster, data))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    OnRegisteredAsMaster?.Invoke(resp.errCode);
                    Utility.LogDebug("MasterSlavesGroupService", "RegisterAsMaster End", resp.errCode);
                }
            }
        }

        /// <summary>
        /// 注销本机作为 master
        /// </summary>
        public async void UnRegisterFromMaster()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnRegisterFromMaster Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_UnregisterFromMaster, null))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    clientState = resp.errCode == ErrCode.OK ? ClientState.Idle : ClientState.IsMaster;
                    OnUnregisteredFromMaster?.Invoke(resp.errCode);
                    Utility.LogDebug("MasterSlavesGroupService", "UnRegisterFromMaster End", resp.errCode);
                }
            }
        }

        /// <summary>
        /// 注册本机作为 slave
        /// </summary>
        public async void RegisterAsSlave(string masterId)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                clientState = ClientState.IsSlave;
                JObject data = new JObject();
                data.Add("masterId", masterId);
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsSlave Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_RegisterAsSlave, data))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    clientState = resp.errCode == ErrCode.OK ? ClientState.IsSlave : ClientState.Idle;
                    OnRegisteredAsSlave?.Invoke(resp.errCode, resp.data);
                    Utility.LogDebug("MasterSlavesGroupService", "RegisterAsSlave End", resp.errCode);
                }
            }
        }

        public async void UnregisterFromSlave()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromSlave Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_UnregisterFromSlave, null))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    clientState = resp.errCode == ErrCode.OK ? ClientState.Idle : ClientState.IsSlave;
                    Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromSlave End", resp. errCode);
                    OnUnregisteredFromSlave?.Invoke(resp.errCode);
                }
            }
        }

        public async void GetAllMasters()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "GetAllMasters Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_GetAllMasters, null))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    List<MasterClient> list = new List<MasterClient>();
                    var arr = JHelper.GetJsonArray(resp.data, "masters");
                    if (arr != null)
                    {
                        foreach (var d in arr)
                        {
                            var master = MasterClient.Parse(d);
                            if (master != null)
                            {
                                list.Add(master);
                            }
                        }
                    }
                    Utility.LogDebug("MasterSlavesGroupService", "GetAllMasters End", $"Get {list.Count} masters");
                    OnGetAllMasters?.Invoke(resp.errCode, list);
                }                
            }
        }

        public async void Broadcast(JToken data)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "Broadcast Begin");
                using (var request = new BackendRequestAsync(serviceName, BackendOps.Cmd_GetAllMasters, data))
                {
                    var resp = await request.Request();
                    _isQuarying = false;
                    Utility.LogDebug("MasterSlavesGroupService", "Broadcast End", resp.errCode);
                    OnBroadcast?.Invoke(resp.errCode);
                }
            }
        }
    }
}
