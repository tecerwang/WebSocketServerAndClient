using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using WebSocketClient;
using WebSocketClient.Utilities.Data;
using static WebSocketClient.MasterSlavesGroupService;

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
    async void Start()
    {
        Utility.logLevel = Utility.LogLevel.Internal;

        await BackendManager.WaitForInitAsync();

        ResetUIState();

        /// һ��һ������
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

    /// <summary>
    /// ����һ���˵���
    /// </summary>
    /// <returns></returns>
    private TreeItem<MenuItem>.Collection CreateMasterMenuData()
    {
        var collection = new TreeItem<MenuItem>.Collection();

        // ��һ�ַ�ʽ���ò˵��ṹ
        collection.StartFromRoot()
        // ����ͬ���˵�
        .Next(new MenuItem("�˵�_1")).Children((children) =>
        {   // �����Ӳ˵�
            children.Next(new MenuItem("�˵�_1_1"))
                    .Next(new MenuItem("�˵�_1_2"))
                    .Next(new MenuItem("�˵�_1_3")).Children(children =>
                    {
                        children.Next(new MenuItem("�˵�_1_3_1"))
                                .Next(new MenuItem("�˵�_1_3_2"))
                                .Next(new MenuItem("�˵�_1_3_3"));
                    })
                    .Next(new MenuItem("�˵�_1_4"));
        })
        .Next(new MenuItem("�˵�_2"))
        .Next(new MenuItem("�˵�_3")).Children((children) =>
         {
             children.Next(new MenuItem("�˵�_3_1")).Children(children =>
             {
                 children.Next(new MenuItem("�˵�_3_1_1"))
                         .Next(new MenuItem("�˵�_3_1_2"));
             })
             .Next(new MenuItem("�˵�_3_2"));
         });

        // �ڶ��ַ�ʽ���ò˵��ṹ��ע�� CreateItem(menu,parent) �ڶ�������ʱ��ǰ����menu�ĸ���
        var menu4 = collection.CreateRootItem(new MenuItem("�˵�_4"));
        var menu4_1 = collection.CreateItem(new MenuItem("�˵�_4_1"), menu4);
        var menu4_2 = collection.CreateItem(new MenuItem("�˵�_4_2"), menu4);
        var menu4_3 = collection.CreateItem(new MenuItem("�˵�_4_3"), menu4);
        return collection;
    }

    private void Singleton_OnBackendStateChanged()
    {
        if (WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            BackendManager.singleton.msGroupManager.RegisterAsListener();
        }       
        ResetUIState();
    }

    private void Click_RegisterAsMaster()
    {
        // ����һ���˵���
        var collection = CreateMasterMenuData();
        Utility.LogDebug("MasterSlavesGroupServiceSample", "CreateMasterData", collection.ToJson());

        string masterName = inputMasterName.text;
        if (!string.IsNullOrEmpty(masterName))
        {
            BackendManager.singleton.msGroupManager.RegisterAsMaster(masterName, collection.ToJson());
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

    private void MsGroupManager_OnGetAllMasters(int errCode, IEnumerable<MasterClient> masters)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnGetAllMasters result begin");
        foreach (var master in masters)
        {
            Utility.LogDebug("MasterSlavesGroupServiceSample", master.ToString());
        }
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnGetAllMasters result end");
    }

    private void MsGroupManager_OnMasterCollectionChanged(MasterClient master)
    {
        Utility.LogDebug("MasterSlavesGroupServiceSample", "OnMasterCollectionChanged", master.ToString());
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
