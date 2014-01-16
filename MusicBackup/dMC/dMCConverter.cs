using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
    /// http://stackoverflow.com/questions/19170131/run-a-commandline-process-and-get-the-output-while-that-process-is-still-running
    /// </summary>
    internal class dMCConverter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(dMCConverter));

        private dMCConverter()
        {
            Log.Info(() => "Creating a new dMCConverter...", _nbCores);

            // Set default number of cores to be used
            NbCores = Properties.Settings.Default.NbCores;

            // Initialize job queues
            _runningJobs = new Job[Environment.ProcessorCount];
            _waitingJobs = new Queue<Job>();

            // Init scheduler
            _scheduler = new Timer(500);
            _scheduler.Elapsed += scheduler_Elapsed;

            Log.Info(() => "dMCConverter successfull created.", _nbCores);
        }

        #region Conversion Methods

        public IdMCJob ToMp3(String input, String output, int bitrate = 320)
        {
            // Instanciate a new job
            var job = new Job(this,
                input,
                output,
                "mp3 (Lame)",
                String.Format("-b={0}", bitrate));

            //Enqueue it
            EnqueueJob(job);

            return job;
        }

        public IdMCJob ToFlac(String input, String output, int level = 5)
        {
            // Verify copression level
            int compression = (level >= 0 && level <= 8)
                                ? level
                                : 5;

            // Instanciate a new job
            var job = new Job(this,
                input,
                output,
                "FLAC",
                String.Format("-compression-level-{0}", compression));
          
            //Enqueue it
            EnqueueJob(job);

            return job;
        }

        #endregion

        #region Core limit

        private int _nbCores = 1;
        public int NbCores
        {
            get { return _nbCores; }
            set
            {
                _nbCores = value <= 0
                    ? Environment.ProcessorCount - 1
                    : Math.Min(value, Environment.ProcessorCount);

                Log.Info(() => "Using {0} cores", _nbCores);
            }
        }

        #endregion

        #region Job Queue

        private readonly Queue<Job> _waitingJobs;

        public IEnumerable<IdMCJob> Queue { get { return _waitingJobs; } } 

        private readonly Object _lock = new Object();

        private void EnqueueJob(Job job)
        {
            // Enqueue the job
            Log.Info(()=>"Enqueuing new job <{0}>", job.Output);
            lock (_lock)
            {
                _waitingJobs.Enqueue(job);
            }

            // Ensure watchdog is running
            if (!_scheduler.Enabled)
            {
                Log.Debug(() => "Resuming watchdog");
                _scheduler.Start();
            }
        }


        #endregion

        #region Job Runner

        private readonly Job[] _runningJobs;

        public IEnumerable<IdMCJob> Runnings { get { return _runningJobs.Where(x=>x!=null); } } 


        private void RunJob(Job job, int core)
        {
            Log.Info(() => "Core <{0}> - Starting job <{1}>", core, job.Output);

            lock (_lock)
            {
                // Check that the core is availble
                if (_runningJobs[core] != null)
                {
                    Log.Error(() => "Core <{0}> is not available. Skip job");
                    job.Status = JobStatus.Failed;
                    return;
                }

                // Add job to the running list
                _runningJobs[core] = job;
                job.Status = JobStatus.Running;
            }

            // Check that the exe is available
            var exepath = Properties.Settings.Default.ExePath;
            if (!File.Exists(exepath))
            {              
                Log.Error(()=>"dBpoweramp CoreConverter.exe couldn't be found at specified location <{0}>", exepath);
                job.Status = JobStatus.Failed;

                // TODO -- refactoring
                // Remove job from runnings one
                lock (_lock)
                {
                    if (_runningJobs[core] == job)
                        _runningJobs[core] = null;
                    else
                        Log.Fatal(() => "Core <{0}> is not hosting the expected job.");
                }
                return;
            }

            // Start dBpoweramp CLI
            var task = Task.Factory.StartNew(() =>
            {
                #region multi-window
                //var processInfo = new ProcessStartInfo("cmd.exe");
                //processInfo.Verb = "runas";
                //processInfo.Arguments = "/C " + exepath; //use /K to keep windows opened
                //processInfo.Arguments +=
                //    " -infile=" + "\"" + job.Input + "\"" +
                //    " -outfile=" + "\"" + job.Output + "\"" +
                //    " -convert_to=" + "\"" + job.Encoder + "\"" +
                //    " -processor=" + "\"" + core + "\"" +
                //    job.Settings +
                //    " -V 2";
                //processInfo.Arguments += " /R /D Y";

                //var p = Process.Start(processInfo);
                //p.WaitForExit();
                #endregion

                #region test 1 (working)

                //var processInfo = new ProcessStartInfo("cmd.exe");
                //processInfo.Verb = "runas";
                //processInfo.Arguments = "/C " + exepath; //use /K to keep windows opened
                //processInfo.Arguments +=
                //    " -infile=" + "\"" + job.Input + "\"" +
                //    " -outfile=" + "\"" + job.Output + "\"" +
                //    " -convert_to=" + "\"" + job.Encoder + "\"" +
                //    " -processor=" + "\"" + core + "\"" +
                //    job.Settings +
                //    " -V 2";
                //processInfo.Arguments += " /R /D Y";

                //processInfo.CreateNoWindow = true;
                //processInfo.UseShellExecute = false;
                //processInfo.RedirectStandardError = true;
                //processInfo.RedirectStandardOutput = true;
                ////processInfo.StandardErrorEncoding = Encoding.Unicode;
                ////processInfo.StandardOutputEncoding = Encoding.Unicode;

                //var p = Process.Start(processInfo);
                //int progress = 0;
                //while (!p.HasExited)
                //{
                //    var ch = (char) p.StandardOutput.Read();

                //    if (ch == '*')
                //    {
                //        progress++;
                //        job.Progress = (int) (100 * (progress / 59f)); // Maximum value is 59, so a ProgressBar Maximum property value would be 59.
                //        Log.Info(()=>"=>{0} : {1}%", job.Output, job.Progress);
                //    }

                //    if (progress == 59)
                //    {
                //        // I store only the last line 'cause it has interesting information:
                //        // Example message: Conversion completed in 30 seconds x44 realtime encoding
                //        var msg = p.StandardOutput.ReadToEnd().Trim();
                //        var splits = msg.Split('\n');
                //        Log.Info(() => "=>{0} : {1}", job.Output, splits.Last());

                //        job.Progress = 100;
                //        job.Status = JobStatus.Succeed;    
                //    }
                //}

                //// necessary?
                //p.WaitForExit();

                #endregion

                var processInfo = new ProcessStartInfo("cmd.exe");
                processInfo.Verb = "runas";
                processInfo.Arguments = "/C " + exepath; //use /K to keep windows opened
                processInfo.Arguments +=
                    " -infile=" + "\"" + job.Input + "\"" +
                    " -outfile=" + "\"" + job.Output + "\"" +
                    " -convert_to=" + "\"" + job.Encoder + "\"" +
                    " -processor=" + "\"" + core + "\"" +
                    job.Settings +
                    " -V 2";
                processInfo.Arguments += " /R /D Y";

                // Redirect outputs and hide cmd windows
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;
                //processInfo.StandardErrorEncoding = Encoding.Unicode;
                //processInfo.StandardOutputEncoding = Encoding.Unicode;

                // Start the process
                var p = new Process();
                p.StartInfo = processInfo;
                p.Start();

                // Progress management
                int progress = 0;
                while (!p.HasExited && progress < 59)
                {
                    var ch = (char)p.StandardOutput.Read();

                    if (ch == '*')
                    {
                        progress++;
                        job.Progress = (int)(100 * (progress / 59f)); // Maximum value is 59, so a ProgressBar Maximum property value would be 59.
                        Log.Debug(() => "=>{0} : {1}%", job.Output, job.Progress);
                    }
                }

                // Ensure process has finished
                p.WaitForExit();

                // Conversion result
                var errors = p.StandardError.ReadToEnd();
                if (errors.Length > 0)
                {
                    Log.Error(() => "An error occured whil converting {0} : {1}", job.Output, errors);
                    job.Status = JobStatus.Failed;
                }
                else
                {
                    // I store only the last line 'cause it has interesting information:
                    // Example message: Conversion completed in 30 seconds x44 realtime encoding
                    var msg = p.StandardOutput.ReadToEnd().Trim();
                    var splits = msg.Split('\n');
                    Log.Info(() => "Conversion succeeded {0} : {1}", job.Output, splits.Last());

                    job.Progress = 100;
                    job.Status = JobStatus.Succeed;
                }
               
                // Close the process
                p.Close();
            });

            // On CLI exit     
            task.ContinueWith(x =>
            {
                Log.Info(() => "Core <{0}> - Ending job <{1}>", core, job.Output);
                job.Status = JobStatus.Succeed;     // TODO - test failed or not
  
                // Remove job from runnings one
                lock (_lock)
                {
                    if (_runningJobs[core] == job)
                        _runningJobs[core] = null;
                    else
                        Log.Fatal(() => "Core <{0}> is not hosting the expected job.");
                }
            });
        }

        public bool IsWorking
        {
            get { return Queue.Any() || Runnings.Any(); }
        }

        private void FireJobUpdated(IdMCJob job)
        {
            if (JobUpdated != null)
                JobUpdated(job);
        }

        #endregion

        #region Job Scheduler

        private readonly Timer _scheduler = new Timer(500);

        private void scheduler_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Find first available core
            int core = -1;
            lock (_lock)
            {
                for (int i = 0; i < _runningJobs.Length; i++)
                    if (_runningJobs[i] == null)
                    {
                        core = i;
                        Log.Debug(() => "Core <{0}> is waiting for a new job.", i);
                        break;
                    }
            }

            // No available cores
            if (core < 0 || core >= NbCores)
                return;

            // Launch a new job
            if (_waitingJobs.Count > 0)
            {
                Job job;
                lock (_lock)
                {
                    job = _waitingJobs.Dequeue();
                }
                RunJob(job, core);
            }
            else
            {
                // Suspend watchdog
                Log.Debug(() => "No more pending job => stopping watchdog");
                _scheduler.Stop();
            }
        }

        #endregion

        #region Job class implementation

        public event Action<IdMCJob> JobUpdated;

        class Job : IdMCJob
        {
            public Job(dMCConverter parent, String input, String output, String encoder, String settings = "")
            {
                Parent      = parent;
                Input       = input;
                Output      = output;
                Encoder     = encoder;
                Settings    = settings;

                Progress    = 0;
                Status      = JobStatus.Created;
            }

            public dMCConverter Parent   { get; private set; }

            public string   Input        { get; private set; }
            public string   Output       { get; private set; }
            public string   Encoder      { get; private set; }
            public string   Settings     { get; private set; }

            public DateTime CreationTime { get; private set; }
            public DateTime StartTime    { get; private set; }
            public DateTime StopTime     { get; private set; }

            public int      Progress     { get; set; }

            private JobStatus _status;
            public JobStatus Status 
            {
                get { return _status; }
                set 
                {
                    _status = value;
                    switch (value)
                    {
                        case JobStatus.Created  : CreationTime  = DateTime.Now; break;
                        case JobStatus.Running  : StartTime     = DateTime.Now; break;
                        case JobStatus.Succeed  :
                        case JobStatus.Failed   : StopTime      = DateTime.Now; break;
                        case JobStatus.InQueue  :
                        default                 : break;
                    }
                    Parent.FireJobUpdated(this);
                }
            }
        }

        #endregion

        #region Singleton

        private static readonly Lazy<dMCConverter> _instance = new Lazy<dMCConverter>(() => new dMCConverter());

        public static dMCConverter Instance { get { return _instance.Value; } }

        #endregion
    }
}
