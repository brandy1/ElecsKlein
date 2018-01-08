using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public static string Exe_WhiSky_ConfigtPath = null;
        public static string Exe_ELECS_ConfigPath = null;
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
        private string LienChars =  "\r\n" ;
        private char[] SplitExtName = { '.' };
        private string STRHEX = "0x";
        private int HexVal = 0;
        private bool bHex = false;
        private string FullFilePath = null;

        private string GetExtensionName(string FilePath) { return Path.GetExtension(FilePath); }

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

        public bool VerfiyDirPath(string DirPath) {return System.IO.Directory.Exists(DirPath);}
        public string getFullFilePath(){return this.FullFilePath;}
        public bool isFileExist(string FilePath) { return System.IO.File.Exists(FilePath); }
        public void FileDelete(string FilePath) { System.IO.File.Delete(FilePath); }

        public bool FileExist(string FilePath)
        { 
            string fullPath = Setting.ExeSptDirPath + "\\" + FilePath;
            if (isFileExist(fullPath)) { this.FullFilePath = fullPath; return true; }
            if (isFileExist(FilePath)) { this.FullFilePath = FilePath; return true; }
            return false;
        }

        public bool FileExist(string FilePath,ref string FullPath)
        {
            string fullPath = Setting.ExeSptDirPath + "\\" + FilePath;
            if (isFileExist(fullPath)) { FullPath = fullPath; return true; }
            if (isFileExist(FilePath)) { FullPath = FilePath; return true; }
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

        /*Determine the value of the size*/
        public bool isWithinRange(uint Value, uint Low, uint High)
        {
            if (Low <= Value && Value <= High)
                return true;
            else
                return false;
        }

        /*Determine the value of the size*/
        public bool isWithinRange(int Value, int Low, int High)
        {
            if (Low <= Value && Value <= High)
                return true;
            else
                return false;
        }

        /*Determine the value of the size*/
        public bool isWithinRange(float  Value, float Low, float High)
        {
            if (Low <= Value && Value <= High)
                return true;
            else
                return false;
        }

        /*Determine the value of the size*/
        public bool isInnerRange(uint Value, double Low, double High)
        {
            if (Low <= Value && Value < High)
                return true;
            else
                return false;
        }

        /*Determine the value of the size*/
        public bool isInnerRange(uint Value, uint Low, uint High)
        {
            if (Low <= Value && Value < High)
                return true;
            else
                return false;
        }

        /*Determine the value of the size and string to the number*/
        public bool ExamStrAndWithin(string strval,int Low,int Max, ref int Value)
        {
            int Val = 0;
            bool ret = false ;
            if (isStrtoInt(strval, ref Val) && isWithinRange(Val, Low, Max))
                ret = true;
            return ret;
        }

        public bool VerifyStrLength(string strval)
        {
           string str = (bHex) ? STRHEX+HexVal.ToString("X2") : HexVal.ToString();
           if (strval.CompareTo(str) == 0) return true;
           return false;
        }

        /*Determine string to the byte Number*/
        public bool isStrtoByte(string strval, ref byte Value)
        {
            bool ret = true;
            int Val = 0;
            if (strval.Length == 0) return false;
            char[] StrAscii = strval.ToLower().ToCharArray();
            if (StrAscii.Length > 1 && StrAscii[0] == ASCII_0 && StrAscii[1] == ASCII_x)
            {
                if(!VerifyHex(strval.Substring(2).ToLower(), ref Val, true)) return false;
                if (Val < 256)
                    Value = (byte)(Val & 0xff);
                else
                    ret = false;
            }
            else
            {
                if (!byte.TryParse(strval, out Value))
                    ret = false;
            }

            return ret;
        }


        /*Determine string to the float Number*/
        public bool isStrtoFloat(string strval, ref float Value)
        {
            bool ret = true;
            int Val = 0;
            if (strval.Length == 0) return false;
            char[] StrAscii = strval.ToLower().ToCharArray();
            if (StrAscii[0] == ASCII_0 && StrAscii[1] == ASCII_x)
            {
                ret = VerifyHex(strval.Substring(2).ToLower(), ref Val, true);
                Value = Val;
            }
            else
            {
                if (!float.TryParse(strval, out Value))
                    ret = false;
            }

            return ret;
        }


        /*Determine string to the Integer*/
        public bool isStrtoInt(string strval, ref int Value)
        {
            bool ret = bHex = false;
  
            if (strval.Length == 0) return false;
            char[] StrAscii = strval.ToLower().ToCharArray();

            if (StrAscii.Length > 1 && StrAscii[0] == ASCII_0 && StrAscii[1] == ASCII_x)
            {
                bHex = true;
                ret = VerifyHex(strval.Substring(2).ToLower(), ref Value, true);
            }
            else         
                ret = VerifyHex(strval, ref Value, false);

            HexVal = Value;
            return ret;
        }

        /*Determine string to the unsigned integer*/
        public bool isStrtoUInt(string strval, ref uint Value)
        {
            bool ret = true;
            if (strval.Length == 0) return false;
            char[] StrAscii = strval.ToLower().ToCharArray();

            if (StrAscii.Length > 1 && StrAscii[0] == ASCII_0 && StrAscii[1] == ASCII_x)
            { 
                ret = VerifyHex(strval.Substring(2).ToLower(), ref Value, true);
                bHex = true;
            }
            else
                ret = VerifyHex(strval, ref Value, false);
            
            return ret;
        }

        public bool WriteByteToTxt(string FilePath, byte[] Data, bool delFile)
        {
            string Msg = null, TxtFilePath = FilePath;
            if (delFile) new SL_IO_Util().FileDelete(TxtFilePath);
            FileStream fs = new FileStream(TxtFilePath, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
            for (int i = 0; i < Data.Length; i++)
            {
                if (i != 0 && i % 16 == 0)
                {
                    sw.WriteLine(Msg);
                    Msg = null;
                }
                Msg += STRHEX + Data[i].ToString("X2");
                if (i % 16 != 15) Msg += ",\t";
            }
            sw.Write(Msg + LienChars + LienChars);
            sw.Close();
            return true;
        }

        private bool VerifyHex(string strval, ref int Val, bool ishex)
        {
            bool ret = true;
            if (ishex)
            {
                // User input Error Value , "0x"
                if(String.IsNullOrEmpty(strval)) return false;
       
                foreach (char str in strval)
                {
                    if ( !(str >= ASCII_0 && str <= ASCII_9) && !(str >= ASCII_a && str <= ASCII_f))
                        return false;
                }
                ret = Int32.TryParse(strval, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Val);
            }
            else
            {
                foreach (char str in strval)
                {       
                    if (!(str >= ASCII_0 && str <= ASCII_9)) 
                        return false;
                }
                ret = Int32.TryParse(strval, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out Val);
            }
            return true;
        }


        private bool VerifyHex(string strval, ref uint Val, bool ishex)
        {
            bool ret = true;
            if (ishex)
            {
                foreach (char str in strval)
                {
                    if (!(str >= ASCII_0 && str <= ASCII_9) && !(str >= ASCII_a && str <= ASCII_f))
                        return false;
                }
                ret = UInt32.TryParse(strval, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Val);
            }
            else
            {
                foreach (char str in strval)
                {
                    if (!(str >= ASCII_0 && str <= ASCII_9))
                        return false;
                }
                ret = UInt32.TryParse(strval, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out Val);
            }
            return true;
        }   
    }
}

