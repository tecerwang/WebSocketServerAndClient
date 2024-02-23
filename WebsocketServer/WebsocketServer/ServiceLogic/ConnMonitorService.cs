using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketServer.ServerKernal.Data;
using WebSocketServer.Utilities;

namespace WebSocketServer.ServiceLogic
{
    /// <summary>
    /// websocket 网络连接业务逻辑管理
    /// 1.如果网络心跳超时，复制通知 server 关闭连接
    /// 2.todo:可扩展短线重连的业务逻辑
    /// </summary>
    class ConnMonitorService : AbstractServiceLogic
    {
        public override string serviceName => "ConnMonitorService";

        protected override Task OnClientOpen(string clientId)
        {
            return Task.CompletedTask;
        }

        protected override Task OnClientClose(string clientId)
        {
            return Task.CompletedTask;
        }

        protected override async Task OnMessageRecieved(RequestPack request)
        {
            if (request == null)
            {
                return;
            }
            if (request.cmd == BackendOps.WSPing)
            {
                DebugLog.Print("[ConnMonitorService]", "recieve wsPing");
                await CreateResponseToClient(request, null, ErrCode.OK);
            }
        }       
    }
}
