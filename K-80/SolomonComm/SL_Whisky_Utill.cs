#define DEBUG 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    public class SL_WhiskyComm_Util
    {
        private byte VSA = 0, HSA = 0;
        private byte VBP = 0, HBP = 0;
        private byte VFP = 0, HFP = 0;
        private byte HD_H = 0, HD_L = 0;
        private byte VD_H = 0, VD_L = 0;
        private const byte POWERCTRL_ADDR = 0x7c;
        private const double FPGA_OSC = 40.0;
        SL_Comm_Mipi Mipi = new SL_Comm_Mipi();
      
        public bool MipiBridgeSelect(byte SelVal)
        {
            Mipi.MipiBridgeSelect(SelVal);
            return true;
        }

        public bool MipiWrite(byte type, byte data)
        {
            byte[] MipiData = new byte[2];
            MipiData[0] = type;
            MipiData[1] = data;
            return Mipi.MipiWrite(MipiData);
        }

        public bool MipiWrite(byte type, byte data, byte data1)
        {
            byte[] MipiData = new byte[3];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            return Mipi.MipiWrite(MipiData);
        }

        public bool MipiWrite(byte type, byte data, byte data1, byte data2)
        {
            byte[] MipiData = new byte[4];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            MipiData[3] = data2;
            return Mipi.MipiWrite(MipiData);
        }

        public bool MipiWrite(byte type, byte data, byte data1, byte data2, byte data3)
        {  
            byte[] MipiData = new byte[5];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            MipiData[3] = data2;
            MipiData[4] = data3;
            return Mipi.MipiWrite(MipiData);
        }

        public bool MipiWrite(byte[] Mipidata)
        {
            return Mipi.MipiWrite(Mipidata);
        }

        public bool MipiHSWrite(byte type, byte data)
        {
            byte[] MipiData = new byte[2];
            MipiData[0] = type;
            MipiData[1] = data;
            return Mipi.MipiHSWrite(MipiData);
        }

        public bool MipiHSWrite(byte type, byte data, byte data1)
        {
            byte[] MipiData = new byte[3];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            return Mipi.MipiHSWrite(MipiData);
        }

        public bool MipiHSWrite(byte type, byte data, byte data1, byte data2)
        {
            byte[] MipiData = new byte[4];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            MipiData[3] = data2;
            return Mipi.MipiHSWrite(MipiData);
        }

        public bool MipiHSWrite(byte type, byte data, byte data1, byte data2, byte data3)
        {
            byte[] MipiData = new byte[5];
            MipiData[0] = type;
            MipiData[1] = data;
            MipiData[2] = data1;
            MipiData[3] = data2;
            MipiData[4] = data3;
            return Mipi.MipiHSWrite(MipiData);
        }

        public bool MipiHSWrite(byte[] Mipidata)
        {
            return Mipi.MipiHSWrite(Mipidata);
        }

        public bool MipiRead(byte Addr, byte RdNum, ref string RdStr)
        {
            byte RdNumH = 0, RdNumeL = 0, C2_1 = 0, C2_2 = 0, C6_1 = 0, C6_2 = 0, Ready = 0, BTAR = 0, LPTO = 0, DST = 0, CST = 0;
            int i = 0, k = 0, l = 0, j = 2;
            uint Value = 0;
            bool ret = true;
            RdStr = null;

            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x80);
            SL_Comm_Base.SPI_WriteReg(0xbd, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xbc, 0x00, 0x01);

            RdNumH = (byte)(RdNum >> 8);
            RdNumeL = (byte)(RdNum & 0xff);


            SL_Comm_Base.SPI_WriteReg(0xc1, RdNumH, RdNumeL);

            SL_Comm_Base.SPI_AddrWr(0xbf);
            SL_Comm_Base.SPI_DataWr(Addr);
            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc2);
            Thread.Sleep(1);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C2_1 = SL_Comm_Base.SL_DataDummyRd();
            C2_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc6);
            Thread.Sleep(1);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C6_1 = SL_Comm_Base.SL_DataDummyRd();
            C6_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(10);


            CST = (byte)((C6_2 & 0x8) >> 3);
            DST = (byte)((C6_2 & 0x4) >> 2);
            LPTO = (byte)((C6_1 & 0x40) >> 6);
            BTAR = (byte)((C6_1 & 0x04) >> 2);
            Ready = (byte)(C6_1 & 0x01);

            if (LPTO == 1) { SL_Comm_Base.SPI_WriteReg(0xc0, 0x00, 0x01); Thread.Sleep(50); }



            if (Ready == 1)
            {
                SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());

                i = ((C2_2 * 256) + C2_1);

                SL_Comm_Base.SPI_AddrWrNoCs(0xff);

                l = 16 * (1 + i / 16);

                for (k = 0; k < l; k++)
                {
                    if (j == 2)
                    {
                        SL_Comm_Base.SPI_AddrWrNoCs(0xFA);
                        SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
                        j = 0;
                    }

                    Value = SL_Comm_Base.SL_DataDummyRd();

                    if (k < i)
                    {
                        RdStr += "Rd[" + k + "]= 0x" + Convert.ToString(Value, 16) + " ";
                    }
                    j++;
                }

                SL_Comm_Base.UnBgeSel();
                ret = true;
            }
            else
            {
                ret = false;
                RdStr = "Mipi Read Not Ready";
            }
            return ret;
        }

        public bool MipiRead(byte Addr, byte RdNum, ref byte[] RdVal)
        {
            byte RdNumH = 0, RdNumeL = 0, C2_1 = 0, C2_2 = 0, C6_1 = 0, C6_2 = 0, Ready = 0, BTAR = 0, LPTO = 0, DST = 0, CST = 0;
            int i = 0, k = 0, l = 0, j = 2;
            uint Value = 0;
            bool ret = true;

            if (RdVal.Length != RdNum) return false;

            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x80);
            SL_Comm_Base.SPI_WriteReg(0xbd, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xbc, 0x00, 0x01);

            RdNumH = (byte)(RdNum >> 8);
            RdNumeL = (byte)(RdNum & 0xff);

            SL_Comm_Base.SPI_WriteReg(0xc1, RdNumH, RdNumeL);

            SL_Comm_Base.SPI_AddrWr(0xbf);
            SL_Comm_Base.SPI_DataWr(Addr);
            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc2);
            Thread.Sleep(1);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C2_1 = SL_Comm_Base.SL_DataDummyRd();
            C2_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc6);
            Thread.Sleep(1);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C6_1 = SL_Comm_Base.SL_DataDummyRd();
            C6_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(10);

            Ready = (byte)(C6_1 & 0x01);


            CST = (byte)((C6_2 & 0x8) >> 3);
            DST = (byte)((C6_2 & 0x4) >> 2);
            LPTO = (byte)((C6_1 & 0x40) >> 6);
            BTAR = (byte)((C6_1 & 0x04) >> 2);
            Ready = (byte)(C6_1 & 0x01);

            if (LPTO == 1) SL_Comm_Base.SPI_WriteReg(0xc0, 0x00, 0x01);

            if (Ready == 1)
            {
                SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());

                i = ((C2_2 * 256) + C2_1);

                SL_Comm_Base.SPI_AddrWrNoCs(0xff);

                l = 16 * (1 + i / 16);

                for (k = 0; k < l; k++)
                {
                    if (j == 2)
                    {
                        SL_Comm_Base.SPI_AddrWrNoCs(0xFA);
                        SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
                        j = 0;
                    }
                    Value = SL_Comm_Base.SL_DataDummyRd();
                    if (k < i)
                    {
                        RdVal[k] = (byte)Value;
                    }
                    j++;
                }

                SL_Comm_Base.UnBgeSel();
                ret = true;
            }
            else
                ret = false;
            return ret;
        }




        public bool MipiHSRead(byte Addr, byte RdNum, ref string RdStr)
        {
            byte RdNumH = 0, RdNumeL = 0, C2_1 = 0, C2_2 = 0, C6_1 = 0, C6_2 = 0, Ready = 0;
            int i = 0, k = 0, l = 0;
            uint Value = 0;

            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x89);
            SL_Comm_Base.SPI_WriteReg(0xbd, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xbc, 0x00, 0x01);

            RdNumH = (byte)(RdNum >> 8);
            RdNumeL = (byte)(RdNum & 0xff);

            SL_Comm_Base.SPI_WriteReg(0xc1, RdNumH, RdNumeL);

            SL_Comm_Base.SPI_AddrWr(0xbf);
            SL_Comm_Base.SPI_DataWr(Addr);

            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc2);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C2_1 = SL_Comm_Base.SL_DataDummyRd();
            C2_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(20);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());
            SL_Comm_Base.SPI_AddrWrNoCs(0xc6);
            SL_Comm_Base.SPI_AddrWrNoCs(0xfa);
            SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
            C6_1 = SL_Comm_Base.SL_DataDummyRd();
            C6_2 = SL_Comm_Base.SL_DataDummyRd();
            SL_Comm_Base.UnBgeSel();

            Thread.Sleep(20);

            Ready = (byte)(C6_1 & 0x01);
            if (Ready == 1)
            {
                SL_Comm_Base.SL_CommBase_WriteReg(0xb3, SL_Comm_Base.ChipSel());

                i = ((C2_2 * 256) + C2_1);

                SL_Comm_Base.SPI_AddrWrNoCs(0xff);

                l = 16 * (1 + i / 16);

                for (k = 0; k < l; k++)
                {
                    if (k == 0)
                    {
                        SL_Comm_Base.SPI_AddrWrNoCs(0xFA);
                        SL_Comm_Base.SL_AddrWrite(SL_Comm_Base.DATARDMODE_2828);
                    }
                    Value = SL_Comm_Base.SL_DataDummyRd();
                    if (k < i)
                    {
                        RdStr += "Rd[" + k + "]= 0x" + Convert.ToString(Value, 16) + " ";
                    }
                }

                SL_Comm_Base.UnBgeSel();
            }
            return true;
        }

        public bool ImageFill(byte R,byte G, byte B)
        {
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x59);
            SL_Comm_Base.SL_CommBase_WriteReg(0x92, 0x20);
            SL_Comm_Base.SL_CommBase_WriteReg(0xad, R, G, B);
            SL_Comm_Base.SL_CommBase_WriteReg(0xbc, 0x00);
            return true;
        }

        public bool SetFpgaTiming(byte BankEn, byte BankWrRd, byte Interface, byte RgbMode, byte DcmMul, byte DcmDiv)
        {
            return SetBoardTiming( BankEn,  BankWrRd,  Interface,  RgbMode,  DcmMul,  DcmDiv);
        }

        public bool SetFpgaTiming(byte BankEn, byte BankWrRd, byte Interface, byte RgbMode, byte DcmMul, byte DcmDiv,ref string RdStr)
        {
            double Value = 0;
            bool ret = SetBoardTiming(BankEn, BankWrRd, Interface, RgbMode, DcmMul, DcmDiv); 
            Value = FPGA_OSC * DcmMul / DcmDiv;
            RdStr = "DCM Freq: " + Value.ToString() + "M";
            Thread.Sleep(2);
            return ret;
        }

        public bool SetMipiDsi(int LaneCout, int MipiSpeed, string Mode)
        {
            double HS = Convert.ToDouble(MipiSpeed) / 10;
            int LaneNum = Convert.ToInt32(LaneCout);
            byte Lane = 0, LpVal = 0, FR = 0;

            if (HS > 6.25 && HS <= 12.5) FR = 0x02;
            if (HS > 12.6 && HS <= 25) FR = 0x42;
            if (HS > 25.1 && HS <= 50) FR = 0x82;
            if (HS > 50.1) FR = 0xC2;

            Lane = (LaneNum >= 1 && LaneNum <= 4) ? (byte)(LaneNum - 1) : (byte)0x03;
            LpVal = (byte)(MipiSpeed / 8 / 6);

            SL_Comm_Base.SPI_WriteReg(0xb9, 0x00, 0x00); //PLL disable
            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00); //VC(Virtual ChannelID) Control Register
            SL_Comm_Base.SPI_WriteReg(0xde, 0x00, Lane); //DSI lane setting,0x00:1 data lane ,0x01:2 data lane 0x02:3 data lane ,0x03:4 data lane

            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x50);
            SL_Comm_Base.SPI_WriteReg(0xba, FR, (byte)HS);
            SL_Comm_Base.SPI_WriteReg(0xbb, 0x00, LpVal);
            SL_Comm_Base.SPI_WriteReg(0xb9, 0x00, 0x01);

            SL_Comm_Base.SPI_WriteReg(0xd6, 0x00, 0x05);

            if (Mode.CompareTo("syncpulse") == 0)
            {
                SL_Comm_Base.SPI_WriteReg(0xb1, VSA, HSA);    //VICR1=> VSA,HSA
                SL_Comm_Base.SPI_WriteReg(0xb2, VBP, HBP);    //VICR2=> VBP,HBP
                SL_Comm_Base.SPI_WriteReg(0xb3, VFP, HFP);    //VICR3=> VFP,HFP
                SL_Comm_Base.SPI_WriteReg(0xb4, HD_H, HD_L);  //VICR4=> HACT HDISP
                SL_Comm_Base.SPI_WriteReg(0xb5, VD_H, VD_L);  //VICR5=> VACT VDISP

                // -----------clk lane in HS,when no data send--------------
                SL_Comm_Base.SPI_WriteReg(0xb6, 0x00, 0x03);  // 24bit Non burst mode with Sync pulses
                                                              //SSD2825_spi_reg_data_wr(0xb6,0x00,0x02);	// 18bit Non burst mode with Sync pulses, loosely packed
                                                              //SSD2825_spi_reg_data_wr(0xb6,0x00,0x01);	// 18bit Non burst mode with Sync pulses, packed                                                           //SSD2825_spi_reg_data_wr(0xb6,0x00,0x00);	// 16bit Non burst mode with Sync pulses
            }

            if (Mode.CompareTo("burst") == 0)
            {
                SL_Comm_Base.SPI_WriteReg(0xb1, VSA, HSA);    //VICR1=> VSA,HSA
                SL_Comm_Base.SPI_WriteReg(0xb2, (byte)(VBP + VSA), (byte)(HBP + HSA));    //VICR2=> VBP,HBP (BP+SA)
                SL_Comm_Base.SPI_WriteReg(0xb3, VFP, HFP);    //VICR3=> VFP,HFP
                SL_Comm_Base.SPI_WriteReg(0xb4, HD_H, HD_L);  //VICR4=> HACT HDISP
                SL_Comm_Base.SPI_WriteReg(0xb5, VD_H, VD_L);  //VICR5=> VACT VDISP  
                SL_Comm_Base.SPI_WriteReg(0xb6, 0x00, 0x1b);	// 0x1b , bit5 : 0: continue clock ,1: non continue clock
            }

            if (Mode.CompareTo("syncevent") == 0)
            {
                SL_Comm_Base.SPI_WriteReg(0xb1, VSA, HSA);    //VICR1=> VSA,HSA
                SL_Comm_Base.SPI_WriteReg(0xb2, (byte)(VBP + VSA), (byte)(HBP + HSA));    //VICR2=> VBP,HBP (BP+SA)
                SL_Comm_Base.SPI_WriteReg(0xb3, VFP, HFP);    //VICR3=> VFP,HFP
                SL_Comm_Base.SPI_WriteReg(0xb4, HD_H, HD_L);  //VICR4=> HACT HDISP
                SL_Comm_Base.SPI_WriteReg(0xb5, VD_H, VD_L);  //VICR5=> VACT VDISP

                // -----------clk lane in HS,when no data send--------------
                SL_Comm_Base.SPI_WriteReg(0xb6, 0x00, 0x07);	//0x03,bit5 
            }

            return true;
        }

        public bool SetMipiVideo(int vLanes, int hPixels, byte framerate, byte vbp, byte vfp, byte hbp, byte hfp, byte vsa, byte hsa)
        {
            byte HT_H = 0, HT_L = 0, VT_H = 0, VT_L = 0;
            int Value = 0;
            uint Val = 0;
 

            Value = hPixels + hsa + hbp + hfp;

            HT_H = (byte)(Value >> 8);
            HT_L = (byte)(Value & 0xff);

            HD_H = (byte)(hPixels >> 8);
            HD_L = (byte)(hPixels & 0xff);

            Value = vLanes + vsa + vbp + vfp;
            VT_H = (byte)(Value >> 8);
            VT_L = (byte)(Value & 0xff);

            VD_H = (byte)(vLanes >> 8);
            VD_L = (byte)(vLanes & 0xff);

            VBP = (byte)(vbp & 0xff);
            VFP = (byte)(vfp & 0xff);
            HBP = (byte)(hbp & 0xff);
            HFP = (byte)(hfp & 0xff);
            VSA = (byte)(vsa & 0xff);
            HSA = (byte)(hsa & 0xff);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb4, HT_H, HT_L, HT_H, HT_L);    //TH
            SL_Comm_Base.SL_CommBase_WriteReg(0xb6, HD_H, HD_L, HD_H, HD_L);    //TH
            SL_Comm_Base.SL_CommBase_WriteReg(0xb8, 0x00, HBP, 0x00, HBP);
            SL_Comm_Base.SL_CommBase_WriteReg(0xba, HSA, HSA);
            SL_Comm_Base.SL_CommBase_WriteReg(0xb5, VT_H, VT_L, VT_H, VT_L);
            SL_Comm_Base.SL_CommBase_WriteReg(0xb7, VD_H, VD_L, VD_H, VD_L);
            SL_Comm_Base.SL_CommBase_WriteReg(0xb9, 0x00, VBP, 0x00, VBP);
            SL_Comm_Base.SL_CommBase_WriteReg(0xbb, VSA, VSA);
            SL_Comm_Base.SL_CommBase_WriteReg(0xad, 0x00, 0x00, 0x00);//Normal Data

            /*2828 Setting*/
            SL_Comm_Base.SPI_WriteReg(0xb1, VSA, HSA);
            SL_Comm_Base.SPI_WriteReg(0xb2, VBP, HBP);
            SL_Comm_Base.SPI_WriteReg(0xb3, VFP, HFP);
            SL_Comm_Base.SPI_WriteReg(0xb4, VD_H, VD_L);
            SL_Comm_Base.SPI_WriteReg(0xb5, HD_H, HD_L);

