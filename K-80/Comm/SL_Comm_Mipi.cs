#define DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    class SL_Comm_Mipi : SL_Comm
    {
        public override void SetInterfaceParm(int index) { }
        public override void Comm_RegWrite() { Console.WriteLine("SC_Mipi Write Reg "); }
        public override void Comm_RegRead() { Console.WriteLine("SC_Mipi Read Reg "); }
        public override void Comm_IO() { Console.WriteLine("SC_Mipi Comm_IO"); }


        public void MipiBridgeSelect(byte SelVal)
        {
            SL_Comm_Base.BrigeSelection = SelVal;
        }

        public bool ImageFill(byte R, byte G, byte B)
        {
            SL_Comm_Base.SPI_WriteReg(0xb7, 0x02, 0x59);
            SL_Comm_Base.SL_CommBase_WriteReg(0x92, 0x20);
            SL_Comm_Base.SL_CommBase_WriteReg(0xad, R, G, B);
            return true;
        }

        public bool MipiWrite(byte[] Data)
        {
            byte[] WhiskyValue = Data;
            int DataNum = WhiskyValue.Length - 1;
            byte HD = 0, M_HD = 0, M_LD = 0, LD = 0, ConfRegH = 0, ConfRegL = 0;

            //General Packet
            if (WhiskyValue[0] == 0x29) { ConfRegH = 0x06; ConfRegL = 0x10; }
            if (WhiskyValue[0] == 0x03) { ConfRegH = 0x02; ConfRegL = 0x10; }
            if (WhiskyValue[0] == 0x13) { ConfRegH = 0x02; ConfRegL = 0x10; }
            if (WhiskyValue[0] == 0x23) { ConfRegH = 0x02; ConfRegL = 0x10; }
            //DCS
            if (WhiskyValue[0] == 0x39) { ConfRegH = 0x06; ConfRegL = 0x50; }
            if (WhiskyValue[0] == 0x05) { ConfRegH = 0x02; ConfRegL = 0x50; }
            if (WhiskyValue[0] == 0x15) { ConfRegH = 0x02; ConfRegL = 0x50; }

            LD = (byte)(DataNum & 0xff);
            M_LD = (byte)((DataNum >> 8) & 0xff);
            M_HD = (byte)((DataNum >> 16) & 0xff);
            HD = (byte)((DataNum >> 24) & 0xff);

            SL_Comm_Base.SPI_WriteReg(0xb7, ConfRegH, ConfRegL);
            SL_Comm_Base.SPI_WriteReg(0xbd, HD, M_HD);
            SL_Comm_Base.SPI_WriteReg(0xbc, M_LD, LD);   
        
            SL_Comm_Base.BdgeSel(true);            
            SL_Comm_Base.SL_CommBase_WriteReg(0x8b, 0xbf);
            SL_Comm_Base.SL_AddrWrite(0x8c);
            for (int i = 1; i < WhiskyValue.Length; i++) SL_Comm_Base.SL_DataWrite(WhiskyValue[i]);

            SL_Comm_Base.UnBgeSel();
          
            return true;
        }

        public bool MipiHSWrite(byte[] Data)
        {
            byte[] WhiskyValue = Data;
            int DataNum = WhiskyValue.Length - 1;
            byte HD = 0, M_HD = 0, M_LD = 0, LD = 0, ConfRegH = 0, ConfRegL = 0;
            uint tmpb7 = 0, tmpbd = 0, tmpbc = 0;
            //General Packet
            if (WhiskyValue[0] == 0x29) { ConfRegH = 0x06; ConfRegL = 0x19; }
            if (WhiskyValue[0] == 0x03) { ConfRegH = 0x02; ConfRegL = 0x19; }
            if (WhiskyValue[0] == 0x13) { ConfRegH = 0x02; ConfRegL = 0x19; }
            if (WhiskyValue[0] == 0x23) { ConfRegH = 0x06; ConfRegL = 0x19; }
            //DCS
            if (WhiskyValue[0] == 0x39) { ConfRegH = 0x06; ConfRegL = 0x59; }
            if (WhiskyValue[0] == 0x05) { ConfRegH = 0x02; ConfRegL = 0x59; }
            if (WhiskyValue[0] == 0x15) { ConfRegH = 0x02; ConfRegL = 0x59; }

            LD = (byte)(DataNum & 0xff);
            M_LD = (byte)((DataNum >> 8) & 0xff);
            M_HD = (byte)((DataNum >> 16) & 0xff);
            HD = (byte)((DataNum >> 24) & 0xff);
            SL_Comm_Base.SPI_WriteReg(0xb7, ConfRegH, ConfRegL);
            SL_Comm_Base.SPI_WriteReg(0xbd, HD, M_HD);
            SL_Comm_Base.SPI_WriteReg(0xbc, M_LD, LD);

            SL_Comm_Base.SPI_ReadReg(0xb7, 2, ref tmpb7);
            SL_Comm_Base.SPI_ReadReg(0xbd, 2, ref tmpbd);
            SL_Comm_Base.SPI_ReadReg(0xbc, 2, ref tmpbc);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, 0x10);
            SL_Comm_Base.SL_CommBase_WriteReg(0x8b, 0xbf);

            SL_Comm_Base.SL_AddrWrite(0x8c);
            for (int i = 1; i < WhiskyValue.Length; i++) SL_Comm_Base.SL_DataWrite(WhiskyValue[i]);

            SL_Comm_Base.SL_CommBase_WriteReg(0xb3, 0x11);
            return true;
        }
    }
}
