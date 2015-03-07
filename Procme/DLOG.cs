using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
    class DLOG
    {
        public static StreamWriter File { get; private set; }
        static void Init()
        {
            if (File == null)
            {
                File = new StreamWriter("dlog.txt", true);
                File.AutoFlush = true;
                WriteLine("\r\n\r\n---------- DLOG STARTED - {0, 10}---------", DateTime.Now);
            }
        }

        public static void WriteLine()
        {
            Init();
            Console.WriteLine();
            File.WriteLine();
        }

        public static void WriteLine(string format, params object[] arg)
        {
            Init();
            Console.Write("[{0}] ", DateTime.Now);
            Console.WriteLine(format, arg);
            File.Write("[{0}] ", DateTime.Now);
            File.WriteLine(format, arg);
        }

        public static void WriteLine(byte[] pArr)
        {
            Init();
            string ret = "";
            foreach (byte b in pArr) ret += b.ToString("X2") + " ";

            Console.Write("[{0}] ", DateTime.Now);
            Console.WriteLine(ret);
            File.Write("[{0}] ", DateTime.Now);
            File.WriteLine(ret);
        }

        public static void Write(string format, params object[] arg)
        {
            Init();
            Console.Write(format, arg);
            File.Write(format, arg);
        }

        public static void Write(byte[] pArr)
        {
            Init();
            string ret = "";
            foreach (byte b in pArr) ret += b.ToString("X2") + " ";

            Console.Write(ret);
            File.Write(ret);
        }
    }
