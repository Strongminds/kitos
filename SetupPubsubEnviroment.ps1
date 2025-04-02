.$PSScriptRoot\..\..\deploymentscripts\AwsApi.ps1

Function Load-Enviroment-Secrets-From-Aws([String] $envName) {
	$parameters = Get-SSM-Parameters -environmentName "$envName"

    if($parameters.Count -eq 0) {
        throw "No parameters found for environment $envName"
    }

    $Env:RABBIT_MQ_USER         = $parameters["RABBIT_MQ_USER"]
    $Env:RABBIT_MQ_PASSWORD     = $parameters["RABBIT_MQ_PASSWORD"]
    $Env:PUBSUB_API_KEY         = $parameters["PUBSUB_API_KEY"]
    $Env:CERT_PASSWORD          = $parameters["CERT_PASSWORD"]
    $Env:IDP_HOST_MAPPING       = $parameters["IDP_HOST_MAPPING"]
}