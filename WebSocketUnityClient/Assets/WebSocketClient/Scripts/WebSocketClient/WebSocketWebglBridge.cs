using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace WebSocketClient
{
    public class WebSocketWebglBridge : MonoBehaviour
    {
#if UNITY_WEBGL
    private IntPtr wsPtr;

    [DllImport("__Internal")]
    private static extern System.IntPtr WebSocket_Connect(string url, int length);

    [DllImport("__Internal")]
    private static extern void WebSocket_Send(System.IntPtr ws, string message);

    [DllImport("__Internal")]
    private static extern void WebSocket_Close(System.IntPtr ws);

    public void OnWebSocketOpen()
    {
        Debug.Log("WebSocketWebglBridge connection opened");
    }

    public void OnWebSocketMessage(string message)
    {
        Debug.Log("WebSocketWebglBridge message received: " + message);
    }

    public void OnWebSocketError(string errorMessage)
    {
        Debug.LogError("WebSocketWebglBridge error: " + errorMessage);
    }

    public void OnWebSocketClose(string closeCode)
    {
        Debug.Log("WebSocketWebglBridge connection closed with code: " + closeCode);
    }
#endif
    }
}
