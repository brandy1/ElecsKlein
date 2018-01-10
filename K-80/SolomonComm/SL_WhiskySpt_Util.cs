#define DEBUG 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    class SL_WhiskySpt_Util
    {
        private const string SSL_GPIOH_WRITE = "ssl.gpioh.write";
        private const string SSL_GPIOL_WRITE = "ssl.gpiol.write";
        private const string SSL_GPIO_DIR = "ssl.gpio.dir";
        private const string SSL_MIPI_VIDEO = "ssl.mipi.video";
        private const string SSL_MIPI_DSI = "ssl.mipi.dsi";
        private const string SSL_FPGA_SET = "ssl.fpga.set";
        private const string SSL_BRIDGE_WR = "ssl.bridge.write";
        private const string SSL_BRIDGE_RD = "ssl.bridge.read";
        private const string SSL_BRIDGE_SEL = "ssl.bridge.select";
        private const string SSL_MIPI_WR = "ssl.mipi.write";
        private const string SSL_I2C_WR = "ssl.i2c.write";
        private const string SSL_PMIC_WR = "ssl.pmic.write";
        private const string SSL_I2C_RD = "ssl.i2c.read";
        private const string SSL_IMAGE_FILL = "ssl.image.fill";
        private const string SSL_MIPI_READ = "ssl.mipi.read";
        private const string SSL_IMAGE_SHOW = "ssl.image.show";
        private const string SSL_FPGA_WRITE = "ssl.fpga.write";
        private const string SSL_FPGA_READ = "ssl.fpga.read";
        private const string SSL_SLEEP_CMD = "sleep";
        private const string SSL_USB_WRITE = "ssl.usb.write";
        private const string SSL_HMIPI_WR = "ssl.mipi.write.hs";
        private const string SSL_HMIPI_READ = "ssl.mipi.read.hs";
        private const string SSL_MIPI_ULP = "ssl.mipi.ulp";
        private const string SSL_MIPI_BTA = "ssl.mipi.bta";

        private const double FPGA_OSC = 40.0;  
        private char[] DelimiterChars = { ' ', ',', ':', '\t' };
        SL_WhiskyComm_Util WhiskyUtil = new SL_WhiskyComm_Util();
        public bool ExcuteCmd(string Cmd, byte CmdType, ref string RdStr)
        {
            bool ret = false;

            SL_IO_Util Util = new SL_IO_Util();
            string[] WhiskyCmd = (string[])MergeElecsCmds(Cmd.Trim()).ToArray(typeof(string));
            string[] WhiskyData = new string[WhiskyCmd.Length-1];
            Array.Copy(WhiskyCmd, 1, WhiskyData, 0, WhiskyData.Length);

             if(WhiskyCmd[0].CompareTo(SSL_GPIOH_WRITE) == 0) ret = SLFpgaGpioH(WhiskyData, ref RdStr);
            if (WhiskyCmd[0].CompareTo(SSL_GPIOL_WRITE) == 0) ret = SLFpgaGpioL(WhiskyData, ref RdStr);
            if (WhiskyCmd[0].CompareTo(SSL_GPIO_DIR) == 0) ret = SLFpgaGpioDir(WhiskyData, ref RdStr);
            if (WhiskyCmd[0].CompareTo(SSL_MIPI_VIDEO) == 0) ret = SetMipiVideo(WhiskyData);
            if(WhiskyCmd[0].CompareTo(SSL_MIPI_DSI) == 0)   ret = SetMipiDsi(WhiskyData);
            if(WhiskyCmd[0].CompareTo(SSL_FPGA_SET) == 0)   ret = SetFpgaParm(WhiskyData,ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_BRIDGE_WR) == 0)  ret = SLBrigeWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_BRIDGE_RD) == 0)  ret = SLBrigeRead(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_BRIDGE_SEL) == 0) ret = SLBridgeSelect(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_MIPI_WR) == 0) ret = SLMipiWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_I2C_WR) == 0) ret = SLI2CWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_PMIC_WR) == 0) ret = SLPmicWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_I2C_RD)==0) ret =  SLI2CRead(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_IMAGE_FILL) == 0) ret = SLImageFill(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_MIPI_READ) == 0) ret = SLMipiRead(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_IMAGE_SHOW) == 0) ret = SLImageShow(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_FPGA_WRITE) == 0) ret = SLFpgaWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_FPGA_READ) == 0) ret = SLFpgaRead(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_SLEEP_CMD) == 0) ret = SLFpgaSleep(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_USB_WRITE) == 0) ret = SLFpgaCommandWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_HMIPI_WR) == 0) ret = SLHightMipiWrite(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_HMIPI_READ) == 0) ret = SLHighMipiRead(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_MIPI_ULP) == 0) ret = SLMipiUlp(WhiskyData, ref RdStr);
            if(WhiskyCmd[0].CompareTo(SSL_MIPI_BTA) == 0) ret = SLMipiBta(WhiskyData, ref RdStr);
            return ret;
        }

        private bool SLFpgaGpioH(string[] WhiskyData, ref string RdStr)
        {

            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.FpgaWrite(0xfb, WhiskyValue);
        }

        private bool SLFpgaGpioL(string[] WhiskyData, ref string RdStr)
        {

            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.FpgaWrite(0xfc, WhiskyValue);
        }

        private bool SLFpgaGpioDir(string[] WhiskyData, ref string RdStr)
        {
 
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.FpgaWrite(0xfa, WhiskyValue);
        }

        private bool SLFpgaSleep(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            Thread.Sleep(WhiskyValue[0]);
            return true; 
        }

        private bool SLFpgaRead(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.FpgaRead(WhiskyValue[0], WhiskyValue[1],ref RdStr); 
        }

        private bool SLFpgaWrite(string[] WhiskyData, ref string RdStr)
        {
            if (WhiskyData.Length > 5) { RdStr = "Much Parameter\n"; return false; }
            byte[] WhiskyValue = stringToByte(WhiskyData);
            byte[] RegData = new byte[WhiskyValue.Length - 1];
            Array.Copy(WhiskyValue, 1, RegData, 0, RegData.Length);
            return WhiskyUtil.FpgaWrite(WhiskyValue[0], RegData);

        }

        private bool SLFpgaCommandWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.UsbCmdWrite(WhiskyValue);
        }

        private bool SLImageShow(string[] WhiskyData, ref string RdStr)
        {
            return WhiskyUtil.ImageShow(WhiskyData[0],ref RdStr);
        }

        private bool SLMipiBta(string[] WhiskyData, ref string RdStr)
        {
            return WhiskyUtil.MipiBta();
        }

        private bool SLMipiUlp(string[] WhiskyData, ref string RdStr)
        {  
            return WhiskyUtil.MipiUlp();
        }

        private bool SLMipiRead(string[] WhiskyData, ref string RdStr)
        {
            string Msg = null;
            byte[] WhiskyValue = stringToByte(WhiskyData);
            bool ret = WhiskyUtil.MipiRead(WhiskyValue[0], WhiskyValue[1],ref Msg);
            RdStr += Msg;
            return ret;
        }

        private bool SLHighMipiRead(string[] WhiskyData, ref string RdStr)
        {
            string Msg = null;
            byte[] WhiskyValue = stringToByte(WhiskyData);
            bool ret = WhiskyUtil.MipiHSRead(WhiskyValue[0], WhiskyValue[1], ref Msg);
            RdStr += Msg;
            return ret;
        }

        private bool SLImageFill(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            WhiskyUtil.ImageFill((byte)WhiskyValue[0], (byte)WhiskyValue[1], (byte)WhiskyValue[2]);
            return true;
        }

        private bool SLI2CRead(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.i2cRead(WhiskyValue[0], WhiskyValue[1], WhiskyValue[2],ref RdStr);
        }


        private bool SLI2CWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            byte[] RegData = new byte[WhiskyValue.Length - 1];
            Array.Copy(WhiskyValue, 1, RegData, 0, RegData.Length);
            return WhiskyUtil.i2cWrite(WhiskyValue[0], RegData);
        }

        private bool SLPmicWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.pmicWrite(WhiskyValue[0], WhiskyValue[1], WhiskyValue[2],ref RdStr);
        }

        private bool SLMipiWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.MipiWrite(WhiskyValue);       
        }

        private bool SLHightMipiWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.MipiHSWrite(WhiskyValue);
        }

        private bool SLBridgeSelect(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            WhiskyUtil.MipiBridgeSelect(WhiskyValue[0]);
            return true;
        }

        private bool SLBrigeRead(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return WhiskyUtil.BridgeRead(WhiskyValue[0],  WhiskyValue[1],ref RdStr);
        }

        private bool SLBrigeWrite(string[] WhiskyData, ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            if(WhiskyValue.Length ==2)
                return WhiskyUtil.BridgeWrite(WhiskyValue[0], WhiskyValue[1]);
            else
                return WhiskyUtil.BridgeWrite(WhiskyValue[0], WhiskyValue[1], WhiskyValue[2]);
        }

        private bool SetFpgaParm(string[] WhiskyData,ref string RdStr)
        {
            byte[] WhiskyValue = stringToByte(WhiskyData);
            return  WhiskyUtil.SetFpgaTiming(WhiskyValue[0], WhiskyValue[1], WhiskyValue[2], WhiskyValue[3], WhiskyValue[4], WhiskyValue[5],ref RdStr );
        }

        private bool SetMipiDsi(string[] WhiskyData)
        {
            int Value = 0, MipiLane = 4, MipiSpeed = 1000;
            if (int.TryParse(WhiskyData[0], out Value)) MipiLane = Value;
            if (int.TryParse(WhiskyData[1], out Value)) MipiSpeed = Value;
            return WhiskyUtil.SetMipiDsi(MipiLane, MipiSpeed, WhiskyData[2]);
            
        }

        private bool SetMipiVideo(string[] WhiskyData)
        {
            int[] WhiskyValue = stringToInt(WhiskyData);
            return WhiskyUtil.SetMipiVideo(WhiskyValue[0], WhiskyValue[1], (byte)WhiskyValue[2], (byte)WhiskyValue[3], (byte)WhiskyValue[4], (byte)WhiskyValue[5], (byte)WhiskyValue[6], (byte)WhiskyValue[7], (byte)WhiskyValue[8]);
        }

        private byte[] stringToByte(string[] WiskeyData)
        {
            SL_IO_Util Util = new SL_IO_Util();
            byte Value = 0;
            byte[] WhiskyValue = new byte[WiskeyData.Length];
            for (int i = 0; i < WhiskyValue.Length; i++)
            {
                if (new SL_Digital_Util().isStrtoByte(WiskeyData[i], ref Value))
                    WhiskyValue[i] = Value;
                else
                    WhiskyValue[i] = 0;
            }
            return WhiskyValue;
        }

        private int[] stringToInt(string[] WiskeyData)
        {
            SL_IO_Util Util = new SL_IO_Util();
            int Value = 0;
            int[] WhiskyValue = new int[WiskeyData.Length];
            for (int i = 0; i < WhiskyValue.Length; i++)
            {
                if (new SL_Digital_Util().isStrtoInt(WiskeyData[i], ref Value))
                    WhiskyValue[i] = Value;
                else
                    WhiskyValue[i] = 0;
            }
            return WhiskyValue;
        }

        private ArrayList MergeElecsCmds(string Command)
        {
            ArrayList eCmdList = new ArrayList();
            string[] SplitStr = Command.Split(DelimiterChars);

            foreach (string CmdStr in SplitStr)
            {
                if (!string.IsNullOrEmpty(CmdStr))
                    eCmdList.Add(CmdStr);
            }

            return eCmdList;
        }
    }
}
