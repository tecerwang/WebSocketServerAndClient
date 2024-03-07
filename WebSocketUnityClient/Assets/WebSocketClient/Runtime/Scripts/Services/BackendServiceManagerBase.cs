using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// 所有 manager 都是平等的，没有相互依赖关系
    /// </summary>
    public abstract class BackendServiceManagerBase
    {
        public abstract Task Init();

        public abstract Task Release();
    }
}
