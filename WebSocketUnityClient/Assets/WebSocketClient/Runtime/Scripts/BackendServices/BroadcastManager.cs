using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// �㲥����
    /// </summary>
    public class BroadcastManager : BackendServiceManagerBase
    {
        private const string serviceName = "ClientGroupBroadcastService";

        /// <summary>
        /// Э�������͵���Ϣ
        /// </summary>
        public class Data
        {
            /// <summary>
            /// ��ȺID
            /// </summary>
            public string groupId;
            /// <summary>
            /// �ͻ��˵�����
            /// </summary>
            public string clientName;
            /// <summary>
            /// ��Ҫ�㲥����Ϣ
            /// </summary>
            public string msg;
            /// <summary>
            /// �Ƿ��ٹ㲥��ϢȺ��, �Է�������ϢΪ׼
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
        /// �����������ͨѶ
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
                // �յ��㲥��Ϣ
                if (not.cmd == BackendOps.Cmd_BroadcastMsg)
                {
                    var msg = JHelper.GetJsonString(not.data, "msg");
                    Utility.LogDebug("BroadcastManager", $"recieve broadcasted msg {msg}");
                }
            }
        }

        /// <summary>
        /// ����״̬�仯
        /// </summary>
        private void Singleton_OnBackendStateChanged()
        {

        }


        /// <summary>
        /// ���뵽�㲥��Ⱥ
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
        /// �뿪��Ⱥ 
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
        /// �㲥��Ϣ
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
