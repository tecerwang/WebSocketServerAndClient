using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{

    /// <summary>
    /// Websocket client unity �̴߳��������� webSocketClient ���н���
    /// </summary>
    public class WebSocketClientProxy : MonoBehaviour
    {
        [Tooltip("һ���ͻ��˿��ܴ���������ӣ���Ҫ�õ������Խ������֣������ظ�")]
        public string clientSubName;

        //public string wsUrl = "ws://localhost:8080/websocket";
        public string wsUrl = "ws://localhost:8080/ws";

        private string _clientId;
        private WebSocketClient _client;

        public event Action<MsgPack> OnClientProxyRecievedMsg;

        public event Action OnClientStateChanged;
        public WebSocketClient.ClientState State => _client == null ? WebSocketClient.ClientState.unkown : _client.State;
        /// <summary>
        /// �����������ͬ������result���첽���� onComplete
        /// </summary>
        public class ProxyResult
        {
            /// <summary>
            /// ������ɻص�
            /// </summary>
            public Action<bool> onComplete;
            /// <summary>
            /// �����Ƿ����
            /// </summary>
            public bool isComplete = false;
            /// <summary>
            /// �����Ƿ�ɹ�
            /// </summary>
            public bool isSuccessful = false;
            /// <summary>
            /// request id
            /// </summary>
            public int rid;
        }

        public ProxyResult Connect()
        {
            ProxyResult result = new ProxyResult();
            StartCoroutine(ConnectAsync(result));
            InvokeCompleteIfResultIsCompleteAtTheSameFrame(result);
            return result;
        }

        public ProxyResult Close()
        {
            ProxyResult result = new ProxyResult();
            StartCoroutine(CloseAsync(result));
            StartCoroutine(InvokeCompleteIfResultIsCompleteAtTheSameFrame(result));
            return result;
        }

        /// <summary>
        /// �ͻ������������������
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public ProxyResult SendRequest(string serviceName, string cmd, JToken data)
        {
            ProxyResult result = new ProxyResult();
            int rid = RequestPack.GetRequestId();
            result.rid = rid;
            StartCoroutine(SendRequestAsync(data, serviceName, cmd, rid, result));
            InvokeCompleteIfResultIsCompleteAtTheSameFrame(result);
            return result;
        }

        private IEnumerator ConnectAsync(ProxyResult result)
        {
            result.isComplete = false;

            result.isSuccessful = false;
            // �˴�ΪΨһ�� clientId ��ֵ
            _clientId = GetClientConnectionId(clientSubName);
            var urlWithParam = wsUrl + "?clientId=" + _clientId;
            var task = WebSocketClient.Factory.CreateAndConnectAsync(urlWithParam);

            while (!task.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }

            if (task.Exception == null)
            {
                _client = task.Result;
                if (_client != null && _client.State == WebSocketClient.ClientState.open)
                {
                    /// client ״̬�����仯ʱ������״̬Ҳ�����仯
                    _client.OnClientStateChanged += () => OnClientStateChanged?.Invoke();
                    Utility.LogInternalDebug($"[WebSocket Client Proxy {clientSubName}] is opend");
                    result.isSuccessful = true;

                    Utility.LogInternalDebug($"[WebSocket Client Proxy {clientSubName}] begin to receiveMsg");
                    StartCoroutine(HandleReceivedMsg());
                }
                else
                {
                    Utility.LogInternalDebug($"[WebSocket Client Proxy {clientSubName}] connect fail");
                }
            }
            else
            {
                Utility.LogExpection($"[WebSocket Client Proxy {clientSubName}] ConnectAsync expection {task.Exception}");
            }

            result.isComplete = true;
            result?.onComplete?.Invoke(result.isSuccessful);
        }

        private IEnumerator CloseAsync(ProxyResult result)
        {
            if (_client != null)
            {
                if (_client != null)
                {
                    var task = _client.CloseAsync();

                    // �첽�ر� �ȴ�task����
                    while (!task.IsCompleted)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    if (task.Exception == null)
                    {
                        result.isSuccessful = true;
                        _client = null;
                        Utility.LogInternalDebug($"[WebSocket Client Proxy {clientSubName}] is closed");
                    }
                    else
                    {
                        Utility.LogExpection($"[WebSocket Client Proxy {clientSubName}] CloseAsync expection {task.Exception}");
                    }
                }
            }
            result.isComplete = true;
            result?.onComplete?.Invoke(result.isSuccessful);
        }

        private IEnumerator SendRequestAsync(JToken data, string serviceName, string cmd, int rid, ProxyResult result)
        {
            if (_client != null && _client.State == WebSocketClient.ClientState.open)
            {
                if (_client != null)
                {
                    RequestPack requestPack = new RequestPack()
                    {
                        rid = rid,
                        clientId = _clientId,
                        cmd = cmd,
                        data = data,
                        serviceName = serviceName
                    };
                    var sendMsg = requestPack.ToString();
                    var task = _client.SendMessageAsync(sendMsg);

                    // �첽�ر� �ȴ�task����
                    while (!task.IsCompleted)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    if (task.Exception == null)
                    {
                        result.isSuccessful = true;
                        Utility.LogInternalDebug($"[WebSocket Client Proxy {clientSubName}] sent msg {sendMsg}");
                    }
                    else
                    {
                        Utility.LogExpection($"[WebSocket Client Proxy {clientSubName}] SendMsgAsync expection {task.Exception}");
                    }
                }
            }
            result.isComplete = true;
            result?.onComplete?.Invoke(result.isSuccessful);
        }

        private IEnumerator HandleReceivedMsg()
        {
            while (true)
            {
                var data = _client.GetCurrentRecievedDataFromQueue();
                if (data != null)
                {
                    OnClientProxyRecievedMsg?.Invoke(data);
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// �����������ͬһ֡�Ѿ�����������Ҫ�ȴ���һ֡���������� onComplete����Ϊ result �����Ҫ��һ֡���ݳ�ȥ��a
        /// ��mock ����ʱ���ܻ��õ���
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private IEnumerator InvokeCompleteIfResultIsCompleteAtTheSameFrame(ProxyResult result)
        {
            if (result.isSuccessful)
            {
                yield return new WaitForEndOfFrame();
                result?.onComplete?.Invoke(result.isSuccessful);
            }
        }

        /// <summary>
        /// ��ȡ clientId
        /// </summary>
        /// <param name="extroInfo"></param>
        /// <returns></returns>
        private static string GetClientConnectionId(string extroInfo)
        {
            return SystemInfo.deviceType.ToString() + "_" + Utility.GetDeviceId() + (!string.IsNullOrEmpty(extroInfo) ? ("_" + extroInfo) : string.Empty);
        }

        /// <summary>
        /// ����ر�ʱ�����Ͽ�����
        /// </summary>
        private void OnApplicationQuit()
        {
            if (_client != null && _client.State != WebSocketClient.ClientState.close)
            {
                _ = _client.CloseAsync();
            }
        }
    }
}
