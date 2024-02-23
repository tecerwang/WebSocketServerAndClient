using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// 抽象一次请求
    /// </summary>
    public class BackendRequest
    {
        public delegate void OnResponse(int errCode, JToken data, object context);

        private OnResponse _responseDel;

        private struct RequestContext
        {
            public string serviceName;
            public string cmd;
            public JToken data;
            public object context;
        }

        /// <summary>
        /// 记录请求信息，断线重连后使用
        /// </summary>
        private RequestContext _requestContext;
        /// <summary>
        /// 默认为 -1，代表未发起有效请求
        /// </summary>
        private int _rid = -1;
        private int _retryTimes;

        private int _retryCount = 0;

        private BackendRequest()
        {
            WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;
            WSBackend.singleton.OnBackendResponse += Singleton_OnBackendResponse;
        }

        private void Singleton_OnBackendStateChanged()
        {
            // rid < 0，说明之前请求没有成功
            if (_rid < 0 && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                Request(_requestContext);
            }
        }

        private void Singleton_OnBackendResponse(string serviceName, string cmd, int errCode, int rid, JToken data)
        {
            // 收到这个请求的回执
            if (_rid > 0 && rid == _rid)
            {
                _responseDel?.Invoke(errCode, data, _requestContext.context);
                Release();
            }
        }

        private void Request(RequestContext context)
        {
            if (_retryTimes < 0 || _retryCount < _retryTimes)
            {
                var result = WSBackend.singleton.CreateBackendRequest(context.serviceName, context.cmd, context.data);
                _retryCount++;
                _rid = result == null ? -1 : result.rid;
            }
            else
            {
                _responseDel?.Invoke(ErrCode.Internal_RetryTimesOut, null, _requestContext.context);
                Release();
            }
        }

        private void Release()
        {
            WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
            WSBackend.singleton.OnBackendResponse -= Singleton_OnBackendResponse;
        }

        /// <summary>
        /// 创建retry请求
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        /// <param name="onResponse"></param>
        /// <param name="retryTimes">重试次数, -1 一直重试</param>
        /// <returns></returns>
        public static bool CreateRetry(string serviceName, string cmd, JToken data, object context, OnResponse onResponse, int retryTimes = -1)
        {
            /// 没有创建 backend 单例
            if (WSBackend.singleton == null)
            {
                return false;
            }

            BackendRequest request = new BackendRequest();
            request._responseDel = onResponse;
            request._retryTimes = retryTimes;
            request._requestContext = new RequestContext()
            {
                serviceName = serviceName,
                cmd = cmd,
                data = data,
                context = context
            };

            if (WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                request.Request(request._requestContext);
            }
            return true;
        }
    }
}
