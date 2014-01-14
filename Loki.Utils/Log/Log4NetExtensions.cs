using System;
using log4net;

namespace Loki.Utils
{
    public static class Log4NetExtensionMethods
    {
        #region Deferred string formating, using lambda expression

        public static void Debug(this ILog log, Func<string> formattingCallback, params Object[] args)
        {
            if (!log.IsDebugEnabled)
                return;
            
            if (args.Length == 0)
                 log.Debug(formattingCallback());
             else
                 log.DebugFormat(formattingCallback(), args);            
        }

        public static void Info(this ILog log, Func<string> formattingCallback, params Object[] args)
        {
            if (!log.IsInfoEnabled)
                return;
            
            if (args.Length == 0)
                 log.Info(formattingCallback());
             else
                 log.InfoFormat(formattingCallback(), args);
        }

        public static void Warn(this ILog log, Func<string> formattingCallback, params Object[] args)
        {
            if (!log.IsWarnEnabled)
                return;
           
            if (args.Length == 0)
                log.Warn(formattingCallback());
            else
                log.WarnFormat(formattingCallback(), args);
        }

        public static void Error(this ILog log, Func<string> formattingCallback, params Object[] args)
        {
            if (!log.IsErrorEnabled)
                return;
            
            if (args.Length == 0)
                 log.Error(formattingCallback());
             else
                 log.ErrorFormat(formattingCallback(), args);
            
        }

        public static void Fatal(this ILog log, Func<string> formattingCallback, params Object[] args)
        {
            if (!log.IsFatalEnabled)
                return;
            
            if (args.Length == 0)
                 log.Fatal(formattingCallback());
             else
                 log.FatalFormat(formattingCallback(), args);
        }

        #endregion
    }
}
