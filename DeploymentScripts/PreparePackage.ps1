Function Prepare-Package([String] $environmentName, $pathToArchive) {

	Write-Host "Environment is $environmentName"

	Write-Host "Zip path raw: >$pathToArchive<"
	Write-Host "Type: $($pathToArchive.GetType().FullName)"

	$item = Get-Item -LiteralPath $pathToArchive -ErrorAction Stop
	Write-Host "Resolved file: $($item.FullName)"
	Write-Host "Exists: $($item.Exists)"
	Write-Host "Length: $($item.Length)"
	Write-Host "Extension: $($item.Extension)"

	Add-Type -AssemblyName System.IO.Compression.FileSystem

	try {
		$zip = [System.IO.Compression.ZipFile]::OpenRead($item.FullName)
		Write-Host "Zip opened successfully. Entry count: $($zip.Entries.Count)"
		$zip.Dispose()
	}
	catch {
		Write-Host "Zip validation failed: $($_.Exception.Message)"
		throw
	}

[System.IO.Compression.ZipFile]::ExtractToDirectory($item.FullName, (Join-Path (Get-Location) "TEMP_PresentationWeb"))

	#Write-Host "Zip path raw: >$pathToArchive<"
	#Write-Host "Testing path..."
	#
	#if (-not (Test-Path -LiteralPath $pathToArchive)) {
	#	Write-Host "File not found by Test-Path"
	#	$directory = Split-Path -Path $pathToArchive -Parent
	#	Get-ChildItem -Path $directory
	#	throw "Zip file does not exist at runtime: $pathToArchive"
	#}
	#
	#Write-Host "Unzipping $pathToArchive to TEMP_PresentationWeb"
	#Expand-Archive -LiteralPath $pathToArchive -DestinationPath .\TEMP_PresentationWeb -Force
	Remove-Item -Path "$pathToArchive"

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
	# TODO: Verify if SsoServiceProviderServer ($Env:SsoServiceProviderServer) and SsoIDPEndPoints ($Env:SsoIDPEndPoints)
	# are still required. These had no matching key in appsettings.json (previously came from the <Federation> XML
	# config section). If the app still reads them, add the keys under AppSettings and set them here.

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