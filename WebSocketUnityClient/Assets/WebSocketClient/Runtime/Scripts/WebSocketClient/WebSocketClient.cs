using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
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
                Utility.LogInternalDebug($"[WebSocketClient] client state changed -> {State}");
                OnClientStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 使用 tokensource 中断线程, source for connect op, connect 完成后会自动关闭
        /// </summary>
        private CancellationTokenSource _connectCancellationTokenSource;

        /// <summary>
        /// <para>通过这个方法来完成整个 websocket 连接的生命周期</para>
        /// 使用 _connectCancellationTokenSource 来中断线程
        /// </summary>
        /// <returns>true链接成功</returns>
        public async Task ConnectAndRecvAsync()
        {
            _connectCancellationTokenSource = new CancellationTokenSource();

            /// 控制连接超时
            CancellationTokenSource timeoutTokenSource = null;
            try
            {
                // 创建一个 cancellationTokenSource 实例

                UpdateClientState(ClientState.connecting);

                Utility.LogDebug("WebSocket Client", $"Connect to WebSocketServer '{_wsUrl}'");

                // 新创建一个 ClientWebSocket，避免重连冲突
                _clientWS = new ClientWebSocket();
                _clientWS.Options.KeepAliveInterval = new System.TimeSpan(0, 0, (int)KeepAliveIntervalMS / 1000);

                // 连接超时设置
                timeoutTokenSource = new CancellationTokenSource();
                timeoutTokenSource.CancelAfter(TryConnectTimeoutMS);

                // 连接操作会阻塞线程
                await _clientWS.ConnectAsync(new System.Uri(_wsUrl), timeoutTokenSource.Token);
                timeoutTokenSource.Dispose();
                timeoutTokenSource = null;

                if (_clientWS == null)
                {
                    return;
                }
                if (_connectCancellationTokenSource.IsCancellationRequested)
                {
                    if (_clientWS.State == WebSocketState.Open)
                    {
                        // 此时会调用 state == open 的情况
                        await CloseAsync();
                    }
                    else
                    {
                        _clientWS.Dispose();
                        UpdateClientState(ClientState.close);
                    }
                }
                // 更新一次状态
                else
                {
                    UpdateClientState(ClientState.open);
                }
            }
            catch (WebSocketException wsEx) // Catching only WebSocketException
            {
                Utility.LogExpection("[WebSocket Client] WebSocketException occurred: " + wsEx.Message);
                _clientWS.Dispose();
                UpdateClientState(ClientState.close);
            }
            catch (Exception ex)
            {
                Utility.LogExpection("[WebSocket Client] tryConnect exception: " + ex.Message);
                await CloseAsync();
            }
            finally
            {
                // 确保异常时关闭
                if (timeoutTokenSource != null)
                {
                    timeoutTokenSource.Dispose();
                }
                /// 如果连接出现异样，需要在此处 cancel 这个 token
                if (_connectCancellationTokenSource != null)
                {
                    if (_connectCancellationTokenSource.IsCancellationRequested)
                    {
                        _connectCancellationTokenSource.Dispose();
                    }
                    _connectCancellationTokenSource = null;
                }
            }


            // 如果没有 cancel 线程，在这个线程中开始接收数据
            if (_clientWS != null && _clientWS.State == WebSocketState.Open)
            {
                await RecieveMsgAsync();
            }
        }

        private async Task RecieveMsgAsync()
        {
            try
            {
                while (_clientWS != null && _clientWS.State == WebSocketState.Open)
                {
                    // Use a List<byte> to dynamically accumulate the bytes of the message
                    List<byte> messageBytes = new List<byte>();

                    WebSocketReceiveResult result;

                    // Continue receiving until the entire message is received
                    do
                    {
                        byte[] buffer = new byte[4096];
                        result = await _clientWS.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            messageBytes.AddRange(buffer.Take(result.Count));
                        }
                        // 如果收到 服务器传回来的关闭消息，跳出循环 执行关闭操作
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Utility.LogInternalDebug($"[WebSocket Client] Received close frame from serverSide");
                            await CloseAsync();
                            // 收到服务器握手返回后，直接设置为 close
                            UpdateClientState(ClientState.close);
                            break;
                        }
                    } while (!result.EndOfMessage);

                    // 如果当前连接依然开启
                    if (_clientWS != null && _clientWS.State == WebSocketState.Open)
                    {
                        // Process the received message
                        string receivedMessage = Encoding.UTF8.GetString(messageBytes.ToArray());

                        if (receivedMessage.Length == 0)
                        {
                            continue;
                        }
                        try
                        {
                            Utility.LogInternalDebug("WebsocketClient", $"<-- {receivedMessage}");
                            _receievedDataPackQueue.Enqueue(receivedMessage);                           
                        }
                        catch (Exception ex)
                        {
                            Utility.LogInternalDebug($"[WebSocket Client] Parse Response json fail {ex}");
                        }
                    }
                }
            }
            catch (WebSocketException wsEx) // Catching only WebSocketException
            {
                Utility.LogExpection("[WebSocket Client] WebSocketException occurred: " + wsEx.Message);
                _clientWS.Dispose();
                UpdateClientState(ClientState.close);
            }
            catch (Exception ex)
            {              
                await CloseAsync();
                Utility.LogExpection(ex);
            }
        }

        /// <summary>
        /// <para>主动关闭一个链接，服务器响应关闭</para>
        /// <para>或者相应服务器发过来的关闭连接</para>
        /// <para>分为5种情况</para>
        /// <para>1.已经关闭或者退出 closed or aborted</para>
        /// <para>2.正在连接中 connecting</para>
        /// <para>3.连接状态 open</para>
        /// <para>4.向服务器发出了连接关闭请求 CloseSent</para>
        /// <para>5.收到服务器发出的连接关闭的请求 CloseReceived</para>
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            var state = _clientWS?.State ?? WebSocketState.None;
            Utility.LogDebug("WebSocketClient", $"==> CloseAsync by state {state}");
            switch (state)
            {
                // 已经关闭或者正在关闭
                case WebSocketState.None:
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                    return;

                // 正在连接中, 调用 _connectCancellationTokenSource.Cancel() 在连接完成或者连接超时时检查是否需要 close 连接
                case WebSocketState.Connecting:
                    _connectCancellationTokenSource?.Cancel();
                    return;

                // 正在开启状态时，此时 wsClient 被 acceptReceive 阻塞，此时需要发出连接关闭的请求，等待服务器返回握手
                case WebSocketState.Open:
                    try
                    {
                        var timeoutToken = new CancellationTokenSource();
                        // 设置一个定时
                        timeoutToken.CancelAfter(TryCloseTimeoutMS);
                        UpdateClientState(ClientState.closing);
                        await _clientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", timeoutToken.Token);

                        // 说明服务器没有接收到请求 remote endpoint is not avaliable,否则等待服务器返回握手
                        // 这种情况比较棘手，需要多测试
                        if (timeoutToken.IsCancellationRequested)
                        {
                            _clientWS.Dispose();
                            UpdateClientState(ClientState.close);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.LogExpection("WebSocketClient", $"State : {state}, Exception : {ex}");
                    }
                    return;

                // 收到服务器的返回消息或者正在等待服务器的消息，握手操作有效，这种情况不需要处理
                case WebSocketState.CloseReceived:
                    _clientWS.Dispose();
                    return;
                case WebSocketState.CloseSent:
                    return;
            }
        }

        public enum SendMsgState
        { 
            Unkown,
            /// <summary>
            /// 出现异常
            /// </summary>
            ThrowException,
            /// <summary>
            /// 已经发送出去
            /// </summary>
            Sent,
            /// <summary>
            /// 连接关闭，不在发送
            /// </summary>
            InteruptByConnectionClose,
            /// <summary>
            /// 发送超时
            /// </summary>
            Timeout
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns>发送消息是否成功</returns>
        public async Task<SendMsgState> SendMessageAsync(string message)
        {
            // 如果已经发起关闭连接，则不在发送消息
            if (_connectCancellationTokenSource != null && _connectCancellationTokenSource.IsCancellationRequested)
            {
                return SendMsgState.InteruptByConnectionClose;
            }

            SendMsgState result = SendMsgState.Unkown;
            if (_clientWS != null && _clientWS.State == WebSocketState.Open)
            {
                var timeOutCancellation = new CancellationTokenSource();
                try
                {
                    // 如果超时,则cancel掉线程
                    timeOutCancellation.CancelAfter(SendMsgTimeoutMs);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                    await _clientWS.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, timeOutCancellation.Token);
                    if (_connectCancellationTokenSource != null && _connectCancellationTokenSource.IsCancellationRequested)
                    {
                        Utility.LogInternalDebug("[WebSocket Client] SendMessage terminate by close connection");
                        result = SendMsgState.InteruptByConnectionClose;
                    }
                    else if (timeOutCancellation.IsCancellationRequested)
                    {
                        Utility.LogInternalDebug("[WebSocket Client] SendMessage terminate by send timeout");
                        result = SendMsgState.Timeout;
                    }
                    else
                    {
                        Utility.LogInternalDebug("WebsocketClient", $"--> {message}");
                        result = SendMsgState.Sent;
                    }
                }
                catch (WebSocketException ex)
                {
                    result = SendMsgState.ThrowException;
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
                        // how to fix : 添加下一行代码 Close()
                        _clientWS.Dispose();
                        UpdateClientState(ClientState.close);
                    }
                }
                finally
                {
                    // 如果在发出消息时出现 ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
                    // 此时 timeOutCancellation 已经设置为Null
                    timeOutCancellation?.Dispose();
                }
            }
            else
            {
                Utility.LogError("[WebSocket Client] SendMessageAsync is null or connection is not open");
            }
            return result;
        }

        /// <summary>
        /// 使用线程安全的 ConcurrentQueue
        /// </summary>
        private ConcurrentQueue<string> _receievedDataPackQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// 注意，此方法在 unity 线程调用
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetCurrentRecievedDataFromQueue()
        {
            // 把所有的消息一次性弹出
            int count = _receievedDataPackQueue.Count;
            for (int i = count; i > 0; i--)
            {
                if (_receievedDataPackQueue.TryDequeue(out string msg))
                {
                    yield return msg;
                }
            }           
        }

        private const int TryConnectTimeoutMS = 10000;
        private const int TryCloseTimeoutMS = 5000;
        private const int KeepAliveIntervalMS = 10000;
        private const int SendMsgTimeoutMs = 10000;
    }
}
