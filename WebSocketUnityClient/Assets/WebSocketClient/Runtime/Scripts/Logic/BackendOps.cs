using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebSocketClient
{
    static class BackendOps
    {
        #region ClientGroupBroadcastService
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
        #endregion

        #region CentralControllerService
        /// <summary>
        /// ע��һ�� Master
        /// </summary>
        public static string Cmd_RegisterAsMaster = "RegisterAsMaster";
        /// <summary>
        /// ע��һ�� Master
        /// </summary>
        public static string Cmd_UnregisterFromMaster = "UnregisterFromMaster";
        /// <summary>
        /// ��Master���Ϸ����仯�����ӣ�ɾ�����޸�
        /// </summary>
        public static string Notify_OnMasterCollectionChanged = "OnMasterCollectionChanged";
        /// <summary>
        /// ��ȡ����master
        /// </summary>
        public static string Cmd_GetAllMasters = "GetAllMasters";
        /// <summary>
        /// ע��һ��slave
        /// </summary>
        public static string Cmd_RegisterAsSlave = "RegisterAsSlave";
        /// <summary>
        /// ע��һ��slave
        /// </summary>
        public static string Cmd_UnregisterFromSlave = "UnregisterFromSlave";
        /// <summary>
        /// ������Ϣ
        /// </summary>
        public static string Cmd_Broadcast = "Broadcast";
        #endregion

        /// <summary>
        /// ��������
        /// </summary>
        public const string WSPing = "WSPing";

    }
}