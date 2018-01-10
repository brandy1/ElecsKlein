using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace SL_Tek_Studio_Pro
{

    class SL_Digital_Util
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
        private string LienChars = "\r\n";
        private char[] SplitExtName = { '.' };
        private string STRHEX = "0x";
        private int HexVal = 0;
        private bool bHex = false;
 

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
        public bool isWithinRange(float Value, float Low, float High)
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
        public bool ExamStrAndWithin(string strval, int Low, int Max, ref int Value)
        {
            int Val = 0;
            bool ret = false;
            if (isStrtoInt(strval, ref Val) && isWithinRange(Val, Low, Max))
                ret = true;
            return ret;
        }

        public bool VerifyStrLength(string strval)
        {
            string str = (bHex) ? STRHEX + HexVal.ToString("X2") : HexVal.ToString();
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
                if (!VerifyHex(strval.Substring(2).ToLower(), ref Val, true)) return false;
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
                if (String.IsNullOrEmpty(strval)) return false;

                foreach (char str in strval)
                {
                    if (!(str >= ASCII_0 && str <= ASCII_9) && !(str >= ASCII_a && str <= ASCII_f))
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
