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
        //public static bool IsApplicationPlaying = true;

        private ClientWebSocket _clientWS;

        /// <summary>
        /// Valid states are: 'Open, CloseReceived, CloseSent'
        /// </summary>
        /// <returns></returns>
        private bool IsValid2CloseClient() => _clientWS != null && (_clientWS.State == WebSocketState.Open || _clientWS.State == WebSocketState.CloseReceived || _clientWS.State == WebSocketState.CloseSent);

        private string _wsUrl;

        /// <summary>
        /// �Զ�����
        /// </summary>
        public bool autoReconnect = true;

        public WebSocketClient(string wsUrl)
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

        private CancellationTokenSource _tryConnectCancelTokenSource;
        private CancellationTokenSource _receiveDataCancelTokenSource;

        /// <summary>
        /// ���Խ������ӣ�������ӳɹ�������true
        /// </summary>
        /// <returns>true���ӳɹ�</returns>
        public async Task TryConnectAsync()
        {
            _tryConnectCancelTokenSource = new CancellationTokenSource();
            try
            {
                UpdateClientState(ClientState.connecting);
                Utility.LogDebug("WebSocket Client", $"Connect to WebSocketServer '{_wsUrl}'");
                // �´���һ��������������ͻ
                _clientWS = new ClientWebSocket();
                _clientWS.Options.KeepAliveInterval = new System.TimeSpan(0, 0, (int)TimeoutMS / 1000);
                // ���ӳ�ʱ����
                _tryConnectCancelTokenSource.CancelAfter(TimeoutMS);
                // ���Ӳ����������߳�
                await _clientWS.ConnectAsync(new System.Uri(_wsUrl), _tryConnectCancelTokenSource.Token);
                // ����һ��״̬
                if (_clientWS.State == WebSocketState.Open)
                {
                    lock (_tryConnectCancelTokenSource)
                    {
                        _tryConnectCancelTokenSource.Dispose();
                        _tryConnectCancelTokenSource = null;
                    }
                    UpdateClientState(ClientState.open);
                }

                while (_clientWS.State == WebSocketState.Open)
                {
                    // Use a List<byte> to dynamically accumulate the bytes of the message
                    List<byte> messageBytes = new List<byte>();

                    WebSocketReceiveResult result;

                    // Continue receiving until the entire message is received
                    do
                    {
                        _receiveDataCancelTokenSource = new CancellationTokenSource();
                        byte[] buffer = new byte[4096];
                        result = await _clientWS.ReceiveAsync(new ArraySegment<byte>(buffer), _receiveDataCancelTokenSource.Token);

                        _receiveDataCancelTokenSource.Dispose();
                        _receiveDataCancelTokenSource = null;

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
                            if (IsValid2CloseClient())
                            {
                                UpdateClientState(ClientState.closing);
                                Utility.LogInternalDebug($"[WebSocket Client] Received close frame from client");
                                await _clientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                                _clientWS.Dispose();
                                UpdateClientState(ClientState.close);
                            }
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
            }
            catch (WebSocketException ex)
            {
                Utility.LogExpection("[WebSocket Client] WebSocket error: " + ex.Message);
            }
            finally
            {
                if (_tryConnectCancelTokenSource != null)
                {
                    _tryConnectCancelTokenSource.Dispose();
                    _tryConnectCancelTokenSource = null;
                }
                if (_tryConnectCancelTokenSource != null)
                {
                    _receiveDataCancelTokenSource.Dispose();
                    _receiveDataCancelTokenSource = null;
                }
                // Ensure the WebSocket connection is closed properly
                if (_clientWS != null && _clientWS.State != WebSocketState.Closed)
                {
                    if (IsValid2CloseClient())
                    {
                        await _clientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                        _clientWS.Dispose();
                    }
                }
                UpdateClientState(ClientState.close);
            }
        }

        /// <summary>
        /// �����ر�һ�����ӣ���������Ӧ�ر�
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            if (_clientWS != null)
            {
                UpdateClientState(ClientState.closing);
                try
                {
                    if (_clientWS.State == WebSocketState.Connecting || _clientWS.State == WebSocketState.Open)
                    {
                        // �������� �رպ�Ҫ�ر����ӵ��߳�
                        _tryConnectCancelTokenSource?.Cancel();
                        if (IsValid2CloseClient())
                        {
                            await _clientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
                            _clientWS.Dispose();
                        }
                        UpdateClientState(ClientState.close);
                    }
                }                
                finally
                {
                    // �ر����� ��Ҫͬʱ�ر� ������Ϣ ����
                    // todo ��ʱ����Ըĳ� ����token��������
                    lock (_receiveMesgCancelTokenLock)
                    {
                        _receiveMsgCancellationToken?.Cancel();
                        _receiveMsgCancellationToken?.Dispose();
                        _receiveMsgCancellationToken = null;
                    }
                    _clientWS.Dispose();
                    _clientWS = null;
                    UpdateClientState(ClientState.close);
                }
            }
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="message"></param>
        /// <returns>������Ϣ�Ƿ�ɹ�</returns>
        public async Task<bool> SendMessageAsync(string message)
        {
            bool result = false;
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
                    result = true;
                }
                catch (System.Net.WebSockets.WebSocketException ex)
                {
                    if (timeOutCancellation.IsCancellationRequested)
                    {
                        Utility.LogExpection("[WebSocket Client] SendMessage time out");
                    }
                    else
                    {
                        Utility.LogExpection("[WebSocket Client] SendMessage exception : ", ex.ToString());

                        // doc :
                        //"ConnectionClosedPrematurely" in the context of WebSocket communication typically means that the WebSocket connection was closed unexpectedly before the expected
                        //closing handshake was completed. In WebSocket communication, both the client and server are supposed to perform a closing handshake to properly terminate the
                        //connection.This handshake involves exchanging close control frames between the client and server to signal the intention to close the connection and ensure that
                        //both sides are aware that the connection is being closed. However, if the connection is terminated abruptly without completing this handshake, it indicates that
                        //the connection was closed prematurely. This could happen due to various reasons such as network issues, server - side errors, or intentional closing of the connection
                        //by one of the parties without following the proper protocol.
                        if (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                        {
                            // ����ʱ������ ' System.Net.WebSockets.WebSocketException (0x80004005): The remote party closed the WebSocket connection without completing the close handshake.'
                            // ������������ڹرշ���������󣬳�ʱ�䲻����
                            // how to fix : �����һ�д��� await CloseAsync()
                            if (IsValid2CloseClient())
                            {
                                await _clientWS.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, null, CancellationToken.None);
                                _clientWS?.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    // ����ڷ�����Ϣʱ���� ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
                    // ��ʱ timeOutCancellation �Ѿ�����ΪNull
                    timeOutCancellation?.Dispose();
                    timeOutCancellation = null;
                }
            }
            else
            {
                Utility.LogError("[WebSocket Client] SendMessageAsync is null or connection is not open");
            }
            return result;
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

        public static int TimeoutMS = 10000;
    }
}
