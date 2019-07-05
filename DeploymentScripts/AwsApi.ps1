Function Configure-Aws-From-User-Input() {
    Write-Host "Configuring AWS based on inputs from Teamcity user"
	
	if (-Not (Test-Path 'env:AwsAccessKeyId')) { 
		throw "Error: Remember to set the AwsAccessKeyId input before starting the build"
	} 
	if (-Not (Test-Path 'env:AwsSecretAccessKey')) { 
		throw "Error: Remember to set the AwsSecretAccessKey input before starting the build"
	} 

	# Set defaults
	$Env:AWS_DEFAULT_REGION="eu-west-1"
	
	# Set access keys passed by user
	$Env:AWS_ACCESS_KEY_ID=$Env:AwsAccessKeyId
	$Env:AWS_SECRET_ACCESS_KEY=$Env:AwsSecretAccessKey
	
	Write-Host "Finished configuring AWS. Active Key Id: $Env:AWS_ACCESS_KEY_ID"
}

Function Get-SSM-Parameter($environmentName, $parameterName) {
    Write-Host "Getting $parameterName from SSM"
	(aws ssm get-parameter --with-decryption --name "/kitos/$environmentName/$parameterName" | ConvertFrom-Json).Parameter.Value
	if($LASTEXITCODE -ne 0)	{ Throw "FAILED TO LOAD $parameterName from $environmentName" }
}

Function Load-Environment-Secrets-From-Aws($envName) {
	Write-Host "Loading environment secrets from SSM"
	
	$Env:MsDeployUserName = Get-SSM-Parameter -environmentName "$envName" -parameterName "MsDeployUserName"
	$Env:MsDeployPassword = Get-SSM-Parameter -environmentName "$envName" -parameterName "MsDeployPassword"
	$Env:KitosDbConnectionStringForIIsApp = Get-SSM-Parameter -environmentName "$envName" -parameterName "KitosDbConnectionStringForIIsApp"
	$Env:HangfireDbConnectionStringForIIsApp = Get-SSM-Parameter -environmentName "$envName" -parameterName "HangfireDbConnectionStringForIIsApp"
	$Env:KitosDbConnectionStringForTeamCity = Get-SSM-Parameter -environmentName "$envName" -parameterName "KitosDbConnectionStringForTeamCity"
	$Env:HangfireDbConnectionStringForTeamCity = Get-SSM-Parameter -environmentName "$envName" -parameterName "HangfireDbConnectionStringForTeamCity"
	
	Write-Host "Finished loading environment secrets from SSM"
}