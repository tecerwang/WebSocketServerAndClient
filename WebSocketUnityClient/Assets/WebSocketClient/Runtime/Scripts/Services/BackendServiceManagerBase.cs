using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// ���� manager ����ƽ�ȵģ�û���໥������ϵ
    /// </summary>
    public abstract class BackendServiceManagerBase
    {
        public abstract Task Init();

        public abstract Task Release();
    }
}
