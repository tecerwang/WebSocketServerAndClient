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
    /// websocket 网络连接业务逻辑管理
    /// 1.如果网络心跳超时，复制通知 server 关闭连接
    /// </summary>
    public class ConnMonitor
    {
        private const string serviceName = "ConnMonitorService";

        private WSBackend _backend;

        private DateTime _lastCommunicateTime;

        private const int _intervalMS = 2000;

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
            _lastCommunicateTime = DateTime.Now;
        }

        private void Backend_OnBackendNotify(NotifyPack not)
        {
            _lastCommunicateTime = DateTime.Now;
        }

        private CancellationTokenSource _heartBeatCTS;

        private void Singleton_OnBackendStateChanged()
        {
            return;
            if (_backend.State == WSBackend.WSBackendState.Open)
            {
                _heartBeatCTS = new CancellationTokenSource();
                _lastCommunicateTime = DateTime.Now;
                Task.Run(async () =>
                {
                    Utility.LogDebug("ConnMonitor", "heart beat monitor start");
                    while (WSBackend.singleton.State == WSBackend.WSBackendState.Open)
                    {
                        var curTime = DateTime.Now;
                        var curInterval = (curTime - _lastCommunicateTime).TotalMilliseconds;
                        if (curInterval > _intervalMS * 2)
                        {
                            await WSBackend.singleton.CloseAsync();
                            break;
                        }
                        else if (curInterval > _intervalMS)
                        {
                            await _backend.CreateBackendRequest(serviceName, BackendOps.WSPing, null);
                            await Task.Delay(_intervalMS, _heartBeatCTS.Token);
                        }
                        else
                        {
                            await Task.Delay((int)(_intervalMS - curInterval), _heartBeatCTS.Token);
                        }
                        if (_backend.State == WSBackend.WSBackendState.Close)
                        {
                            break;
                        }
                    }
                    Utility.LogDebug("ConnMonitor", "heart beat monitor end");
                    await WSBackend.singleton.Connect2ServerAsync();
                }, _heartBeatCTS.Token);
            }
            else
            {
                _heartBeatCTS.Cancel();
                _heartBeatCTS.Dispose();
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
