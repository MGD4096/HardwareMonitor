using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HardwareMonitorService
{
    internal class DeviceManagement
    {
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSShutdownSystem(IntPtr hServer, long ShutdownFlag);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);
        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        private enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
        public enum WTS_SHUTDOWN_FLAG
        {
            WTS_WSD_LOGOFF = 0x01,
            WTS_WSD_SHUTDOWN = 0x02,
            WTS_WSD_REBOOT = 0x04,
            WTS_WSD_POWEROFF = 0x08,
            WTS_WSD_FASTREBOOT = 0x10
        }

        public static void LockWorkStation()
        {
            IntPtr ppSessionInfo = IntPtr.Zero;
            Int32 count = 0;
            Int32 retval = WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref count);
            Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            IntPtr currentSession = ppSessionInfo;

            if (retval == 0) return;

            for (int i = 0; i < count; i++)
            {
                WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession, typeof(WTS_SESSION_INFO));
                if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive) WTSDisconnectSession(IntPtr.Zero, si.SessionID, false);
                currentSession += dataSize;
            }
            WTSFreeMemory(ppSessionInfo);
        }
        public static void Sleep()
        {
            SetSuspendState(false, false, false);
        }

        public static void Reboot()
        {
            WTSShutdownSystem(IntPtr.Zero, (long)WTS_SHUTDOWN_FLAG.WTS_WSD_REBOOT);
        }
        public static void Shutdown()
        {
            WTSShutdownSystem(IntPtr.Zero, (long)WTS_SHUTDOWN_FLAG.WTS_WSD_POWEROFF);
        }
    }
}
