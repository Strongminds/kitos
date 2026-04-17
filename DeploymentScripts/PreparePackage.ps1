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