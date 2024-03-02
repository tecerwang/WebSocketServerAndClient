using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// 负责与服务器进行通讯管理，通讯由一个 wsclient Proxy 实际负责收发
    /// </summary>
    public class WSBackend
    {
        public enum WSBackendState
        {
            Close,
            Open
        }

        public WSBackendState State { get; private set; } = WSBackendState.Close;

        public event Action OnBackendStateChanged;

        public static WSBackend singleton { get; private set; }

        public MonoBehaviour monoGameObject { get; private set; }

        private WSBackend()
        { }

        public static bool CreateSingleton(MonoBehaviour monoBehaviour)
        {
            if (singleton == null)
            {
                singleton = new WSBackend();
                singleton.monoGameObject = monoBehaviour;
                return true;
            }
            return false;
        }

        private bool _isInited = false;

        /// <summary>
        /// 实际负责收发的通讯代理
        /// </summary>
        private WebSocketClientProxy _wsClientProxy;

        /// <summary>
        /// 目前只负责网络心跳
        /// </summary>
        private ConnMonitor _monitor;

        public void Init(string backendUrl)
        {
            if (!_isInited)
            {
                Utility.LogDebug("Backend", "Create ws client proxy Gameobject");
                // 实例化一个 websocketclientproxy
                var proxyObj = new GameObject("[WebSocketClientProxy]");
                proxyObj.transform.parent = singleton.monoGameObject.transform;
                _wsClientProxy = proxyObj.AddComponent<WebSocketClientProxy>();
                _wsClientProxy.OnClientStateChanged += WsClientProxy_OnClientStateChanged;
                _wsClientProxy.OnClientProxyRecievedMsg += WsClientProxy_OnClientProxyRecievedMsg;
                _wsClientProxy.clientSubName = "wsClientSingletion";
                _wsClientProxy.wsUrl = backendUrl;
                _isInited = true;
            }
            else
            {
                Utility.LogDebug("Backend", "Already inited");
            }
        }

        /// <summary>
        /// 开始连接服务器
        /// </summary>
        /// <returns></returns>
        public async Task Connect2Server()
        {
            Utility.LogDebug("Backend", "Connect to ws server start");
            TaskCompletionSource<bool> completeSource = new TaskCompletionSource<bool>();
            Connect2Server(() =>
            {
                // 实例化连接监视器
                _monitor = ConnMonitor.Create(this);
                _monitor.Init();
                completeSource.SetResult(true);
            });
            await completeSource.Task;
        }

        private void WsClientProxy_OnClientStateChanged()
        {
            if (_wsClientProxy.State == WebSocketClient.ClientState.open)
            {
                State = WSBackendState.Open;
                OnBackendStateChanged?.Invoke();
            }
            else
            {
                State = WSBackendState.Close;
                OnBackendStateChanged?.Invoke();
            }
        }

        private void Connect2Server(Action onComplete)
        {
            _wsClientProxy.Connect().onComplete = (result) =>
            {
                if (result.isSuccessful)
                {
                    Utility.LogDebug("Backend", "connect to ws server end");
                    State = WSBackendState.Open;
                    onComplete?.Invoke();
                    WsClientProxy_OnClientStateChanged();
                }
                else
                {
                    Utility.LogDebug("Backend", "connect to ws server fail,reconnect start");
                    Connect2Server(onComplete);
                }
            };
        }

        private void WsClientProxy_OnClientProxyRecievedMsg(MsgPack data)
        {
            if (data.type == MsgPack.ResponseType)
            {
                var response = (data as ResponsePack);
                if (response != null)
                {
                    int errCode = response.errCode;
                    OnBackendResponse?.Invoke(data.serviceName, data.cmd, errCode, response.rid, data.data);
                }
            }
            else if (data.type == MsgPack.NotifyType)
            {
                var notify = (data as NotifyPack);
                if (notify != null)
                {
                    OnBackendNotify?.Invoke(data.serviceName, data.cmd, data.data);
                }
            }
        }

        /// <summary>
        /// 收到 backend 回执消息
        /// </summary>
        public event Action<string, string, int, int, JToken> OnBackendResponse;

        /// <summary>
        /// 收到 backend Notify
        /// </summary>
        public event Action<string, string, JToken> OnBackendNotify;

        /// <summary>
        /// 向 backend 发送请求
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        /// <returns> rid (request id) </returns>
        public WebSocketClientProxy.ProxyResult CreateBackendRequest(string serviceName, string cmd, JToken data)
        {
            if (_wsClientProxy == null || _wsClientProxy.State != WebSocketClient.ClientState.open)
            {
                return null;
            }
            return _wsClientProxy.SendRequest(serviceName, cmd, data);
        }
    }
}
