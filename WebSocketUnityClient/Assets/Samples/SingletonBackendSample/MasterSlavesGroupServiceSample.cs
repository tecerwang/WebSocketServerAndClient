using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using WebSocketClient;

public class MasterSlavesGroupServiceSample : MonoBehaviour
{
    public Button btnTestProtocols;
    public InputField inputMasterName;
    public InputField inputBroadcast;

    // Start is called before the first frame update
    void Start()
    {
        /// 一步一步测试
        btnTestProtocols.onClick.AddListener(()=>
        {
            TestProtocols();           
        });

        ResetBtnText();

        BackendManager.singleton.msGroupManager.OnRegisteredAsMaster += MsGroupManager_OnRegisteredAsMaster;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromMaster += MsGroupManager_OnUnregisteredFromMaster;
        BackendManager.singleton.msGroupManager.OnRegisteredAsSlave += MsGroupManager_OnRegisteredAsSlave;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromSlave += MsGroupManager_OnUnregisteredFromSlave;
        BackendManager.singleton.msGroupManager.OnMasterCollectionChanged += MsGroupManager_OnMasterCollectionChanged;
        BackendManager.singleton.msGroupManager.OnGetAllMasters += MsGroupManager_OnGetAllMasters;
        BackendManager.singleton.msGroupManager.OnBroadcast += MsGroupManager_OnBroadcast;
        BackendManager.singleton.msGroupManager.OnRecievedBroadcast += MsGroupManager_OnRecievedBroadcast;
    }

    private void OnDestroy()
    {
        BackendManager.singleton.msGroupManager.OnRegisteredAsMaster -= MsGroupManager_OnRegisteredAsMaster;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromMaster -= MsGroupManager_OnUnregisteredFromMaster;
        BackendManager.singleton.msGroupManager.OnRegisteredAsSlave -= MsGroupManager_OnRegisteredAsSlave;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromSlave -= MsGroupManager_OnUnregisteredFromSlave;
        BackendManager.singleton.msGroupManager.OnMasterCollectionChanged -= MsGroupManager_OnMasterCollectionChanged;
        BackendManager.singleton.msGroupManager.OnGetAllMasters -= MsGroupManager_OnGetAllMasters;
        BackendManager.singleton.msGroupManager.OnBroadcast -= MsGroupManager_OnBroadcast;
        BackendManager.singleton.msGroupManager.OnRecievedBroadcast -= MsGroupManager_OnRecievedBroadcast;
    }

    private enum ProtocalStep
    {       
        registerAsMaster,
        getAllMasters,
        registerAsSlave,
        broadcastMsg,
        unregisterAsSlave,
        unregisterAsMaster,
        complete
    }


    private int step = 0;

    private void TestProtocols()
    {
        // 注册一个 master
        if (step == (int)ProtocalStep.registerAsMaster)
        {
            string name = inputMasterName.text;
            if (!string.IsNullOrEmpty(name))
            {
                BackendManager.singleton.msGroupManager.RegisterAsMaster(name);
            }
        }
        // 获取到所有 master
        else if (step == (int)ProtocalStep.getAllMasters)
        {
            BackendManager.singleton.msGroupManager.GetAllMasters();
        }
        // 注册一个 slave
        else if (step == (int)ProtocalStep.registerAsSlave)
        {
            BackendManager.singleton.msGroupManager.RegisterAsSlave(name);
        }
        // 广播消息
        else if (step == (int)ProtocalStep.broadcastMsg)
        {
            string msg = inputBroadcast.text;
            if (!string.IsNullOrEmpty(msg))
            {
                BackendManager.singleton.msGroupManager.Broadcast(msg);
            }
        }
        // 注销一个slave
        else if (step == (int)ProtocalStep.unregisterAsSlave)
        {
            BackendManager.singleton.msGroupManager.UnregisterFromSlave();
        }
        // 注销一个master
        else if (step == (int)ProtocalStep.unregisterAsMaster)
        {
            BackendManager.singleton.msGroupManager.UnRegisterFromMaster();
        }
        // 测试结束
        else if (step == (int)ProtocalStep.complete)
        {
            step = 0;
            ResetBtnText();
        }
    }

    private void ResetBtnText()
    {
        btnTestProtocols.GetComponentInChildren<Text>().text = ((ProtocalStep)step).ToString();
    }

    private void MsGroupManager_OnRecievedBroadcast(Newtonsoft.Json.Linq.JToken data)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnRecievedBroadcast", data);
    }

    private void MsGroupManager_OnBroadcast(bool obj)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnBroadcast", obj);
        if (obj)
        {
            step++;
            ResetBtnText();
        }
    }

    private void MsGroupManager_OnGetAllMasters(int errCode, Newtonsoft.Json.Linq.JToken data)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnGetAllMasters", errCode, data);
        if (errCode == ErrCode.OK)
        {
            step++;
            ResetBtnText();
        }
    }

    private void MsGroupManager_OnMasterCollectionChanged()
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnMasterCollectionChanged");
    }

    private void MsGroupManager_OnUnregisteredFromSlave(bool obj)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnUnregisteredFromSlave", obj);
        if (obj)
        {
            step++;
            ResetBtnText();
        }
    }

    private void MsGroupManager_OnRegisteredAsSlave(bool obj)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnRegisteredAsSlave", obj);
        if (obj)
        {
            step++;
            ResetBtnText();
        }
    }

    private void MsGroupManager_OnUnregisteredFromMaster(bool obj)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnUnregisteredFromMaster", obj);
        if (obj)
        {
            step++;
            ResetBtnText();
        }
    }

    private void MsGroupManager_OnRegisteredAsMaster(bool obj)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnRegisteredAsMaster", obj);
        if (obj)
        {
            step++;
            ResetBtnText();
        }
    }
}
