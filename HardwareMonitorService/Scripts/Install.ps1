$servicePath = split-path  -parent $PSScriptRoot
New-Service -Name "HardwareMonitor" -BinaryPathName "$servicePath\HardwareMonitorService.exe"