using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebSocketClient
{
    static class BackendOps
    {
        /// <summary>
        /// �������
        /// </summary>
        public const string Cmd_JoinGroup = "JoinGroup";

        /// <summary>
        /// �뿪����
        /// </summary>
        public const string Cmd_LeaveGroup = "LeaveGroup";

        /// <summary>
        /// �㲥��Ϣ
        /// </summary>
        public const string Cmd_BroadcastMsg = "BroadcastMsg";

        /// <summary>
        /// ��������
        /// </summary>
        public const string WSPing = "WSPing";

    }
}