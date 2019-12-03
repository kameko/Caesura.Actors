
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using System.Diagnostics;
    
    public enum ActorLogLevel
    {
        Info,
        Warning,
        Error,
        Fatal,
        Debug,
        Verbose,
    }
    
    public class ActorLogToken
    {
        public DateTime Time { get; set; }
        public int ThreadId { get; set; }
        public string SourceFile { get; set; }
        public int SourceLine { get; set; }
        public ActorLogLevel Level { get; set; }
        public ActorPath Path { get; set; }
        public Exception? Exception { get; set; }
        public string Message { get; set; }
        public IEnumerable<object> Items { get; set; }
        
        public ActorLogToken(
            ActorLogLevel level,
            string source_file,
            int source_line,
            ActorPath path,
            string message,
            IEnumerable<object> items
        ) : this(level, source_file, source_line, path, null, message, items)
        {
            
        }
        
        public ActorLogToken(
            ActorLogLevel level,
            string source_file,
            int source_line,
            ActorPath path,
            string message
        ) : this(level, source_file, source_line, path, null, message, null)
        {
            
        }
        
        public ActorLogToken(
            ActorLogLevel level,
            string source_file,
            int source_line,
            ActorPath path,
            Exception? exception,
            string message
        ) : this(level, source_file, source_line, path, exception, message, null)
        {
            
        }
        
        public ActorLogToken(
            ActorLogLevel level,
            string source_file,
            int source_line,
            ActorPath path,
            Exception? exception,
            string message,
            IEnumerable<object>? items
        )
        {
            Time       = DateTime.UtcNow;
            ThreadId   = Thread.CurrentThread.ManagedThreadId;
            SourceFile = source_file;
            SourceLine = source_line;
            Level      = level;
            Path       = path;
            Exception  = exception;
            Message    = message;
            Items      = new List<object>(items ?? new object[0]);
        }
        
        public string FormatMessage()
        {
            var message = Message;
            if (Items.Count() > 0)
            {
                for (var i = 0; i < Items.Count(); i++)
                {
                    var item = Items.ElementAtOrDefault(i);
                    if (item is null)
                    {
                        break;
                    }
                    message = message.Replace($"{{{i}}}", item.ToString());
                }
            }
            return message;
        }
        
        public override string ToString()
        {
            var message = FormatMessage();
            var log_message = $"[{Time}][{Level}][{Path}]: {message}";
            if (Exception is { })
            {
                log_message = $"{log_message} {Exception.ToString()}";
            }
            return log_message;
        }
    }
    
    public class ActorLogger
    {
        private Actor Owner { get; set; }
        public static event Action<ActorLogToken> OnLog;
        
        private static bool default_logger_enabled;
        public static bool DefaultLoggerEnabled
        {
            get => default_logger_enabled;
            set
            {
                // prevent the logger from being added multiple times
                // if the user sets it to true multiple times.
                if (value && !default_logger_enabled)
                {
                    OnLog += DefaultLogger;
                }
                else if (!value && default_logger_enabled)
                {
                    OnLog -= DefaultLogger;
                }
                default_logger_enabled = value;
            }
        }
        
        static ActorLogger()
        {
            OnLog = delegate { };
        }
        
        public ActorLogger(Actor owner)
        {
            Owner = owner;
            
        }
        
        private static void DefaultLogger(ActorLogToken token)
        {
            switch (token.Level)
            {
                case ActorLogLevel.Info:
                    
                    break;
                case ActorLogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case ActorLogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case ActorLogLevel.Fatal:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case ActorLogLevel.Debug:
                    
                    break;
                case ActorLogLevel.Verbose:
                    
                    break;
            }
            Console.WriteLine(token.ToString());
            Console.ResetColor();
        }
        
        private void Construct(
            ActorLogLevel level,
            string source_file,
            int source_line,
            Exception? exception,
            string message,
            IEnumerable<object>? items
        )
        {
            var token = new ActorLogToken(level, source_file, source_line, Owner.Path, exception, message, items);
            OnLog.Invoke(token);
        }
        
        // --- INFO --- //
        
        public void Info(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Info, source_file, source_line, null, message, null);
        }
        
        public void Info(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Info, source_file, source_line, exception, message, null);
        }
        
        public void Info(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Info, source_file, source_line, null, message, items);
        }
        
        public void Info(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Info, source_file, source_line, exception, message, items);
        }
        
        // --- WARNING --- //
        
        public void Warning(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Warning, source_file, source_line, null, message, null);
        }
        
        public void Warning(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Warning, source_file, source_line, exception, message, null);
        }
        
        public void Warning(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Warning, source_file, source_line, null, message, items);
        }
        
        public void Warning(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Warning, source_file, source_line, exception, message, items);
        }
        
        // --- ERROR --- //
        
        public void Error(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Error, source_file, source_line, null, message, null);
        }
        
        public void Error(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Error, source_file, source_line, exception, message, null);
        }
        
        public void Error(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Error, source_file, source_line, null, message, items);
        }
        
        public void Error(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Error, source_file, source_line, exception, message, items);
        }
        
        // --- FATAL --- //
        
        public void Fatal(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Fatal, source_file, source_line, null, message, null);
        }
        
        public void Fatal(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Fatal, source_file, source_line, exception, message, null);
        }
        
        public void Fatal(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Fatal, source_file, source_line, null, message, items);
        }
        
        public void Fatal(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Fatal, source_file, source_line, exception, message, items);
        }
        
        // --- DEBUG --- //
        
        [Conditional("DEBUG")]
        public void Debug(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Debug, source_file, source_line, null, message, null);
        }
        
        [Conditional("DEBUG")]
        public void Debug(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Debug, source_file, source_line, exception, message, null);
        }
        
        [Conditional("DEBUG")]
        public void Debug(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Debug, source_file, source_line, null, message, items);
        }
        
        [Conditional("DEBUG")]
        public void Debug(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Debug, source_file, source_line, exception, message, items);
        }
        
        // --- VERBOSE --- //
        
        [Conditional("DEBUG")]
        public void Verbose(
            string message, 
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Verbose, source_file, source_line, null, message, null);
        }
        
        [Conditional("DEBUG")]
        public void Verbose(
            Exception exception,
            string message,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Verbose, source_file, source_line, exception, message, null);
        }
        
        [Conditional("DEBUG")]
        public void Verbose(
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Verbose, source_file, source_line, null, message, items);
        }
        
        [Conditional("DEBUG")]
        public void Verbose(
            Exception exception,
            string message,
            IEnumerable<object> items,
            [CallerFilePath] string source_file = "",
            [CallerLineNumber] int source_line = 0
        )
        {
            Construct(ActorLogLevel.Verbose, source_file, source_line, exception, message, items);
        }
    }
}
