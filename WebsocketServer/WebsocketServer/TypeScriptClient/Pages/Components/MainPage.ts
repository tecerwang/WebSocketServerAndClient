// main-page.ts
namespace HTMLClient
{
    type MasterClient = WebsocketTSClient.MasterClient;
    type EventHandler<T extends any[]> = WebsocketTSClient.EventHandler<T>;

    export class MainPage extends HTMLElement
    {
        private rootDiv: HTMLElement;
        private mastersBtnPanel: HTMLElement;
        private mastersPanel: HTMLElement;
        private menuPanel: HTMLElement;
        private menuPanelTitle: HTMLElement;
        private menuBtnsPanel: HTMLElement;
        private btnBack: HTMLElement;

        /** 点击进入设备控制页面 */
        public OnMasterBtnClick: EventHandler<[MasterClient, HTMLButtonElement]> = new WebsocketTSClient.EventHandler<[MasterClient, HTMLButtonElement]>();

        /** 返回到主页面 */
        public OnBack2Main: EventHandler<[]> = new WebsocketTSClient.EventHandler<[]>();

        constructor()
        {
            super();
            this.loadStyles();
            this.render();
        }

        private loadStyles(): void
        {
            // Create a link element for the CSS file
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.type = 'text/css';
            link.href = 'Components/MainPage.css'; 

            // Append the link element to the document head
            document.head.appendChild(link);
        }

        connectedCallback()
        {
            // This method is called when the element is added to the DOM
            console.log('MainPage connected to the DOM');
        }

        disconnectedCallback()
        {
            // This method is called when the element is removed from the DOM
            console.log('MainPage disconnected from the DOM');
        }

        private render(): void
        {
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
        private renderMasterPanel()
        {
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
        private renderMenuPanel()
        {
            this.menuPanel = document.createElement('div');
            const titleDiv = document.createElement('div');
            this.menuPanelTitle = document.createElement('h3');
            this.menuPanelTitle.classList.add('title');
            this.menuBtnsPanel = document.createElement('div');
            this.btnBack = document.createElement('button');
            this.btnBack.textContent = "返回主页";
            this.btnBack.addEventListener('click', () =>
            {
                this.OnBack2Main.Trigger();
            });
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


        public ResetMasterBtns(masters: MasterClient[] | null): void
        {
            if (masters == null)
            {
                return;
            }
            this.RemoveChildren(this.mastersBtnPanel);
            masters.forEach((master) =>
            {
                this.AddButton(master);
            });
        }

        private RemoveChildren(root: HTMLElement) : void
        {
            while (root.firstChild)
            {
                root.removeChild(root.firstChild);
            }
        }

        public AddButton(master: MasterClient): void
        {
            // Create button
            const button = document.createElement('button');
            button.textContent = master.masterName;
            button.id = "masterBtn_" + master.clientId;
            button.classList.add('btnMaster');
            button.addEventListener('click', () =>
            {
                this.OnMasterBtnClick.Trigger(master, button);
            });
            this.mastersBtnPanel.appendChild(button);
        }

        public RemoveBtn(btn: HTMLButtonElement): void
        {
            this.mastersBtnPanel.removeChild(btn);
        }

        public RemoveBtnById(clientId: string): void
        {
            var id = "masterBtn_" + clientId;
            var btn = document.getElementById(id);
            if (btn != null)
            {
                this.mastersBtnPanel.removeChild(btn);
            }
        }

        public DisplayMenuItems(masterName : string, menus : MenuItem[]) : void
        {
            this.mastersPanel.hidden = true;
            this.menuPanel.hidden = false;
            this.menuPanelTitle.textContent = masterName;
            this.RemoveChildren(this.menuBtnsPanel);
            // todo show menu btns
        }

        public Back2MainPage(): void
        {
            this.mastersPanel.hidden = false;
            this.menuPanel.hidden = true;
            this.RemoveChildren(this.menuBtnsPanel);
        }
    }
}
