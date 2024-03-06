using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;

namespace WebSocketClient
{
    /// <summary>
    /// ����һ������
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
        /// ��¼������Ϣ������������ʹ��
        /// </summary>
        private RequestContext _requestContext;
        /// <summary>
        /// Ĭ��Ϊ -1������δ������Ч����
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
            // rid < 0��˵��֮ǰ����û�гɹ�
            if (_rid < 0 && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                Request(_requestContext);
            }
        }

        private void Singleton_OnBackendResponse(ResponsePack response)
        {
            // �յ��������Ļ�ִ
            if (_rid > 0 && response.rid == _rid)
            {
                _responseDel?.Invoke(response.errCode, response.data, _requestContext.context);
                Release();
            }
        }

        private async void Request(RequestContext context)
        {
            if (WSBackend.singleton.State == WSBackend.WSBackendState.Open &&(_retryTimes < 0 || _retryCount < _retryTimes))
            {
                _rid = await WSBackend.singleton.CreateBackendRequest(context.serviceName, context.cmd, context.data);
                _retryCount++;
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
        /// ����retry����
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        /// <param name="onResponse"></param>
        /// <param name="retryTimes">���Դ���, -1 һֱ����</param>
        /// <returns></returns>
        public static bool CreateRetry(string serviceName, string cmd, JToken data, object context, OnResponse onResponse, int retryTimes = -1)
        {
            /// û�д��� backend ����
            if (WSBackend.singleton != null && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
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
                try
                {
                    request.Request(request._requestContext);
                    return true;
                }
                catch (Exception ex)
                {
                    // ���������Ҫ�ͷŵ���� request
                    Utility.LogExpection(ex.ToString());
                    request.Release();
                }
            }
            onResponse?.Invoke(ErrCode.Internal_RetryTimesOut, null, context);
            return false;
        }
    }
}
