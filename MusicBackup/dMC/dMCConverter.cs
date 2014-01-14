using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using  System.Timers;
using log4net;
using Loki.Utils;

namespace MusicBackup.dMC
{
    /// <summary>
    /// http://dbpoweramp.com/developer-scripting-dmc.htm
    /// http://dbpoweramp.com/developer-cli-encoder.htm
    /// http://lame.sourceforge.net/vbr.php
    /// </summary>
    internal class dMCConverter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(dMCConverter));

        private int _nbCores = 1;
        public int NbCores
        {
            get { return _nbCores; }
            set
            {
                _nbCores = Math.Min(value, Environment.ProcessorCount);

                if (_nbCores <= 0)
                    _nbCores = Environment.ProcessorCount - 1;

                Log.Debug(() => "dMC.Converter NbCores = {0}", _nbCores);
            }
        }

        private readonly DMCSCRIPTINGLib.Converter dbPoweramp;

        private readonly Queue<Job>  WaitingJobs = new Queue<Job>();
        private readonly List<Job>   RunningJobs = new List<Job>();

        private dMCConverter()
        {
            // Default converter options
            dbPoweramp = new DMCSCRIPTINGLib.Converter();
            dbPoweramp.DeleteSourceFiles = 0;
            dbPoweramp.PreserveTags = 1;

            // Set NbCores
            NbCores = -1;

            // Watchdog
            jobWatchdog = new Timer(500);
            jobWatchdog.Elapsed += new ElapsedEventHandler(jobWatchdog_Elapsed);
        }

        public Guid ConvertToMP3(String input, String output, int bitrate = 320, string logfile=null)
        {
            // Instanciate a new job
            var job = new Job()
            {
                Input = input,
                Output = output,
                Encoder = "mp3 (Lame)",
                EncoderSettings = String.Format("-b={0}", bitrate),
                Logfile = logfile
            };

            //Enqueue it
            EnqueueJob(job);

            return job.ID;
        }

        private void EnqueueJob(Job job)
        {
            // Enqueue the job
            Log.Info(()=>"Enqueing new job: {0}", job.Output);
            lock (_lock)
            {
                WaitingJobs.Enqueue(job);
            }

            // Ensure watchdog is running
            if (!jobWatchdog.Enabled)
            {
                Log.Info(()=>"resuming watchdog");
                jobWatchdog.Start();
            }
        }

        private void ExecuteJob(Job job, int core)
        {
            Console.WriteLine("Core<{0}> start running Job: {1}", core, job.Output);

            job.Core = core;
            job.IsRunning = true;
            job.EncoderSettings += String.Format(" -processor={0}", core);

            lock (_lock)
            {
                RunningJobs.Add(job);
            }

            dMCConverter DMC = new dMCConverter();


            var task = Task.Factory.StartNew(() =>
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe");
                    processInfo.Verb = "runas";
                    processInfo.Arguments = "/C C:\\dBpoweramp\\CoreConverter.exe"; //use /K to keep windows opened
                    processInfo.Arguments +=
                        " -infile=" + "\"" + job.Input + "\"" +
                        " -outfile=" + "\"" + job.Output + "\"" +
                        " -convert_to=" + "\"" + "mp3 (Lame)" + "\"" +
                        " -processor=" + "\"" + job.Core + "\"" +
                        " -b " + "320" + 
                        " -V 2";
                    processInfo.Arguments += " /R /D Y";
                    var p = Process.Start(processInfo);
                    p.WaitForExit();
                    //DMC.Convert(job.Input, job.Output, job.Encoder, job.EncoderSettings, job.Logfile));
                });
    
                                             
            task.ContinueWith(x =>
            {
                job.IsRunning = false;
                job.Core = -1;

                // Remove job from runnings one
                lock (_lock)
                {
                    RunningJobs.Remove(job);
                }
               
                // Notify job is done
                if (JobDone != null)
                    JobDone(job.ID);
            });
        }

        private Object _lock = new Object();

        private readonly Timer jobWatchdog = new Timer(500);

        void  jobWatchdog_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Find available core
            var usedCores = RunningJobs.Select(x => x.Core).Distinct().ToList();
            var availableCore = -1;
            for (int i = 0; i < NbCores; i++)
            {
                if (!usedCores.Contains(i))
                {
                    availableCore = i;
                    Log.Debug(()=>"Core<{0}> is waiting for a new job.", i);
                    break;
                }
            }

            // No available cores
            if (availableCore < 0)
                return;    

 	        // Launch a new job
            if (WaitingJobs.Count > 0)
            {
                Job job;
                lock (_lock)
                {
                    job = WaitingJobs.Dequeue();
                }
                ExecuteJob(job, availableCore);
            }
            else
            {
                // Suspend watchdog
                Log.Info(()=>"No more pending job => stopping watchdog");
                jobWatchdog.Stop();
            }

         
        }

        public bool IsRunning
        {
            get { return WaitingJobs.Count != 0 || RunningJobs.Count != 0; }
        }

        public event Action<Guid> JobDone;


        #region Singleton

        private static readonly Lazy<dMCConverter> _instance = new Lazy<dMCConverter>(() => new dMCConverter());

        public static dMCConverter Instance { get { return _instance.Value; } }

        #endregion

        #region Job class

        public class Job
        {
            public Job()
            {
                ID = new Guid();
                IsRunning = false;
                Core = -1;
            }

            public Guid ID;

            public string Input;
            public string Output;
            public string Encoder;
            public string EncoderSettings;
            public string Logfile;

            public bool   IsRunning;
            public int    Core;
        }

        #endregion
    }
}
