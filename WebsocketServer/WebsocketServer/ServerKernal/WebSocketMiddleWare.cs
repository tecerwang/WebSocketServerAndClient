using System.Net.WebSockets;
using System.Net;
using WebSocketServer.ServerKernal;
using WebSocketServer.ServiceLogic;
using WebSocketServer.Utilities;
using Newtonsoft.Json.Linq;
using System.Text;
using static WebSocketServer.ServiceLogic.ClientGroupBroadcastService;
using Microsoft.AspNetCore.Components;
using WebSocketServer.DataService;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketServer.ServerKernal
{
    public class WebSocketMiddleWare : IMiddleware
    {
        private WebSocketClientData? _wsData;
        private IEnumerable<IWebSocketLogic>? _logicProviders;

        public WebSocketMiddleWare(IServiceProvider serviceProvider)
        {
            _wsData = serviceProvider.GetService<WebSocketClientData>();
            _logicProviders = serviceProvider.GetServices<IWebSocketLogic>();            
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest && _wsData != null)
                {
                    string? clientId = context.Request.Query["clientId"];
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        WebSocketClientConnection connection = new WebSocketClientConnection(clientId, Utility.GetCurrentUTC(), webSocket);
                        if (_logicProviders != null && await _wsData.GetConnectionById(clientId) != null)
                        {
                            DebugLog.Print($"WebSocket connection already contains for client with ID: {clientId}, new connection will replace the old one");
                            // 需要等待所有关闭链接 OnClientClose 处理完成
                            Task.WaitAll(_logicProviders.Select(p => p.OnClientClose(clientId)).ToArray());
                            // 先移除原有的 connection，用新的 connection 替代
                            await _wsData.RemoveConnection(clientId);
                        }
                        /// 添加成功
                        if (await _wsData.AddConnection(connection))
                        {
                            DebugLog.Print($"WebSocket connection add client connection with ID: {clientId}");
                            if (_logicProviders != null)
                            {
                                Task.WaitAll(_logicProviders.Select(p => p.OnClientOpen(clientId, this)).ToArray());
                            }
                            /// 启动网络监听
                            await Task.Run(() => ProcessWebSocketRequest(connection));
                        }
                    }
                    else
                    {
                        // 如果没有client Id，则关闭这个链接
                        DebugLog.Print($"WebSocket connection will close, because of the invalid connection info");
                        try
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            DebugLog.Print($"WebSocket close exception: " + ex.ToString());
                        }
                    }
                    if (clientId != null)
                    {
                        // 客户端主动关闭连接，需要通知 logic 客户端移除
                        if (_logicProviders != null)
                        {
                            Task.WaitAll(_logicProviders.Select(p => p.OnClientClose(clientId)).ToArray());
                        }
                        // 客户端移除
                        await _wsData.RemoveConnection(clientId);
                        DebugLog.Print($"WebSocket connection remove client connection with ID: {clientId}");
                    }
                }
                else
                {
                    DebugLog.Print($"WebSocketClientData service is null or this is not webscoket request ");
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
            if (_wsData == null)
            {
                return;
            }
            while (connection.webSocket.State == WebSocketState.Open)
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
                            if (await _wsData.RemoveConnection(connection))
                            {
                                DebugLog.Print($"WebSocket connection closed for client with ID: {connection.clientId}");
                                if (_logicProviders != null)
                                {
                                    foreach (var provider in _logicProviders)
                                    {
                                        provider?.OnClientClose(connection.clientId);
                                    }
                                }
                                await connection.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                                connection.webSocket.Dispose();
                            }
                            break;
                        }
                    } while (!result.EndOfMessage);

                    // Process the received message
                    string receivedMessage = Encoding.UTF8.GetString(messageBytes.ToArray());

                    if (!string.IsNullOrEmpty(receivedMessage) && _logicProviders != null)
                    {
                        foreach (var provider in _logicProviders)
                        {
                            provider?.OnMessageRecieved(receivedMessage);
                        }
                        //DebugLog.Print($"Received message from client {connection.clientId}: {receivedMessage}");
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
            if (_wsData == null || string.IsNullOrEmpty(clientId))
            {
                return;
            }
            var conn = await _wsData.GetConnectionById(clientId);
            if (conn == null)
            {
                return;
            }
            var webSocket = conn.webSocket;
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
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
