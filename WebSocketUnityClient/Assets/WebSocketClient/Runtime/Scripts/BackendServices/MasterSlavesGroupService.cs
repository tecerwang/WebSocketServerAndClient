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
    /// ע���Ϊ master
    /// </summary>
    public event Action<bool> OnRegisteredAsMaster;

    /// <summary>
    /// ע�� master
    /// </summary>
    public event Action<bool> OnUnregisteredFromMaster;

    /// <summary>
    /// ע���Ϊ slave
    /// </summary>
    public event Action<bool> OnRegisteredAsSlave;

    /// <summary>
    /// ע�� slave
    /// </summary>
    public event Action<bool> OnUnregisteredFromSlave;

    /// <summary>
    /// �������ϵ� master ���Ϸ����仯
    /// </summary>
    public event Action OnMasterCollectionChanged;

    /// <summary>
    /// ��ȡ���е� master
    /// </summary>
    public event Action<int, JToken> OnGetAllMasters;

    /// <summary>
    /// �㲥��Ϣ���
    /// </summary>
    public event Action<bool> OnBroadcast;

    /// <summary>
    /// �յ����˷����Ĺ㲥��Ϣ
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
            // �յ����˷��͵���Ϣ
            else if (cmd == BackendOps.Cmd_Broadcast)
            {
                OnRecievedBroadcast?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// ע�᱾����Ϊ master
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
    /// ע��������Ϊ master
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
    /// ע�᱾����Ϊ slave
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
