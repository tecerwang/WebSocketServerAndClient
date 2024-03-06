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
                var isOnline = JHelper.GetJsonBool(token, "Online");

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
        public event Action<int> OnRegisteredAsSlave;

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
        public void RegisterAsListener()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsListener Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_RegisterAsListener, null, null, 
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        Utility.LogDebug("MasterSlavesGroupService", "RegisterAsListener End", errCode);
                        OnRegisterAsListener?.Invoke(errCode);
                    });
            }
        }

        /// <summary>
        /// 注销 Listenr
        /// </summary>
        /// <param name="masterName"></param>
        public void UnregisterFromListener()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromListener Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_RegisterAsListener, null, null,
                    (int errCode, JToken data, object context) => {
                        _isQuarying = false;
                        Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromListener End", errCode);
                        OnUnregisteredFromListener?.Invoke(errCode);
                    });
            }
        }

        /// <summary>
        /// 注册本机作为 master
        /// </summary>
        /// <param name="masterName"></param>
        public void RegisterAsMaster(string masterName, JToken masterData)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                clientState = ClientState.IsMaster;
                JObject data = new JObject();
                data.Add("masterName", masterName);
                data.Add("masterData", masterData);
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsMaster Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_RegisterAsMaster, data, null,
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        clientState = errCode == ErrCode.OK ? ClientState.IsMaster : ClientState.Idle;
                        Utility.LogDebug("MasterSlavesGroupService", "RegisterAsMaster End", errCode);
                        OnRegisteredAsMaster?.Invoke(errCode);
                    });
            }
        }

        /// <summary>
        /// 注销本机作为 master
        /// </summary>
        public void UnRegisterFromMaster()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnRegisterFromMaster Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_UnregisterFromMaster, null, null,
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        clientState = errCode == ErrCode.OK ? ClientState.Idle : ClientState.IsMaster;
                        Utility.LogDebug("MasterSlavesGroupService", "UnRegisterFromMaster End", errCode);
                        OnUnregisteredFromMaster?.Invoke(errCode);
                    });
            }
        }

        /// <summary>
        /// 注册本机作为 slave
        /// </summary>
        public void RegisterAsSlave(string masterId)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                clientState = ClientState.IsSlave;
                JObject data = new JObject();
                data.Add("masterId", masterId);
                Utility.LogDebug("MasterSlavesGroupService", "RegisterAsSlave Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_RegisterAsSlave, data, null,
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        clientState = errCode == ErrCode.OK ? ClientState.IsSlave : ClientState.Idle;
                        Utility.LogDebug("MasterSlavesGroupService", "RegisterAsSlave End", errCode);
                        OnRegisteredAsSlave?.Invoke(errCode);
                    });
            }
        }

        public void UnregisterFromSlave()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromSlave Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_UnregisterFromSlave, null, null,
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        clientState = errCode == ErrCode.OK ? ClientState.Idle : ClientState.IsSlave;
                        Utility.LogDebug("MasterSlavesGroupService", "UnregisterFromSlave End", errCode);
                        OnUnregisteredFromSlave?.Invoke(errCode);
                    });
            }
        }

        public void GetAllMasters()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "GetAllMasters Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_GetAllMasters, null, null, 
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        List<MasterClient> list = new List<MasterClient>();
                        var arr = JHelper.GetJsonArray(data, "masters");
                        if (data != null)
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
                        OnGetAllMasters?.Invoke(errCode, list);
                    });
            }
        }

        public void Broadcast(JToken data)
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                _isQuarying = true;
                Utility.LogDebug("MasterSlavesGroupService", "Broadcast Begin");
                BackendRequest.Create(serviceName, BackendOps.Cmd_Broadcast, data, null,
                    (int errCode, JToken data, object context)=> {
                        _isQuarying = false;
                        Utility.LogDebug("MasterSlavesGroupService", "Broadcast End", errCode);
                        OnBroadcast?.Invoke(errCode);
                    });
            }
        }
    }
}
