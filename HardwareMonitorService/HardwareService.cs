using LibreHardwareMonitor.Hardware;

namespace HardwareMonitorService
{
    public class HardwareService : BackgroundService
    {
        private readonly ILogger<HardwareService> _logger;
        //LibHardwareMonitor
        Computer computer;
        UpdateVisitor UpdateVisitor;
        Func<IHardware, bool> cpuSelector = x => x.HardwareType == HardwareType.Cpu;
        Func<IHardware, bool> gpuSelector = x => x.HardwareType == HardwareType.GpuNvidia || x.HardwareType == HardwareType.GpuAmd || x.HardwareType == HardwareType.GpuIntel;
        Func<IHardware, bool> ramSelector = x => x.HardwareType == HardwareType.Memory;

        //serial port
        System.IO.Ports.SerialPort serialPort;

        public HardwareService(ILogger<HardwareService> logger, IConfiguration configuration)
        {
            _logger = logger;
            //Set up serial port
            serialPort = new System.IO.Ports.SerialPort(configuration["Port"].ToString());
            serialPort.BaudRate = int.Parse(configuration["BaudRate"].ToString());
            serialPort.Parity = System.IO.Ports.Parity.None;
            serialPort.StopBits = System.IO.Ports.StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = System.IO.Ports.Handshake.None;
            serialPort.RtsEnable = true;

            //Set up computer
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };
            UpdateVisitor = new UpdateVisitor();
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            computer.Open();
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            serialPort.Close();
            computer.Close();
            return base.StopAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                SendData_Timer_Tick();
                await Task.Delay(1000, stoppingToken);
            }
        }
        private void SendData_Timer_Tick()
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.Open();
                }
                catch (Exception) { }
            }
            else
            {
                computer.Accept(UpdateVisitor);
                //Get cpu data from all processors
                var cpu_load = (int)((
                    computer.Hardware
                        .Where(cpuSelector)
                        .SelectMany(x => x.Sensors)
                        .Where(x => x.SensorType == SensorType.Load && x.Name == "CPU Total")
                        .Sum(x => x.Value) ?? 0)
                    / computer.Hardware.Count(cpuSelector));
                cpu_load = cpu_load < 0 ? 0 : (cpu_load > 100 ? 100 : cpu_load);

                var cpu_temp = (int)(
                    computer.Hardware
                        .Where(cpuSelector)
                        .SelectMany(x => x.Sensors)
                        .Where(x => x.SensorType == SensorType.Temperature && x.Name == "CPU Package")
                        .Max(x => x.Value) ?? 0);
                cpu_temp = cpu_temp < 0 ? 0 : (cpu_temp > 150 ? 150 : cpu_temp);

                //Get gpu data from all graphics processors
                var gpu_load = (int)((
                    computer.Hardware
                        .Where(gpuSelector)
                        .SelectMany(x => x.Sensors)
                        .Where(x => x.SensorType == SensorType.Load && x.Name == "GPU Core")
                        .Sum(x => x.Value) ?? 0)
                    / computer.Hardware.Count(gpuSelector));
                gpu_load = gpu_load < 0 ? 0 : (gpu_load > 100 ? 100 : gpu_load);

                var gpu_temp = (int)(
                    computer.Hardware
                        .Where(gpuSelector)
                        .SelectMany(x => x.Sensors)
                        .Where(x => x.SensorType == SensorType.Temperature && x.Name == "GPU Core")
                        .Max(x => x.Value) ?? 0);
                gpu_temp = gpu_temp < 0 ? 0 : (gpu_temp > 150 ? 150 : gpu_temp);

                var vram_total = computer.Hardware
                    .Where(gpuSelector)
                    .SelectMany(x => x.Sensors)
                    .Where(x => x.SensorType == SensorType.SmallData && x.Name == "GPU Memory Total")
                    .ToList()
                    .Sum(x => x.Value);

                var vram_used = computer.Hardware
                    .Where(gpuSelector)
                    .SelectMany(x => x.Sensors)
                    .Where(x => x.SensorType == SensorType.SmallData && x.Name == "GPU Memory Used")
                    .ToList()
                    .Sum(x => x.Value);

                var vram_load = (int)((vram_used / vram_total) * 100);
                vram_load = vram_load < 0 ? 0 : (vram_load > 100 ? 100 : vram_load);

                //Get ram data
                var ram_load = (int)(
                    computer.Hardware
                    .Where(ramSelector)
                    .SelectMany(x => x.Sensors)
                    .Where(x => x.SensorType == SensorType.Load && x.Name == "Memory")
                    .FirstOrDefault()?.Value ?? 0);
                ram_load = ram_load < 0 ? 0 : (ram_load > 100 ? 100 : ram_load);

                //Get power consumption from all components
                var power_consumption = (int)computer.Hardware
                    .SelectMany(x => x.Sensors)
                    .Where(x => x.SensorType == SensorType.Power && x.Name.Contains("Package"))
                    .ToList()
                    .Sum(x => x.Value ?? 0);
                power_consumption = power_consumption < 0 ? 0 : (power_consumption > 2500 ? 2500 : power_consumption);
                try
                {
                    //send data to arduino
                    serialPort
                        .Write($"{DateTime.Now.ToString("HH:mm")};m;{cpu_load};{cpu_temp};{gpu_temp};{gpu_load};{ram_load};{vram_load};{power_consumption}*");
                    var command = serialPort.ReadExisting();
                    if (command.Contains("command") && command.Split(';')[1].ToUpper().Contains("SHUTDOWN"))
                    {
                        DeviceManagement.Shutdown();
                    }
                    else if (command.Contains("command") && command.Split(';')[1].ToUpper().Contains("REBOOT"))
                    {
                        DeviceManagement.Reboot();
                    }
                    else if (command.Contains("command") && command.Split(';')[1].ToUpper().Contains("LOCK"))
                    {
                        DeviceManagement.LockWorkStation();
                    }
                    else if (command.Contains("command") && command.Split(';')[1].ToUpper().Contains("SLEEP"))
                    {
                        DeviceManagement.Sleep();
                    }

                }
                catch (Exception)
                {
                    //do nothing while exception
                }
            }
        }
    }
}