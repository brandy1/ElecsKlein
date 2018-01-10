using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    public class Setting
    {
        public static string ExePath = null;
        public static string ExeImgDirPath = null;
        public static string ExeConfigDirPath = null;
        public static string ExeSptDirPath = null;
        public static string ExeSysDirPath = null;
        public static string ExeScopeDirPath = null;
        public static string Exe_WhiSky_ConfigtPath = null;
        public static string ExeSysIniPath = null;
        //Debug Process
        public static bool TxCmd = true;
        public static int T_EveryCmd = 35;
    }



    class SL_IO_Util
    {
        private const byte ASCII_0 = 0x30;
        private const byte ASCII_9 = 0x39;
        private const byte ASCII_x = 0x78;
        private const byte ASCII_a = 0x61;
        private const byte ASCII_f = 0x66;
        private char[] DelimiterDot = { '.' };
        private string[] ImgExtName = { "bmp", "jpg", };
        private string[] TxtExtName = { "csv", "txt", };
        private char[] DelimiterChars = { ' ', ',', ':', '\t' };     
        private char[] SplitExtName = { '.' };
        private string FullFilePath = null;

        public bool OutputDll(string DllFileName, string Resource)
        {
            if (File.Exists(DllFileName)) return true;

            Assembly aObj = Assembly.GetExecutingAssembly();
            Stream sStream = aObj.GetManifestResourceStream(Resource);
            if (sStream != null)
            {
                byte[] bySave = new byte[sStream.Length];
                sStream.Read(bySave, 0, bySave.Length);
                FileStream fsObj = new FileStream(DllFileName, FileMode.CreateNew);
                fsObj.Write(bySave, 0, bySave.Length);
                fsObj.Close();
            }
            else
                return false;
            return true;
        }

        public bool outputTxt(string FileName, string InnerTxt)
        {
            FileInfo file = new FileInfo(FileName);
            StreamWriter sw = file.CreateText();
            sw.Write(InnerTxt);
            sw.Close();
            return true;
        }

        public string GetExtName(string FilePath)
        {
            string ExtName = GetExtensionName(FilePath);
            if (String.IsNullOrEmpty(ExtName)) return ExtName;
            string[] Words = ExtName.Split(SplitExtName);
            if (Words.Length < 1) return null;
            return Words[1];
        }

        public string GetFileName(string FilePath)
        {
            return Path.GetFileName(FilePath);
        }

        public bool isMatchExtName(string DirPath)
        {
            string extName = GetExtName(DirPath).ToLower();
            for(int i =0;i< TxtExtName.Length; i++)
            {
                if (extName == TxtExtName[i])
                    return true;
            }
            for (int i = 0; i < ImgExtName.Length; i++)
            {
                if (extName == ImgExtName[i])
                    return true;
            }
            return false;
        }

        public string SetSptFileName(string SptNamePath)
        {
            string rootName = Path.GetDirectoryName(SptNamePath);
            string ExtensionName = GetExtName(SptNamePath).ToLower();
            string BaseName = null;

            foreach (string extName in ImgExtName)
            {
                if (extName == ExtensionName)
                {
                    BaseName = Setting.ExeImgDirPath;
                    break;
                }
            }

            foreach (string extName in TxtExtName)
            {
                if (extName == ExtensionName)
                {
                    BaseName = Setting.ExeSysDirPath;
                    break;
                }
            }

            if (String.IsNullOrEmpty(rootName))
                return Path.Combine(BaseName, SptNamePath);
            else
                return SptNamePath;
        }

        public void CreateDir(string DirPath)
        {
            if (!System.IO.Directory.Exists(DirPath))
                System.IO.Directory.CreateDirectory(DirPath);
        }

        public bool isDirPath(string DirPath) {return System.IO.Directory.Exists(DirPath);}
        public string getFullFilePath(){return this.FullFilePath;}
        public bool isFileExist(string FilePath) { return System.IO.File.Exists(FilePath); }
        public void FileDelete(string FilePath) { System.IO.File.Delete(FilePath); }

        public bool FileExist(string FileName)
        { 
            string fullPath =  Setting.ExeSptDirPath + "\\" + FileName;
            if (isFileExist(fullPath)) { this.FullFilePath = fullPath; return true; }
            if (isFileExist(FileName)) { this.FullFilePath = FileName; return true; }
            return false;
        }

        public bool FileExist(string FileName, ref string CompletePath)
        {
            string fullPath = Setting.ExeSptDirPath + "\\" + FileName;
            if (isFileExist(FileName)) { CompletePath = FileName; return true; }
            if (isFileExist(fullPath)) { CompletePath = fullPath; return true; }      
            return false;
        }

        public string[] ReadFile(string FileName)
        {
            string innerTxt = null;
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(FileName))
                {
                    // Read the stream to a string, and write the string to the console.
                    innerTxt = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return innerTxt.Split('\n');
        }
        private string GetExtensionName(string FilePath) { return Path.GetExtension(FilePath); }
       
    }
}

