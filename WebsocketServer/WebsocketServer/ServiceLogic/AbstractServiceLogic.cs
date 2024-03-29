﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketServer.ServerKernal;
using WebSocketServer.ServerKernal.MsgPack;
using WebSocketServer.Utilities;

namespace WebSocketServer.ServiceLogic
{
    /// <summary>
    /// Server Logic Handler，
    /// 所有 service 处理逻辑需要继承这个接口
    /// </summary>
    public abstract class AbstractServiceLogic : IWebSocketLogic
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public abstract string serviceName { get; }

        private WebSocketMiddleWare? wsMiddleWare;

        protected abstract Task OnClientOpen(string clientId);

        protected abstract Task OnClientClose(string clientId);   

        protected abstract Task OnMessageRecieved(RequestPack data);

        // 接口继承下来方法会阻塞线程，这在个 logic 方法中需要使用异步方法，如果需要同步，需要 override 一个 await 方法

        async Task IWebSocketLogic.OnClientOpen(string clientId, WebSocketMiddleWare ws)
        {
            wsMiddleWare = ws;
            await OnClientOpen(clientId);
        }

        async Task IWebSocketLogic.OnClientClose(string clientId)
        {
            await OnClientClose(clientId);
        }

        async Task IWebSocketLogic.OnMessageRecieved(string receivedMessage)
        {
            if (string.IsNullOrEmpty(receivedMessage))
            {
                return;
            }
            var rawData = JObject.Parse(receivedMessage);
            if (rawData == null)
            {
                return;
            }
            var requesetPack = RequestPack.Parse(rawData);
            if (requesetPack != null && requesetPack.serviceName == serviceName)
            {
                await OnMessageRecieved(requesetPack);
            }
        }

        /// <summary>
        /// 创建一个请求的回执
        /// </summary>
        /// <param name="request"></param>
        /// <param name="msg"></param>
        /// <param name="errCode"></param>
        /// <returns></returns>
        protected async Task CreateResponseToClient(RequestPack request, JToken? data, int errCode)
        {
            if (wsMiddleWare == null || request == null)
            {
                return;
            }
            var response = ResponsePack.CreateFromRequest(request, data, errCode);
            var msg = response.ToString();
            await wsMiddleWare.SendMsgToClientAsync(request.clientId, msg);
        }

        protected async Task CreateNotifyToClient(string clientId, string serviceName, string cmd, JToken? data)
        {
            if (wsMiddleWare == null)
            {             
                return; 
            }
            NotifyPack notify = new NotifyPack()
            {
                clientId = clientId,
                serviceName = serviceName,
                cmd = cmd,
                utcTicks = Utility.UTCNowSeconds(),
                data = data
            };
            var msg = notify.ToString();
            await wsMiddleWare.SendMsgToClientAsync(clientId, msg);
        }
    }
}
