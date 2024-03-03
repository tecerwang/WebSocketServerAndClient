using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using WebSocketClient;

public class MasterSlavesGroupServiceSample : MonoBehaviour
{
    public Button registerMasterBtn;
    public Button unRegisterMasterBtn;
    public Button registerSlaveBtn;
    public Button unRegisterSlaveBtn;
    public Button getAllMastersBtn;
    public Button broadcastBtn;
    public InputField inputMasterName;
    public InputField inputMasterId;
    public InputField inputBroadcast;

    // Start is called before the first frame update
    void Start()
    {
        ResetUIState();

        /// 一步一步测试
        registerMasterBtn.onClick.AddListener(Click_RegisterAsMaster);
        unRegisterMasterBtn.onClick.AddListener(Click_UnregisterFromMaster);
        registerSlaveBtn.onClick.AddListener(Click_RegisterAsSlave);
        unRegisterSlaveBtn.onClick.AddListener(Click_UnregisterFromSlave);
        getAllMastersBtn.onClick.AddListener(Click_GetAllMasters);
        broadcastBtn.onClick.AddListener(Click_BroadCast);


        WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;

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
        WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;

        BackendManager.singleton.msGroupManager.OnRegisteredAsMaster -= MsGroupManager_OnRegisteredAsMaster;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromMaster -= MsGroupManager_OnUnregisteredFromMaster;
        BackendManager.singleton.msGroupManager.OnRegisteredAsSlave -= MsGroupManager_OnRegisteredAsSlave;
        BackendManager.singleton.msGroupManager.OnUnregisteredFromSlave -= MsGroupManager_OnUnregisteredFromSlave;
        BackendManager.singleton.msGroupManager.OnMasterCollectionChanged -= MsGroupManager_OnMasterCollectionChanged;
        BackendManager.singleton.msGroupManager.OnGetAllMasters -= MsGroupManager_OnGetAllMasters;
        BackendManager.singleton.msGroupManager.OnBroadcast -= MsGroupManager_OnBroadcast;
        BackendManager.singleton.msGroupManager.OnRecievedBroadcast -= MsGroupManager_OnRecievedBroadcast;
    }

    private void Singleton_OnBackendStateChanged()
    {
        ResetUIState();
    }

    private void Click_RegisterAsMaster()
    {
        string masterName = inputMasterName.text;
        if (!string.IsNullOrEmpty(masterName))
        {
            BackendManager.singleton.msGroupManager.RegisterAsMaster(masterName);
            ResetUIState();
        }
    }

    private void MsGroupManager_OnRegisteredAsMaster(int errCode)
    {
        ResetUIState();
    }

    private void Click_UnregisterFromMaster()
    {
        BackendManager.singleton.msGroupManager.UnRegisterFromMaster();
        ResetUIState();
    }

    private void MsGroupManager_OnUnregisteredFromMaster(int errCode)
    {
        ResetUIState();
    }

    private void Click_RegisterAsSlave()
    {
        string masterId = inputMasterId.text;
        if (!string.IsNullOrEmpty(masterId))
        {
            BackendManager.singleton.msGroupManager.RegisterAsSlave(masterId);
            ResetUIState();
        }
    }

    private void MsGroupManager_OnRegisteredAsSlave(int errCode)
    {
        ResetUIState();
    }

    private void Click_UnregisterFromSlave()
    {
        BackendManager.singleton.msGroupManager.UnregisterFromSlave();
        ResetUIState();
    }

    private void MsGroupManager_OnUnregisteredFromSlave(int errCode)
    {
        ResetUIState();
    }

    private void Click_BroadCast()
    {
        string msg = inputBroadcast.text;
        if (!string.IsNullOrEmpty(msg))
        {
            JObject jobj = new JObject();
            jobj.Add("msg", msg);
            BackendManager.singleton.msGroupManager.Broadcast(jobj);
        }
    }

    
    private void MsGroupManager_OnBroadcast(int errCode)
    {

    }

    private void Click_GetAllMasters()
    {
        BackendManager.singleton.msGroupManager.GetAllMasters();
    }

    private void MsGroupManager_OnGetAllMasters(int errCode, Newtonsoft.Json.Linq.JToken data)
    {

    }

    private void MsGroupManager_OnMasterCollectionChanged()
    {

    }

    private void MsGroupManager_OnRecievedBroadcast(Newtonsoft.Json.Linq.JToken data)
    {

    }

    private void ResetUIState()
    {
        var clientState = BackendManager.singleton.msGroupManager.clientState;

        registerMasterBtn.interactable = clientState == MasterSlavesGroupService.ClientState.Idle;
        unRegisterMasterBtn.interactable = clientState == MasterSlavesGroupService.ClientState.IsMaster;
        registerSlaveBtn.interactable = clientState == MasterSlavesGroupService.ClientState.Idle;
        unRegisterSlaveBtn.interactable = clientState == MasterSlavesGroupService.ClientState.IsSlave;
        getAllMastersBtn.interactable = true;
        broadcastBtn.interactable = clientState != MasterSlavesGroupService.ClientState.Idle;
        inputBroadcast.interactable = clientState != MasterSlavesGroupService.ClientState.Idle;
        inputMasterName.interactable = clientState == MasterSlavesGroupService.ClientState.Idle;
        inputMasterId.interactable = clientState == MasterSlavesGroupService.ClientState.Idle;

    }   
}
