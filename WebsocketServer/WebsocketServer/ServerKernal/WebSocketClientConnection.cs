using System.Net.WebSockets;

namespace WebSocketServer.ServerKernal
{
    /// <summary>
    /// web socket 连接对象；
    /// 用于管理链接；
    /// 显示状态；
    /// 提供当前链接状态；
    /// </summary>
    public class WebSocketClientConnection
    {
        public WebSocketClientConnection(
            string clientId,
            DateTime connectedTime,
            WebSocket webSocket)
        {
            this.clientId = clientId;
            this.connectedTime = connectedTime;
            this.webSocket = webSocket;
        }

        /// <summary>
        /// 由客户端提供的 id
        /// </summary>
        public string clientId { get; private set; }

        public DateTime connectedTime { get; private set; }

        public WebSocket webSocket { get; private set; }

        public override int GetHashCode()
        {
            return clientId.GetHashCode();
        }

        /// <summary>
        /// 只有当前提供有效的的ClientId，此链接才可以有效
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(clientId);
        }
    }
}
