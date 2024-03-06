using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// 广播服务
    /// </summary>
    public class BroadcastManager : BackendServiceManagerBase
    {
        private const string serviceName = "ClientGroupBroadcastService";

        /// <summary>
        /// 协议所发送的消息
        /// </summary>
        public class Data
        {
            /// <summary>
            /// 组群ID
            /// </summary>
            public string groupId;
            /// <summary>
            /// 客户端的名称
            /// </summary>
            public string clientName;
            /// <summary>
            /// 需要广播的消息
            /// </summary>
            public string msg;
            /// <summary>
            /// 是否再广播消息群中, 以服务器消息为准
            /// </summary>
            public bool isInGroup { get; private set; }

            public override int GetHashCode() => (groupId + "_" + clientName).GetHashCode();

            public static Data Parse(JToken jdata)
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

            public JObject ToJson()
            {
                JObject jobj = new JObject();
                jobj.Add("groupId", groupId);
                jobj.Add("clientName", clientName);
                jobj.Add("msg", msg);
                jobj.Add("isInGroup", isInGroup);
                return jobj;
            }
        }

        private string _groupId = "Test Group";
        private string _clientName = "Test Client";
        public bool IsClientInGroup { get; private set; } = false;

        /// <summary>
        /// 正在与服务器通讯
        /// </summary>
        private bool _isQuarying = false;

        public override async Task Init()
        {
            WSBackend.singleton.OnBackendNotify += Singleton_OnBackendNotify;
            WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;
            await Task.CompletedTask;
        }

        public override async Task Release()
        {
            WSBackend.singleton.OnBackendNotify -= Singleton_OnBackendNotify;
            WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
            await Task.CompletedTask;
        }

        private void Singleton_OnBackendNotify(NotifyPack not)
        {
            if (not.serviceName == BroadcastManager.serviceName)
            {
                // 收到广播消息
                if (not.cmd == BackendOps.Cmd_BroadcastMsg)
                {
                    var msg = JHelper.GetJsonString(not.data, "msg");
                    Utility.LogDebug("BroadcastManager", $"recieve broadcasted msg {msg}");
                }
            }
        }

        /// <summary>
        /// 网络状态变化
        /// </summary>
        private void Singleton_OnBackendStateChanged()
        {

        }


        /// <summary>
        /// 加入到广播组群
        /// </summary>
        /// <param name="groupId"></param>
        public void JoinInGroup()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                Data rdata = new Data()
                {
                    groupId = _groupId,
                    clientName = _clientName
                };
                _isQuarying = true;
                BackendRequest.Create(serviceName, BackendOps.Cmd_JoinGroup, rdata.ToJson(), null, OnJoinOrLeaveGroupResponse);
            }
        }

        /// <summary>
        /// 离开组群 
        /// </summary>
        public void LeaveFromGroup()
        {
            if (!_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                Data rdata = new Data()
                {
                    groupId = _groupId,
                    clientName = _clientName
                };
                _isQuarying = true;
                BackendRequest.Create(serviceName, BackendOps.Cmd_LeaveGroup, rdata.ToJson(), null, OnJoinOrLeaveGroupResponse);
            }
        }

        private void OnJoinOrLeaveGroupResponse(int errCode, JToken data, object context)
        {
            _isQuarying = false;
            if (errCode == ErrCode.OK && data != null)
            {
                var rdata = Data.Parse(data);
                if (rdata != null)
                {
                    IsClientInGroup = rdata.isInGroup;
                }
            }
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="msg"></param>
        public void Broadcast(string msg)
        {
            if (!string.IsNullOrEmpty(msg) && !_isQuarying && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                Data rdata = new Data()
                {
                    groupId = _groupId,
                    clientName = _clientName,
                    msg = msg
                };
                _isQuarying = true;
                BackendRequest.Create(serviceName, BackendOps.Cmd_BroadcastMsg, rdata.ToJson(), null, OnBroadcastResponse);
            }
        }

        private void OnBroadcastResponse(int errCode, JToken data, object context)
        {
            _isQuarying = false;
            if (errCode == ErrCode.OK && data != null)
            {
                var rdata = Data.Parse(data);
                if (rdata != null)
                {
                    IsClientInGroup = rdata.isInGroup;
                    Utility.LogDebug("BroadcastManager", $"broadcast msg {rdata.msg}");
                }
            }
        }
    }
}
