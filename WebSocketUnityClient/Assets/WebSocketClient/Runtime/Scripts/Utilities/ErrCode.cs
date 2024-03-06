using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketClient
{
    public static class ErrCode
    {
        public static int Internal_Error = -1000;
        public static int Internal_RetryTimeout = -1001;
        public static int Internal_ConnectionClose = -1002;

        public static int Unkown = -100;

        public static int OK = 0;

        #region ClientGroupBroadcastService start from 10000
        public static int AlreadyInGroup = 10000;
        public static int NotInGroup = 10001;
        #endregion

        #region MasterSlavesGroupService start from 11000
        /// <summary>
        /// client �Ѿ�ע���
        /// </summary>
        public static int AlreadyRegistered = 11000;
        /// <summary>
        /// masterId Ϊ��
        /// </summary>
        public static int MasterIdIsNull = 11001;
        /// <summary>
        /// master name Ϊ��
        /// </summary>
        public static int MasterNameIsNull = 11002;
        /// <summary>
        /// master �Ѿ�����
        /// </summary>
        public static int MasterIsOffline = 11003;
        /// <summary>
        /// ȱʧ���ݰ�
        /// </summary>
        public static int DataIsNull = 11004;
        #endregion
    }
}