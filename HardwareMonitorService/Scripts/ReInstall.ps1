Stop-Service -Name "HardwareMonitor"
SC.exe DELETE HardwareMonitor
$servicePath = split-path  -parent $PSScriptRoot
New-Service -Name "HardwareMonitor" -BinaryPathName "$servicePath\HardwareMonitorService.exe"
Start-Service -Name "HardwareMonitor"