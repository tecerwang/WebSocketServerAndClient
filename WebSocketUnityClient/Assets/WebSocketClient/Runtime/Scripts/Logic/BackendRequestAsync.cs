using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketClient;

/// <summary>
/// BackendRequest ���첽�汾
/// </summary>
public class BackendRequestAsync : IDisposable
{
    private struct RequestContext
    {
        public string serviceName;
        public string cmd;
        public JToken data;
        /// <summary>
        /// Ĭ��Ϊ -1������δ������Ч����
        /// </summary>
        public int rid;
    }

    public struct Response
    {
        public int errCode;
        public JToken data;
    }

    /// <summary>
    /// ��¼������Ϣ������������ʹ��
    /// </summary>
    private RequestContext _requestContext;

    private TaskCompletionSource<Response> _requestCompleteSource = null;

    public BackendRequestAsync(string serviceName, string cmd, JToken data)
    {
        WSBackend.singleton.OnBackendStateChanged += Singleton_OnBackendStateChanged;
        WSBackend.singleton.OnBackendResponse += Singleton_OnBackendResponse;

        _requestContext = new RequestContext()
        {
            serviceName = serviceName,
            cmd = cmd,
            data = data,
            rid = -1
        };
    }

    public void Dispose()
    {
        WSBackend.singleton.OnBackendStateChanged -= Singleton_OnBackendStateChanged;
        WSBackend.singleton.OnBackendResponse -= Singleton_OnBackendResponse;     
    }

    private void Singleton_OnBackendStateChanged()
    {
        /// ���ӶϿ����ȴ���Ϣ����������Ҫ֪��
        if (WSBackend.singleton.State == WSBackend.WSBackendState.Close && _requestCompleteSource != null)
        {
            _requestCompleteSource.SetResult(new Response()
            {
                errCode = ErrCode.Internal_ConnectionClose,
                data = null
            });
            _requestCompleteSource = null;
        }
    }

    private void Singleton_OnBackendResponse(ResponsePack response)
    {
        if (response.rid == _requestContext.rid && _requestCompleteSource != null)
        {
            _requestCompleteSource.SetResult(new Response() 
            {
                 data = response.data,
                 errCode = response.errCode
            });
        }
    }

    public async Task<Response> Request()
    {
        if (WSBackend.singleton != null && WSBackend.singleton.State == WSBackend.WSBackendState.Open)
        {
            var errCode = ErrCode.Internal_Error;
            var result = await WSBackend.singleton.CreateBackendRequest(_requestContext.serviceName, _requestContext.cmd, _requestContext.data);
            if (result.state == WebSocketClient.WebSocketClient.SendMsgState.Sent)
            {
                _requestContext.rid = result.rid;
                _requestCompleteSource = new TaskCompletionSource<Response>();
                /// �����Ϣ���� �ȴ���ִ���������ӿ��ܶϿ�
                return await _requestCompleteSource.Task;
            }
            else
            {
                switch (result.state)
                {
                    case WebSocketClient.WebSocketClient.SendMsgState.Timeout:
                        {
                            errCode = ErrCode.Internal_RetryTimeout;
                            break;
                        }
                    case WebSocketClient.WebSocketClient.SendMsgState.InteruptByConnectionClose:
                        {
                            errCode = ErrCode.Internal_ConnectionClose;
                            break;
                        }
                }
                return new Response()
                {
                    errCode = errCode,
                    data = null
                };
            }
        }
        else
        {
            // ����ǰ�ˣ������Ѿ��Ͽ�
            return new Response()
            {
                errCode = ErrCode.Internal_ConnectionClose,
                data = null
            };
        }
    }
}
