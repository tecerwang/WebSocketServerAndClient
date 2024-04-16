using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Net;
using System.Net.WebSockets;
using WebSocketServer.DataService;
using WebSocketServer.ServerKernal;
using WebSocketServer.ServiceLogic;
using WebSocketServer.Utilities;

public class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // ÿ�����󴴽�һ�� middleware
        builder.Services.AddTransient<WebSocketMiddleWare>();

        // connection ��������
        builder.Services.AddSingleton<WebSocketClientData>();
        // master slaves group ����
        builder.Services.AddSingleton<MasterSlavesGroupData>();
        // ����㲥����
        builder.Services.AddSingleton<IWebSocketLogic, ClientGroupBroadcastService>();
        // master slaves group ��Ϣ�㲥����
        builder.Services.AddSingleton<IWebSocketLogic, MasterSlavesGroupService>();
        // connection ��ط���
        builder.Services.AddSingleton<IWebSocketLogic, ConnMonitorService>();

        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            serverOptions.Listen(IPAddress.Loopback, 5000);
            //serverOptions.Listen(IPAddress.Loopback, 8081, listenOptions =>
            //{
            //    listenOptions.UseHttps("testCert.pfx", "testPassword");
            //});
        });
        var app = builder.Build();
        

        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)            
        };

        // websocket must be at top most of request pipeline
        app.UseWebSockets(webSocketOptions);
        app.UseWebSocketMiddleware();

        var provider = new FileExtensionContentTypeProvider();
        //provider.Mappings[".data"] = "application/octet-stream";
        //provider.Mappings[".data.gz"] = "application/octet-stream";
        //provider.Mappings[".wasm"] = "application/wasm";
        //provider.Mappings[".wasm.gz"] = "application/wasm";
        //provider.Mappings[".js.gz"] = "application/octet-stream";
        //provider.Mappings[".symbols.json.gz"] = "application/octet-stream";

        app.UseStaticFiles(new StaticFileOptions
        {            
            ContentTypeProvider = provider
        });

        app.Run();

    }
}