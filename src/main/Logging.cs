using vilark;

class Log
{
    static private object m_lock = new();
    static private StreamWriter? sw;
    static private int log_level = 0;

    public static void Open(string logName, string logLevel) {
        if (logLevel == "INFO") {
            log_level = 1;
        } else if (logLevel == "DEBUG") {
            log_level = 2;
        } else {
            throw new Exception("Unknown logLevel");
        }

        sw = File.CreateText(logName);
        sw.AutoFlush = true;
    }

    public static void Info(string message) {
        if (log_level >= 1)
            WriteLogMessage(message);
    }

    public static void Debug(string message) {
        if (log_level >= 2)
            WriteLogMessage(message);
    }

    public static void Exception(Exception e) {
        if (log_level >= 1)
            WriteLogMessage("[EXCEPTION] " + e.ToString());
    }

    private static void WriteLogMessage(string message) {
        lock(m_lock) {
            DateTime aDate = DateTime.Now;
            string dt = aDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string tname = Thread.CurrentThread.Name ?? "noname";
            //string tid = Thread.CurrentThread.ManagedThreadId.ToString() ;
            string m = $"{dt} {tname} {message}";
            sw!.WriteLine(m);
        }
    }

}
