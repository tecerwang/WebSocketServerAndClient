using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UIFramework;
using UnityEngine;
using UnityEngine.Assertions;

namespace WebSocketClient.Utilities.Data
{
    public interface INetworkTransport
    {
        JToken ToJson();
    }

    /// <summary>
    /// 可以携带信息的树状数据结构,一对多结构
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeItem<T>
    {
        public T data;
        /// <summary>
        /// 不允许外部创建，只能继承类和 Collection 创建
        /// </summary>
        protected TreeItem(T data)
        {
            this.data = data;
        }

        /// <summary>
        /// Id 由 Collection 创建，在 Collection 保持唯一
        /// </summary>
        public int Id { get; private set; }
        public int? ParentId { get; private set; }
        public List<int> ChildrenIds { get; private set; } = new List<int>();

        public int Depth { get; private set; } = 0;

        /// <summary>
        /// to json for network transport,携带的信息继承了 ITreeItem， 也可以一并传输
        /// </summary>
        /// <returns></returns>
        public JToken ToJson()
        {
            JObject jobj = new JObject();
            jobj.Add("Id", Id);
            if (ParentId.HasValue)
            {
                jobj.Add("ParentId", ParentId);
            }
            jobj.Add("ChildrenIds", JHelper.MakeIntArray(ChildrenIds));           
            // 如果对象继承了ToJson接口，将携带的信息也传输出去
            if (typeof(T).GetInterfaces().Contains(typeof(INetworkTransport)))
            {
                jobj.Add("data", (data as INetworkTransport).ToJson());
            }
            return jobj;
        }

        /// <summary>
        /// 负责管理树状数据结构
        /// </summary>
        public class Collection
        {
            /// <summary>
            /// flat list tree item，用于网络传输与检索
            /// </summary>
            private readonly List<TreeItem<T>> items = new List<TreeItem<T>>();
           
            /// <summary>
            /// 获取指定 ID 的节点
            /// </summary>
            /// <param name="id">节点 ID</param>
            /// <returns>节点</returns>
            public TreeItem<T> GetItemById(int id)
            {
                if (id >= 0 && id < items.Count)
                {
                    return items[id];
                }
                return null;
            }

            /// <summary>
            /// 获取所有节点
            /// </summary>
            /// <returns>所有节点</returns>
            public IEnumerable<TreeItem<T>> GetAllItems()
            {
                return items;
            }

            /// <summary>
            /// 获取所有顶级节点
            /// </summary>
            /// <returns></returns>
            public IEnumerable<TreeItem<T>> GetTopMostItems()
            {
                return items.Where(p => p.Depth == 0);
            }        

            /// <summary>
            /// Safe remove item, if the item has any children, it means it's a dependency node, it can not be removed until the children is clear
            /// </summary>
            /// <param name="id">The ID of the item to remove</param>
            /// <returns>True if the item was removed successfully, otherwise false</returns>
            public bool SafeRemoveItem(int id)
            {
                TreeItem<T> itemToRemove = items.FirstOrDefault(item => item.Id == id);
                if (itemToRemove == null)
                {
                    return false; // Item with the given ID not found
                }

                if (itemToRemove.ChildrenIds.Count > 0)
                {
                    return false; // Item has children, cannot be removed
                }
                return RemoveItem(id);
            }

            /// <summary>
            /// Remove an item from the collection by its ID
            /// </summary>
            /// <param name="id">The ID of the item to remove</param>
            /// <returns>True if the item was removed successfully, otherwise false</returns>
            public bool RemoveItem(int id)
            {       

                TreeItem<T> itemToRemove = items.FirstOrDefault(item => item.Id == id);
                if (itemToRemove != null)
                {                    
                    int removedIndex = items.IndexOf(itemToRemove);

                    // Remove the item from its parent's ChildrenIds list
                    if (itemToRemove.ParentId.HasValue)
                    {
                        TreeItem<T> parent = items.FirstOrDefault(item => item.Id == itemToRemove.ParentId);
                        if (parent != null)
                        {
                            parent.ChildrenIds.Remove(id);
                        }
                    }

                    // Remove the item from the flat list
                    items.Remove(itemToRemove);

                    // Adjust IDs of items after the removed item
                    for (int i = removedIndex; i < items.Count; i++)
                    {
                        items[i].Id--;
                    }

                    return true;
                }
                return false;
            }

            /// <summary>
            /// 创建根节点
            /// </summary>
            /// <returns>新节点</returns>
            public TreeItem<T> CreateRootItem(T data)
            {
                return CreateItem(data, null);
            }

            /// <summary>
            /// 创建新的树状数据结构节点
            /// </summary>
            /// <returns>新节点</returns>
            public TreeItem<T> CreateItem(T data, TreeItem<T> parent)
            {
                var item = new TreeItem<T>(data)
                {
                    ParentId = parent?.Id,
                    Id = items.Count
                };

                if (parent != null)
                {
                    item.Depth = parent.Depth + 1;
                    parent.ChildrenIds.Add(item.Id);
                }
                else 
                {
                    /// 没有 parent 就是顶级节点
                    item.Depth = 0;
                }

                items.Add(item);
                return item;
            }

            /// <summary>
            /// 从根节点开始操作，parent = null
            /// </summary>
            /// <returns></returns>
            public Result StartFromRoot()
            {
                return new Result(null, null, this);
            }

            /// <summary>
            /// 从某个节点开始操作
            /// </summary>
            /// <returns></returns>
            public Result StartFrom(TreeItem<T> item)
            {
                return new Result(null, item, this);
            }

            public JToken ToJson()
            {
                JArray arr = new JArray();
                foreach (var item in items)
                {
                    arr.Add(item.ToJson().ToString(Newtonsoft.Json.Formatting.None));
                }
                return arr;
            }

            public static Collection Parse(JToken json)
            {
                Collection collection = new Collection();

                if (json is JArray array)
                {
                    foreach (var jstr in array)
                    {
                        var itemToken = JObject.Parse(jstr.ToString());
                        int id = JHelper.GetJsonInt(itemToken, "Id");
                        int? parentId = null;
                        if (itemToken.ContainsKey("ParentId"))
                        {
                            parentId = JHelper.GetJsonInt(itemToken, "ParentId");
                        }
                        List<int> childrenIds = JHelper.GetJsonIntArray(itemToken, "ChildrenIds").ToList();
                        // Since the data might be an object implementing INetworkTransport, we'll need to deserialize it
                        
                        var jData = JHelper.GetJsonToken(itemToken, "data");
                        T data = jData.ToObject<T>();

                        // Create the tree item
                        TreeItem<T> treeItem = new TreeItem<T>(data)
                        {
                            Id = id,
                            ParentId = parentId,
                            ChildrenIds = childrenIds
                        };

                        // Add the tree item to the collection
                        collection.items.Add(treeItem);
                    }
                }
                return collection;
            }
        }

        public class TopmostResult
        {
            private Collection collection;

            public TopmostResult(Collection collection)
            {
                this.collection = collection;
            }

            /// <summary>
            /// 设置没有 parent 的顶层节点
            /// </summary>
            public Result Next(T data)
            {
                var topMostItem = collection.CreateItem(data, null);
                return new Result(topMostItem, null, collection);
            }
        }

        public class Result
        {
            public TreeItem<T> parent { get; private set; }
            public TreeItem<T> item { get; private set; }

            private Collection collection;

            public Result(TreeItem<T> item, TreeItem<T> parent, Collection collection)
            {
                this.item = item;
                this.parent = parent;
                this.collection = collection;
            }                      

            /// <summary>
            /// 设置子节点
            /// </summary>
            public Result Children(Action<Result> action)
            {
                action?.Invoke(collection.StartFrom(item));                
                return this;
            }

            /// <summary>
            /// 设置平级节点
            /// </summary>
            public Result Next(T data)
            {
                var nextItem = collection.CreateItem(data, parent);
                return new Result(nextItem, parent, collection);
            }
        }
    }
}
