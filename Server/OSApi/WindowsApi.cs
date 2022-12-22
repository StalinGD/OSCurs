using System;
using System.Collections;
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
            var info = new SystemInfo();
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
            var info = new SystemInfo();
            GetNativeSystemInfo(ref info);
            return (int)info.dwNumberOfProcessors;
        }

        public override int GetPhysicalCoresCount()
        {
            var buffer = new SystemLogicalProcessorInformationEx_Processor[64];
            uint returnedLength = 0;

            _ = GetLogicalProcessorInformationEx(
                0, // RelationProcessorCore
                buffer,
                ref returnedLength);

            var size = 48; // should be 48 bytes, trust me
            return (int)returnedLength / size;
        }

        public override int GetProcessModulesCount()
        {
            // TH32CS_SNAPMODULE, TH32CS_SNAPMODULE32
            uint snapshotOptions = 0x00000008 | 0x00000010;
            var snapshot = CreateToolhelp32Snapshot(snapshotOptions, 0);

            var count = 0;
            foreach (ModuleEntry32 module in new EnumerableFromEnumerator(new ModuleEnumerator(snapshot)))
            {
                count++;
            }
            return count;
        }



        [DllImport("Kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref SystemInfo info);
        [DllImport("Kernel32.dll")]
        private static extern bool GetLogicalProcessorInformationEx(
            int relationshipType,
            SystemLogicalProcessorInformationEx_Processor[] buffer, 
            ref uint returnedLength);


        private struct SystemInfo
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

        private struct SystemLogicalProcessorInformationEx_Processor
        {
            public uint relationshipType;
            public uint size; 
            // _PROCESSOR_RELATIONSHIP
            public byte flags;
            public byte efficiencyClass;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] reserved;
            public ushort mask;
            // _GROUP_AFFINITY 
            public uint mask1;
            public ushort group;
            public ushort reserved0;
            public ushort reserved1;
            public ushort reserved2;
        }


        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        private static extern bool Module32First(IntPtr hSnapshot, ref ModuleEntry32 lpte);
        [DllImport("kernel32.dll")]
        private static extern bool Module32Next(IntPtr hSnapshot, ref ModuleEntry32 lpte);
        private delegate bool Module32Delegate(IntPtr hSnapshot, ref ModuleEntry32 lpte);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct ModuleEntry32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblentUsage;
            public uint procentUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        private struct EnumerableFromEnumerator : IEnumerable
        {
            private IEnumerator enumerator;

            public EnumerableFromEnumerator(IEnumerator enumerator)
            {
                this.enumerator = enumerator;
            }

            public IEnumerator GetEnumerator()
            {
                return enumerator;
            }
        }


        private class ModuleEnumerator : IEnumerator<ModuleEntry32>
        {
            public ModuleEntry32 Current { get; private set; }
            object IEnumerator.Current => Current;

            private readonly IntPtr snapshot;
            private bool isFirst = true;

            public ModuleEnumerator(IntPtr snapshot)
            {
                this.snapshot = snapshot;
            }

            public bool MoveNext()
            {
                Module32Delegate method = isFirst ? Module32First : Module32Next;
                isFirst = false;

                var entry = new ModuleEntry32();
                entry.dwSize = (uint)Marshal.SizeOf(entry);
                var result = method(snapshot, ref entry);
                Current = entry;
                return result;
            }

            public void Dispose() { }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

    }
}
