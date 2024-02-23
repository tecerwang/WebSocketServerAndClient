namespace WebSocketServer.ServerKernal
{
    /// <summary>
    /// WebSocket Service 所使用的需要持久化的数据都通过这个接口存放，如果后期有需要可以改成radis或者数据库
    /// </summary>
    public interface IWebSocketDataProvider
    {
        /// <summary>
        /// 所有连接客户端，key->客户端唯一的连接名
        /// </summary>
        public Dictionary<string, WebSocketClientConnection> connectedClients { get; }
    }
}