#if (DEBUG)
            SL_Comm_Base.SL_CommBase_ReadReg(0xb4, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xb5, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xb6, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xb7, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xb8, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xb9, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xba, ref Val, 2);
            SL_Comm_Base.SL_CommBase_ReadReg(0xbb, ref Val, 2);
#endif
            return true;

        }



        public bool ImageShow(string FilePath)
        {
            SL_Img_Lib ImgLib = new SL_Img_Lib();
            if (!ImgLib.isFileExist(FilePath)) return false; 
            return ImageWrite(ImgLib);
        }

        public bool ImageShow(string FilePath,ref string RdStr)
        {
            SL_Img_Lib ImgLib = new SL_Img_Lib();
            if (!ImgLib.isFileExist(FilePath)) { RdStr = "File Not Exits"; return false; }
            return ImageWrite(ImgLib);
        }

        public bool PowerCtrl(byte Enable, byte chipAddr, byte i2cData)
        {
            SL_Comm_Base.SL_CommBase_WriteReg(0x9c, 0x32, 0x32);//i2c 400k
            SL_Comm_Base.SL_CommBase_WriteReg(0x9d, 0x00, 0x00, 0x01);//i2c read count
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, 0x00); //pmic disable
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, Enable); //pmic access enable
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x00, 0x02); //i2c start
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, POWERCTRL_ADDR); //slave addr,wr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, chipAddr); // addr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, i2cData); //data
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x00, 0x01); //i2c stop
            return true;
        }

        public bool i2cRead(byte i2c_W_Addr, byte ICAddr,byte Rdnum, ref string RdStr)
        {
            byte outPort = 0;
            byte i2c_R_Addr = (byte)(i2c_W_Addr + 1) ;
            SL_Comm_Base.SL_CommBase_ReadReg(0xa0, ref outPort);
            SL_Comm_Base.SL_CommBase_WriteReg(0xa0, 0x20);//i2c output
            SL_Comm_Base.SL_CommBase_WriteReg(0x9c, 0x32, 0x32);//i2c 400k
            SL_Comm_Base.SL_CommBase_WriteReg(0x9d, 0x00, 0x00, Rdnum);//i2c read count
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);//i2c Start
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, i2c_W_Addr);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, ICAddr);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);//i2c stop

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);//i2c Start
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, i2c_R_Addr);
            SL_Comm_Base.SL_AddrWrite(0x83);
            for (int i = 0; i < Rdnum; i++) { RdStr += "0x" + Convert.ToString(SL_Comm_Base.SL_DataDummyRd(), 16); }
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);//i2c stop
            SL_Comm_Base.SL_CommBase_WriteReg(0xa0, outPort);
            return true;
        }

        public bool pmicWrite(byte Enable,  byte ChipReg, byte Data,ref string RdStr)
        {
            SL_Comm_Base.SL_CommBase_WriteReg(0x9c, 0x32, 0x32);//i2c 400k
            SL_Comm_Base.SL_CommBase_WriteReg(0x9d, 0x00, 0x00, 0x01);//i2c read count
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, 0x00); //pmic disable
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, Enable); //pmic access enable
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02); //i2c start
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, 0x7c); //slave addr,wr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, ChipReg); // addr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, Data); //data
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01); //i2c stop
            return true;
        }

        public bool pmicWrite(byte Enable, byte ChipReg, byte Data)
        {
            SL_Comm_Base.SL_CommBase_WriteReg(0x9c, 0x32, 0x32);//i2c 400k
            SL_Comm_Base.SL_CommBase_WriteReg(0x9d, 0x00, 0x00, 0x01);//i2c read count
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, 0x00); //pmic disable
            SL_Comm_Base.SL_CommBase_WriteReg(0xbd, Enable); //pmic access enable
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02); //i2c start
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, 0x7c); //slave addr,wr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, ChipReg); // addr
            SL_Comm_Base.SL_CommBase_WriteReg(0x81, Data); //data
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01); //i2c stop
            return true;
        }

        public bool i2cWrite(byte i2cAddr,byte data,byte data1)
        {
            byte[] i2cdata = new byte[2];
            i2cdata[0] = data;
            i2cdata[1] = data1;
            return i2cWrite(i2cAddr, i2cdata);
        }

        public bool i2cWrite(byte i2cAddr, byte data, byte data1, byte data2)
        {
            byte[] i2cdata = new byte[3];
            i2cdata[0] = data;
            i2cdata[1] = data1;
            i2cdata[2] = data1;
            return i2cWrite(i2cAddr, i2cdata);
        }

        public bool i2cWrite(byte i2cAddr, byte[] i2cData)
        {
            byte outPort = 0;
            SL_Comm_Base.SL_CommBase_ReadReg(0xa0, ref outPort);
            SL_Comm_Base.SL_CommBase_WriteReg(0xa0, 0x20);//i2c output
            SL_Comm_Base.SL_CommBase_WriteReg(0x9c, 0x32, 0x32);//i2c 400k
            SL_Comm_Base.SL_CommBase_WriteReg(0x9d, 0x00, 0x00, 0x01);//i2c read count
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);//i2c Start
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, i2cAddr);
            foreach (byte Data in i2cData) SL_Comm_Base.SL_CommBase_WriteReg(0x81, Data);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);//i2c Start
            SL_Comm_Base.SL_CommBase_WriteReg(0xa0, outPort);
            return true;
        }

        public bool UsbCmdWrite(byte[] xferData)
        {
            SL_Comm_Base.SL_CommBase_WriteCommand(xferData, xferData.Length);
            return true;
        }

        public bool GpioCtrl(byte Direct, byte G_Hb, byte G_Lb)
        {
            if (!(Direct == 0x00 || Direct == 0x01 || Direct == 0x10 || Direct == 0x11)) Direct = 0x11;
            SL_Comm_Base.SL_CommBase_WriteReg(0xfa, Direct);
            SL_Comm_Base.SL_CommBase_WriteReg(0xfb, G_Hb);
            SL_Comm_Base.SL_CommBase_WriteReg(0xfc, G_Lb);
            return true;
        }

        public bool BridgeRead(byte addr, byte RdNum ,ref string RdStr)
        {
            uint xferValue = 0;
            int ret  =  SL_Comm_Base.SPI_ReadReg(addr, RdNum, ref xferValue);
            RdStr = "0x" + Convert.ToString(xferValue, 16);
            return (ret == 0) ? true : false;
        }

        public bool BridgeWrite(byte Addr ,byte Data)
        {
            int ret = SL_Comm_Base.SPI_WriteReg(Addr, Data);
            return (ret == 0) ? true : false;
        }

        private bool SetBoardTiming(byte BankEn, byte BankWrRd, byte Interface, byte RgbMode, byte DcmMul, byte DcmDiv)
        {
            uint Val = 0;
            SL_Comm_Base.SL_CommBase_WriteReg(0x92, 0x20); //Default(CPU/SPI From EPP, RGB from SRAM) , In_Data_Source
            SL_Comm_Base.SL_CommBase_WriteReg(0xa0, Interface);
            SL_Comm_Base.SL_CommBase_WriteReg(0xa1, RgbMode);

            SL_Comm_Base.SL_CommBase_WriteReg(0x95, 0xaa); //SPI Setting
            SL_Comm_Base.SL_CommBase_WriteReg(0x96, 0xaa); //SPI Setting

            SL_Comm_Base.SL_CommBase_WriteReg(0xf3, 0x11); //Reset
            SL_Comm_Base.SL_CommBase_WriteReg(0xf3, 0x10);
            SL_Comm_Base.SL_CommBase_WriteReg(0xf3, 0x11);

            Thread.Sleep(10);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb0, BankEn);
            SL_Comm_Base.SL_CommBase_WriteReg(0xb2, BankWrRd);
            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, 0x11);
            Thread.Sleep(2);

            //DCM Reset
            SL_Comm_Base.SL_CommBase_WriteReg(0xf0, 0x11);
            Thread.Sleep(2);
            SL_Comm_Base.SL_CommBase_WriteReg(0xf0, 0x10);
            Thread.Sleep(2);
            if (DcmMul < 2) DcmMul = 2;
            if (DcmDiv < 1) DcmDiv = 1;
            SL_Comm_Base.SL_CommBase_WriteReg(0xf1, DcmMul);
            SL_Comm_Base.SL_CommBase_WriteReg(0xf2, DcmDiv);
            SL_Comm_Base.SL_AddrWrite(0x8a); //Reload

