using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private BackendRequest()
        {
            WSBackend.singleton.OnBackendResponse += Singleton_OnBackendResponse;
        }

        private void Singleton_OnBackendResponse(ResponsePack response)
        {
            // �յ��������Ļ�ִ
            if (response.rid == _rid)
            {
                _responseDel?.Invoke(response.errCode, response.data, _requestContext.context);
                Release();
            }
        }

        private async Task<bool> Request(RequestContext context)
        {
            var errCode = ErrCode.Internal_Error;
            if (WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                var result = await WSBackend.singleton.CreateBackendRequest(context.serviceName, context.cmd, context.data);
                if (result.state == WebSocketClient.SendMsgState.Sent)
                {
                    _rid = result.rid;
                    /// �����Ϣ���� �ȴ���ִ
                    return await Task.FromResult(true);
                }

                switch (result.state)
                {
                    case WebSocketClient.SendMsgState.Timeout:
                        {
                            errCode = ErrCode.Internal_RetryTimeout;
                            break;
                        }
                    case WebSocketClient.SendMsgState.InteruptByConnectionClose:
                        {
                            errCode = ErrCode.Internal_ConnectionClose;
                            break;
                        }
                }
            }
            _responseDel?.Invoke(errCode, null, _requestContext.context);
            Release();
            return await Task.FromResult(false);
        }

        private void Release()
        {
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
        public static void Create(string serviceName, string cmd, JToken data, object context, OnResponse onResponse)
        {
            /// û�д��� backend ����
            if (WSBackend.singleton != null && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
            {
                BackendRequest request = new BackendRequest();
                request._responseDel = onResponse;
                request._requestContext = new RequestContext()
                {
                    serviceName = serviceName,
                    cmd = cmd,
                    data = data,
                    context = context
                };
                try
                {
                    _= request.Request(request._requestContext);
                    return;
                }
                catch (Exception ex)
                {
                    // ���������Ҫ�ͷŵ���� request
                    Utility.LogExpection(ex.ToString());
                    request.Release();
                }
            }
            onResponse?.Invoke(ErrCode.Internal_ConnectionClose, null, context);
        }
    }
}
