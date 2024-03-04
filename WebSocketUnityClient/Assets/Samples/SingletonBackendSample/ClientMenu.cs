using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketClient;
using WebSocketClient.Utilities.Data;

public class MenuItem : INetworkTransport
{
    public MenuItem(string name, string paramater = null)
    {
        this.name = name;
        this.paramater = paramater;
    }

    public string name { get; private set; }
    /// <summary>
    /// 可以携带一个参数
    /// </summary>
    public string paramater { get; private set; }

    public JToken ToJson()
    {
        JObject jobj = new JObject();
        jobj.Add("name", name);
        jobj.Add("paramater", paramater);
        return jobj;
    }

    public static MenuItem Parse(JToken token)
    {
        if (token == null)
        {
            return null;
        }
        var name = JHelper.GetJsonString(token, "name");
        var paramater = JHelper.GetJsonString(token, "paramater");
        MenuItem item = new MenuItem(name, paramater);
        return item;
    }

    public override string ToString()
    {
        return $"MenuItem: name {name}, paramter {paramater}";
    }
}

