using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace WebSocketClient
{
    /// <summary>
    /// websocket 网络连接业务逻辑管理
    /// 1.如果网络心跳超时，复制通知 server 关闭连接
    /// </summary>
    public class ConnMonitor
    {
        private const string serviceName = "ConnMonitorService";

        /// <summary>
        /// 最后一次成功通讯的时间，如果长时间没有通讯，负责发送网络心跳
        /// </summary>
        private float _lastestWSTick;

        private WSBackend _backend;

        private Coroutine _monitorCoroutine;

        public void Init()
        {
            _backend.OnBackendStateChanged += Singleton_OnBackendStateChanged;
            _backend.OnBackendResponse += Backend_OnBackendResponse;
            _backend.OnBackendNotify += Backend_OnBackendNotify;
        }

        public void Release()
        {
            _backend.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
            _backend.OnBackendResponse -= Backend_OnBackendResponse;
            _backend.OnBackendNotify -= Backend_OnBackendNotify;
        }

        private void Backend_OnBackendResponse(string serviceName, string cmd, int errCode, int rid, JToken data)
        {
            _lastestWSTick = Time.realtimeSinceStartup;
        }

        private void Backend_OnBackendNotify(string serviceName, string cmd, JToken data)
        {
            _lastestWSTick = Time.realtimeSinceStartup;
        }

        private void Singleton_OnBackendStateChanged()
        {
            if (_backend.State == WSBackend.WSBackendState.Open)
            {
                if (_monitorCoroutine != null)
                {
                    _backend.monoGameObject.StopCoroutine(_monitorCoroutine);
                }
                _monitorCoroutine = _backend.monoGameObject.StartCoroutine(HeartbeatMonitor());
            }
            else
            {
                if (_monitorCoroutine != null)
                {
                    _backend.monoGameObject.StopCoroutine(_monitorCoroutine);
                    _monitorCoroutine = null;
                }
            }
        }

        private IEnumerator HeartbeatMonitor()
        {
            _lastestWSTick = Time.realtimeSinceStartup;
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                if (_backend.State == WSBackend.WSBackendState.Close)
                {
                    yield break;
                }
                var curRealTime = Time.realtimeSinceStartup;
                /// 5秒一次 wsping
                if (curRealTime - _lastestWSTick > 5)
                {
                    _lastestWSTick = curRealTime;
                    _backend.CreateBackendRequest(serviceName, BackendOps.WSPing, null);
                }
            }
        }

        /// <summary>
        /// 网络心跳超时
        /// </summary>
        public event Action WSPingTimeout;

        public static ConnMonitor Create(WSBackend backend)
        {
            ConnMonitor instance = new ConnMonitor();
            instance._backend = backend;
            return instance;
        }
    }
}
