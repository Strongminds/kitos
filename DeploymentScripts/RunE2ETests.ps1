
param(
[Parameter(Mandatory=$true)][string]$usrname,
[Parameter(Mandatory=$true)][string]$pwd,
[Parameter(Mandatory=$true)][string]$url
)

Try 
{
    $oldSeleniumServer = Get-Process -Id (Get-NetTCPConnection -LocalPort 4445 -ErrorAction Stop).OwningProcess | Where-Object {$_.ProcessName.Equals("java")}

    if ($oldSeleniumServer){
        kill -Id $oldSeleniumServer.Id
        while(-Not $oldSeleniumServer.HasExited){
            Write-Output "Not yet killed"
            sleep 1
        }
    }
}
Catch [Microsoft.PowerShell.Cmdletization.Cim.CimJobException]
{

}
Catch
{
    Write-Host "An unexpected error ocurred. Is your selenium server shut-down correctly?"
}

try {
    $app = Start-Process powershell.exe -ArgumentList "webdriver-manager start" -PassThru -WindowStyle Hidden
    $gulpApp = Start-Process powershell.exe -ArgumentList "gulp e2e:headless --params.login.email='$usrname' --params.login.pwd='$pwd' --baseUrl='$url'" -PassThru -Wait -WindowStyle Hidden
    
    If ($gulpApp.ExitCode.Equals(1)){
        throw "At least 1 E2E test failed, check tmp folder for log and report"
    }
}
catch{
    Write-Host "An unexpected error ocurred. Not sure what could have caused this. Please try again or investigate..."
}
finally{
    Stop-Process $app.Id

    $seleniumServer = Get-Process -Id (Get-NetTCPConnection -LocalPort 4444).OwningProcess | Where-Object {$_.ProcessName.Equals("java")}

    kill -Id $seleniumServer.Id
}





