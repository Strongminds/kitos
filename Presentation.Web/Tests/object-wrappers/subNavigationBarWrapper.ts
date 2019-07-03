class subNavigationBarWrapper {

    private subNavigationElement = element(by.id("navbar-top"));

    public bellIcon = this.subNavigationElement.all(by.css("a")).get(0);

    public chartIcon = this.subNavigationElement.all(by.css("a")).get(1);

    public helpIcon = this.subNavigationElement.all(by.css("a")).get(2);

}

export = subNavigationBarWrapper;