#if (DEBUG)
            SL_Comm_Base.SL_CommBase_ReadReg(0x92, ref Val, 1); //0x20
            SL_Comm_Base.SL_CommBase_ReadReg(0x95, ref Val, 1); //0xaa
            SL_Comm_Base.SL_CommBase_ReadReg(0x96, ref Val, 1); //0xaa

            SL_Comm_Base.SL_CommBase_ReadReg(0xb0, ref Val, 1); //WhiskyValue[0]
            SL_Comm_Base.SL_CommBase_ReadReg(0xb2, ref Val, 1); //WhiskyValue[1]
            SL_Comm_Base.SL_CommBase_ReadReg(0xb3, ref Val, 1); //0x11

            SL_Comm_Base.SL_CommBase_ReadReg(0xa0, ref Val, 1); //WhiskyValue[2]
            SL_Comm_Base.SL_CommBase_ReadReg(0xa1, ref Val, 1); //WhiskyValue[3]

            SL_Comm_Base.SL_CommBase_ReadReg(0xf1, ref Val, 1); // WhiskyValue[4]
            SL_Comm_Base.SL_CommBase_ReadReg(0xf2, ref Val, 1); // WhiskyValue[5]
#endif
            return true;
        }

        private bool ImageWrite(SL_Img_Lib ImgLib)
        {
            List<byte> lXferData = new List<byte>();
            ImgLib.BmpToList(ref lXferData);
            SL_Comm_Base.SL_CommBase_WriteReg(0xbc, 0x00);
            SL_Comm_Base.SL_CommBase_WriteReg(0x93, 0x03);

            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x06, 0x59);
            Thread.Sleep(10);
            SL_Comm_Base.DDR_MassWrite(0x84, 1, lXferData.ToArray(), lXferData.Count);

            SL_Comm_Base.SPI_WriteReg(0xb8, 0x00, 0x00);
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x06, 0x59);
            SL_Comm_Base.SL_CommBase_WriteReg(0xbc, 0x11);
            Thread.Sleep(2);
            return true;
        }

        public bool FpgaWrite(byte Addr, byte[] Data)
        {
            switch (Data.Length)
            {
                case 1:
                    SL_Comm_Base.SL_CommBase_WriteReg(Addr, Data[0]);
                    break;
                case 2:
                    SL_Comm_Base.SL_CommBase_WriteReg(Addr, Data[0], Data[1]);
                    break;
                case 3:
                    SL_Comm_Base.SL_CommBase_WriteReg(Addr, Data[0], Data[1], Data[2]);
                    break;
                case 4:
                    SL_Comm_Base.SL_CommBase_WriteReg(Addr, Data[0], Data[1], Data[2], Data[3]);
                    break;
                default:
                    break;
            }
            return true;
        }

        public bool FpgaRead(byte Addr , int RdNum,ref string RdStr)
        {          
            uint Val = 0;
            int Ret = SL_Comm_Base.SL_CommBase_ReadReg(Addr,ref Val, RdNum);
            RdStr = "0x" + Convert.ToString(Val, 16);
            return (Ret == 0) ? true : false;
        }
       
    }
}
