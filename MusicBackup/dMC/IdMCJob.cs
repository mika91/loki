using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBackup.dMC
{
    internal enum JobStatus { Created, InQueue, Running, Succeed, Failed }

    internal interface IdMCJob
    {
        string      Input        { get; }
        string      Output       { get; }
        string      Encoder      { get; }
        string      Settings     { get; }
                                 
        DateTime    CreationTime { get; }
        DateTime    StartTime    { get; }
        DateTime    StopTime     { get; }

        JobStatus   Status       { get; }
        int         Progress     { get; }
    }


}
