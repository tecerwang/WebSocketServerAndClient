using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketClient;

internal class MenuItem
{
    /// <summary>
    /// Item id
    /// </summary>
    public string id;

    public string name;

    public string parentItemId;

    public string[] childrenItemIds;
    /// <summary>
    /// 可以携带一个参数
    /// </summary>
    public string paramater;

    ///// <summary>
    ///// 深度
    ///// </summary>
    //public int depth;

    public JObject ToJson()
    {
        JObject jobj = new JObject();
        jobj.Add("id", id);
        jobj.Add("name", name);
        jobj.Add("paramater", paramater);
        if (!string.IsNullOrEmpty(parentItemId))
        {
            jobj.Add("parentItemId", parentItemId);
        }
        if (childrenItemIds?.Length > 0)
        {
            var jarr = new JArray();
            foreach (var itemId in childrenItemIds)
            {
                jarr.Add(itemId);
            }
            jobj.Add("childrenItemIds", jarr);
        }
        return jobj;
    }

    internal static MenuItem Parse(JToken token)
    {
        if (token == null)
        {
            return null;
        }
        var id = JHelper.GetJsonString(token, "id");
        var name = JHelper.GetJsonString(token, "name");
        var paramater = JHelper.GetJsonString(token, "paramater");
        var parentItemId = JHelper.GetJsonString(token, "parentItemId");
        var childrenItemIds = JHelper.GetJsonStringArray(token, "childrenItemIds");
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
        {
            return null;
        }
        MenuItem item = new MenuItem()
        {
            id = id,
            name = name,
            parentItemId = parentItemId,
            childrenItemIds = childrenItemIds,
            paramater = paramater
        };
        return item;
    }
}

internal class ClientMenu
{
    public MenuItem[] _menuItems;
}
