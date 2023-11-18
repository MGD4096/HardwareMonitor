Get-Service -DisplayName "HardwareMonitor" | Remove-Service
New-Service -Name "HardwareMonitor" -BinaryPathName ../HardwareMonitor.exe