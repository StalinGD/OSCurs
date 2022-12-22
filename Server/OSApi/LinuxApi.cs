using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server.OSApi
{
    internal sealed class LinuxApi : OperatingSystemApi
    {
        public override string GetArchitecture()
        {
            var info = new SystemInfo();
            _ = SyscallUname(ref info);
            return info.machine;
        }

        public override int GetLogicalCoresCount()
        {
            return SyscallGetNProcsConf();
        }

        public override int GetPhysicalCoresCount()
        {
            var lines = File.ReadAllLines("/proc/cpuinfo");

            var maxCoreId = 0;
            var coreIdEntryCount = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("core id"))
                {
                    var semicolonIndex = line.LastIndexOf(':');
                    var coreId = int.Parse(line.AsSpan()[(semicolonIndex + 1)..].Trim());
                    maxCoreId = Math.Max(maxCoreId, coreId);
                    coreIdEntryCount++;
                }
            }

            if (coreIdEntryCount > 0)
            {
                return maxCoreId + 1;
            }

            foreach (var line in lines)
            {
                if (line.StartsWith("processor"))
                {
                    var semicolonIndex = line.LastIndexOf(':');
                    var coreId = int.Parse(line.AsSpan()[(semicolonIndex + 1)..].Trim());
                    maxCoreId = Math.Max(maxCoreId, coreId);
                }
            }

            return maxCoreId + 1;
        }

        public override int GetProcessModulesCount()
        {
            var lines = File.ReadAllLines("/proc/self/maps");

            var count = 0;

            foreach (var line in lines)
            {
                var firstSpaceIndex = line.IndexOf(' ');
                var secondSpaceIndex = line.IndexOf(' ', firstSpaceIndex + 1);

                if (line.AsSpan()[firstSpaceIndex..secondSpaceIndex].Contains('x'))
                {
                    count++;
                }
            }

            return count;
        }



        [DllImport("libc", EntryPoint = "uname", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallUname(ref SystemInfo info);
        [DllImport("libc", EntryPoint = "get_nprocs_conf", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallGetNProcsConf();


        private struct SystemInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string sysname;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string nodename;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string release;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string machine;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string domainName;
        }
    }
}
