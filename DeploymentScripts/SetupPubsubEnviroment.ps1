.$PSScriptRoot\..\..\deploymentscripts\AwsApi.ps1

Function Load-Environment-Secrets-From-Aws([String] $envName) {
	$parameters = Get-SSM-Parameters -environmentName "$envName"

    if($parameters.Count -eq 0) {
        throw "No parameters found for environment $envName"
    }

    $Env:RABBIT_MQ_USER         = $parameters["RabbitMqUsername"]
    $Env:RABBIT_MQ_PASSWORD     = $parameters["RabbitMqPassword"]
    $Env:PUBSUB_API_KEY         = $parameters["PubSubApiKey"]
    $Env:CERT_PASSWORD          = $parameters["CertPassword"]
    $Env:IDP_HOST_MAPPING       = $parameters["IdpHostMapping"]
}