// main-page.ts
var HTMLClient;
(function (HTMLClient) {
    class MainPage extends HTMLElement {
        constructor() {
            super();
            /** 点击进入设备控制页面 */
            this.OnMasterBtnClick = new WebsocketTSClient.EventHandler();
            /** 返回到主页面 */
            this.OnBack2Main = new WebsocketTSClient.EventHandler();
            /** 当点击某一个菜单按键  topMost:boolean , id:number */
            this.OnMenuItemClick = new WebsocketTSClient.EventHandler();
            /** 当前的父节点 Id */
            this.curParentId = null;
            this.loadStyles();
            this.render();
        }
        loadStyles() {
            // Create a link element for the CSS file
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.type = 'text/css';
            link.href = 'Components/MainPage.css';
            // Append the link element to the document head
            document.head.appendChild(link);
        }
        connectedCallback() {
            // This method is called when the element is added to the DOM
            console.log('MainPage connected to the DOM');
        }
        disconnectedCallback() {
            // This method is called when the element is removed from the DOM
            console.log('MainPage disconnected from the DOM');
        }
        render() {
            this.rootDiv = document.createElement('div');
            this.rootDiv.classList.add("mainPage");
            this.appendChild(this.rootDiv);
            const title = document.createElement('h1');
            title.classList.add('title');
            title.textContent = '中控平台';
            this.rootDiv.appendChild(title);
            this.renderMasterPanel();
            this.renderMenuPanel();
        }
        // 终端设备
        renderMasterPanel() {
            // masters panel
            this.mastersPanel = document.createElement('div');
            this.rootDiv.appendChild(this.mastersPanel);
            // title
            const title = document.createElement('h3');
            title.textContent = '终端设备';
            title.classList.add('title');
            this.mastersPanel.appendChild(title);
            // master btns
            this.mastersBtnPanel = document.createElement('div');
            this.mastersPanel.appendChild(this.mastersBtnPanel);
        }
        // 终端设备菜单控制面板
        renderMenuPanel() {
            this.menuPanel = document.createElement('div');
            const titleDiv = document.createElement('div');
            this.menuPanelTitle = document.createElement('h3');
            this.menuPanelTitle.classList.add('title');
            this.menuBtnsPanel = document.createElement('div');
            this.btnBack = document.createElement('button');
            this.btnBack.classList.add('btnReturn');
            this.btnBack.textContent = "返回主页";
            this.btnBack.addEventListener('click', () => this.BtnReturnClick());
            // panel
            this.rootDiv.appendChild(this.menuPanel);
            // pane->title div
            this.menuPanel.appendChild(titleDiv);
            // pane->title div->title
            titleDiv.appendChild(this.menuPanelTitle);
            // pane->title div->btnBack
            titleDiv.appendChild(this.btnBack);
            // pane-> menuBtnsPanel
            this.menuPanel.appendChild(this.menuBtnsPanel);
            this.menuPanel.hidden = true;
        }
        ResetMasterBtns(masters) {
            if (masters == null) {
                return;
            }
            this.RemoveChildren(this.mastersBtnPanel);
            masters.forEach((master) => {
                this.AddButton(master);
            });
        }
        RemoveChildren(root) {
            while (root.firstChild) {
                root.removeChild(root.firstChild);
            }
        }
        AddButton(master) {
            // Create button
            const button = document.createElement('button');
            button.textContent = master.masterName;
            button.id = "masterBtn_" + master.clientId;
            button.setAttribute("displayIndex", master.displayIndex.toString());
            button.classList.add('btnMaster');
            button.addEventListener('click', () => {
                this.OnMasterBtnClick.Trigger(master, button);
            });
            let insertIndex = 0;
            for (let i = 0; i < this.mastersBtnPanel.children.length; i++) {
                const child = this.mastersBtnPanel.children[i];
                const childDisplayIndex = parseInt(child.getAttribute('displayIndex') || '0');
                if (childDisplayIndex > master.displayIndex) {
                    insertIndex = i;
                    break;
                }
                else {
                    insertIndex = i + 1;
                }
            }
            // Insert the button at the correct position
            const nextSibling = this.mastersBtnPanel.children[insertIndex];
            this.mastersBtnPanel.insertBefore(button, nextSibling);
        }
        RemoveBtn(btn) {
            this.mastersBtnPanel.removeChild(btn);
        }
        RemoveBtnById(clientId) {
            var id = "masterBtn_" + clientId;
            var btn = document.getElementById(id);
            if (btn != null) {
                this.mastersBtnPanel.removeChild(btn);
            }
        }
        SetupMenus(masterName, menus) {
            this.mastersPanel.hidden = true;
            this.menuPanel.hidden = false;
            this.menuPanelTitle.textContent = masterName;
            // 解析数据
            this.collection = WebsocketTSClient.TreeItem.Collection.parse(menus);
            // 获取顶层按键
            this.DisplayMenuItems(this.collection.getTopMostItems());
            this.updateBtnBackUI();
            this.curParentId = null;
        }
        DisplayMenuItems(menus) {
            if (menus == null || menus.length == 0) {
                return false;
            }
            this.RemoveChildren(this.menuBtnsPanel);
            menus.forEach((menu) => {
                var btn = document.createElement('button');
                const hasChildren = menu.childrenIds != null && menu.childrenIds.length > 0;
                btn.classList.add(hasChildren ? 'btnMenu' : "btnSealedMenu");
                btn.textContent = menu.data.name;
                btn.addEventListener("click", () => {
                    var _a, _b;
                    const childrenMenus = this.collection.getChildrenItemsById(menu.id);
                    if (this.DisplayMenuItems(childrenMenus)) {
                        this.curParentId = menu.id;
                        (_a = this.OnMenuItemClick) === null || _a === void 0 ? void 0 : _a.Trigger(false, menu.id);
                    }
                    else {
                        (_b = this.OnMenuItemClick) === null || _b === void 0 ? void 0 : _b.Trigger(menu.parentId == null, menu.id);
                    }
                    this.updateBtnBackUI();
                });
                this.menuBtnsPanel.appendChild(btn);
            });
            return true;
        }
        Back2MainPage() {
            this.mastersPanel.hidden = false;
            this.menuPanel.hidden = true;
            this.RemoveChildren(this.menuBtnsPanel);
        }
        BtnReturnClick() {
            var _a;
            // 如果没有父级菜单，则返回到主页
            if (this.curParentId == null) {
                this.mastersPanel.hidden = false;
                this.menuPanel.hidden = true;
                this.RemoveChildren(this.menuBtnsPanel);
                this.OnBack2Main.Trigger();
            }
            else {
                var parentItem = this.collection.getItemById(this.curParentId);
                var parentParentItem = this.collection.getItemById(parentItem.parentId);
                var parentMenus;
                if (parentParentItem == null) {
                    parentMenus = this.collection.getTopMostItems();
                }
                else {
                    parentMenus = this.collection.getChildrenItemsById(parentParentItem.id);
                }
                if (this.DisplayMenuItems(parentMenus)) {
                    this.curParentId = parentParentItem != null ? parentParentItem.id : null;
                    (_a = this.OnMenuItemClick) === null || _a === void 0 ? void 0 : _a.Trigger(parentParentItem == null, this.curParentId);
                }
            }
            this.updateBtnBackUI();
        }
        updateBtnBackUI() {
            this.btnBack.textContent = this.curParentId == null ? "返回主页" : "返回上级";
        }
    }
    HTMLClient.MainPage = MainPage;
})(HTMLClient || (HTMLClient = {}));
