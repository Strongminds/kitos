﻿class LocalItSystemNavigation {

    public static mainPage() {
        element(by.css(`[data-ui-sref="${LocalItSystemNavigationSrefs.mainPageSref}"`)).click();
    }

    public static exposedInterfacesPage() {
        element(by.css(`[data-ui-sref="${LocalItSystemNavigationSrefs.exposedInterfacesSref}"`)).click();
    }

    public static relationsPage() {
        element(by.css(`[data-ui-sref="${LocalItSystemNavigationSrefs.relationsSref}"`)).click();
    }
    
    
}

export = LocalItSystemNavigation;

class LocalItSystemNavigationSrefs {
    static mainPageSref = "it-system.usage.main";
    static exposedInterfacesSref = "it-system.usage.interfaces";
    static relationsSref = "it-system.usage.relation";
}