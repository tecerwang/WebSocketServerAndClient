using System.Collections;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using WebSocketClient;

public class MultiClientSample : MonoBehaviour
{
    public List<WebSocketClientProxy> proxies;

    public InputField textInput;

    public void StartConnect()
    {
        foreach (var p in proxies)
        {
            var result = p.Connect();
            result.onComplete = (result) => 
            {
                Utility.LogDebug($"WebSocketSample '{p.clientSubName}' connect operation completed");
            };
        }
    }

    public void CloseConnection()
    {
        foreach (var p in proxies)
        {
            var result = p.Close();
            result.onComplete = (result) =>
            {
                Utility.LogDebug($"WebSocketSample '{p.clientSubName}' close operation completed");
            };
        }
    }

    public void SendMsg()
    {
        var msg = textInput.text;
        foreach (var p in proxies)
        {
            var result = p.SendRequest("defaultService", "NoCmd", msg);
            result.onComplete = (result) =>
            {
                Utility.LogDebug($"WebSocketSample '{p.clientSubName}' SendMsg operation completed");
            };
        }
    }
}
