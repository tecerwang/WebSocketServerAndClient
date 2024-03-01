using System.Net.WebSockets;
using WebSocketServer.ServerKernal;
using WebSocketServer.Utilities;

namespace WebSocketServer.DataProvider
{
    /// <summary>
    /// 运行在本机上的数据 Provider 实例
    /// </summary>
    internal class WebSocketClientData
    {
        /// <summary>
        /// 客户端Id -> 客户端连接 字典
        /// </summary>
        private Dictionary<string, WebSocketClientConnection> connectedClients = new Dictionary<string, WebSocketClientConnection>();

        /// <summary>
        /// 添加到一个客户端
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<bool> AddConnection(WebSocketClientConnection conn)
        {
            if (connectedClients.ContainsKey(conn.clientId))
            {
                return Task.FromResult(false);
            }
            else
            {
                connectedClients.Add(conn.clientId, conn);
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// 移除到一个客户端 by ID
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<bool> RemoveConnection(string connId)
        {
            if (!string.IsNullOrEmpty(connId) && connectedClients.ContainsKey(connId))
            {
                connectedClients.Remove(connId);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 移除到一个客户端
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<bool> RemoveConnection(WebSocketClientConnection conn)
        {
            return RemoveConnection(conn.clientId);
        }

        /// <summary>
        /// 获取到一个客户端 by ID
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<WebSocketClientConnection?> GetConnectionById(string connId)
        {
            if (!string.IsNullOrEmpty(connId) && connectedClients.ContainsKey(connId))
            {
                return Task.FromResult<WebSocketClientConnection?>(connectedClients[connId]);
            }
            else
            {
                return Task.FromResult<WebSocketClientConnection?>(null);
            }
        }
    }
}
