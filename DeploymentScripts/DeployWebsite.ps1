Function Deploy-Website($packageDirectory, $msDeployUrl, $msDeployUser, $msDeployPassword, $logLevel, $esUrl, $securityKeyString, $smtpFromMail, $smtpNwHost, $resetPwTtl, $mailSuffix, $baseUrl, $kitosEnvName, $buildNumber, $kitosDbConnectionString, $hangfireConnectionString, $defaultUserPassword, $useDefaultUserPassword, $ssoServiceProviderServer, $ssoIDPEndPoints, $ssoServiceProviderId, $ssoCertificateThumbPrint, $stsOrganisationEndpointHost, $robotsFileName, $smtpNetworkPort, $smtpNetworkUsername, $smtpNetworkPassword) {

    $msdeploy = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe";
    
    if (!(Test-Path $robotsFileName) -and !(Test-Path $robotsFileName)) {
          Write-Warning "Robots not found"
    }

    $fullCommand=$(("`"{0}`" " +  
                    "-verb:sync " +
                    "-source:package=`"{1}\Presentation.Web.zip`" " +
                    "-dest:auto,computerName=`"{2}`",userName=`"{3}`",password=`"{4}`",authtype=`"Basic`",includeAcls=`"False`" " +
                    "-disableLink:AppPoolExtension " +
                    "-disableLink:ContentExtension " + 
                    "-disableLink:CertificateExtension " + 
                    "-skip:objectname=`"dirPath`",absolutepath=`"C:\\kitos_tmp\\app\\App_Data$`" " + 
                    "-skip:objectname=`"dirPath`",absolutepath=`"Default Web \Site\\App_Data$`" " + 
                    "-setParamFile:`"{1}\Presentation.Web.SetParameters.xml`" -allowUntrusted " + 
                    "-setParam:name=`"serilog:minimum-level`",value=`"{5}`" " + 
                    "-setParam:name=`"serilog:write-to:Elasticsearch.nodeUris`",value=`"{6}`" " + 
                    "-setParam:name=`"SecurityKeyString`",value=`"{7}`" " + 
                    "-setParam:name=`"SmtpFromEmail`",value=`"{8}`" " + 
                    "-setParam:name=`"SmtpNetworkHost`",value=`"{9}`" " + 
                    "-setParam:name=`"ResetPasswordTTL`",value=`"{10}`" " + 
                    "-setParam:name=`"BaseUrl`",value=`"{11}`" " + 
                    "-setParam:name=`"MailSuffix`",value=`"{12}`" " + 
                    "-setParam:name=`"Environment`",value=`"{13}`" " + 
                    "-setParam:name=`"DeploymentVersion`",value=`"{14}`" " + 
                    "-setParam:name=`"KitosContext-Web.config Connection String`",value=`"{15}`" " + 
                    "-setParam:name=`"kitos_HangfireDB-Web.config Connection String`",value=`"{16}`" " +
                    "-setParam:name=`"DefaultUserPassword`",value=`"{17}`" " +
                    "-setParam:name=`"UseDefaultPassword`",value=`"{18}`" " +
                    "-setParam:name=`"SsoServiceProviderServer`",value=`"{19}`" " +
                    "-setParam:name=`"SsoIDPEndPoints`",value=`"{20}`" " +
                    "-setParam:name=`"SsoServiceProviderId`",value=`"{21}`" " +
                    "-setParam:name=`"SsoCertificateThumbPrint`",value=`"{22}`" " +
                    "-setParam:name=`"StsOrganisationEndpointHost`",value=`"{23}`" " +
                    "-replace:objectName=filePath,match=`"{24}`",replace=Robots.txt " +
                    "-setParam:name=`"SmtpPort`",value=`"{25}`" " +
                    "-setParam:name=`"SmtpUserName`",value=`"{26}`" " +
                    "-setParam:name=`"SmtpPassword`",value=`"{27}`"") `
    -f $msdeploy, $packageDirectory, $msDeployUrl, $msDeployUser, $msDeployPassword, $logLevel, $esUrl, $securityKeyString, $smtpFromMail, $smtpNwHost, $resetPwTtl, $baseUrl, $mailSuffix, $kitosEnvName, $buildNumber, $kitosDbConnectionString, $hangfireConnectionString, $defaultUserPassword, $useDefaultUserPassword, $ssoServiceProviderServer, $ssoIDPEndPoints, $ssoServiceProviderId, $ssoCertificateThumbPrint, $stsOrganisationEndpointHost, $robotsFileName, $smtpNetworkPort, $smtpNetworkUsername, $smtpNetworkPassword)
    
    & cmd.exe /C $fullCommand
 
    if($LASTEXITCODE -ne 0)	{ throw "FAILED TO DEPLOY" } 
}