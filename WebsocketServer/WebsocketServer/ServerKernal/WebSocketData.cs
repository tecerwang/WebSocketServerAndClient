namespace WebSocketServer.ServerKernal
{
    /// <summary>
    /// 运行在本机上的数据 Provider 实例
    /// </summary>
    public class WebSocketData : IWebSocketDataProvider
    {
        /// <summary>
        /// 客户端Id -> 客户端连接 字典
        /// </summary>
        public Dictionary<string, WebSocketClientConnection> connectedClients { get; private set; } 
            = new Dictionary<string, WebSocketClientConnection>();
        
    }
}
