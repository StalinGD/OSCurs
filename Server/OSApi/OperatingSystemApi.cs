using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.OSApi
{
    internal abstract class OperatingSystemApi
    {
        public static OperatingSystemApi Current
        {
            get
            {
                if (instance == null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        instance = new WindowsApi();
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        instance = new LinuxApi();
                    }
                }
                return instance;
            }
        } 
        private static OperatingSystemApi instance;


        public abstract string GetArchitecture();
        public abstract int GetLogicalCoresCount();
        public abstract int GetPhysicalCoresCount();
        public abstract int GetProcessModulesCount();
    }
}
