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
        private ClientWebSocket _clientWS;

        private string _wsUrl;

        /// <summary>
        /// 自动重连
        /// </summary>
        public bool autoReconnect = true;

        private WebSocketClient(string wsUrl)
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
            /// 重新连接
            /// </summary>
            reconnecting,
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

        /// <summary>
        /// 尝试进行链接，如果链接成功，返回true
        /// </summary>
        /// <returns>true链接成功</returns>
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
                // 如果超时,则cancel掉线程
                timeOutCancellation.CancelAfter(TimeoutMS);
                // 连接操作会阻塞线程
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
        /// 关闭一个链接
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
                    // 如果超时,则cancel掉线程
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
                    // 关闭连接 需要同时关闭 接收消息 任务
                    // todo 有时间可以改成 级联token依赖控制
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
        /// 恢复连接
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
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns>发送消息是否成功</returns>
        public async Task<bool> SendMessageAsync(string message)
        {
            bool isSendMsgSuccessful = false;
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

        private async Task ReceiveMessegeAsync()
        {
            // Handle incoming messages
            while (_clientWS.State == WebSocketState.Open)
            {
                // 如果意外关闭，需要终端线程
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
                            //收到 close connection 消息
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
                    // 如果意外关闭，需要断线重连
                    if (autoReconnect)
                    {
                        Utility.LogExpection($"[WebSocket Client] Receieve messege exception : {ex.Message}");
                        //case : 远程方在没有完成关闭握手的情况下关闭了 WebSocket 连接
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
            /// 创建一个 WebsocketClient 
            /// </summary>
            /// <returns></returns>
            public static async Task<WebSocketClient> CreateAndConnectAsync(string wsUrl)
            {
                var client = new WebSocketClient(wsUrl);
                var isConnected = await client.TryConnectAsync();
                if (isConnected)
                {
                    // 连接成功后进行消息监听
                    _= client.ReceiveMessegeAsync();
                }
                return isConnected ? client : null;
            }         
        }
    }
}
