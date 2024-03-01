using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;
using WebSocketClient;

public class MasterSlavesGroupService : BackendServiceManagerBase
{

    private bool _isQuarying = false;
    private const string serviceName = "MasterSlavesGroupService";

    /// <summary>
    /// 注册成为 master
    /// </summary>
    public event Action<bool> OnRegisteredAsMaster;

    /// <summary>
    /// 注销 master
    /// </summary>
    public event Action<bool> OnUnregisteredFromMaster;

    /// <summary>
    /// 注册成为 slave
    /// </summary>
    public event Action<bool> OnRegisteredAsSlave;

    /// <summary>
    /// 注销 slave
    /// </summary>
    public event Action<bool> OnUnregisteredFromSlave;

    /// <summary>
    /// 服务器上的 master 集合发生变化
    /// </summary>
    public event Action OnMasterCollectionChanged;

    /// <summary>
    /// 获取所有的 master
    /// </summary>
    public event Action<int, JToken> OnGetAllMasters;

    /// <summary>
    /// 广播消息完成
    /// </summary>
    public event Action<bool> OnBroadcast;

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

    private void Singleton_OnBackendStateChanged()
    {
        
    }

    private void Singleton_OnBackendNotify(string serviceName, string cmd, JToken data)
    {
        if (serviceName == MasterSlavesGroupService.serviceName)
        {
            // master collectoion on server changed
            if (cmd == BackendOps.Notify_OnMasterCollectionChanged)
            {
                OnMasterCollectionChanged?.Invoke();
            }
            // 收到别人发送的消息
            else if (cmd == BackendOps.Cmd_Broadcast)
            {
                OnRecievedBroadcast?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// 注册本机作为 master
    /// </summary>
    /// <param name="masterName"></param>
    public void RegisterAsMaster(string masterName)
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            _isQuarying = true;
            JObject data = new JObject();
            data.Add("masterName", masterName);
            data.Add("masterData", null);
            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_RegisterAsMaster, data, null, OnRegisterMasterResponse);
        }
    }

    private void OnRegisterMasterResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnRegisteredAsMaster?.Invoke(errCode == ErrCode.OK);       
    }

    /// <summary>
    /// 注销本机作为 master
    /// </summary>
    public void UnRegisterFromMaster()
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_UnregisterFromMaster, null, null, OnUnregisterMasterResponse);
        }
    }

    private void OnUnregisterMasterResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnUnregisteredFromMaster?.Invoke(errCode == ErrCode.OK);
    }

    /// <summary>
    /// 注册本机作为 slave
    /// </summary>
    public void RegisterAsSlave(string masterId)
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            _isQuarying = true;
            JObject data = new JObject();
            data.Add("masterId", masterId);
            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_RegisterAsSlave, data, null, OnRegisterSlaveResponse);
        }
    }

    private void OnRegisterSlaveResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnRegisteredAsSlave?.Invoke(errCode == ErrCode.OK);
    }

    public void UnregisterFromSlave()
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            _isQuarying = true;
            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_UnregisterFromSlave, null, null, OnUnregisterSlaveResponse);
        }
    }

    private void OnUnregisterSlaveResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnUnregisteredFromSlave?.Invoke(errCode == ErrCode.OK);
    }

    public void GetAllMasters()
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            _isQuarying = true;
            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_GetAllMasters, null, null, OnGetAllMastersResponse);
        }
    }

    private void OnGetAllMastersResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnGetAllMasters?.Invoke(errCode, data);
    }

    public void Broadcast(JToken data)
    {
        if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            _isQuarying = true;

            BackendRequest.CreateRetry(serviceName, BackendOps.Cmd_Broadcast, data, null, OnBroadcastResponse);
        }
    }

    private void OnBroadcastResponse(int errCode, JToken data, object context)
    {
        _isQuarying = false;
        OnBroadcast?.Invoke(errCode == ErrCode.OK);
    }
}
