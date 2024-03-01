using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketClient;

public class BroadcastServiceSample : MonoBehaviour
{
    public Button BtnJoinInGroup;
    public Button BtnLeaveGroup;
    public Button BtnBroadcastMsg;
    public InputField InputBroadcastContent;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitLogicManagerInitComplete());
    }

    private IEnumerator WaitLogicManagerInitComplete()
    {
        while (BackendManager.singleton == null || !BackendManager.singleton.IsInited)
        {
            yield return new WaitForEndOfFrame();
        }
        BtnJoinInGroup.onClick.AddListener(ClickBtnJoinInGroup);
        BtnLeaveGroup.onClick.AddListener(ClickBtnLeaveGroup);
        BtnBroadcastMsg.onClick.AddListener(ClickBtnBroadcastMsg);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsBackendAvaliable())
        {
            BtnJoinInGroup.enabled = true;
            BtnLeaveGroup.enabled = true;
            BtnBroadcastMsg.enabled = true;
            InputBroadcastContent.enabled = true;
        }
        else
        {
            BtnJoinInGroup.enabled = false;
            BtnLeaveGroup.enabled = false;
            BtnBroadcastMsg.enabled = false;
            InputBroadcastContent.enabled = false;
        }
    }

    private void ClickBtnJoinInGroup()
    {
        if (IsBackendAvaliable())
        {
            BackendManager.singleton.broadcastManager.JoinInGroup();
        }
    }

    private void ClickBtnLeaveGroup()
    {
        if (IsBackendAvaliable())
        {
            BackendManager.singleton.broadcastManager.LeaveFromGroup();
        }
    }

    private void ClickBtnBroadcastMsg()
    {
        var content = InputBroadcastContent.text;
        if (IsBackendAvaliable() && !string.IsNullOrEmpty(content))
        {
            BackendManager.singleton.broadcastManager.Broadcast(content);
        }
    }

    private bool IsBackendAvaliable()
    {
        return BackendManager.singleton != null 
            && BackendManager.singleton.IsInited 
            && BackendManager.singleton.wsState == WSBackend.WSBackendState.Open;
    }

}
