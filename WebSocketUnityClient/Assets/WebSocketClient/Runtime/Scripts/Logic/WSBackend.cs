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
    /// ���������������ͨѶ����ͨѶ��һ�� wsclient Proxy ʵ�ʸ����շ�
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
        /// ʵ�ʸ����շ���ͨѶ����
        /// </summary>
        private WebSocketClientProxy _wsClientProxy;

        /// <summary>
        /// Ŀǰֻ������������
        /// </summary>
        private ConnMonitor _monitor;

        public void Init(string backendUrl)
        {
            if (!_isInited)
            {
                Utility.LogDebug("Backend", "Create ws client proxy Gameobject");
                // ʵ����һ�� websocketclientproxy
                var proxyObj = new GameObject("[WebSocketClientProxy]");
                proxyObj.transform.parent = singleton.monoGameObject.transform;
                _wsClientProxy = proxyObj.AddComponent<WebSocketClientProxy>();
                _wsClientProxy.OnClientStateChanged += WsClientProxy_OnClientStateChanged;
                _wsClientProxy.OnClientProxyRecievedMsg += WsClientProxy_OnClientProxyRecievedMsg;
                _wsClientProxy.clientSubName = "wsClientSingleton";
                _wsClientProxy.wsUrl = backendUrl;
                _isInited = true;
            }
            else
            {
                Utility.LogDebug("Backend", "Already inited");
            }
        }

        /// <summary>
        /// ��ʼ���ӷ�����
        /// </summary>
        /// <returns></returns>
        public async Task Connect2ServerAsync()
        {
            Utility.LogDebug("Backend", "Connect to ws server start");
            while (!await Connect2Server());
            // ʵ�������Ӽ�����
            _monitor = ConnMonitor.Create(this);
            _monitor.Init();
        }

        private void WsClientProxy_OnClientStateChanged()
        {
            if (_wsClientProxy.State == WebSocketClient.ClientState.open)
            {
                if (State != WSBackendState.Open)
                {
                    State = WSBackendState.Open;
                    OnBackendStateChanged?.Invoke();
                }
            }
            else
            {
                if (State != WSBackendState.Close)
                {
                    State = WSBackendState.Close;
                    OnBackendStateChanged?.Invoke();
                }
            }
        }

        private async Task<bool> Connect2Server()
        {
            TaskCompletionSource<bool> completeSource = new TaskCompletionSource<bool>();
            _wsClientProxy.Connect().onComplete = (isSuccessful) =>
            {
                if (isSuccessful)
                {
                    Utility.LogDebug("Backend", "connect to ws server end");
                    completeSource.SetResult(true);
                    WsClientProxy_OnClientStateChanged();
                }
                else
                {
                    Utility.LogDebug("Backend", "connect to ws server fail,reconnect start");
                    completeSource.SetResult(false);
                }
            };
            return await completeSource.Task;
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
        /// �յ� backend ��ִ��Ϣ
        /// </summary>
        public event Action<string, string, int, int, JToken> OnBackendResponse;

        /// <summary>
        /// �յ� backend Notify
        /// </summary>
        public event Action<string, string, JToken> OnBackendNotify;

        /// <summary>
        /// �� backend ��������
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
