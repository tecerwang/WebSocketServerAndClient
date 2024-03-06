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

        private string _clientId;

        /// <summary>
        /// ʵ�ʸ����շ���ͨѶ����
        /// </summary>
        private WebSocketClient _wsClient;

        public void Init(string url)
        {
            if (!_isInited)
            {
                _clientId = GetClientConnectionId();
                _wsClient = new WebSocketClient(url + $"?clientId={_clientId}");
                monoGameObject.StartCoroutine(HandleReceivedMsg());
                _wsClient.OnClientStateChanged += OnClientStateChanged;           
                _isInited = true;
            }
            else
            {
                Utility.LogDebug("Backend", "Already inited");
            }
        }

        private void OnClientStateChanged()
        {
            if (_wsClient.State == WebSocketClient.ClientState.open)
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

        /// <summary>
        /// ��ʼ���ӷ�����
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAndRecvAsync()
        {
            while (Application.isPlaying)
            {
                // ������ӳɹ����������߳�
                await _wsClient.ConnectAndRecvAsync();
                Utility.LogDebug("Backend", "connect to ws server fail, reconnect start after 1 sec", _wsClient.State);
                await Task.Delay(1000);
            }

            Utility.LogDebug("Backend", "Reconnect stop by app exit", _wsClient.State);
        }
       
        private IEnumerator HandleReceivedMsg()
        {         
            while (true)
            {
                foreach (var receivedMessage in _wsClient.GetCurrentRecievedDataFromQueue())
                {
                    var data = JObject.Parse(receivedMessage);
                    if (data == null)
                    {
                        continue;
                    }
                    var pack = MsgPack.Parse(data);
                    if (pack != null)
                    {
                        switch (pack.type)
                        {
                            case MsgPack.RequestType:
                                if (pack is RequestPack request)
                                {
                                    OnBackendRequest?.Invoke(request);
                                }
                                break;
                            case MsgPack.ResponseType:
                                if (pack is ResponsePack response)
                                {
                                    OnBackendResponse?.Invoke(response);
                                }
                                break;
                            case MsgPack.NotifyType:
                                if (pack is NotifyPack notify)
                                {
                                    OnBackendNotify?.Invoke(notify);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        Utility.LogInternalDebug($"[WebSocket Client] Received message from server: {receivedMessage} Deserialize fail");
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// �յ� backend ������Ϣ �����ִ
        /// </summary>
        public event Action<RequestPack> OnBackendRequest;

        /// <summary>
        /// �յ� backend ��ִ��Ϣ
        /// </summary>
        public event Action<ResponsePack> OnBackendResponse;

        /// <summary>
        /// �յ� backend Notify
        /// </summary>
        public event Action<NotifyPack> OnBackendNotify;

        public class BackendRequestResult
        {
            public WebSocketClient.SendMsgState state;
            public int rid;
        }

        /// <summary>
        /// �� backend ��������
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        /// <returns> rid (request id) </returns>
        public async Task<BackendRequestResult> CreateBackendRequest(string serviceName, string cmd, JToken data)
        {
            if (_wsClient != null && _wsClient.State == WebSocketClient.ClientState.open)
            {
                RequestPack requestPack = new RequestPack()
                {
                    rid = RequestPack.GetRequestId(),
                    clientId = _clientId,
                    cmd = cmd,
                    data = data,
                    serviceName = serviceName
                };
                var sendMsg = requestPack.ToString();
                var state = await _wsClient.SendMessageAsync(sendMsg);
                return new BackendRequestResult()
                {
                    state = state,
                    rid = requestPack.rid
                };
            }
            return new BackendRequestResult()
            {
                state = WebSocketClient.SendMsgState.Unkown,
                rid = -1
            };
        }

        public async Task CloseAsync()
        {
             await _wsClient?.CloseAsync();
        }

        /// <summary>
        /// ��ȡ clientId
        /// </summary>
        /// <param name="extroInfo"></param>
        /// <returns></returns>
        private static string GetClientConnectionId()
        {
            return SystemInfo.deviceType.ToString() + "_" + Utility.GetDeviceId();
        }
    }
}
