namespace WebsocketTSClient
{
    /**
     * 可以携带信息的树状数据结构，一对多结构
     */
    export class TreeItem<T extends object>
    {
        /**
         * 携带的信息
         */
        public data: T | null = null;

        public constructor(data: T | null)
        {
            this.data = data;
        }

        /**
         * Id 由 Collection 创建，在 Collection 保持唯一
         */
        public id: number = 0;
        public parentId: number | null = null;
        public childrenIds: number[] = [];


        /**
         * to json for network transport, 携带的信息继承了 ITreeItem，也可以一并传输
         */
        public toJson(): any
        {
            const jobj = {
                Id: this.id,
                ParentId: this.parentId,
                ChildrenIds: this.childrenIds,
                data: this.data
            };
            return jobj;
        }
    }

    export namespace TreeItem
    {
        /**
        * 负责管理树状数据结构
        */
        export class Collection<T extends object>
        {
            /**
             * flat list tree item，用于网络传输与检索
             */
            private items: TreeItem<T>[] = [];
            private topMostItems: TreeItem<T>[] = [];

            /**
             * 获取指定 ID 的节点
             */
            public getItemById(id: number): TreeItem<T> | null
            {
                if (id >= 0 && id < this.items.length)
                {
                    return this.items[id];
                }
                return null;
            }

            public getChildrenItemsById(id: number): TreeItem<T>[] | null
            {
                const parentItem = this.getItemById(id);
                if (parentItem)
                {
                    const childrenItems: TreeItem<T>[] = [];
                    parentItem.childrenIds.forEach(childId =>
                    {
                        const childItem = this.getItemById(childId);
                        if (childItem)
                        {
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
            public getAllItems(): TreeItem<T>[]
            {
                return this.items;
            }

            /**
             * 获取所有顶级节点
             */
            public getTopMostItems(): TreeItem<T>[]
            {
                return this.topMostItems;
            }

            /**
             * Safe remove item, if the item has any children, it means it's a dependency node, it can not be removed until the children are clear
             */
            public safeRemoveItem(id: number): boolean
            {
                const itemToRemove = this.items.find(item => item.id === id);
                if (!itemToRemove)
                {
                    return false; // Item with the given ID not found
                }

                if (itemToRemove.childrenIds.length > 0)
                {
                    return false; // Item has children, cannot be removed
                }
                return this.removeItem(id);
            }

            /**
             * Remove an item from the collection by its ID
             */
            public removeItem(id: number): boolean
            {
                const itemToRemove = this.items.find(item => item.id === id);
                if (itemToRemove)
                {
                    if (itemToRemove.parentId === null)
                    {
                        this.topMostItems = this.topMostItems.filter(item => item.id !== id);
                    }

                    const removedIndex = this.items.indexOf(itemToRemove);

                    // Remove the item from its parent's ChildrenIds list
                    if (itemToRemove.parentId !== null)
                    {
                        const parent = this.items.find(item => item.id === itemToRemove.parentId);
                        if (parent)
                        {
                            parent.childrenIds = parent.childrenIds.filter(childId => childId !== id);
                        }
                    }

                    // Remove the item from the flat list
                    this.items.splice(removedIndex, 1);

                    // Adjust IDs of items after the removed item
                    for (let i = removedIndex; i < this.items.length; i++)
                    {
                        this.items[i].id--;
                    }

                    return true;
                }
                return false;
            }

            /**
             * 创建根节点
             */
            public createRootItem(data: T): TreeItem<T> | null
            {
                return this.createItem(data, null);
            }

            /**
             * 创建新的树状数据结构节点
             */
            public createItem(data: T, parent: TreeItem<T> | null): TreeItem<T>
            {
                const item = new TreeItem<T>(data);
                item.parentId = parent ? parent.id : null;
                item.id = this.items.length;

                if (parent)
                {
                    parent.childrenIds.push(item.id);
                }
                else
                {
                    // 没有 parent 就是顶级节点
                    this.topMostItems.push(item);
                }

                this.items.push(item);
                return item;
            }

            /**
             * 从根节点开始操作，parent = null
             */
            public startFromRoot(): Result<T>
            {
                return new Result(null, null, this);
            }

            /**
             * 从某个节点开始操作
             */
            public startFrom(item: TreeItem<T>): Result<T>
            {
                return new Result(null, item, this);
            }

            public toJson(): any
            {
                const arr: any[] = [];
                this.items.forEach(item =>
                {
                    arr.push(item.toJson());
                });
                return arr;
            }

            public static parse<T extends object>(json: any): TreeItem.Collection<T>
            {
                const collection = new TreeItem.Collection<T>();

                if (Array.isArray(json))
                {
                    json.forEach(str =>
                    {
                        const itemToken = JSON.parse(str);
                        const id = itemToken['Id'];
                        const parentId = itemToken['ParentId'];
                        const childrenIds = itemToken['ChildrenIds'] || [];
                        const data = itemToken['data'] as T;
                        

                        const treeItem = new TreeItem<T>(data);
                        treeItem.id = id;
                        treeItem.parentId = parentId;
                        treeItem.childrenIds = childrenIds;

                        collection.items.push(treeItem);
                        if (parentId == null)
                        {
                            collection.topMostItems.push(treeItem);
                        }
                    });
                }
                return collection;
            }
        };

        export class TopmostResult<T extends object>
        {
            private collection: TreeItem.Collection<T>;

            constructor(collection: TreeItem.Collection<T>)
            {
                this.collection = collection;
            }

            /**
             * 设置没有 parent 的顶层节点
             */
            public next(data: T): Result<T>
            {
                const topMostItem = this.collection.createItem(data, null);
                return new Result(topMostItem, null, this.collection);
            }
        };

        export class Result<T extends object>
        {
            public parent: TreeItem<T> | null = null;
            public item: TreeItem<T> | null = null;

            private collection: TreeItem.Collection<T>;

            constructor(item: TreeItem<T> | null, parent: TreeItem<T> | null, collection: TreeItem.Collection<T>)
            {
                this.item = item;
                this.parent = parent;
                this.collection = collection;
            }

            /**
             * 设置子节点
             */
            public children(action: (result: Result<T>) => void): Result<T>
            {
                action?.(this.collection.startFrom(this.item));
                return this;
            }

            /**
             * 设置平级节点
             */
            public next(data: T): Result<T>
            {
                const nextItem = this.collection.createItem(data, this.parent);
                return new Result(nextItem, this.parent, this.collection);
            }
        };
    }
}