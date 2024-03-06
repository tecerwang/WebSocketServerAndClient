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
    /// ws 客户端：
    /// 1.负责连接；
    /// 2.收发原始 text 数据；
    /// 3.断线重连；
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
        /// 自动重连
        /// </summary>
        public bool autoReconnect = true;

        public WebSocketClient(string wsUrl)
        {
            this._wsUrl = wsUrl;
        }

        /// <summary>
        /// Client 到服务器的连接状态
        /// </summary>
        public enum ClientState
        {
            /// <summary>
            /// WebSocketClient 还未被创建出来
            /// </summary>
            unkown,
            /// <summary>
            /// 关闭状态
            /// </summary>
            close,
            /// <summary>
            /// 连接中
            /// </summary>
            connecting,
            /// <summary>
            /// 连接状态
            /// </summary>
            open,
            /// <summary>
            /// 关闭中
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
        /// 尝试进行链接，如果链接成功，返回true
        /// </summary>
        /// <returns>true链接成功</returns>
        public async Task TryConnectAsync()
        {
            _tryConnectCancelTokenSource = new CancellationTokenSource();
            try
            {
                UpdateClientState(ClientState.connecting);
                Utility.LogDebug("WebSocket Client", $"Connect to WebSocketServer '{_wsUrl}'");
                // 新创建一个，避免重连冲突
                _clientWS = new ClientWebSocket();
                _clientWS.Options.KeepAliveInterval = new System.TimeSpan(0, 0, (int)TimeoutMS / 1000);
                // 连接超时设置
                _tryConnectCancelTokenSource.CancelAfter(TimeoutMS);
                // 连接操作会阻塞线程
                await _clientWS.ConnectAsync(new System.Uri(_wsUrl), _tryConnectCancelTokenSource.Token);
                // 更新一次状态
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
                            //收到 close connection 消息
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
        /// 主动关闭一个链接，服务器响应关闭
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
                        // 主动调用 关闭后，要关闭连接的线程
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
                    // 关闭连接 需要同时关闭 接收消息 任务
                    // todo 有时间可以改成 级联token依赖控制
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
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns>发送消息是否成功</returns>
        public async Task<bool> SendMessageAsync(string message)
        {
            bool result = false;
            if (_clientWS != null && _clientWS.State == WebSocketState.Open)
            {
                var timeOutCancellation = new CancellationTokenSource();
                try
                {
                    // 如果超时,则cancel掉线程
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
                            // 测试时出现了 ' System.Net.WebSockets.WebSocketException (0x80004005): The remote party closed the WebSocket connection without completing the close handshake.'
                            // 这种情况出现在关闭服务器程序后，长时间不连接
                            // how to fix : 添加下一行代码 await CloseAsync()
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
                    // 如果在发出消息时出现 ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
                    // 此时 timeOutCancellation 已经设置为Null
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
        /// 注意，此方法在 unity 线程调用
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
