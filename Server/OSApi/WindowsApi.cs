using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server.OSApi
{
    internal sealed class WindowsApi : OperatingSystemApi
    {
        public override string GetArchitecture()
        {
            var info = new WinSystemInfo();
            GetNativeSystemInfo(ref info);
            return info.wProcessorArchitecture switch
            {
                9 => "x86-64",
                5 => "arm",
                12 => "arm64",
                6 => "ia64",
                0 => "x86",

                _ => "unknown"
            };
        }

        public override int GetLogicalCoresCount()
        {
            var info = new WinSystemInfo();
            GetNativeSystemInfo(ref info);
            return (int)info.dwNumberOfProcessors;
        }



        [DllImport("Kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref WinSystemInfo info);


        private struct WinSystemInfo
        {
            public ushort wProcessorArchitecture;       //  WORD wProcessorArchitecture;
            public ushort wReserved;                    //  WORD wReserved;
            public uint dwPageSize;                     //  DWORD dwPageSize;
            public IntPtr lpMinimumApplicationAddress;  //  LPVOID lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;  //  LPVOID lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;        //  DWORD_PTR dwActiveProcessorMask;
            public uint dwNumberOfProcessors;           //  DWORD dwNumberOfProcessors;
            public uint dwProcessorType;                //  DWORD dwProcessorType;
            public uint dwAllocationGranularity;        //  DWORD dwAllocationGranularity;
            public ushort wProcessorLevel;              //  WORD wProcessorLevel;
            public ushort wProcessorRevision;           //  WORD wProcessorRevision;
        }
    }
}
