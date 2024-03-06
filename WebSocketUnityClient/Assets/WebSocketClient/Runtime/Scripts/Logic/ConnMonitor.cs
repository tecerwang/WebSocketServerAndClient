using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using UIFramework;

namespace WebSocketClient
{
    /// <summary>
    /// websocket ��������ҵ���߼�����
    /// 1.�������������ʱ������֪ͨ server �ر�����
    /// </summary>
    public class ConnMonitor
    {
        private const string serviceName = "ConnMonitorService";

        private WSBackend _backend;

        private const int _intervalMS = 5000;

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

        private void Backend_OnBackendResponse(ResponsePack resp)
        {
            StartNewHeartBeatTick();
        }

        private void Backend_OnBackendNotify(NotifyPack not)
        {
            StartNewHeartBeatTick();
        }

        /// <summary>
        /// �յ��κ���Ϣ����Ҫ��������ʱ�䣬�����ն˵�ǰ��������ʱ
        /// </summary>
        private CancellationTokenSource _heartBeatCTS;

        private void StartNewHeartBeatTick()
        {
            if (_heartBeatCTS != null)
            {
                _heartBeatCTS.Cancel();
                _heartBeatCTS.Dispose();
            }
            _heartBeatCTS = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (_heartBeatCTS != null && !_heartBeatCTS.IsCancellationRequested)
                {
                    // Delay before sending ping
                    await Task.Delay(_intervalMS, _heartBeatCTS.Token);

                    // Check cancellation before sending ping
                    if (_heartBeatCTS.IsCancellationRequested)
                    {
                        break;
                    }

                    // Send ping
                    Utility.LogDebug("ConnMonitor", "send wsping");
                    await _backend.CreateBackendRequest(serviceName, BackendOps.WSPing, null);
                }
            });
        }

        private void Singleton_OnBackendStateChanged()
        {
            if (_backend.State == WSBackend.WSBackendState.Open)
            {
                Utility.LogDebug("ConnMonitor", "heart beat monitor start");
                StartNewHeartBeatTick();
            }
            else
            {
                _heartBeatCTS.Cancel();
                _heartBeatCTS.Dispose();
                _heartBeatCTS = null;
                Utility.LogDebug("ConnMonitor", "heart beat monitor cancel by ws close");
            }
        }

        public static ConnMonitor Create(WSBackend backend)
        {
            ConnMonitor instance = new ConnMonitor();
            instance._backend = backend;
            return instance;
        }
    }
}
