Function Deploy-Website($packageDirectory, $msDeployUrl, $msDeployUser, $msDeployPassword) {

    $msdeploy = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
    $destArgs = "computerName=`"{0}`",userName=`"{1}`",password=`"{2}`",authtype=`"Basic`",includeAcls=`"False`"" `
        -f $msDeployUrl, $msDeployUser, $msDeployPassword

    # AppOfflineRule is unavailable in some Web Deploy installations.
    # Instead, manually push app_offline.htm before syncing and delete it after.
    $appOfflineTmp = [System.IO.Path]::GetTempFileName()
    Copy-Item "$PSScriptRoot\..\App_Offline.html" $appOfflineTmp -Force

    $putOffline = ("`"{0}`" -verb:sync -source:contentPath=`"{1}`" " +
                   "-dest:contentPath=`"Default Web Site/app_offline.htm`",{2} -allowUntrusted") `
        -f $msdeploy, $appOfflineTmp, $destArgs
    & cmd.exe /C $putOffline
    if($LASTEXITCODE -ne 0) {
        Remove-Item $appOfflineTmp -Force -ErrorAction SilentlyContinue
        throw "FAILED TO PUT SITE OFFLINE"
    }

    # All environment-specific configuration is now baked into appsettings.json by Prepare-Package.
    # This function only handles the file sync to the remote IIS server.
    # The backend always deploys to the Default Web Site root (C:\inetpub\wwwroot) across all environments.
    $fullCommand = ("`"{0}`" " +
                    "-verb:sync " +
                    "-source:package=`"{1}\Presentation.Web.zip`" " +
                    "-dest:contentPath=`"Default Web Site`",{2} " +
                    "-disableLink:AppPoolExtension " +
                    "-disableLink:ContentExtension " +
                    "-disableLink:CertificateExtension " +
                    "-skip:objectname=`"dirPath`",absolutepath=`"App_Data$`" " +
                    "-skip:objectname=`"dirPath`",absolutepath=`".*\\runtimes($|\\.*)`" " +
                    "-skip:objectname=`"filePath`",absolutepath=`".*\\runtimes\\.*`" " +
                    "-allowUntrusted") `
    -f $msdeploy, $packageDirectory, $destArgs

    try {
        & cmd.exe /C $fullCommand
        $deployExitCode = $LASTEXITCODE
    }
    finally {
        # Always bring the site back online by removing app_offline.htm
        $deleteOffline = ("`"{0}`" -verb:delete " +
                          "-dest:contentPath=`"Default Web Site/app_offline.htm`",{1} -allowUntrusted") `
            -f $msdeploy, $destArgs
        & cmd.exe /C $deleteOffline
        $deleteExitCode = $LASTEXITCODE

        Remove-Item $appOfflineTmp -Force -ErrorAction SilentlyContinue

        if($deleteExitCode -ne 0) {
            throw "FAILED TO BRING SITE BACK ONLINE - app_offline.htm may still be present on the server"
        }
    }

    if($deployExitCode -ne 0) { throw "FAILED TO DEPLOY" }
}