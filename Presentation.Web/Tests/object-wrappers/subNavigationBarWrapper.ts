
// Typings not yet working with protractor 
//import protractor = require("protractor");

//type iconLinks = { bell: protractor.ElementFinder, chart: protractor.ElementFinder, help: protractor.ElementFinder }

type iconLinks = { bell, chart, help }

class subNavigationBarWrapper {

    public icons(): iconLinks {
        var iconLinks: iconLinks = {
            bell: this.getAnchor(0),
            chart: this.getAnchor(1),
            help: this.getAnchor(2)
        }
        return iconLinks;
    }

    private getAnchor(index: number) {
        return element(by.id("navbar-top")).all(by.css("a")).get(index);
    }
}

export = subNavigationBarWrapper;