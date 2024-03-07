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
    /// 管理游戏与服务器 ws 通信逻辑
    /// </summary>
    public class BackendManager : MonoBehaviour
    {
        #region Manager Reference
        public BroadcastManager broadcastManager { get; private set; }
        public MasterSlavesGroupService msGroupManager { get; private set; }
        #endregion

        public static BackendManager singleton;

        private bool _isInited = false;

        public WSBackend.WSBackendState wsState => WSBackend.singleton.State;

        public const string defaultBackendUrl = "ws://localhost:8080/ws";

        public const string defaultClientId = "Unity";

        private static TaskCompletionSource<bool> _waitForInitedAsyncTCS;

        /// <summary>
        /// 目前只负责网络心跳
        /// </summary>
        private ConnMonitor _monitor;

        /// <summary>
        /// 异步等待 backend manager 完成初始化
        /// </summary>
        /// <returns></returns>
        public static async Task WaitForInitAsync()
        {
            if (singleton != null && singleton._isInited)
            {
                return;
            }
            _waitForInitedAsyncTCS = new TaskCompletionSource<bool>();
            await _waitForInitedAsyncTCS.Task;
        }

        // 初始化 backend
        public async static Task<bool> Init(string backendUrl, string clientId)
        {
            if (BackendManager.singleton != null)
            {
                return false;
            }

            TaskCompletionSource<bool> completeSource = new TaskCompletionSource<bool>();
            var gameObject = new GameObject();
            gameObject.name = "[BackendManager]";
            DontDestroyOnLoad(gameObject);
            singleton = gameObject.AddComponent<BackendManager>();

            if (WSBackend.CreateSingleton(singleton))
            {
                // 初始化一个 wsBackend
                WSBackend.singleton.Init(backendUrl, clientId);

                // 生成 manager instance
                var managers = singleton.InitServiceManagerRefs();

                // 初始化所有 manager 完成后尝试连接 backend
                await Task.WhenAll(managers.Select(p => p.Init()));

                // 实例化连接监视器
                singleton._monitor = ConnMonitor.Create(WSBackend.singleton);
                singleton._monitor.Init();

                // managers 初始化完成
                singleton._isInited = true;

                // 让 WaitForInitedAsync 结束等待
                _waitForInitedAsyncTCS?.SetResult(true);

                _= WSBackend.singleton.ConnectAndRecvAsync();
                return true;
            }
            return false;
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

        private async void OnApplicationQuit()
        {
            UIFramework.Utility.LogDebug("BackendManager", "Invoke close connection by application quit");
            await WSBackend.singleton?.CloseAsync();
        }
    }
}
