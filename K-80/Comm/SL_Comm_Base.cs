using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{

    /// <summary>
    ///  Purpose: For GL660 High Speed protocol, it must redefine the protocol and let the user easy to use.
    ///  DLL: EPP2USB_DLL_V10.dll
    /// </summary>
    /// 

    internal sealed partial class SSLREGNAME
    {
        internal const string CS_RST_PINCTRL = "CS_RST_PINCTRL";
        internal const string DSX_RGBDSX = "DSX_RGBDSX";
        internal const string DDR3SEL = "DDR3SEL";
        internal const string SPI_RDDUMMYCLK = "SPI_RDDUMMYCLK";
        internal const string SPI_WRCLKH_L = "SPI_WRCLKH_L";
        internal const string SPI_RDCLKH_L = "SPI_RDCLKH_L";
        internal const string CS123SEL = "CS123SEL";
        internal const string I2C_STOP_START = "I2C_STOP_START";
        internal const string I2C_SCKH_L = "I2C_SCKH_L";
        internal const string I2C_RDBYTE_CNT = "I2C_RDBYTE_CNT";
        internal const string INTERFACEMODE = "INTERFACEMODE";
        internal const string RGBMODE_POLARITY = "RGBMODE_POLARITY";
        internal const string RGB_TH = "RGB_TH";
        internal const string RGB_TV = "RGB_TV";
        internal const string RGB_THDS = "RGB_THDS";
        internal const string RGB_TVDS = "RGB_TVDS";
        internal const string RGB_THBP = "RGB_THBP";
        internal const string RGB_TVBP = "RGB_TVBP";
        internal const string RGB_THPW = "RGB_THPW";
        internal const string RGB_TVPW = "RGB_TVPW";
        internal const string DDR3_1ADDR = "DDR3_1ADDR";
        internal const string DDR3_3ADDR = "DDR3_3ADDR";
        internal const string RGB_NORMALDATA = "RGB_NORMALDATA";
        internal const string SSD2828_BANKEN_MODESET = "SSD2828_BANKEN_MODESET";
        internal const string SSD2828_BANKCOLOR_SHUTDOWN = "SSD2828_BANKCOLOR_SHUTDOWN";
        internal const string REG_2828BX_WRRDEN = "REG_2828BX_WRRDEN";
        internal const string REG_2828BX_SPICSX = "REG_2828BX_SPICSX";
        internal const string RGB_TH_2828BX = "RGB_TH_2828BX";
        internal const string RGB_TV_2828BX = "RGB_TV_2828BX";
        internal const string RGB_THDS_2828BX = "RGB_THDS_2828BX";
        internal const string RGB_TVDS_2828BX = "RGB_TVDS_2828BX";
        internal const string RGB_THBP_2828BX = "RGB_THBP_2828BX";
        internal const string RGB_TVBP_2828BX = "RGB_TVBP_2828BX";
        internal const string RGB_THPW_2828BX = "RGB_THPW_2828BX";
        internal const string RGB_TVPW_2828BX = "RGB_TVPW_2828BX";
        internal const string RGB_RGBDSX_2828BX = "RGB_RGBDSX_2828BX";
        internal const string LCD_LED_POWER_SET = "LCD_LED_POWER_SET";
        internal const string DCM_EN_RST = "DCM_EN_RST";
        internal const string DCM_MULTIPLY = "DCM_MULTIPLY";
        internal const string DCM_DIVIDE = "DCM_DIVIDE";
        internal const string PLLEN_RST = "PLLEN_RST";
        internal const string GPIO_DIR = "GPIO_DIR";
        internal const string GPIOA_HB = "GPIOA_HB";
        internal const string GPIOA_LB = "GPIOA_LB";
        internal const string REG_GPIO = "REG_GPIO";
    }

    internal sealed partial class SSLREGADDR
    {
        internal const byte ADDR_WR_MODE = 0x80;
        internal const byte DATA_WR_MODE = 0x81;
        internal const byte DATA_RD_MODE = 0x83;
        internal const byte DDR3_WR_MODE = 0x84;
        internal const byte DDR3_RD_MODE = 0x85;
        internal const byte DCM_REPROG_MODE = 0x8a;
        internal const byte ADDR_WR_MODE_2828 = 0x8b;
        internal const byte DATA_WR_MODE_2828 = 0x8c;
        internal const byte DATA_RD_MODE_2828 = 0x8d;

        internal const byte CS_RST_PINCTRL = 0x90;
        internal const byte DSX_RGBDSX = 0x92;
        internal const byte DDR3SEL = 0x93;
        internal const byte SPI_RDDUMMYCLK = 0x94;
        internal const byte SPI_WRCLKH_L = 0x95;

        internal const byte SPI_RDCLKH_L = 0x96;
        internal const byte CS123SEL = 0x98;
        internal const byte I2C_STOP_START = 0x9b;
        internal const byte I2C_SCKH_L = 0x9c;
        internal const byte I2C_RDBYTE_CNT = 0x9d;

        internal const byte INTERFACEMODE = 0xa0;
        internal const byte RGBMODE_POLARITY = 0xa1;
        internal const byte RGB_TH = 0xa2;
        internal const byte RGB_TV = 0xa3;
        internal const byte RGB_THDS = 0xa4;

        internal const byte RGB_TVDS = 0xa5;
        internal const byte RGB_THBP = 0xa6;
        internal const byte RGB_TVBP = 0xa7;
        internal const byte RGB_THPW = 0xa8;
        internal const byte RGB_TVPW = 0xa9;

        internal const byte DDR3_1ADDR = 0xab;
        internal const byte DDR3_3ADDR = 0xac;
        internal const byte RGB_NORMALDATA = 0xad;
        internal const byte SSD2828_BANKEN_MODESET = 0xb0;
        internal const byte SSD2828_BANKCOLOR_SHUTDOWN = 0xb1;

        internal const byte REG_2828BX_WRRDEN = 0xb2;
        internal const byte REG_2828BX_SPICSX = 0xb3;
        internal const byte RGB_TH_2828BX = 0xb4;
        internal const byte RGB_TV_2828BX = 0xb5;
        internal const byte RGB_THDS_2828BX = 0xb6;

        internal const byte RGB_TVDS_2828BX = 0xb7;
        internal const byte RGB_THBP_2828BX = 0xb8;
        internal const byte RGB_TVBP_2828BX = 0xb9;
        internal const byte RGB_THPW_2828BX = 0xba;
        internal const byte RGB_TVPW_2828BX = 0xbb;

        internal const byte RGB_RGBDSX_2828BX = 0xbc;
        internal const byte LCD_LED_POWER_SET = 0xbd;
        internal const byte DCM_EN_RST = 0xf0;
        internal const byte DCM_MULTIPLY = 0xf1;
        internal const byte DCM_DIVIDE = 0xf2;

        internal const byte PLLEN_RST = 0xf3;
        internal const byte GPIO_DIR = 0xfa;
        internal const byte GPIOA_HB = 0xfb;
        internal const byte GPIOA_LB = 0xfc;
        internal const byte REG_GPIO = 0xff;

    }

    class SL_Comm_Base
    {
        private const uint IOCTL_GPIO_WRITE = 0x8B;
        private const uint IOCTL_EPP_ADDR = 0x83;
        private const byte RWSCANNER_EPPCTL_8BIT = 0x00;
        private const byte RWSCANNER_EPPCTL_16BIT = 0x82;

        public const byte ADDRWRMODE = 0x80;
        public const byte DATAWRMODE = 0x81;
        public const byte DATARDMODE = 0x83;
        public const byte ADDRWRMODE_2828 = 0x8b;
        public const byte DATAWRMODE_2828 = 0x8c;
        public const byte DATARDMODE_2828 = 0x8d;

        //Switch 2808 Register
        public const byte REG_2828_READMODE = 0xfa;

        public static bool Device_Open(ushort Vid, ushort Pid)  {   return Epp2USB.FindScanner(Vid, Pid); }
        public static bool Device_Close(){return Epp2USB.GeneCloseHandle();}
        public static bool SL_AddrWrite(byte Data){return Epp2USB.GLWriteEPPAddressPort(Data);}
        public static bool SL_DataWrite(byte Data){return Epp2USB.GLWriteEPPDataPort(Data);}
        public static byte SL_DataRead(){return Epp2USB.GLReadEPPDataPort();}

        public static byte BrigeSelection = 0x00;

        public static byte SL_DataDummyRd()
        {
            byte value = SL_DataRead();      
            return SL_DataRead();
        }

        public static int SPI_DataWr(byte Data)
        {
            BdgeSel(true);
            SL_AddrWrite(DATAWRMODE_2828);
            SL_DataWrite(Data);
            UnBgeSel();
            return 0;
        }

        public static int SPI_DataWrNoCs(byte Data)
        {
            SL_AddrWrite(DATAWRMODE_2828);
            SL_DataWrite(Data);
            return 0;
        }

        public static int SPI_AddrWrNoCs(byte Data)
        {
            SL_AddrWrite(ADDRWRMODE_2828);
            SL_DataWrite(Data);
            return 0;
        }

        public static int SPI_AddrWr(byte Data)
        {
            BdgeSel(true);
            SL_AddrWrite(ADDRWRMODE_2828);
            SL_DataWrite(Data);
            UnBgeSel();
            return 0;
        }

        /* 
         *  1.Write Data to Register 
         *  2.Switch Mode with 2828 Reg: 0xfa
         *  3.Switch FPGA Read Mode Reg:0x8d
         *  4.Read 
        */
        public static int SPI_ReadReg(byte xRegAddr, ref uint xRegVal, int Num)
        {
            BdgeSel(false);
            if (Num < 1 || Num > 4) return Chip.ERROR_BUFLEN_ERR;
            if (SL_Comm_Base.SPI_AddrWrNoCs(xRegAddr) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (SL_Comm_Base.SPI_AddrWrNoCs(REG_2828_READMODE) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (!SL_AddrWrite(DATARDMODE_2828)) return Chip.ERROR_SPI_RD_FAIL;
            for (int i = 0; i < Num; i++)  xRegVal +=  (uint)SL_DataDummyRd() << (8*i);
            UnBgeSel();
            return Chip.ERROR_RESULT_OK;
        }

        public static int SPI_ReadReg(byte xRegAddr, int Num, ref uint xRegVal)
        {
            xRegVal = 0;
            BdgeSel(false);
            if (Num < 1 || Num > 4) return Chip.ERROR_BUFLEN_ERR;
            if (SL_Comm_Base.SPI_AddrWrNoCs(xRegAddr) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (SL_Comm_Base.SPI_AddrWrNoCs(REG_2828_READMODE) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (!SL_AddrWrite(DATARDMODE_2828)) return Chip.ERROR_SPI_RD_FAIL;
            for (int i = 0; i < Num; i++) xRegVal += (uint)SL_DataDummyRd() << (8 * i);
            UnBgeSel();
            return Chip.ERROR_RESULT_OK;
        }

        public static int SPI_ReadRegNoCs(byte xRegAddr, ref uint xRegVal, int Num)
        {
            xRegVal = 0;
            if (Num < 1 || Num > 4) return Chip.ERROR_BUFLEN_ERR;
            if (SL_Comm_Base.SPI_AddrWrNoCs(xRegAddr) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (SL_Comm_Base.SPI_AddrWrNoCs(REG_2828_READMODE) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (!SL_AddrWrite(DATARDMODE_2828)) return Chip.ERROR_SPI_RD_FAIL;
            for (int i = 0; i < Num; i++) xRegVal += (uint)SL_DataDummyRd() << (8 * i);
            return Chip.ERROR_RESULT_OK;
        }

        public static int SPI_ReadRegNoCs(byte xRegAddr, int Num,ref uint xRegVal)
        {
            xRegVal = 0;
            if (Num < 1 || Num > 4) return Chip.ERROR_BUFLEN_ERR;
            if (SL_Comm_Base.SPI_AddrWrNoCs(xRegAddr) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (SL_Comm_Base.SPI_AddrWrNoCs(REG_2828_READMODE) != 0) return Chip.ERROR_SPI_RD_FAIL;
            if (!SL_AddrWrite(DATARDMODE_2828)) return Chip.ERROR_SPI_RD_FAIL;
            for (int i = 0; i < Num; i++) xRegVal += (uint)SL_DataDummyRd() << (8 * i);
            return Chip.ERROR_RESULT_OK;
        }

        public static int SPI_WriteReg(byte Addr, byte Data)
        {
            byte[] WrData = new byte[4];
            int ret = 0;
            BdgeSel(true);
            WrData[0] = ADDRWRMODE_2828;
            WrData[1] = Addr;
            WrData[2] = DATAWRMODE_2828;
            WrData[3] = Data;
            ret = SL_CommBase_WriteCommand(WrData, WrData.Length);
            UnBgeSel();
            return ret;
        }

        public static int SPI_WriteRegNoCs(byte Addr, byte Data)
        {
            byte[] WrData = new byte[4];
            int ret = 0;
            WrData[0] = ADDRWRMODE_2828;
            WrData[1] = Addr;
            WrData[2] = DATAWRMODE_2828;
            WrData[3] = Data;
            ret = SL_CommBase_WriteCommand(WrData, WrData.Length);
            return ret;
        }


        public static int SPI_WriteRegNoCs(byte Addr, byte Data_H, byte Data_L)
        {
            SL_CommBase_WriteReg(0x8b, Addr);
            SL_CommBase_WriteReg(0x8c, Data_L, Data_H);
            return 0;
        }

        public static int SPI_WriteReg(byte Addr, byte Data_H, byte Data_L)
        {
            BdgeSel(true);
            SL_CommBase_WriteReg(0x8b, Addr);
            SL_CommBase_WriteReg(0x8c, Data_L, Data_H);
            UnBgeSel();
            return 0;
        }

        public static int SL_CommBase_ReadReg(byte xRegBuf, ref byte xRegVal)
        {
            xRegVal = 0;
            bool ret = SL_AddrWrite(xRegBuf);
            if (!ret) return Chip.ERROR_READREG_FAIL;
            xRegVal = SL_DataRead();
            return Chip.ERROR_RESULT_OK;
        }



        public static int SL_CommBase_ReadReg(byte xRegBuf, ref ushort xRegVal)
        {
            xRegVal = 0;
            bool ret = SL_AddrWrite(xRegBuf);
            if (!ret) return Chip.ERROR_READREG_FAIL;
            xRegVal = SL_DataRead();
            return Chip.ERROR_RESULT_OK;
        }

        //xRegVal = (xRegVal << 8) + SL_DataRead();
        public static int SL_CommBase_ReadReg(byte xRegBuf, ref uint xRegVal, int Num)
        {
            xRegVal = 0;
            if (Num < 1 || Num > 4) return Chip.ERROR_BUFLEN_ERR;
            bool ret = SL_AddrWrite(xRegBuf);
            if (!ret) return Chip.ERROR_READREG_FAIL;

            for (int i = 0; i < Num; i++)
                xRegVal = (xRegVal << 8) + SL_DataRead();

            return Chip.ERROR_RESULT_OK;
        }

        //word
        public static int SL_CommBase_ReadReg(byte xRegBuf, byte[] xRegVal, int Num)
        {
            if (Num < 1) return Chip.ERROR_BUFLEN_ERR;
            bool ret = SL_AddrWrite(xRegBuf);
            if (!ret) return Chip.ERROR_READREG_FAIL;
            for (int i = 0; i < Num; i++)
                xRegVal[i] = SL_DataRead();

            return Chip.ERROR_RESULT_OK;
        }

        

        public static int SL_CommBase_MassRead(ref byte[] xferBuf, int xbufLen, bool Mass)
        {
            bool ret = true;
            if (!SL_AddrWrite(DATARDMODE)) return Chip.ERROR_DISDATAWR;
            if (Mass)
            {
                IntPtr ReadBackDatapPtr = Marshal.AllocHGlobal(xbufLen);
                ret = Epp2USB.UsbReadScanner(ReadBackDatapPtr, (uint)xbufLen, RWSCANNER_EPPCTL_8BIT);
                Marshal.Copy(ReadBackDatapPtr, xferBuf, 0, xbufLen); //ReadBackDatapPtr to xferBuf
                Marshal.FreeHGlobal(ReadBackDatapPtr);
            }
            else
            {
                for (int i = 0; i < xbufLen; i++)
                    xferBuf[i] = SL_DataRead();
            }
            return (ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_RESULT_FAIL;
        }

        public static int SL_CommBase_MassRead(uint wCmd, int wCmdLen, ref byte[] xferBuf, int xbufLen)
        {
            if (xferBuf.Length < xbufLen) return Chip.ERROR_BUFLEN_ERR;
            if (wCmdLen < 1 || wCmdLen > 4) return Chip.ERROR_BUFLEN_ERR;
            if (!SL_AddrWrite(ADDRWRMODE)) return Chip.ERROR_DISADDRWR;
            for (int i = wCmdLen; i > 0; i--)
                if (!SL_DataWrite((byte)((wCmd >> (8 * (wCmdLen - 1))) & 0xff))) return Chip.ERROR_DISDATAWR;

            if (!SL_AddrWrite(DATARDMODE)) return Chip.ERROR_DISDATAWR;
            IntPtr ReadBackDatapPtr = Marshal.AllocHGlobal(xbufLen);
            bool ret = Epp2USB.UsbReadScanner(ReadBackDatapPtr, (uint)xbufLen, RWSCANNER_EPPCTL_8BIT);
            Marshal.Copy(ReadBackDatapPtr, xferBuf, 0, xbufLen); //ReadBackDatapPtr to xferBuf
            Marshal.FreeHGlobal(ReadBackDatapPtr);
            return (ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_RESULT_FAIL;
        }

        public static int SL_CommBase_Read(ref uint xferBuf, int xbufLen)
        {
            if (xbufLen > 5) return Chip.ERROR_BUFLEN_ERR;
            if (!SL_AddrWrite(DATARDMODE)) return Chip.ERROR_DISDATAWR;

            for (int i = 0; i < xbufLen; i++)
                xferBuf = (xferBuf << 8) + SL_DataRead();

            return Chip.ERROR_RESULT_OK;
        }

        public static int SL_CommBase_Read(uint wCmd, int wCmdLen, ref uint xferBuf, int xbufLen)
        {
            xferBuf = 0;
            if (wCmdLen < 1 || wCmdLen > 4) return Chip.ERROR_BUFLEN_ERR;
            if (!SL_AddrWrite(ADDRWRMODE)) return Chip.ERROR_DISADDRWR;
            for (int i = wCmdLen; i > 0; i--)
                if (!SL_DataWrite((byte)((wCmd >> (8 * (wCmdLen - 1))) & 0xff))) return Chip.ERROR_DISDATAWR;

            if (!SL_AddrWrite(DATARDMODE)) return Chip.ERROR_DISDATAWR;

            for (int i = 0; i < xbufLen; i++)
                xferBuf = (xferBuf << 8) + SL_DataRead();

            return Chip.ERROR_RESULT_OK;
        }
     
        public static int SL_CommBase_WriteCommand(byte[] xferBuf, int xbufLen)
        {
            if (xferBuf.Length < xbufLen || xbufLen > 64) return Chip.ERROR_BUFLEN_ERR;
            if (xbufLen % 2 == 1) return Chip.ERROR_BUFLEN_ERR;
            IntPtr WriteDataPtr = Marshal.AllocHGlobal(xbufLen);
            Marshal.Copy(xferBuf, 0, WriteDataPtr, xbufLen); // xferBuf to WriteDataPtr
            bool ret = Epp2USB.UsbWriteCommand(WriteDataPtr, xbufLen);
            Marshal.FreeHGlobal(WriteDataPtr);
            return (ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_DISDATAWR;
        }

   
        public static int SL_CommBase_AddrWrite(byte[] wCmd)
        {
          if (!SL_AddrWrite(ADDRWRMODE)) return Chip.ERROR_DISADDRWR;
          foreach (byte Val in wCmd)
              if (!SL_DataWrite(Val)) return Chip.ERROR_DISDATAWR;

          return Chip.ERROR_RESULT_OK;
        }

        public static int SL_CommBase_MassDataWrite(byte[] xferBuf)
        {
            bool Ret = true;
            int xferLen = xferBuf.Length;
            if (!SL_AddrWrite(DATAWRMODE)) return Chip.ERROR_DISADDRWR;
            IntPtr WriteDataPtr = Marshal.AllocHGlobal(xferLen);
            Marshal.Copy(xferBuf, 0, WriteDataPtr, xferLen); // xferBuf to WriteDataPtr
            Ret = Epp2USB.UsbWriteScanner(WriteDataPtr, (uint)xferLen, RWSCANNER_EPPCTL_8BIT);
            Marshal.FreeHGlobal(WriteDataPtr);
            return (Ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_DISDATAWR;
        }

        public static int DDR_MassWrite(uint wCmd, int wCmdLen, byte[] xferBuf, int wDataLen)
        {
            bool ret = true;
            if (wDataLen > xferBuf.Length) return Chip.ERROR_BUFLEN_ERR;
            if (wCmdLen < 1 || wCmdLen > 4) return Chip.ERROR_BUFLEN_ERR;
            IntPtr WriteDataPtr = Marshal.AllocHGlobal(wDataLen);

            for (int i = wCmdLen; i > 0; i--)
                if (!SL_AddrWrite((byte)((wCmd >> (8 * (wCmdLen - 1))) & 0xff))) return Chip.ERROR_DISDATAWR;


            Marshal.Copy(xferBuf, 0, WriteDataPtr, wDataLen); // xferBuf to WriteDataPtr
            ret = Epp2USB.UsbWriteScanner(WriteDataPtr, (uint)wDataLen, RWSCANNER_EPPCTL_8BIT);
            Marshal.FreeHGlobal(WriteDataPtr);
            return (ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_DISDATAWR;
        }

        public static int SL_CommBase_Write(uint wCmd, int wCmdLen, uint wData, int wDataLen)
        {
            bool ret = true;
            if (!SL_AddrWrite(ADDRWRMODE)) return Chip.ERROR_DISADDRWR;
            if (wCmdLen < 0 || wCmdLen > 4) return Chip.ERROR_BUFLEN_ERR;

            for (int i = wCmdLen; i > 0; i--)
                ret = SL_DataWrite((byte)((wCmd >> (8 * (wCmdLen - 1))) & 0xff));

            if (!ret) return Chip.ERROR_DISDATAWR;
            if (wDataLen > 4 || wDataLen < 0) return Chip.ERROR_BUFLEN_ERR;

            if (!SL_AddrWrite(DATAWRMODE)) return Chip.ERROR_DISDATAWR;

            for (int i = wDataLen; i > 0; i--)
                ret = SL_DataWrite((byte)((wData >> (8 * (wDataLen - 1))) & 0xff));

            return (ret == true) ? Chip.ERROR_RESULT_OK : Chip.ERROR_DISDATAWR;
        }

        //FPGA : Write Reg Value by Name
        public static int SL_CommBase_WriteReg(string RegName, uint Val)
        {
            SL_Param_Util PramUtil = new SL_Param_Util();
            if (SL_Param_Util.ListRegInfo == null) return Chip.ERROR_LOOKUPREG_FAL;
            return SL_CommBase_WriteReg(RegName, Val, PramUtil.getWriteNum(RegName));
        }

        //FPGA :Write Reg Value by Name
        public static int SL_CommBase_WriteReg(string RegName, uint Val, int xbufLen)
        {
            uint RegVal = 0;
            int errcode = 0;
            SL_Param_Util PramUtil = new SL_Param_Util();
            if (SL_Param_Util.ListRegInfo == null) return Chip.ERROR_LOOKUPREG_FAL;
            byte RegAddr = PramUtil.getAddrByName(RegName);
            if (SL_CommBase_ReadReg(RegAddr, ref RegVal, xbufLen) == Chip.ERROR_RESULT_OK)
            {
                uint RegData = PramUtil.AutoLoopUpGetData(RegName, RegVal, Val);
                errcode = SL_CommBase_WriteReg(RegAddr, RegData, (uint)xbufLen);
                if (errcode ==0) return Chip.ERROR_RESULT_OK;
            }
            return Chip.ERROR_WRITEREG_FAIL;
        }

        //FPGA :Write Value by Addr 
        public static int SL_CommBase_WriteReg(byte addr, uint Value, uint xbufLen)
        {
            byte[] data = new byte[4];
            bool ret = true;
            int errcode = Chip.ERROR_RESULT_FAIL;
            if (xbufLen < 1) return Chip.ERROR_BUFLEN_ERR;
            for (int i = 0; i < 4; i++)
            {
                data[i] = (byte)(Value & 0xff);
                Value = Value >> 8;
            }

            switch (xbufLen)
            {
                case 1:
                    ret = SL_CommBase_WriteReg(addr, data[0]);
                    break;
                case 2:
                    ret = SL_CommBase_WriteReg(addr, data[1], data[0]);
                    break;
                case 3:
                    ret = SL_CommBase_WriteReg(addr, data[2], data[1], data[0]);
                    break;
                case 4:
                    ret = SL_CommBase_WriteReg(addr, data[3], data[2], data[1], data[0]);
                    break;
            }

            if(ret) errcode = Chip.ERROR_RESULT_OK;
            return errcode;
        }

        public static bool SL_CommBase_WriteReg(byte addr, byte data)  
        {
            SL_AddrWrite(addr);
            SL_DataWrite(data);
            return true;
        }
              
        public static bool SL_CommBase_WriteReg(byte addr, byte data, byte data1)
        {
            SL_AddrWrite(addr);
            SL_DataWrite(data);
            SL_DataWrite(data1);
            return true;
        }

        public static bool SL_CommBase_WriteReg(byte addr, byte data, byte data1, byte data2)
        {
            SL_AddrWrite(addr);
            SL_DataWrite(data);
            SL_DataWrite(data1);
            SL_DataWrite(data2);
            return true;
        }

        public static bool SL_CommBase_WriteReg(byte addr, byte data, byte data1, byte data2, byte data3)
        {
            SL_AddrWrite(addr);
            SL_DataWrite(data);
            SL_DataWrite(data1);
            SL_DataWrite(data2);
            SL_DataWrite(data3);
            return true; 
        }

        public static byte ChipSel()
        {
            return (byte)(~BrigeSelection & 0x11);
        }

        public static int BdgeSel(bool WrEn)
        {
            byte Data = BrigeSelection, Val = 0;
            if(!WrEn) Data = (BrigeSelection == 0x11) ? (byte)0x10 : (byte)BrigeSelection;
            Val = (byte)(~Data & 0x11);
            SL_CommBase_WriteReg(0xb2, Data);
            SL_CommBase_WriteReg(0xb3, Val);
            return 0;
        }

        public static int UnBgeSel()
        {
            SL_AddrWrite(0xb3);
            SL_DataWrite(0x11);
            return 0;
        }
        internal class GL600IOCtrl
        {
            public static byte GpioOE(int g1, int g2, int g3, int g4, int g5, int g6, int g7)
            {
                // Bit 0~6 are mapped to GPIO 1~7, "0"-> input, "1"-> output
                byte x = (byte)((g7 << 6) + (g6 << 5) + (g5 << 4) + (g4 << 3) + (g3 << 2) + (g2 << 1) + g1);
                Epp2USB.GLGpioOE(x);
                return x;
            }

            public static byte GpioWR(int g1, int g2, int g3, int g4, int g5, int g6, int g7)
            {
                // Bit 0~6 are mapped to GPIO 1~7, "0"-> input, "1"-> output
                byte x = (byte)((g7 << 6) + (g6 << 5) + (g5 << 4) + (g4 << 3) + (g3 << 2) + (g2 << 1) + g1);
                Epp2USB.GLGpioWrite(x);
                return x;
            }

            public static int GpioRD()
            {
                // Bit 0~6 are mapped to GPIO 1~7, "0"-> input, "1"-> output
                return Epp2USB.GLGpioRead();
            }
        }


    }


    /*
    * 1.FPGA Reg Table
    */
    internal sealed partial class RegFunc
    {
        //byte RegI2C = 0x80;
        //byte RegSPI = 0x81;
        //byte RegUsb = 0x82;
        //byte RegSATA = 0x83;
        //byte RegSSCI = 0x86;
        //byte RegFiber = 0x87;
    }

    /***********************************************************
    Error Code Define
    ***********************************************************/
    internal sealed partial class Chip
    {
        internal const byte ERROR_RESULT_OK = 0;
        internal const byte ERROR_RESULT_FAIL = 1;
        internal const byte ERROR_BUFLEN_ERR = 2;
        internal const byte ERROR_READREG_FAIL = 3;
        internal const byte ERROR_WRITEREG_FAIL = 4;
        internal const byte ERROR_LOOKUPREG_FAL = 5;
        internal const byte ERROR_DISADDRWR = 6;
        internal const byte ERROR_DISDATAWR = 7;
        internal const byte ERROR_SPI_WR_FAIL = 8;
        internal const byte ERROR_SPI_RD_FAIL = 8;
        /*
        internal const byte ERROR_IVALIDTOKEN = 8;
        internal const byte ERROR_ADDRWR_NOPARAM = 9;
        internal const byte ERROR_DATAWR_NOPARAM = 10;
        internal const byte ERROR_STOREFORMAT_ERR = 11;
        internal const byte ERROR_CMPIMG_NOPARAM = 12;
        internal const byte ERROR_CMPIMG_FILEEXIST = 13;
        */
    }

    public static class Epp2USB
    {
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool FindScanner(ushort vid, ushort pid);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool GeneCloseHandle();

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool GLGpioOE(byte x);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool GLGpioWrite(byte y);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern int GLGpioRead();

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool GLWriteEPPDataPort(byte x);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool GLWriteEPPAddressPort(byte x);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern byte GLReadEPPDataPort();

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbReadScanner(IntPtr buffer, uint len, byte EppCtl);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbReadScanner(ref byte[] buffer, uint len, byte EppCtl);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteScanner(IntPtr buffer, uint len, byte Eppctl);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteScanner(ref byte[] buffer, int len, byte Eppctl);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteCommand(IntPtr buffer, int len);

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool SetFastEPP();

        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD02(byte a1, byte d1);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD04(byte a1, byte d1, byte a2, byte d2);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD06(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD08(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD10(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD12(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5
                                              , byte a6, byte d6);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD14(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5
                                              , byte a6, byte d6, byte a7, byte d7);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD16(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5
                                              , byte a6, byte d6, byte a7, byte d7, byte a8, byte d8);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD18(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5
                                              , byte a6, byte d6, byte a7, byte d7, byte a8, byte d8, byte a9, byte d9);
        [DllImport("EPP2USB_DLL_V12.dll")]
        public static extern bool UsbWriteAD20(byte a1, byte d1, byte a2, byte d2, byte a3, byte d3, byte a4, byte d4, byte a5, byte d5
                                              , byte a6, byte d6, byte a7, byte d7, byte a8, byte d8, byte a9, byte d9, byte a10, byte d10);
    }

}
