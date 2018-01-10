using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace SL_Tek_Studio_Pro
{

    /***********************************************************
    Error Code Define
    ***********************************************************/
    internal sealed partial class SystemInfo
    {
        internal const byte ERROR_RESULT_OK = 0;
        internal const byte ERROR_IVALIDTOKEN = 1;
        internal const byte ERROR_ADDRWR_NOPARAM = 2;
        internal const byte ERROR_DATAWR_NOPARAM = 3;
        internal const byte ERROR_STOREFORMAT_ERR = 4;
        internal const byte ERROR_CMP_NOPARAM = 5;
        internal const byte ERROR_CMP_FILENOTEXIST = 6;
        internal const byte ERROR_CMP_MATCH = 7;
        internal const byte ERROR_CMP_ERROR = 8;
    }

    class SL_Img_Lib
    {
        private string ImgPath = null;
        private Bitmap ResultBmp = null;
        private char[] SplitLineChars = { '\n', '\t' };
        private char[] LineChars = { '\t', ',', '(', ')' };
        private const string FILENOTEXIST = "File not Exist\n";
        private const string BMPCMPMATCH = "BMP COMPARE MATCH";
        public SL_Img_Lib()
        {
            ImgPath = null;
        }
        public SL_Img_Lib(string _path)
        {
            ImgPath = _path;
        }

        public void SetImagePath(string bmpPath)
        {
            this.ImgPath = bmpPath;
        }

        public Bitmap OriginalBmp()
        {
            Bitmap bmp = new Bitmap(ImgPath);
            return bmp;
        }

        public bool  isFileExist(string ImagePath)
        {
            SL_IO_Util imgUtil = new SL_IO_Util();
            string fullPath = Setting.ExeImgDirPath + "\\" +ImagePath;
            if (imgUtil.isFileExist(ImagePath)) { this.ImgPath = ImagePath; return true; }
            if (imgUtil.isFileExist(fullPath)) { this.ImgPath = fullPath; return true; }        
            return false;
        }

        public bool BmpToList(ref List<byte>XferData)
        {
            byte temp = 0;
            PixelFormat pxf = PixelFormat.Format24bppRgb;
            Bitmap bmp = new Bitmap(ImgPath);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
               pxf);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            //bgr
            for (int x = 0; x < rgbValues.Length; x = x + 3)
            {
                temp = rgbValues[x];
                rgbValues[x] = rgbValues[x + 2];
                rgbValues[x + 2] = temp;

                XferData.Add(rgbValues[x]);
                XferData.Add(rgbValues[x+1]);
                XferData.Add(rgbValues[x+2]);
            }

            return true;
        }


        public byte[] BmpToArray()
        {
            byte temp = 0;
            PixelFormat pxf = PixelFormat.Format24bppRgb;
            Bitmap bmp = new Bitmap(ImgPath);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
               pxf);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            //bgr
            for (int x = 0; x < rgbValues.Length; x = x + 3)
            {
                temp = rgbValues[x];
                rgbValues[x] = rgbValues[x + 2];
                rgbValues[x + 2] = temp;
            }

            return rgbValues;
        }

        public Bitmap getResultBmp()
        {
            return (ResultBmp != null) ? ResultBmp : null;
        }

        public int CompareGrpah(string CmpGraphFile, ref string SysMsg, ref List<string> lCmpInfo)
        {
            int ret = SystemInfo.ERROR_RESULT_OK;
            string AddrTmp = null;
            SL_IO_Util FileVerfiy = new SL_IO_Util();
            if (!FileVerfiy.isFileExist(ImgPath)) return SystemInfo.ERROR_CMP_FILENOTEXIST;
            if (!FileVerfiy.isFileExist(CmpGraphFile)) return SystemInfo.ERROR_CMP_FILENOTEXIST;
            Bitmap DefaultBmp = new Bitmap(ImgPath);
            Bitmap CmpBmp = new Bitmap(CmpGraphFile);
            Rectangle DefaultRect = new Rectangle(0, 0, DefaultBmp.Width, DefaultBmp.Height);
            Rectangle CmpRect = new Rectangle(0, 0, CmpBmp.Width, CmpBmp.Height);

            System.Drawing.Imaging.BitmapData DefaultBmpData =
                DefaultBmp.LockBits(DefaultRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, DefaultBmp.PixelFormat);

            System.Drawing.Imaging.BitmapData CmpBmpData =
                CmpBmp.LockBits(CmpRect, System.Drawing.Imaging.ImageLockMode.ReadWrite, CmpBmp.PixelFormat);

            // Get the address of the first line.
            IntPtr DefualtPtr = DefaultBmpData.Scan0;
            IntPtr CmpPtr = CmpBmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int DefaultBytes = Math.Abs(DefaultBmpData.Stride) * DefaultBmp.Height;
            int CmpBytes = Math.Abs(CmpBmpData.Stride) * CmpBmpData.Height;
            int ResultBytes = Math.Abs(DefaultBmpData.Stride) * DefaultBmpData.Height;

            byte[] DrgbValues = new byte[DefaultBytes];
            byte[] CrgbValues = new byte[CmpBytes];
            byte[] RrgbValues = new byte[ResultBytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(DefualtPtr, DrgbValues, 0, DefaultBytes);
            System.Runtime.InteropServices.Marshal.Copy(CmpPtr, CrgbValues, 0, CmpBytes);

            // Set every third value to 255. A 24bpp bitmap will look red.  
            for (int Counter = 0; Counter < DrgbValues.Length; Counter += 3)
            {
                if (DrgbValues[Counter] == CrgbValues[Counter] && DrgbValues[Counter + 1] == CrgbValues[Counter + 1]
                    && DrgbValues[Counter + 2] == CrgbValues[Counter + 2])
                        RrgbValues[Counter] = RrgbValues[Counter + 1] = RrgbValues[Counter + 2] = 255;
                else
                {
                    RrgbValues[Counter + 2] = 255;
                    RrgbValues[Counter + 1] = RrgbValues[Counter] = 0;
                    AddrTmp = CountLocation(DefaultBmp.Width, DefaultBmp.Height, Counter);
                    ShowCmpMsg(ref lCmpInfo, AddrTmp, DrgbValues[Counter + 2], DrgbValues[Counter + 1], DrgbValues[Counter],
                         CrgbValues[Counter + 2], CrgbValues[Counter + 1], CrgbValues[Counter]);
                    ret = SystemInfo.ERROR_CMP_ERROR;
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(DrgbValues, 0, DefualtPtr, DefaultBytes);
            System.Runtime.InteropServices.Marshal.Copy(CrgbValues, 0, CmpPtr, CmpBytes);

            // Unlock the bits.
            DefaultBmp.UnlockBits(DefaultBmpData);
            CmpBmp.UnlockBits(CmpBmpData);

            ResultBmp = CreateBmp(RrgbValues, DefaultBmp.Width, DefaultBmp.Height);

            return ret;
        }

        private void ShowCmpMsg(ref List<string> lCmpInfo, string Location, int Dr, int Dg, int Db, int Cr, int Cg, int Cb)
        {
            string Str = Location + LineChars[0] + LineChars[1];
            Str += LineChars[0] + "(" + string.Format("{0,3}", Dr) + LineChars[1] + string.Format("{0,3}", Dg) + LineChars[1] + string.Format("{0,3}", Db) + LineChars[3] + LineChars[0] + LineChars[1];
            Str += LineChars[0] + "(" + string.Format("{0,3}", Cr) + LineChars[1] + string.Format("{0,3}", Cg) + LineChars[1] + string.Format("{0,3}", Cb) + LineChars[3] + LineChars[0];
            lCmpInfo.Add(Str);
        }

        private string CountLocation(int Width, int Height, int Addr)
        {
            int Location = Addr / 3;
            int X = Location % Width;
            int Y = Location / Width;
            return "[" + string.Format("{0,3}", X) + "," + string.Format("{0,3}", Y) + "]";
        }

        public Bitmap ConvertGrayBmp()
        {
            Bitmap bmp = new Bitmap(ImgPath);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            int r = 0, g = 0, b = 0;
            byte color = 0;
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Set every third value to 255. A 24bpp bitmap will look red.  
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                r = rgbValues[counter];
                g = rgbValues[counter + 1];
                b = rgbValues[counter + 2];
                color = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
                rgbValues[counter] = rgbValues[counter + 1] = rgbValues[counter + 2] = color;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public byte[] ArrageData(ResultInfo FileInfo, byte[] rdData)
        {
            int MaxVal = FileInfo.Heigth * FileInfo.Width * 3;
            byte Tmp = 0;
            byte[] bgrColor = new byte[MaxVal];
            for (int i = 0; i < MaxVal; i++)
                bgrColor[i] = rdData[(2 * i) + 1];

            //RGB->BGR
            for (int counter = 0; counter < MaxVal; counter += 3)
            {
                Tmp = bgrColor[counter];
                bgrColor[counter] = bgrColor[counter + 2];    //Blue
                bgrColor[counter + 2] = Tmp; //Red
            }
            return bgrColor;
        }


        public Bitmap CreateBmp(byte[] bgrData, int Width, int Height)
        {
            Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Set every third value to 255. A 24bpp bitmap will look red.  
            for (int counter = 0; counter < rgbValues.Length; counter += 3)
            {
                rgbValues[counter] = bgrData[counter];
                rgbValues[counter + 1] = bgrData[counter + 1];
                rgbValues[counter + 2] = bgrData[counter + 2];
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public bool WriteImgToTxt(string FilePath, byte[] Pixel)
        {
            SL_Digital_Util imgUtil = new SL_Digital_Util();
            string TxtPath = Path.ChangeExtension(FilePath, "txt");
            imgUtil.WriteByteToTxt(TxtPath, Pixel, true);
            return true;
        }

        public bool SaveTxtResult(string FilePath,ref  List<string> lCmpInfo, bool delFile)
        {
            SL_IO_Util imgUtil = new SL_IO_Util();
            string Addr = string.Format("{0,8}", "Addr");
            Addr += "\t,\t";
            Addr += string.Format("{0,12}", "(R,G,B)");
            Addr += "\t,\t";
            Addr += string.Format("{0,12}", "(CR,CG,CB)");
            string TxtPath = Path.ChangeExtension(FilePath, "txt");
            if (lCmpInfo.Count == 0) lCmpInfo.Add(BMPCMPMATCH);
            if (delFile) imgUtil.FileDelete(TxtPath);           
            lCmpInfo.Insert(0, Addr);
            FileStream fs = new FileStream(TxtPath, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
            foreach (string item in lCmpInfo)
                sw.WriteLine(item);
            sw.Close();
            lCmpInfo.Clear();
            return true;
        }

    }
}
