using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace Server
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            var config = BuildConfig();

            var loggerFactory = BuildLoggerFactory(config);

            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation($"Starting server...");

            var server = new Server(
                () => new ServerHandler1(loggerFactory.CreateLogger<ClientHandler>()), 
                config, 
                loggerFactory.CreateLogger<Server>());
            await server.ListenAsync();
        }

        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("config.json", true)
                .AddEnvironmentVariables("OSC")
                .Build();
        }

        private static ILoggerFactory BuildLoggerFactory(IConfiguration config)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(config)
                    .AddConsole()
                    .AddDebug();
            });
        }
    }

    class ServerHandler1 : ClientHandler
    {
        private Func<string> architectureGetter;
        private Func<int> logicalProcessorsGetter;


        public ServerHandler1(ILogger logger) : base(logger)
        {
            if (OperatingSystem.IsWindows())
            {
                architectureGetter = GetArchitectureWin;
                logicalProcessorsGetter = GetLogicalProcessorsWin;
            }
            else if (OperatingSystem.IsLinux())
            {
                architectureGetter = GetArchitectureLinux;
                logicalProcessorsGetter = GetLogicalProcessorsLinux;
            }
        }

        protected override void HandleRequest(MessageReader reader, MessageWriter writer, out int writed)
        {
            var request = reader.ReadHeader();
            writed = 0;

            switch (request.Code)
            {
                case MessageCode.SingleRequest:
                    writed = CreateAndWriteResponse(writer, request.Code);
                    return;
                case MessageCode.WatchRequest:
                    SetNotifyMode(TimeSpan.FromSeconds(1), () => CreateAndWriteResponse(GetWriter(), request.Code));
                    return;
                default:
                    Logger.LogError("Unsupported request code {Code} reseived", request.Code);
                    return;
            }
        }

        private int CreateAndWriteResponse(MessageWriter writer, byte code)
        {
            var header = new MessageHeader(code, DateTime.Now);

            var architecture = architectureGetter();
            var logicalProcessors = logicalProcessorsGetter();

            writer.Write(header);
            writer.Write(architecture);
            writer.Write(logicalProcessors);
            return writer.CurrentPosition;
        }



        private string GetArchitectureWin()
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

        private int GetLogicalProcessorsWin()
        {
            var info = new WinSystemInfo();
            GetNativeSystemInfo(ref info);
            return (int)info.dwNumberOfProcessors;
        }

        private string GetArchitectureLinux()
        {
            var info = new LinuxSystemInfo();
            _ = SyscallUname(ref info);
            return info.machine.ToString();
        }

        private int GetLogicalProcessorsLinux()
        {
            return SyscallGetNProcsConf();
        }

        [DllImport("Kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref WinSystemInfo info);


        [DllImport("libc", EntryPoint = "uname", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallUname(ref LinuxSystemInfo info);
        [DllImport("libc", EntryPoint = "get_nprocs_conf", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SyscallGetNProcsConf();

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