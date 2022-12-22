using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server.OSApi
{
    internal sealed class LinuxApi : OperatingSystemApi
    {
        public override string GetArchitecture()
        {
            var info = new LinuxSystemInfo();
            _ = SyscallUname(ref info);
            return info.machine.ToString();
        }

        public override int GetLogicalCoresCount()
        {
            return SyscallGetNProcsConf();
        }



        [DllImport("libc", EntryPoint = "uname", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallUname(ref LinuxSystemInfo info);
        [DllImport("libc", EntryPoint = "get_nprocs_conf", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallGetNProcsConf();


        private struct LinuxSystemInfo
        {
            public StringBuilder sysname;
            public StringBuilder nodename;
            public StringBuilder release;
            public StringBuilder version;
            public StringBuilder machine;
            public StringBuilder domainName;
        }
    }
}
