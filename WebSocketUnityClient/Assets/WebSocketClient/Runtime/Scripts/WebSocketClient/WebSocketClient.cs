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
    /// ws �ͻ��ˣ�
    /// 1.�������ӣ�
    /// 2.�շ�ԭʼ text ���ݣ�
    /// 3.����������
    /// </summary>
    public class WebSocketClient
    {
        //public static bool IsApplicationPlaying = true;

        private ClientWebSocket _clientWS;

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
                Utility.LogInternalDebug($"[WebSocketClient] client state changed -> {State}");
                OnClientStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// ʹ�� tokensource �ж��߳�, source for connect op, connect ��ɺ���Զ��ر�
        /// </summary>
        private CancellationTokenSource _connectCancellationTokenSource;

        /// <summary>
        /// <para>ͨ������������������ websocket ���ӵ���������</para>
        /// ʹ�� _connectCancellationTokenSource ���ж��߳�
        /// </summary>
        /// <returns>true���ӳɹ�</returns>
        public async Task ConnectAndRecvAsync()
        {
            _connectCancellationTokenSource = new CancellationTokenSource();

            /// �������ӳ�ʱ
            CancellationTokenSource timeoutTokenSource = null;
            try
            {
                // ����һ�� cancellationTokenSource ʵ��

                UpdateClientState(ClientState.connecting);

                Utility.LogDebug("WebSocket Client", $"Connect to WebSocketServer '{_wsUrl}'");

                // �´���һ�� ClientWebSocket������������ͻ
                _clientWS = new ClientWebSocket();
                _clientWS.Options.KeepAliveInterval = new System.TimeSpan(0, 0, (int)KeepAliveIntervalMS / 1000);

                // ���ӳ�ʱ����
                timeoutTokenSource = new CancellationTokenSource();
                timeoutTokenSource.CancelAfter(TryConnectTimeoutMS);

                // ���Ӳ����������߳�
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
                        // ��ʱ����� state == open �����
                        await CloseAsync();
                    }
                    else
                    {
                        _clientWS.Dispose();
                        UpdateClientState(ClientState.close);
                    }
                }
                // ����һ��״̬
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
                // ȷ���쳣ʱ�ر�
                if (timeoutTokenSource != null)
                {
                    timeoutTokenSource.Dispose();
                }
                /// ������ӳ�����������Ҫ�ڴ˴� cancel ��� token
                if (_connectCancellationTokenSource != null)
                {
                    if (_connectCancellationTokenSource.IsCancellationRequested)
                    {
                        _connectCancellationTokenSource.Dispose();
                    }
                    _connectCancellationTokenSource = null;
                }
            }


            // ���û�� cancel �̣߳�������߳��п�ʼ��������
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
                        // ����յ� �������������Ĺر���Ϣ������ѭ�� ִ�йرղ���
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Utility.LogInternalDebug($"[WebSocket Client] Received close frame from serverSide");
                            await CloseAsync();
                            // �յ����������ַ��غ�ֱ������Ϊ close
                            UpdateClientState(ClientState.close);
                            break;
                        }
                    } while (!result.EndOfMessage);

                    // �����ǰ������Ȼ����
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
        /// <para>�����ر�һ�����ӣ���������Ӧ�ر�</para>
        /// <para>������Ӧ�������������Ĺر�����</para>
        /// <para>��Ϊ5�����</para>
        /// <para>1.�Ѿ��رջ����˳� closed or aborted</para>
        /// <para>2.���������� connecting</para>
        /// <para>3.����״̬ open</para>
        /// <para>4.����������������ӹر����� CloseSent</para>
        /// <para>5.�յ����������������ӹرյ����� CloseReceived</para>
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            var state = _clientWS?.State ?? WebSocketState.None;
            Utility.LogDebug("WebSocketClient", $"==> CloseAsync by state {state}");
            switch (state)
            {
                // �Ѿ��رջ������ڹر�
                case WebSocketState.None:
                case WebSocketState.Closed:
                case WebSocketState.Aborted:
                    return;

                // ����������, ���� _connectCancellationTokenSource.Cancel() ��������ɻ������ӳ�ʱʱ����Ƿ���Ҫ close ����
                case WebSocketState.Connecting:
                    _connectCancellationTokenSource?.Cancel();
                    return;

                // ���ڿ���״̬ʱ����ʱ wsClient �� acceptReceive ��������ʱ��Ҫ�������ӹرյ����󣬵ȴ���������������
                case WebSocketState.Open:
                    try
                    {
                        var timeoutToken = new CancellationTokenSource();
                        // ����һ����ʱ
                        timeoutToken.CancelAfter(TryCloseTimeoutMS);
                        UpdateClientState(ClientState.closing);
                        await _clientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", timeoutToken.Token);

                        // ˵��������û�н��յ����� remote endpoint is not avaliable,����ȴ���������������
                        // ��������Ƚϼ��֣���Ҫ�����
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

                // �յ��������ķ�����Ϣ�������ڵȴ�����������Ϣ�����ֲ�����Ч�������������Ҫ����
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
            /// �����쳣
            /// </summary>
            ThrowException,
            /// <summary>
            /// �Ѿ����ͳ�ȥ
            /// </summary>
            Sent,
            /// <summary>
            /// ���ӹرգ����ڷ���
            /// </summary>
            InteruptByConnectionClose,
            /// <summary>
            /// ���ͳ�ʱ
            /// </summary>
            Timeout
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="message"></param>
        /// <returns>������Ϣ�Ƿ�ɹ�</returns>
        public async Task<SendMsgState> SendMessageAsync(string message)
        {
            // ����Ѿ�����ر����ӣ����ڷ�����Ϣ
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
                    // �����ʱ,��cancel���߳�
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
                        // ����ʱ������ ' System.Net.WebSockets.WebSocketException (0x80004005): The remote party closed the WebSocket connection without completing the close handshake.'
                        // ������������ڹرշ���������󣬳�ʱ�䲻����
                        // how to fix : �����һ�д��� Close()
                        _clientWS.Dispose();
                        UpdateClientState(ClientState.close);
                    }
                }
                finally
                {
                    // ����ڷ�����Ϣʱ���� ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
                    // ��ʱ timeOutCancellation �Ѿ�����ΪNull
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
        /// ʹ���̰߳�ȫ�� ConcurrentQueue
        /// </summary>
        private ConcurrentQueue<string> _receievedDataPackQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// ע�⣬�˷����� unity �̵߳���
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetCurrentRecievedDataFromQueue()
        {
            // �����е���Ϣһ���Ե���
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
