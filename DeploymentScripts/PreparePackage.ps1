Function Prepare-Package([String] $environmentName, $pathToArchive) {

	Write-Host "Environment is $environmentName"

	[string]$archivePath = $pathToArchive.Path
	$item = Get-Item -LiteralPath $archivePath -Force -ErrorAction Stop

	$destination = Join-Path (Get-Location) "TEMP_PresentationWeb"

	Write-Host "Resolved path: $($item.FullName)"
	Write-Host "PSIsContainer: $($item.PSIsContainer)"
	Write-Host "Destination: $destination"

	# Ensure clean destination
	if (Test-Path -LiteralPath $destination) {
		Remove-Item -LiteralPath $destination -Recurse -Force
	}

	New-Item -ItemType Directory -Path $destination | Out-Null

	if ($item.PSIsContainer) {
		Write-Host "Source is a directory → copying contents"

		Copy-Item -Path (Join-Path $item.FullName '*') `
				  -Destination $destination `
				  -Recurse -Force
	}
	else {
		Write-Host "Source is a zip → extracting"

		Expand-Archive -LiteralPath $item.FullName `
					   -DestinationPath $destination `
					   -Force
	}

	Write-Host "Files prepared in: $destination"

	if (Test-Path -LiteralPath $pathToArchive) {
		Remove-Item -LiteralPath $pathToArchive -Recurse -Force -Confirm:$false
	}

	Write-Host "Updating appsettings.json"
	$appSettingsPath = ".\TEMP_PresentationWeb\appsettings.json"
	$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

	# Connection strings
	$appSettings.ConnectionStrings.KitosContext     = $Env:KitosDbConnectionStringForIIsApp
	$appSettings.ConnectionStrings.kitos_HangfireDB = $Env:HangfireDbConnectionStringForIIsApp

	# SMTP
	$appSettings.Smtp.From           = $Env:SmtpFromMail
	$appSettings.Smtp.DeliveryMethod = $Env:SmtpDeliveryMethod
	$appSettings.Smtp.Host           = $Env:SmtpNetworkHost
	$appSettings.Smtp.Port           = $Env:SmtpNetworkPort
	$appSettings.Smtp.UserName       = $Env:SmtpUserName
	$appSettings.Smtp.Password       = $Env:SmtpPassword

	# Serilog
	$appSettings.Serilog.MinimumLevel = $Env:LogLevel

	# AppSettings
	$appSettings.AppSettings.SecurityKeyString                    = $Env:SecurityKeyString
	$appSettings.AppSettings.BaseUrl                              = "https://$Env:KitosHostName/"
	$appSettings.AppSettings.MailSuffix                           = $Env:MailSuffix
	$appSettings.AppSettings.Environment                          = $Env:KitosEnvName
	$appSettings.AppSettings.DeploymentVersion                    = $Env:BUILD_NUMBER
	$appSettings.AppSettings.ResetPasswordTTL                     = $Env:ResetPasswordTtl
	$appSettings.AppSettings.DefaultUserPassword                  = $Env:DefaultUserPassword
	$appSettings.AppSettings.UseDefaultUserPassword               = $Env:UseDefaultUserPassword
	$appSettings.AppSettings.SsoServiceProviderId                 = $Env:SsoServiceProviderId
	$appSettings.AppSettings.SsoCertificateThumbprint             = $Env:SsoCertificateThumbPrint
	$appSettings.AppSettings.StsIssuer                            = $Env:StsIssuer
	$appSettings.AppSettings.StsCertificateEndpoint               = $Env:StsCertificateEndpoint
	$appSettings.AppSettings.ServiceCertificateAliasOrg           = $Env:ServiceCertificateAliasOrg
	$appSettings.AppSettings.StsCertificateAlias                  = $Env:StsCertificateAlias
	$appSettings.AppSettings.StsCertificateThumbprint             = $Env:StsCertificateThumbprint
	$appSettings.AppSettings.OrgService6EntityId                  = $Env:OrgService6EntityId
	$appSettings.AppSettings.StsAdressePort                       = $Env:StsAdressePort
	$appSettings.AppSettings.StsBrugerPort                        = $Env:StsBrugerPort
	$appSettings.AppSettings.StsPersonPort                        = $Env:StsPersonPort
	$appSettings.AppSettings.StsVirksomhedPort                    = $Env:StsVirksomhedPort
	$appSettings.AppSettings.StsOrganisationPort                  = $Env:StsOrganisationPort
	$appSettings.AppSettings.StsOrganisationSystemPort            = $Env:StsOrganisationSystemPort
	$appSettings.AppSettings.StsOrganisationCertificateThumbprint = $Env:StsOrganisationCertificateThumbprint
	$appSettings.AppSettings.PubSubBaseUrl                        = $Env:PubSubBaseUrl

	$appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8

	Write-Host "Update of appsettings.json complete"

	# Patch SAML configuration in Presentation.Web.dll.config.
	# The SAML library (kitos.dk.nita.saml20) uses System.Configuration.ConfigurationManager which,
	# in .NET Core, reads from {assembly}.dll.config (the published App.config). That file is static
	# in the build output, so it must be patched here with environment-specific values that were
	# previously injected by MSDeploy via Parameters.xml into web.config.
	Write-Host "Updating SAML configuration in Presentation.Web.dll.config"
	$dllConfigPath = ".\TEMP_PresentationWeb\Presentation.Web.dll.config"
	if (Test-Path $dllConfigPath) {
		[xml]$dllConfig = Get-Content $dllConfigPath -Raw -Encoding UTF8

		$nsMgr = New-Object System.Xml.XmlNamespaceManager($dllConfig.NameTable)
		$nsMgr.AddNamespace("saml", "urn:dk.nita.saml20.configuration")

		# Federation section: signing certificate thumbprint
		$signingCert = $dllConfig.SelectSingleNode(
			"/configuration/saml:Federation/saml:SigningCertificates/saml:SigningCertificate[@x509FindType='FindByThumbprint']",
			$nsMgr)
		if ($signingCert) { $signingCert.SetAttribute("findValue", $Env:SsoCertificateThumbPrint) }
		else { Write-Warning "dll.config: SigningCertificate node not found" }

		# Federation section: allowed audience URI (must match the SP entity ID in SAML assertions)
		$audience = $dllConfig.SelectSingleNode(
			"/configuration/saml:Federation/saml:AllowedAudienceUris/saml:Audience",
			$nsMgr)
		if ($audience) { $audience.InnerText = $Env:SsoServiceProviderId }
		else { Write-Warning "dll.config: Audience node not found" }

		# SAML20Federation section: ServiceProvider entity ID and server base URL
		$serviceProvider = $dllConfig.SelectSingleNode(
			"/configuration/saml:SAML20Federation/saml:ServiceProvider",
			$nsMgr)
		if ($serviceProvider) {
			$serviceProvider.SetAttribute("id", $Env:SsoServiceProviderId)
			$serviceProvider.SetAttribute("server", $Env:SsoServiceProviderServer)
		}
		else { Write-Warning "dll.config: ServiceProvider node not found" }

		# SAML20Federation section: IDP endpoint entity ID
		$idpEndpoint = $dllConfig.SelectSingleNode(
			"/configuration/saml:SAML20Federation/saml:IDPEndPoints/saml:add",
			$nsMgr)
		if ($idpEndpoint) { $idpEndpoint.SetAttribute("id", $Env:SsoIDPEndPoints) }
		else { Write-Warning "dll.config: IDPEndPoints/add node not found" }

		$dllConfig.Save($dllConfigPath)
		Write-Host "SAML configuration updated in Presentation.Web.dll.config"
	} else {
		Write-Warning "Presentation.Web.dll.config not found at '$dllConfigPath' - SAML configuration not updated"
	}

	# Handle environment-specific robots file (was previously done via msdeploy -replace)
	$wwwroot = ".\TEMP_PresentationWeb\wwwroot"
	$robotsSource = Join-Path $wwwroot $Env:robots
	if (Test-Path $robotsSource) {
		Copy-Item $robotsSource (Join-Path $wwwroot "Robots.txt") -Force
		Write-Host "Robots.txt set from $($Env:robots)"
	}

	Compress-Archive -Path ".\TEMP_PresentationWeb\*" -DestinationPath "$pathToArchive"
	Write-Host "Zipping file back complete"

	Remove-Item -Path ".\TEMP_PresentationWeb" -Recurse -Force
	Write-Host "Clean up done"
}