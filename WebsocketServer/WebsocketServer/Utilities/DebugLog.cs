using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer.Utilities
{
    public static class DebugLog
    {
        private static Queue<string[]> msgPendding2Write2File = new Queue<string[]>();

        public static void Run()
        {
            Task.Run(() => 
            {
                try
                {
                    var logFileName = $"Log{DateTime.Now:yyyyMMdd}.txt";
                    while (true)
                    {
                        Thread.Sleep(1000);
                        lock (msgPendding2Write2File)
                        {
                            var count = msgPendding2Write2File.Count;
                            if (count > 0)
                            {
                                // Use StreamWriter to append messages to the log file
                                using (StreamWriter writer = new StreamWriter(logFileName, true))
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        var args = msgPendding2Write2File.Dequeue();
                                        foreach (var message in args)
                                        {
                                            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            });
        }

        public static void Print(params string[] args)
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }
            lock (msgPendding2Write2File)
            {
                msgPendding2Write2File.Enqueue(args);
            }
        }
    }
}
