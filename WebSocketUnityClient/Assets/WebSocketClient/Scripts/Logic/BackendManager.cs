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
        public static BackendManager singleton;

        private List<BackendServiceManagerBase> managersRepo = new List<BackendServiceManagerBase>();

        // Manager references below
        public BroadcastManager broadcastManager { get; private set; }

        public bool IsInited { get; private set; } = false;

        public WSBackend.WSBackendState wsState => WSBackend.singleton.State;

        public string backendUrl = "ws://localhost:8080/ws";

        private async void Awake()
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);

            if (WSBackend.CreateSingleton(this))
            {
                // ��ʼ��һ�� client proxy gameObject
                WSBackend.singleton.Init(backendUrl);

                // ���� manager instance
                InitServiceManagerRefs();

                // ��ʼ������ manager ��ɺ������� backend
                await Task.WhenAll(managersRepo.Select(p => p.Init()));

                // managers ��ʼ�����
                IsInited = true;
                await WSBackend.singleton.Connect2Server();
            }
        }

        /// <summary>
        /// ��ʼ������ Managers
        /// </summary>
        private void InitServiceManagerRefs()
        {
            // ʹ�÷���������� manager 
            var propertiesInfo = this.GetType().GetProperties();

            // ��������ʵ��
            var baseType = typeof(BackendServiceManagerBase);

            Utility.LogDebug("BackendManager", "Generate manager instances derived from BackendServiceManagerBase");

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
                        managersRepo.Add(mgrInstance);
                        // Ϊ���õ� property ��ֵ
                        propertyInfo.SetValue(this, mgrInstance);
                        Utility.LogDebug("BackendManager", $"intatiate {propertyType.Name}");
                    }
                }
            }
        
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
    
            foreach (var assembly in assemblies)
            {
                try
                {
                    var managerTypes = assembly.GetTypes()?.Where(t => t.BaseType == baseType);
                   
                }
                catch (Exception ex)
                {
                    Utility.LogError(ex.ToString());
                }
            }
        }
    }
}
