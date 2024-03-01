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
    /// ��Ϸ���� manager �߼�
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
                // ��ʼ��һ�� client proxy gameObject
                WSBackend.singleton.Init(backendUrl);

                // ���� manager instance
                var managers = InitServiceManagerRefs();

                // ��ʼ������ manager ��ɺ������� backend
                await Task.WhenAll(managers.Select(p => p.Init()));

                // managers ��ʼ�����
                IsInited = true;
                await WSBackend.singleton.Connect2Server();
            }
        }

        /// <summary>
        /// ��ʼ������ Managers
        /// </summary>
        private List<BackendServiceManagerBase> InitServiceManagerRefs()
        {
            // ʹ�÷���������� manager 
            var propertiesInfo = this.GetType().GetProperties();

            // ��������ʵ��
            var baseType = typeof(BackendServiceManagerBase);

            Utility.LogDebug("BackendManager", "Generate manager instances derived from BackendServiceManagerBase");
            List<BackendServiceManagerBase> mgrRepo = new List<BackendServiceManagerBase>();

            foreach (var propertyInfo in propertiesInfo)
            {
                var propertyType = propertyInfo.PropertyType;
                if (baseType.IsAssignableFrom(propertyType))
                {
                    // ʵ����
                    var mgrInstance = Activator.CreateInstance(propertyType) as BackendServiceManagerBase;
                    if (mgrInstance != null)
                    {
                        // ��ӵ�repo
                        mgrRepo.Add(mgrInstance);
                        // Ϊ property ��ֵ
                        propertyInfo.SetValue(this, mgrInstance);
                        Utility.LogDebug("BackendManager", $"intatiate {propertyType.Name}");
                    }
                }
            }        
            return mgrRepo;
        }
    }
}
