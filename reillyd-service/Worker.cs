using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Mono.Unix.Native;
using System.Linq;

namespace reillyd_service
{

    /*
    struct timeval {
          time_t      tv_sec;    
          suseconds_t tv_usec;   
      };
    */
    [StructLayout(LayoutKind.Sequential)]
    public class TimeVal {
        public long tv_sec;
        public long tv_usec;
    }

    /* 
        struct timezone {
               int tz_minuteswest;     minutes west of Greenwich
               int tz_dsttime;         type of DST correction 
           };
    */
    [StructLayout(LayoutKind.Sequential)]
    public class TimeZone {
        public int tz_minuteswest;
        public int tz_dsttime;
    }


    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        // P/Invoke methods are supposed to use the same naming as the underlying native method
        #pragma warning disable IDE1006
        [DllImport("libc", EntryPoint = "gettimeofday")]
        private static extern int gettimeofday(TimeVal timeVal, TimeZone timeZone);

        [DllImport("libc", EntryPoint = "getpid")]
        private static extern int getpid();
        #pragma warning restore IDE1006

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Current OS: {Environment.OSVersion.Platform}");

            var pid = Syscall.getpid();
            Console.WriteLine($"Current PID: {pid}");

            var procDirectory = $"/proc/{pid}";
            var openFdPaths = Directory.EnumerateFiles($"{procDirectory}/fd");

            var openRegularFiles = openFdPaths.Select(p => new 
                                    {
                                        symlinkPath = p,
                                        linkTargetPath = Mono.Unix.UnixPath.ReadLink(p),
                                        fd = int.Parse(p.Substring(procDirectory.Length + 4)),
                                    })
                                    .Where(f => IsRegularFile(f.fd));
            

            Console.WriteLine("Files that this process has open:");

            foreach(var f in openRegularFiles) {
                Console.WriteLine(f.linkTargetPath);
            }

            // using (StreamReader sr = new StreamReader($"{procDirectory}/status"))
            // {
            //     string file = await sr.ReadToEndAsync();
            //     _logger.LogInformation(file);
            // }

            var tv = new TimeVal();
            gettimeofday(tv, null);
            var timeOffset = DateTimeOffset.FromUnixTimeSeconds(tv.tv_sec);
            _logger.LogInformation($"Seconds elapsed since Unix epoch: {timeOffset.DateTime}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(
                    1000,
                    stoppingToken);
            }
        }

        private static bool IsRegularFile(int fd)
        {
            Stat fstatResult;
            Syscall.fstat(fd, out fstatResult);
            var fileTypeBits = fstatResult.st_mode & FilePermissions.S_IFMT;
            return fileTypeBits == FilePermissions.S_IFREG;
        }
        
        public static bool Tester() => true;
    }
}
