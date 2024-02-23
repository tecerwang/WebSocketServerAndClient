using System.Net.WebSockets;
using System.Net;
using WebSocketServer.ServerKernal;
using WebSocketServer.ServiceLogic;
using WebSocketServer.Utilities;
using Newtonsoft.Json.Linq;
using System.Text;
using static WebSocketServer.ServiceLogic.ClientGroupBroadcastService;
using Microsoft.AspNetCore.Components;

namespace WebSocketServer.ServerKernal
{
    public class WebSocketMiddleWare : IMiddleware
    {
        private IWebSocketDataProvider? _wsData;
        private IWebSocketLogic[]? _providers;

        public WebSocketMiddleWare()
        {
            // 创建一个数据提供者
            this._wsData = new WebSocketData();

            // 启动两个服务
            this._providers = new IWebSocketLogic[]
            {
                new ClientGroupBroadcastService(),
                new ConnMonitorService()
            };
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest && _wsData != null)
                {
                    string? clientId = context.Request.Query["clientId"];
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var connectedClients = _wsData?.connectedClients;
                    if (!string.IsNullOrEmpty(clientId) && connectedClients != null && _providers != null)
                    {
                        if (connectedClients.ContainsKey(clientId))
                        {
                            DebugLog.Print($"WebSocket connection already contains for client with ID: {clientId}, new connection will replace the old one");
                            connectedClients?.Remove(clientId);
                            foreach (var provider in _providers)
                            {
                                provider?.OnClientClose(clientId);
                            }
                        }

                        DebugLog.Print($"WebSocket connection established for client with ID: {clientId}");
                        WebSocketClientConnection connection = new WebSocketClientConnection(clientId, Utility.GetCurrentUTC(), webSocket);

                        connectedClients?.Add(connection.clientId, connection);
                        foreach (var provider in _providers)
                        {
                            provider?.OnClientOpen(clientId, this);
                        }
                        DebugLog.Print($"WebSocket connection add client connection with ID: {clientId}");

                        /// 启动网络监听
                        await Task.Run(() => ProcessWebSocketRequest(connection));
                    }
                    else
                    {
                        // 如果没有client Id，则关闭这个链接
                        DebugLog.Print($"WebSocket connection will close, because of the invalid connection info");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            else
            {
                await next(context);
            }
        }

        private async Task ProcessWebSocketRequest(WebSocketClientConnection connection)
        {
            // Handle incoming messages
            var connectedClients = _wsData?.connectedClients;
            while (connection.webSocket.State == WebSocketState.Open && connectedClients != null)
            {
                try
                {
                    // Use a List<byte> to dynamically accumulate the bytes of the message
                    List<byte> messageBytes = new List<byte>();

                    WebSocketReceiveResult result;

                    // Continue receiving until the entire message is received
                    do
                    {
                        byte[] buffer = new byte[4096];
                        result = await connection.webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            messageBytes.AddRange(buffer.Take(result.Count));
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (connectedClients.ContainsKey(connection.clientId))
                            {
                                DebugLog.Print($"WebSocket connection closed for client with ID: {connection.clientId}");
                                connectedClients.Remove(connection.clientId);
                                if (_providers != null)
                                {
                                    foreach (var provider in _providers)
                                    {
                                        provider?.OnClientClose(connection.clientId);
                                    }
                                }
                                connection.webSocket.Dispose();
                            }
                            break;
                        }
                    } while (!result.EndOfMessage);

                    // Process the received message
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes.ToArray());

                    if (!string.IsNullOrEmpty(receivedMessage) && _providers != null)
                    {
                        foreach (var provider in _providers)
                        {
                            provider?.OnMessageRecieved(receivedMessage);
                        }
                       
                        DebugLog.Print($"Received message from client {connection.clientId}: {receivedMessage}");
                    }                   
                }
                catch (WebSocketException ex)
                {
                    //case : 远程方在没有完成关闭握手的情况下关闭了 WebSocket 连接
                    DebugLog.Print($"Error processing WebSocket request: {ex.Message}");
                }

            }
        }

        /// <summary>
        /// 发送消息请求
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task SendMsgToClientAsync(string? clientId, string msg)
        {
            if (_wsData?.connectedClients == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(clientId) && _wsData.connectedClients.TryGetValue(clientId, out WebSocketClientConnection? conn) && conn != null)
            {
                var webSocket = conn.webSocket;
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }        
    }


    public static class WebSocketExtensions
    {
        public static IApplicationBuilder UseWebSocketMiddleware(this IApplicationBuilder builder)
        {                             
            // 注入 dataProvider 和 LogicProvider
            return builder.UseMiddleware<WebSocketMiddleWare>();
        }
    }
}
