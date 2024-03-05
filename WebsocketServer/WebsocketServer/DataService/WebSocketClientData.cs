using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebSocketServer.ServerKernal;
using WebSocketServer.Utilities;

namespace WebSocketServer.DataService
{
    /// <summary>
    /// 运行在本机上的数据 Provider 实例
    /// </summary>
    internal class WebSocketClientData
    {
        /// <summary>
        /// 客户端Id -> 客户端连接 字典
        /// </summary>
        private ConcurrentDictionary<string, WebSocketClientConnection> connectedClients = new ConcurrentDictionary<string, WebSocketClientConnection>();

        /// <summary>
        /// 添加到一个客户端
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<bool> AddConnection(WebSocketClientConnection conn)
        {
            return Task.FromResult(connectedClients.TryAdd(conn.clientId, conn));
        }

        /// <summary>
        /// 移除到一个客户端 by ID
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal Task<bool> RemoveConnection(string connId)
        {
            if (!string.IsNullOrEmpty(connId))
            {
                return Task.FromResult(connectedClients.TryRemove(connId, out _));
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
