using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketServer.ServerKernal.MsgPack;
using WebSocketServer.Utilities;

namespace WebSocketServer.ServiceLogic
{
    /// <summary>
    /// 同组 client 广播通讯逻辑
    /// </summary>
    internal class ClientGroupBroadcastService : AbstractServiceLogic
    {
        /// <summary>
        /// 协议所发送的消息
        /// </summary>
        internal class Data
        {
            /// <summary>
            /// 组群ID
            /// </summary>
            internal string? groupId;
            /// <summary>
            /// 客户端的名称
            /// </summary>
            internal string? clientName;
            /// <summary>
            /// 需要广播的消息
            /// </summary>
            internal string? msg;
            /// <summary>
            /// 是否再广播消息群中
            /// </summary>
            internal bool isInGroup;

            public override int GetHashCode() => (groupId + "_" + clientName).GetHashCode();

            internal static Data? Parse(JToken? jdata)
            {
                if (jdata == null)
                {
                    return null;
                }
                Data data = new Data();
                data.groupId = JHelper.GetJsonString(jdata, "groupId");
                data.clientName = JHelper.GetJsonString(jdata, "clientName");
                data.msg = JHelper.GetJsonString(jdata, "msg");
                data.isInGroup = JHelper.GetJsonBool(jdata, "isInGroup");
                return data;
            }

            internal JObject ToJson()
            {
                JObject jobj = new JObject();
                jobj.Add("groupId", groupId);
                jobj.Add("clientName", clientName);
                jobj.Add("msg", msg);
                jobj.Add("isInGroup", isInGroup);
                return jobj;
            }
        }

        internal override string serviceName => "ClientGroupBroadcastService";

        /// <summary>
        /// 分好组的 clients
        /// </summary>
        private Dictionary<string, HashSet<string>> GrouppedClients = new Dictionary<string, HashSet<string>>();

       

        protected override Task OnClientOpen(string clientId)
        {
            return Task.CompletedTask;
        }

        protected override Task OnClientClose(string clientId)
        {
            /// 断网后从组中移除
            foreach (var clients in GrouppedClients.Values)
            {
                if (clients.Contains(clientId))
                {
                    clients.Remove(clientId);
                    DebugLog.Print($"ClientGroupBroadcastService Websocket Connection closed and clean the resources : client {clientId}");
                }
            }
            return Task.CompletedTask;
        }

        protected override async Task OnMessageRecieved(RequestPack request)
        {
            if (request == null)
            {
                return;
            }
            var clientId = request.clientId;
            var data = Data.Parse(request.data);

            if (!string.IsNullOrEmpty(clientId) && data != null && !string.IsNullOrEmpty(data.groupId))
            {
                string groupId = data.groupId;
                string? msg = data.msg;

                if (request.cmd == BackendOps.Cmd_JoinGroup)
                {
                    if (!GrouppedClients.TryGetValue(groupId, out HashSet<string>? clients))
                    {
                        if (clients == null)
                        {
                            clients = new HashSet<string>();
                        }
                        GrouppedClients.Add(groupId, clients);
                    }

                    if (clients.Contains(clientId))
                    {
                        DebugLog.Print($"ClientGroupBroadcastService JoinGroup error : client {clientId} already joint in to the group of {groupId}");
                    }
                    else
                    {
                        clients.Add(clientId);
                        DebugLog.Print($"ClientGroupBroadcastService JoinGroup successfully : client {clientId} join in to the group of {groupId}");
                    }
                    data.isInGroup = true;
                    await CreateResponseToClient(request, data.ToJson(), ErrCode.OK);

                }
                else if (request.cmd == BackendOps.Cmd_LeaveGroup)
                {
                    if (GrouppedClients.TryGetValue(groupId, out HashSet<string>? clients) && clients != null && clients.Contains(clientId))
                    {
                        clients.Remove(clientId);
                        DebugLog.Print($"ClientGroupBroadcastService LeaveGroup successfully : client {clientId} leave from the group of {groupId}");
                    }
                    else
                    {
                        DebugLog.Print($"ClientGroupBroadcastService LeaveGroup error : client {clientId} does not contains int the group of {groupId}");
                    }
                    data.isInGroup = false;
                    await CreateResponseToClient(request, data.ToJson(), ErrCode.OK);
                }
                else if (request.cmd == BackendOps.Cmd_BroadcastMsg)
                {
                    // 不可以广播空消息
                    if (!string.IsNullOrEmpty(msg) && GrouppedClients.TryGetValue(groupId, out HashSet<string>? clients) && clients.Contains(clientId))
                    {
                        DebugLog.Print($"ClientGroupBroadcastService BroadcastMsg begin to broadcast msg {msg} to the group of {groupId}");
                        var jdata = data.ToJson();
                        foreach (var cid in clients)
                        {
                            // 异步执行
                            await CreateNotifyToClient(cid, serviceName, request.cmd, jdata);
                            DebugLog.Print($"ClientGroupBroadcastService BroadcastMsg to client: {cid} OK");
                        }
                        // 组内所有 client 接收到消息后，返回OK
                        DebugLog.Print($"ClientGroupBroadcastService BroadcastMsg end");
                        await CreateResponseToClient(request, jdata, ErrCode.OK);
                    }
                    else
                    {
                        DebugLog.Print($"ClientGroupBroadcastService BroadcastMsg error : client {clientId} does not contains in the group of {groupId}, please join group ahead");
                        await CreateResponseToClient(request, null, ErrCode.NotInGroup);
                    }
                }
                else
                {
                    DebugLog.Print($"ClientGroupBroadcastService msg invalid, cmd is null or unexpected");
                    await CreateResponseToClient(request, null, ErrCode.Unkown);
                }
            }
            else
            {
                DebugLog.Print($"ClientGroupBroadcastService msg invalid, groupMsg or groupId is null");
                await CreateResponseToClient(request, null, ErrCode.Unkown);
            }
        }
    }
}
