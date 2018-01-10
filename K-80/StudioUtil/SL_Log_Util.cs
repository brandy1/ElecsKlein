using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    class Log
    {
       public static bool OutLog = false;
       public static string FilePath = Setting.ExePath + "\\.log";
       private static string LogFilePath = Setting.ExePath + "\\.log";

       public static void w(string message)
	   {
            if (!OutLog) return;
            SL_IO_Util fileUtil = new SL_IO_Util();
            if(!fileUtil.FileExist(FilePath))
            {
                FilePath = LogFilePath;
            }
            if (string.IsNullOrEmpty(FilePath)) 
			{
               FilePath = Directory.GetCurrentDirectory();
			}
			FileInfo finfo = new FileInfo(FilePath);
			if (finfo.Directory.Exists == false) {
              finfo.Directory.Create();
			}
			string writeString = string.Format("{0:yyyy/MM/dd HH:mm:ss} {1}", 
            DateTime.Now, message) + Environment.NewLine;
			File.AppendAllText(FilePath, writeString, Encoding.Unicode);
		}

        public static void f(string funName, string message)
        {
            if (!OutLog) return;
            SL_IO_Util fileUtil = new SL_IO_Util();
            if (!fileUtil.FileExist(FilePath))
            {
                FilePath = LogFilePath;
            }
            if (string.IsNullOrEmpty(FilePath))
            {
                FilePath = Directory.GetCurrentDirectory();
            }
            FileInfo finfo = new FileInfo(FilePath);
            if (finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }
            string writeString = string.Format("{0:yyyy/MM/dd HH:mm:ss} {1}: {2}",
            DateTime.Now, funName, message) + Environment.NewLine;
            File.AppendAllText(FilePath, writeString, Encoding.Unicode);
        }
    }
}
