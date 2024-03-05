var WebsocketTSClient;
(function (WebsocketTSClient) {
    /**
     * 可以携带信息的树状数据结构，一对多结构
     */
    var TreeItem = /** @class */ (function () {
        function TreeItem(data) {
            /**
             * 携带的信息
             */
            this.data = null;
            /**
             * Id 由 Collection 创建，在 Collection 保持唯一
             */
            this.id = 0;
            this.parentId = null;
            this.childrenIds = [];
            this.data = data;
        }
        /**
         * to json for network transport, 携带的信息继承了 ITreeItem，也可以一并传输
         */
        TreeItem.prototype.toJson = function () {
            var jobj = {
                Id: this.id,
                ParentId: this.parentId,
                ChildrenIds: this.childrenIds,
                data: this.data
            };
            return jobj;
        };
        return TreeItem;
    }());
    WebsocketTSClient.TreeItem = TreeItem;
    (function (TreeItem) {
        /**
        * 负责管理树状数据结构
        */
        var Collection = /** @class */ (function () {
            function Collection() {
                /**
                 * flat list tree item，用于网络传输与检索
                 */
                this.items = [];
                this.topMostItems = [];
            }
            /**
             * 获取指定 ID 的节点
             */
            Collection.prototype.getItemById = function (id) {
                if (id >= 0 && id < this.items.length) {
                    return this.items[id];
                }
                return null;
            };
            /**
             * 获取所有节点
             */
            Collection.prototype.getAllItems = function () {
                return this.items;
            };
            /**
             * 获取所有顶级节点
             */
            Collection.prototype.getTopMostItems = function () {
                return this.topMostItems;
            };
            /**
             * Safe remove item, if the item has any children, it means it's a dependency node, it can not be removed until the children are clear
             */
            Collection.prototype.safeRemoveItem = function (id) {
                var itemToRemove = this.items.find(function (item) { return item.id === id; });
                if (!itemToRemove) {
                    return false; // Item with the given ID not found
                }
                if (itemToRemove.childrenIds.length > 0) {
                    return false; // Item has children, cannot be removed
                }
                return this.removeItem(id);
            };
            /**
             * Remove an item from the collection by its ID
             */
            Collection.prototype.removeItem = function (id) {
                var itemToRemove = this.items.find(function (item) { return item.id === id; });
                if (itemToRemove) {
                    if (itemToRemove.parentId === null) {
                        this.topMostItems = this.topMostItems.filter(function (item) { return item.id !== id; });
                    }
                    var removedIndex = this.items.indexOf(itemToRemove);
                    // Remove the item from its parent's ChildrenIds list
                    if (itemToRemove.parentId !== null) {
                        var parent_1 = this.items.find(function (item) { return item.id === itemToRemove.parentId; });
                        if (parent_1) {
                            parent_1.childrenIds = parent_1.childrenIds.filter(function (childId) { return childId !== id; });
                        }
                    }
                    // Remove the item from the flat list
                    this.items.splice(removedIndex, 1);
                    // Adjust IDs of items after the removed item
                    for (var i = removedIndex; i < this.items.length; i++) {
                        this.items[i].id--;
                    }
                    return true;
                }
                return false;
            };
            /**
             * 创建根节点
             */
            Collection.prototype.createRootItem = function (data) {
                return this.createItem(data, null);
            };
            /**
             * 创建新的树状数据结构节点
             */
            Collection.prototype.createItem = function (data, parent) {
                var item = new TreeItem(data);
                item.parentId = parent ? parent.id : null;
                item.id = this.items.length;
                if (parent) {
                    parent.childrenIds.push(item.id);
                }
                else {
                    // 没有 parent 就是顶级节点
                    this.topMostItems.push(item);
                }
                this.items.push(item);
                return item;
            };
            /**
             * 从根节点开始操作，parent = null
             */
            Collection.prototype.startFromRoot = function () {
                return new Result(null, null, this);
            };
            /**
             * 从某个节点开始操作
             */
            Collection.prototype.startFrom = function (item) {
                return new Result(null, item, this);
            };
            Collection.prototype.toJson = function () {
                var arr = [];
                this.items.forEach(function (item) {
                    arr.push(item.toJson());
                });
                return arr;
            };
            Collection.parse = function (json) {
                var collection = new TreeItem.Collection();
                if (Array.isArray(json)) {
                    json.forEach(function (itemToken) {
                        var id = itemToken['Id'];
                        var parentId = itemToken['ParentId'];
                        var childrenIds = itemToken['ChildrenIds'] || [];
                        var data = itemToken['data'];
                        var treeItem = new TreeItem(data);
                        treeItem.id = id;
                        treeItem.parentId = parentId;
                        treeItem.childrenIds = childrenIds;
                        collection.items.push(treeItem);
                    });
                }
                return collection;
            };
            return Collection;
        }());
        TreeItem.Collection = Collection;
        ;
        var TopmostResult = /** @class */ (function () {
            function TopmostResult(collection) {
                this.collection = collection;
            }
            /**
             * 设置没有 parent 的顶层节点
             */
            TopmostResult.prototype.next = function (data) {
                var topMostItem = this.collection.createItem(data, null);
                return new Result(topMostItem, null, this.collection);
            };
            return TopmostResult;
        }());
        TreeItem.TopmostResult = TopmostResult;
        ;
        var Result = /** @class */ (function () {
            function Result(item, parent, collection) {
                this.parent = null;
                this.item = null;
                this.item = item;
                this.parent = parent;
                this.collection = collection;
            }
            /**
             * 设置子节点
             */
            Result.prototype.children = function (action) {
                action === null || action === void 0 ? void 0 : action(this.collection.startFrom(this.item));
                return this;
            };
            /**
             * 设置平级节点
             */
            Result.prototype.next = function (data) {
                var nextItem = this.collection.createItem(data, this.parent);
                return new Result(nextItem, this.parent, this.collection);
            };
            return Result;
        }());
        TreeItem.Result = Result;
        ;
    })(TreeItem = WebsocketTSClient.TreeItem || (WebsocketTSClient.TreeItem = {}));
})(WebsocketTSClient || (WebsocketTSClient = {}));
