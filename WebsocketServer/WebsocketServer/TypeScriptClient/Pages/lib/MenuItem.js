var HTMLClient;
(function (HTMLClient) {
    class MenuItem {
        constructor(name, paramater = null) {
            this.name = name;
            this.paramater = paramater;
        }
    }
    HTMLClient.MenuItem = MenuItem;
})(HTMLClient || (HTMLClient = {}));
