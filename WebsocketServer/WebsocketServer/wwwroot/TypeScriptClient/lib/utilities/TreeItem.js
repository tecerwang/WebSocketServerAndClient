var WebsocketTSClient;
(function (WebsocketTSClient) {
    /**
     * 可以携带信息的树状数据结构，一对多结构
     */
    class TreeItem {
        constructor(data) {
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
        toJson() {
            const jobj = {
                Id: this.id,
                ParentId: this.parentId,
                ChildrenIds: this.childrenIds,
                data: this.data
            };
            return jobj;
        }
    }
    WebsocketTSClient.TreeItem = TreeItem;
    (function (TreeItem) {
        /**
        * 负责管理树状数据结构
        */
        class Collection {
            constructor() {
                /**
                 * flat list tree item，用于网络传输与检索
                 */
                this.items = [];
                this.topMostItems = [];
            }
            /**
             * 获取指定 ID 的节点
             */
            getItemById(id) {
                if (id >= 0 && id < this.items.length) {
                    return this.items[id];
                }
                return null;
            }
            getChildrenItemsById(id) {
                const parentItem = this.getItemById(id);
                if (parentItem) {
                    const childrenItems = [];
                    parentItem.childrenIds.forEach(childId => {
                        const childItem = this.getItemById(childId);
                        if (childItem) {
                            childrenItems.push(childItem);
                        }
                    });
                    return childrenItems;
                }
                return null;
            }
            /**
             * 获取所有节点
             */
            getAllItems() {
                return this.items;
            }
            /**
             * 获取所有顶级节点
             */
            getTopMostItems() {
                return this.topMostItems;
            }
            /**
             * Safe remove item, if the item has any children, it means it's a dependency node, it can not be removed until the children are clear
             */
            safeRemoveItem(id) {
                const itemToRemove = this.items.find(item => item.id === id);
                if (!itemToRemove) {
                    return false; // Item with the given ID not found
                }
                if (itemToRemove.childrenIds.length > 0) {
                    return false; // Item has children, cannot be removed
                }
                return this.removeItem(id);
            }
            /**
             * Remove an item from the collection by its ID
             */
            removeItem(id) {
                const itemToRemove = this.items.find(item => item.id === id);
                if (itemToRemove) {
                    if (itemToRemove.parentId === null) {
                        this.topMostItems = this.topMostItems.filter(item => item.id !== id);
                    }
                    const removedIndex = this.items.indexOf(itemToRemove);
                    // Remove the item from its parent's ChildrenIds list
                    if (itemToRemove.parentId !== null) {
                        const parent = this.items.find(item => item.id === itemToRemove.parentId);
                        if (parent) {
                            parent.childrenIds = parent.childrenIds.filter(childId => childId !== id);
                        }
                    }
                    // Remove the item from the flat list
                    this.items.splice(removedIndex, 1);
                    // Adjust IDs of items after the removed item
                    for (let i = removedIndex; i < this.items.length; i++) {
                        this.items[i].id--;
                    }
                    return true;
                }
                return false;
            }
            /**
             * 创建根节点
             */
            createRootItem(data) {
                return this.createItem(data, null);
            }
            /**
             * 创建新的树状数据结构节点
             */
            createItem(data, parent) {
                const item = new TreeItem(data);
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
            }
            /**
             * 从根节点开始操作，parent = null
             */
            startFromRoot() {
                return new Result(null, null, this);
            }
            /**
             * 从某个节点开始操作
             */
            startFrom(item) {
                return new Result(null, item, this);
            }
            toJson() {
                const arr = [];
                this.items.forEach(item => {
                    arr.push(item.toJson());
                });
                return arr;
            }
            static parse(json) {
                const collection = new TreeItem.Collection();
                if (Array.isArray(json)) {
                    json.forEach(str => {
                        const itemToken = JSON.parse(str);
                        const id = itemToken['Id'];
                        const parentId = itemToken['ParentId'];
                        const childrenIds = itemToken['ChildrenIds'] || [];
                        const data = itemToken['data'];
                        const treeItem = new TreeItem(data);
                        treeItem.id = id;
                        treeItem.parentId = parentId;
                        treeItem.childrenIds = childrenIds;
                        collection.items.push(treeItem);
                        if (parentId == null) {
                            collection.topMostItems.push(treeItem);
                        }
                    });
                }
                return collection;
            }
        }
        TreeItem.Collection = Collection;
        ;
        class TopmostResult {
            constructor(collection) {
                this.collection = collection;
            }
            /**
             * 设置没有 parent 的顶层节点
             */
            next(data) {
                const topMostItem = this.collection.createItem(data, null);
                return new Result(topMostItem, null, this.collection);
            }
        }
        TreeItem.TopmostResult = TopmostResult;
        ;
        class Result {
            constructor(item, parent, collection) {
                this.parent = null;
                this.item = null;
                this.item = item;
                this.parent = parent;
                this.collection = collection;
            }
            /**
             * 设置子节点
             */
            children(action) {
                action === null || action === void 0 ? void 0 : action(this.collection.startFrom(this.item));
                return this;
            }
            /**
             * 设置平级节点
             */
            next(data) {
                const nextItem = this.collection.createItem(data, this.parent);
                return new Result(nextItem, this.parent, this.collection);
            }
        }
        TreeItem.Result = Result;
        ;
    })(TreeItem = WebsocketTSClient.TreeItem || (WebsocketTSClient.TreeItem = {}));
})(WebsocketTSClient || (WebsocketTSClient = {}));
