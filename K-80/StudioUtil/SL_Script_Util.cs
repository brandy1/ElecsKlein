using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    public class ResultInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int Heigth { get; set; }
        public int Width { get; set; }
    }

    class SL_Script_Util
    {
        private const string WARNING = "WARNING";
        private const string ERRMSG = "ERROR";
        private const string NOPARAMETER = "No Parameter";
        private const string MUCHPARAMETER = "Much Parameter";
        private const string FILENOTEXIST = "File not Exist";
        private const string BMPCMPMATCH = "Image Match";
        private const string BMPCMPNOTMATCH = "Image NOT Match";
        private const string ADDRWRTOKEN = "addrw";
        private const string DATAWRTOKEN = "dataw";
        private const string DATARDTOKEN = "datar";
        private const string IMGCMPTOKEN = "cmp";
        private const string MASSDATARDTOKEN = "bdatar";
        private const string MASSDATAWRTOKEN = "bdataw";
        private const string TXTEXTNAME = "txt";
        private const string BMPEXTNAME = "bmp";
        private const string DELAYTOKEN = "delay";
        private const string DELAYTOKEN_10 = "delay_10";
        private const string STRHEX = "0x";
        private char[] DelimiterChars = { ' ', ',', ':', '\t' };
        private char[] SplitLineChars = { '\n' };
        private string SplitLine = "\r\n";
        private const string ERRFILEDATAMSG = "File and Data can not exist at the same time";
        public List<string> lCmpInfo = new List<string>();
        /*
         * Purpose: Excute Addr Write Function
         * Parameter: lXferData > 0 && lXferFile == 0
         */
        public int ExeAddrWr(int[] lXferValue, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<byte> lXferData)
        {
            //Exmine Parameter
            int ret = 0;
            string FilePath = null;
            if (lXferValue.Length == 0) { Result = false; ErrResult(ADDRWRTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_ADDRWR_NOPARAM; }
            if (lXferFile.Length > 0) { WarnResult(ADDRWRTOKEN, MUCHPARAMETER, ref Msg, Line); }
            if (lXferValue.Length > 0)
                lXferData = VerifyList(lXferValue, 256);
            else
            {
                if (!VerifySpt(lXferFile[0], ref FilePath)) { Result = false; ErrResult(DATAWRTOKEN, FILENOTEXIST, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
                ret = ImageToXfer(FilePath, ref lXferData);
            }
            return ret;
            //ret = SL_Comm_Base.SL_CommBase_AddrWrite(ListToAry(lXferData, 256));
            //return (ret != Chip.ERROR_RESULT_OK) ? Chip.ERROR_RESULT_FAIL : Chip.ERROR_RESULT_OK;
        }

        public int ExeDataWr(int[] lXferValue, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<byte> lXferData)
        {
            int ret = 0;
            string FilePath = null;

            //Exmine Parameter
            if (lXferValue.Length == 0 && lXferFile.Length == 0) { Result = false; ErrResult(DATAWRTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (lXferValue.Length > 0 && lXferFile.Length > 0) { WarnResult(DATAWRTOKEN, ERRFILEDATAMSG, ref Msg, Line); }
            if (lXferValue.Length == 0) { if (!VerifySpt(lXferFile[0], ref FilePath)) { Result = false; ErrResult(DATAWRTOKEN, FILENOTEXIST, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; } }
            if (lXferValue.Length > 0)
                lXferData = VerifyList(lXferValue, 256);
            else
            {
                if (!VerifySpt(lXferFile[0], ref FilePath)) { Result = false; ErrResult(DATAWRTOKEN, FILENOTEXIST, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
                ret = ImageToXfer(FilePath, ref lXferData);
            }
            return ret;
        }

        public int ExeMassDataWr(int[] lXferValue, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<byte> lXferData)
        {
            int ret = 0;
            string FilePath = null;
            if (lXferValue.Length == 0 && lXferFile.Length == 0) { Result = false; ErrResult(MASSDATAWRTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (lXferFile.Length > 2) { WarnResult(MASSDATAWRTOKEN, ERRFILEDATAMSG, ref Msg, Line); }

            //ret = SL_Comm_Base.SL_CommBase_MassDataWrite(ListToAry(lXferData, 256));
            if (lXferValue.Length > 0) { 
                lXferData = VerifyList(lXferValue, 256);
            }
            else
            {
                if (!VerifySpt(lXferFile[0], ref FilePath)) { Result = false; ErrResult(DATAWRTOKEN, FILENOTEXIST, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
                ret = ImageToXfer(FilePath, ref lXferData);
            }

            return ret;
        }

        public int ExeDataRd(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<ResultInfo> lResultInfo)
        {
            int ret = 0;
            SL_IO_Util Util = new SL_IO_Util();
            if (lXferData.Length == 0 && lXferFile.Length == 0) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            string RdPath = (lXferFile.Length != 0) ? lXferFile[0] : null;
            if (Util.GetExtName(RdPath) == BMPEXTNAME && lXferData.Length < 2) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (Util.GetExtName(RdPath) == TXTEXTNAME && lXferData.Length < 1) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            ret = RdDataToFile(lXferData, RdPath, ref lResultInfo, ref Msg, true);
            return (ret != Chip.ERROR_RESULT_OK) ? Chip.ERROR_RESULT_FAIL : Chip.ERROR_RESULT_OK;
        }


        public int ExeGraphCompare(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line)
        {
            Bitmap BmpImage;
            int ret = 0;

            if (lXferData.Length > 0) { WarnResult(IMGCMPTOKEN, NOPARAMETER, ref Msg, Line); }
            if (lXferFile.Length < 3) { Result = false; ErrResult(IMGCMPTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_CMP_NOPARAM; }
            SL_Img_Lib CmpImg = new SL_Img_Lib(lXferFile[0]);

            ret = CmpImg.CompareGrpah(lXferFile[1], ref Msg, ref lCmpInfo);

            if (ret == SystemInfo.ERROR_CMP_FILENOTEXIST)
                WarnResult(IMGCMPTOKEN, FILENOTEXIST, ref Msg, Line);
            else if (ret == SystemInfo.ERROR_CMP_ERROR)
            {
                CmpImg.SaveTxtResult(lXferFile[2], ref lCmpInfo, true);
                BmpImage = CmpImg.getResultBmp();
                BmpImage.Save(lXferFile[2]);
                WarnResult(IMGCMPTOKEN, BMPCMPNOTMATCH, ref Msg, Line);
            }
            else
                WarnResult(IMGCMPTOKEN, BMPCMPMATCH, ref Msg, Line);

            return Chip.ERROR_RESULT_OK;

        }

        
        public int ExeDummyRd(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<ResultInfo> lResultInfo)
        {
            int ret = 0;
            if (lXferData.Length == 0) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }

            ret = RdDataToFile(lXferData, null, ref lResultInfo, ref Msg, false);
            return (ret != Chip.ERROR_RESULT_OK) ? Chip.ERROR_RESULT_FAIL : Chip.ERROR_RESULT_OK;
        }

        public int ExeMassDataRd(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line, ref List<ResultInfo> lResultInfo)
        {
            int ret = 0;
            SL_IO_Util Util = new SL_IO_Util();
            string RdPath = (lXferFile.Length != 0) ? lXferFile[0] : null;
            if (lXferData.Length == 0 && lXferFile.Length == 0) { Result = false; ErrResult(MASSDATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (Util.GetExtName(RdPath) == BMPEXTNAME && lXferData.Length < 2) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (Util.GetExtName(RdPath) == TXTEXTNAME && lXferData.Length < 1) { Result = false; ErrResult(DATARDTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_DATAWR_NOPARAM; }
            if (lXferFile.Length == 0 && lXferData.Length > 1) { WarnResult(DATARDTOKEN, MUCHPARAMETER, ref Msg, Line); }
            ret = MassRdToFile(lXferData, RdPath, ref lResultInfo, ref Msg);
            return (ret != Chip.ERROR_RESULT_OK) ? Chip.ERROR_RESULT_FAIL : Chip.ERROR_RESULT_OK;
        }

        public int ExeSystemDelay(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line)
        {
            if (lXferData.Length == 0) { ErrResult(DELAYTOKEN, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_ADDRWR_NOPARAM; }
            if (lXferData.Length > 1) { WarnResult(DELAYTOKEN, MUCHPARAMETER, ref Msg, Line); }
            SystemDelay(lXferData[0], true);
            return Chip.ERROR_RESULT_OK;
        }

        public int ExeSystemDelay_10(int[] lXferData, string[] lXferFile, ref string Msg, ref bool Result, int Line)
        {
            if (lXferData.Length == 0) { ErrResult(DELAYTOKEN_10, NOPARAMETER, ref Msg, Line); return SystemInfo.ERROR_ADDRWR_NOPARAM; }
            if (lXferData.Length > 1) { WarnResult(DELAYTOKEN_10, MUCHPARAMETER, ref Msg, Line); }
            SystemDelay(lXferData[0], false);
            return Chip.ERROR_RESULT_OK;
        }

        private bool DatafromTxt(string FilePath,ref List<byte> lXferData)
        {
            string line;
            uint value = 0;
            SL_IO_Util strUtil = new SL_IO_Util();
            StreamReader sr = new StreamReader(FilePath, System.Text.Encoding.Default);
            while ((line = sr.ReadLine()) != null)
            {
                string[] words = line.Split(DelimiterChars);
                foreach (string word in words)
                    if (strUtil.isStrtoUInt(word, ref value))
                        lXferData.Add((byte)value);
                
            }
            return true;
        }

        private byte[] RdBytefromTxt(string FilePath)
        {
            string line;
            bool isRight = true;
            uint value = 0;
            SL_IO_Util Util = new SL_IO_Util();
            List<byte> lXferData = new List<byte>();
            StreamReader sr = new StreamReader(FilePath, System.Text.Encoding.Default);
            while ((line = sr.ReadLine()) != null)
            {
                string[] words = line.Split(DelimiterChars);
                foreach (string word in words)
                {
                    isRight = Util.isStrtoUInt(word, ref value);
                    if (isRight) lXferData.Add((byte)value);
                }
            }
            return lXferData.ToArray();
        }

        public bool OrderTxt(byte[] DataAry, ref string Msg)
        {
            for (int i = 0; i < DataAry.Length; i++)
            {
                if (i != 0 && i % 16 == 0)
                    Msg += SplitLineChars[0];

                Msg += STRHEX + DataAry[i].ToString("X2");
                if (i < DataAry.Length - 1)
                    if (i % 16 != 15)
                        Msg += " , ";

            }
            return true;
        }

        public bool VerifySpt(string SptNamePath, ref string FileFullPath)
        {
            SL_IO_Util IoUtil = new SL_IO_Util();
            string rootName = Path.GetDirectoryName(SptNamePath);
            string fileName = Path.GetFileName(SptNamePath);
            if (String.IsNullOrEmpty(rootName))
            {
                string FullPath = Path.Combine(Setting.ExeImgDirPath, fileName);
                if (IoUtil.isFileExist(FullPath))
                {
                    FileFullPath = FullPath;
                    return true;
                }
                FullPath = Path.Combine(Setting.ExeSysDirPath, fileName);
                if (IoUtil.isFileExist(FullPath))
                {
                    FileFullPath = FullPath;
                    return true;
                }
            }
            else
            {
                if (IoUtil.isFileExist(SptNamePath))
                {
                    FileFullPath = SptNamePath;
                    return true;
                }
            }
            return false;
        }

        public bool WrByteToBmp(ResultInfo FileInfo, byte[] rdData, bool delFile)
        {
            string ImgFilePath = FileInfo.FilePath;
            if (delFile) new SL_IO_Util().FileDelete(ImgFilePath);
            SL_Img_Lib bmpUtil = new SL_Img_Lib(ImgFilePath);
            byte[] Data = bmpUtil.ArrageData(FileInfo, rdData);
            Bitmap SaveImage = bmpUtil.CreateBmp(Data, FileInfo.Width, FileInfo.Heigth);
            SaveImage.Save(FileInfo.FilePath, System.Drawing.Imaging.ImageFormat.Bmp);
            return true;
        }

        public bool WrByteToTxt(ResultInfo FileInfo, byte[] Data, bool delFile)
        {
            string Msg = null, TxtFilePath = FileInfo.FilePath;
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

                Msg += STRHEX + Data[i].ToString("X2")+ DelimiterChars[1] + DelimiterChars[3];
            }
            sw.Write(Msg + SplitLine + SplitLine);
            sw.Close();
            return true;
        }

        public byte[] ListToByteAry(List<int> ListValue, int Max)
        {
            List<byte> ListData = new List<byte>();
            foreach (int Value in ListValue)
            {
                if (Value < Max)
                    ListData.Add((byte)Value);
            }
            return ListData.ToArray();
        }

        public List<byte> VerifyList(int[] ListValue, int Max)
        {
            List<byte> ListData = new List<byte>();
            foreach (int Value in ListValue)
            {
                if (Value < Max)
                    ListData.Add((byte)Value);
            }
            return ListData;
        }

        public byte[] ListToAry(int[] ListValue, int Max)
        {
            List<byte> ListData = new List<byte>();
            foreach (int Value in ListValue)
            {
                if (Value < Max)
                    ListData.Add((byte)Value);
            }
            return ListData.ToArray();
        }

        public string DelLineWithMatch(string[] sLines, string Txt)
        {
            string[] sNewLines = new string[sLines.Length-1];
            Array.Copy(sLines, 0, sNewLines, 0, sLines.Length-2);//去到最後一行
            return string.Join("\n", sNewLines);
        }

        private void SystemDelay(int DelayTime, bool isMs)
        {
            if (isMs)
                Thread.Sleep(DelayTime);
            else
                Thread.Sleep(DelayTime * 10);
        }

        private bool DataToFile(byte[] rdData, ResultInfo Result, ref string Msg)
        {
            bool ret = true;
            if (Result.FileType == TXTEXTNAME)
                ret = WrByteToTxt(Result, rdData, false);
            else if (Result.FileType == BMPEXTNAME)
                ret = WrByteToBmp(Result, rdData, false);
            else
                ret = false;
            return ret;
        }

        private int RdDataToFile(int[] lXferData, string RdPath, ref List<ResultInfo> lResultInfo, ref string Msg, bool StroeShw)
        {
            int ret = 0;
            int RdNum = CalculateRdNum(lXferData, RdPath);
            byte[] rdData = new byte[RdNum];
            ret = SL_Comm_Base.SL_CommBase_MassRead(ref rdData, RdNum, false);
            if (ret != Chip.ERROR_RESULT_OK) return ret;

            if (String.IsNullOrEmpty(RdPath))
            {
                if (StroeShw)
                {
                    OrderTxt(rdData, ref Msg);
                    Msg += SplitLineChars[0];
                }
            }
            else
            {
                ResultInfo FileInfo = new ResultInfo();
                FileInfo.FileName = new SL_IO_Util().GetFileName(RdPath);
                FileInfo.FilePath = RdPath;
                FileInfo.FileType = new SL_IO_Util().GetExtName(RdPath);
                FileInfo.Heigth = (lXferData.Length > 0) ? lXferData[0] : 0;
                FileInfo.Width = (lXferData.Length > 1) ? lXferData[1] : 0;
                lResultInfo.Add(FileInfo);
                DataToFile(rdData, FileInfo, ref Msg);
            }
            return Chip.ERROR_RESULT_OK;
        }

        private int MassRdToFile(int[] lXferData, string RdPath, ref List<ResultInfo> lResultInfo, ref string Msg)
        {
            int ret = 0;
            int RdNum = CalculateRdNum(lXferData, RdPath);
            byte[] rdData = new byte[RdNum];
            SL_IO_Util ArrangeOut = new SL_IO_Util();
            ret = SL_Comm_Base.SL_CommBase_MassRead(ref rdData, RdNum, true);
            if (ret != Chip.ERROR_RESULT_OK) return ret;

            if (String.IsNullOrEmpty(RdPath))
            {
                OrderTxt(rdData, ref Msg);
                Msg += SplitLineChars[0];
            }
            else
            {
                ResultInfo FileInfo = new ResultInfo();
                FileInfo.FileName = new SL_IO_Util().GetFileName(RdPath);
                FileInfo.FilePath = RdPath;
                FileInfo.FileType = new SL_IO_Util().GetExtName(RdPath);
                FileInfo.Width = (lXferData.Length > 0) ? lXferData[0] : 0;
                FileInfo.Heigth = (lXferData.Length > 1) ? lXferData[1] : 0;
                lResultInfo.Add(FileInfo);
                DataToFile(rdData, FileInfo, ref Msg);
            }
            return Chip.ERROR_RESULT_OK;
        }

        private int CalculateRdNum(int[] lXferData, string Path)
        {
            int RdNum = 0;
            if (Path != null)
            {
                string ExtName = new SL_IO_Util().GetExtName(Path);
                if (ExtName == BMPEXTNAME)
                    RdNum = lXferData[0] * lXferData[1] * 3 * 2;
                else if (ExtName == TXTEXTNAME)
                    RdNum = lXferData[0] * 2;
            }
            else
                RdNum = (lXferData[0] % 2 == 1) ? lXferData[0] + 1 : lXferData[0];
            return RdNum;
        }
        /*
         * Input : Image Path
         * Output: Image to Binary Array List
         */
        private int ImageToXfer(string RdPath ,ref List<byte> lXferData)
        {
            int ret = 0;
            string ExtName = new SL_IO_Util().GetExtName(RdPath);
            if (ExtName.ToLower().ToString() == TXTEXTNAME)
                DatafromTxt(RdPath, ref lXferData);
            else if (ExtName.ToLower().ToString() == BMPEXTNAME)
                new SL_Img_Lib(RdPath).BmpToList(ref lXferData);
            else
                ret = SystemInfo.ERROR_IVALIDTOKEN;

            return ret;
        }
        /*
        private int ImageToXferData(string RdPath, ref string Msg, bool Mass)
                {
                    int ret = 0;
                    string  ExtName = new SL_IO_Util().GetExtName(RdPath);
                    if (ExtName.ToLower().ToString() == TXTEXTNAME)
                        ret = SL_Comm_Base.SL_CommBase_MassDataWrite(ReadBytefromTxt(RdPath), Mass);
                    else if (ExtName.ToLower().ToString() == BMPEXTNAME)
                        ret = SL_Comm_Base.SL_CommBase_MassDataWrite(new SL_Img_Lib(RdPath).BmpToArray(), Mass);
                    else
                        ret = SystemInfo.ERROR_IVALIDTOKEN;

                    return ret;
                }
        */
        public void ErrResult(string MarkToken, string Info, ref string Msg, int Line)
        {
            Msg += ERRMSG + " : " + MarkToken + " " + Info + "( Line: " + (Line + 1) + ")" + SplitLine;
        }

        public void WarnResult(string MarkToken, string Result, ref string Msg, int Line)
        {
            Msg += WARNING + " : " + MarkToken + " " + Result + "( Line: " + (Line + 1) + ")" + SplitLine; ;
        }
    }
}
