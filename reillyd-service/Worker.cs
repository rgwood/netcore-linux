using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

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
            _logger.LogInformation($"Current PID: {getpid()}");

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



        public static bool Tester() => true;
    }
}
