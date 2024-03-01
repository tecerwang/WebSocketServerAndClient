using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// 游戏管理 manager 逻辑
    /// </summary>
    public class BackendManager : MonoBehaviour
    {
        #region Manager Reference
        public BroadcastManager broadcastManager { get; private set; }
        public MasterSlavesGroupService msGroupManager { get; private set; }
        #endregion

        public static BackendManager singleton; 

        public bool IsInited { get; private set; } = false;

        public WSBackend.WSBackendState wsState => WSBackend.singleton.State;

        private const string defaultBackendUrl = "ws://localhost:8080/ws";

        public string backendUrl = defaultBackendUrl;

        private async void Awake()
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);

            if (WSBackend.CreateSingleton(this))
            {
                // 初始化一个 client proxy gameObject
                WSBackend.singleton.Init(backendUrl);

                // 生成 manager instance
                var managers = InitServiceManagerRefs();

                // 初始化所有 manager 完成后尝试连接 backend
                await Task.WhenAll(managers.Select(p => p.Init()));

                // managers 初始化完成
                IsInited = true;
                await WSBackend.singleton.Connect2Server();
            }
        }

        /// <summary>
        /// 初始化所有 Managers
        /// </summary>
        private List<BackendServiceManagerBase> InitServiceManagerRefs()
        {
            // 使用反射查找所有 manager 
            var propertiesInfo = this.GetType().GetProperties();

            // 基类类型实例
            var baseType = typeof(BackendServiceManagerBase);

            Utility.LogDebug("BackendManager", "Generate manager instances derived from BackendServiceManagerBase");
            List<BackendServiceManagerBase> mgrRepo = new List<BackendServiceManagerBase>();

            foreach (var propertyInfo in propertiesInfo)
            {
                var propertyType = propertyInfo.PropertyType;
                if (baseType.IsAssignableFrom(propertyType))
                {
                    // 实例化
                    var mgrInstance = Activator.CreateInstance(propertyType) as BackendServiceManagerBase;
                    if (mgrInstance != null)
                    {
                        // 添加到repo
                        mgrRepo.Add(mgrInstance);
                        // 为 property 赋值
                        propertyInfo.SetValue(this, mgrInstance);
                        Utility.LogDebug("BackendManager", $"intatiate {propertyType.Name}");
                    }
                }
            }        
            return mgrRepo;
        }
    }
}
