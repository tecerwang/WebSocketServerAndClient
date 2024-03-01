using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// ws �ͻ��ˣ�
    /// 1.�������ӣ�
    /// 2.�շ�ԭʼ text ���ݣ�
    /// 3.����������
    /// </summary>
    public class WebSocketClient
    {
        private ClientWebSocket _clientWS;

        private string _wsUrl;

        /// <summary>
        /// �Զ�����
        /// </summary>
        public bool autoReconnect = true;

        private WebSocketClient(string wsUrl)
        {
            this._wsUrl = wsUrl;
        }

        /// <summary>
        /// Client ��������������״̬
        /// </summary>
        public enum ClientState
        {
            /// <summary>
            /// WebSocketClient ��δ����������
            /// </summary>
            unkown,
            /// <summary>
            /// �ر�״̬
            /// </summary>
            close,
            /// <summary>
            /// ������
            /// </summary>
            connecting,
            /// <summary>
            /// ��������
            /// </summary>
            reconnecting,
            /// <summary>
            /// ����״̬
            /// </summary>
            open,
            /// <summary>
            /// �ر���
            /// </summary>
            closing
        }

        public ClientState State { get; private set; } = ClientState.close;

        public event Action OnClientStateChanged;

        private void UpdateClientState(ClientState curState)
        {
            if (State != curState)
            {
                State = curState;
                Utility.LogInternalDebug($"[WebSocketClient] client state updated {State}");
                OnClientStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// ���Խ������ӣ�������ӳɹ�������true
        /// </summary>
        /// <returns>true���ӳɹ�</returns>
        private async Task<bool> TryConnectAsync()
        {
            var timeOutCancellation = new CancellationTokenSource();
            timeOutCancellation.CancelAfter((int)TimeoutMS);

            try
            {
                UpdateClientState(ClientState.connecting);
                Utility.LogDebug("WebSocket Client", $"Connect to WebSocketServer '{_wsUrl}'");
                _clientWS = new ClientWebSocket();
                _clientWS.Options.KeepAliveInterval = new System.TimeSpan(0, 0, (int)HeartbeatIntervalMS / 1000 * 2);
                // �����ʱ,��cancel���߳�
                timeOutCancellation.CancelAfter(TimeoutMS);
                // ���Ӳ����������߳�
                await _clientWS.ConnectAsync(new System.Uri(_wsUrl), timeOutCancellation.Token);
                UpdateClientState(ClientState.open);
            }
            catch (WebSocketException ex)
            {
                if (timeOutCancellation.IsCancellationRequested)
                {
                    Utility.LogExpection("[WebSocket Client] TryConnect time out");
                }
                else
                {
                    Utility.LogExpection("[WebSocket Client] TryConnect exception : ", ex.ToString());
                }
                UpdateClientState(ClientState.close);
                await Task.Delay(1000);
            }
            finally
            {
                timeOutCancellation.Dispose();
            }


            return _clientWS != null && _clientWS.State == WebSocketState.Open;
        }

        /// <summary>
        /// �ر�һ������
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            if (_clientWS != null)
            {
                UpdateClientState(ClientState.closing);
                var timeOutCancellation = new CancellationTokenSource();
                try
                {
                    // �����ʱ,��cancel���߳�
                    timeOutCancellation.CancelAfter(TimeoutMS);
                    await _clientWS.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", timeOutCancellation.Token);
                }
                catch (WebSocketException ex)
                {
                    if (timeOutCancellation.IsCancellationRequested)
                    {
                        Utility.LogExpection("[WebSocket Client] TryCloseAsync time out");
                    }
                    else
                    {
                        Utility.LogExpection("[WebSocket Client] TryCloseAsync exception : ", ex.ToString());
                    }
                }
                finally
                {
                    // �ر����� ��Ҫͬʱ�ر� ������Ϣ ����
                    // todo ��ʱ����Ըĳ� ����token��������
                    lock (_receiveMesgCancelTokenLock)
                    {
                        _receiveMsgCancellationToken?.Dispose();
                        _receiveMsgCancellationToken = null;
                    }

                    timeOutCancellation.Dispose();
                    _clientWS.Dispose();
                    _clientWS = null;
                    UpdateClientState(ClientState.close);
                }
            }
        }

        private int _recoveryConnTimes = 0;

        /// <summary>
        /// �ָ�����
        /// </summary>
        /// <returns></returns>
        public async Task RecoveryConnectionAsync()
        {
            try
            {
                if (!Application.isPlaying)
                {
                    Utility.LogDebug("WebSocket Client", "Application is terminate, recovery connection stop");
                    await CloseAsync();
                    return;
                }

                if (_clientWS == null)
                {
                    UpdateClientState(ClientState.close);
                    Utility.LogDebug("WebSocket Client", "can not reconnect because of _clientWS is null(never connected to the server)");
                    return;
                }

                // interval of recovery is 2s
                UpdateClientState(ClientState.reconnecting);
                await Task.Delay(2000);
              
                if (_clientWS.State == WebSocketState.Open)
                {
                    await _clientWS.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, string.Empty, CancellationToken.None);
                }

                _recoveryConnTimes++;
                Utility.LogDebug("WebSocket Client", $"Try to Recovery connection {_recoveryConnTimes} times");
                var isConnected = await TryConnectAsync();
                if (isConnected)
                {
                    Utility.LogDebug("WebSocket Client", "Reconnected");
                    _recoveryConnTimes = 0;
                    UpdateClientState(ClientState.open);
                }
                else
                {
                    await RecoveryConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                Utility.LogExpection("xxx " + ex.ToString());
            }
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="message"></param>
        /// <returns>������Ϣ�Ƿ�ɹ�</returns>
        public async Task<bool> SendMessageAsync(string message)
        {
            bool isSendMsgSuccessful = false;
            if (_clientWS != null && _clientWS.State == WebSocketState.Open)
            {
                var timeOutCancellation = new CancellationTokenSource();
                try
                {
                    // �����ʱ,��cancel���߳�
                    timeOutCancellation.CancelAfter(TimeoutMS);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                    await _clientWS.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, timeOutCancellation.Token);
                    Utility.LogInternalDebug("WebsocketClient",$"--> {message}");
                    isSendMsgSuccessful = true;
                }
                catch (Exception ex)
                {
                    if (timeOutCancellation.IsCancellationRequested)
                    {
                        Utility.LogExpection("[WebSocket Client] SendMessage time out");
                    }
                    else
                    {
                        Utility.LogExpection("[WebSocket Client] SendMessage exception : ", ex.ToString());
                    }
                }
                finally
                {
                    timeOutCancellation.Dispose();
                }
            }
            else
            {
                Utility.LogError("[WebSocket Client] SendMessageAsync is null or connection is not open");
            }
            return isSendMsgSuccessful;
        }

        private object _receiveMesgCancelTokenLock = new object();

        private CancellationTokenSource _receiveMsgCancellationToken;

        private Queue<MsgPack> _receievedDataPackQueue = new Queue<MsgPack>();

        /// <summary>
        /// ע�⣬�˷����� unity �̵߳���
        /// </summary>
        /// <returns></returns>
        public MsgPack GetCurrentRecievedDataFromQueue()
        {
            lock (_receievedDataPackQueue)
            {
                if (_receievedDataPackQueue.Count > 0)
                {
                    return _receievedDataPackQueue.Dequeue();
                }
                return null;
            }
        }

        private async Task ReceiveMessegeAsync()
        {
            // Handle incoming messages
            while (_clientWS.State == WebSocketState.Open)
            {
                // �������رգ���Ҫ�ն��߳�
                CancellationTokenSource receiveAsyncCancellationTokenSource = new CancellationTokenSource();
                try
                {
                    // Use a List<byte> to dynamically accumulate the bytes of the message
                    List<byte> messageBytes = new List<byte>();

                    WebSocketReceiveResult result;

                    // Continue receiving until the entire message is received
                    do
                    {
                        byte[] buffer = new byte[4096];
                        result = await _clientWS.ReceiveAsync(new ArraySegment<byte>(buffer), receiveAsyncCancellationTokenSource.Token);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            messageBytes.AddRange(buffer.Take(result.Count));
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            //�յ� close connection ��Ϣ
                            lock (_receiveMesgCancelTokenLock)
                            {
                                _receiveMsgCancellationToken?.Dispose();
                                _receiveMsgCancellationToken = null;
                            }
                            Utility.LogInternalDebug($"[WebSocket Client] Close the connection in reason of server msg");
                            break;
                        }
                    } while (!result.EndOfMessage);

                    // Process the received message
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes.ToArray());

                    if (receivedMessage.Length == 0)
                    {
                        continue;
                    }
                    try
                    {
                        Utility.LogInternalDebug("WebsocketClient", $"<-- {receivedMessage}");
                        var data = JObject.Parse(receivedMessage);
                        if (data == null)
                        {
                            continue;
                        }
                        var dataPack = MsgPack.Parse(data);
                        if (dataPack != null)
                        {
                            Utility.LogInternalDebug($"[WebSocket Client] Received message from server: {receivedMessage}");
                            lock (_receievedDataPackQueue)
                            {
                                _receievedDataPackQueue.Enqueue(dataPack);
                                Utility.LogInternalDebug($"[WebSocket Client] Enqueue message into datapack queue");
                            }
                        }
                        else
                        {
                            Utility.LogInternalDebug($"[WebSocket Client] Received message from server: {receivedMessage} Deserialize fail");
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.LogInternalDebug($"[WebSocket Client] Parse Response json fail {ex}");
                    }

                }
                catch (WebSocketException ex)
                {
                    receiveAsyncCancellationTokenSource.Cancel();
                    receiveAsyncCancellationTokenSource.Dispose();
                    // �������رգ���Ҫ��������
                    if (autoReconnect)
                    {
                        Utility.LogExpection($"[WebSocket Client] Receieve messege exception : {ex.Message}");
                        //case : Զ�̷���û����ɹر����ֵ�����¹ر��� WebSocket ����
                        await RecoveryConnectionAsync();
                    }
                    else
                    {
                        break;
                    }
                }              
            }
        }

        public static int HeartbeatIntervalMS = 5000;
        public static int TimeoutMS = 10000;

        public static class Factory
        {
            /// <summary>
            /// ����һ�� WebsocketClient 
            /// </summary>
            /// <returns></returns>
            public static async Task<WebSocketClient> CreateAndConnectAsync(string wsUrl)
            {
                var client = new WebSocketClient(wsUrl);
                var isConnected = await client.TryConnectAsync();
                if (isConnected)
                {
                    // ���ӳɹ��������Ϣ����
                    _= client.ReceiveMessegeAsync();
                }
                return isConnected ? client : null;
            }         
        }
    }
}
