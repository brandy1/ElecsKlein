using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    public class SL_Comm_Util
    {
        private SerialPort Comm = null;
        private char[] RxBuf = new char[128];
        string CommPort = null, CommBaudRate = null, CommDatabit = null, CommParity = null, CommStopBits = null;

        private bool isCommOpen = false;

        public SL_Comm_Util(string CommPort, string CommBaudRate, string CommDatabit, string CommParity, string CommStopBits)
        {
            this.CommPort = CommPort;
            this.CommBaudRate = CommBaudRate;
            this.CommDatabit = CommDatabit;
            this.CommParity = CommParity;
            this.CommStopBits = CommStopBits;
        }

        public bool CommOpen()
        {
            bool ret = true;
            Comm = new SerialPort();
            Comm.PortName = CommPort;
            Comm.BaudRate = int.Parse(CommBaudRate);
            Comm.DataBits = int.Parse(CommDatabit);

            switch (CommParity)
            {
                case "None":
                    Comm.Parity = Parity.None;
                    break;
                case "Even":
                    Comm.Parity = Parity.Even;
                    break;
                case "Odd":
                    Comm.Parity = Parity.Odd;
                    break;
                case "Mark":
                    Comm.Parity = Parity.Mark;
                    break;
                case "Space":
                    Comm.Parity = Parity.Space;
                    break;
                default:
                    Comm.Parity = Parity.None;
                    break;
            }

            switch (CommStopBits)
            {
                case "1":
                    Comm.StopBits = StopBits.One;
                    break;
                case "1.5":
                    Comm.StopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    Comm.StopBits = StopBits.Two;
                    break;
                default:
                    Comm.StopBits = StopBits.One;
                    break;
            }

            try
            {
                Comm.Open();
                isCommOpen = true;
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                    ret = false;

                isCommOpen = false;
            }

            if (!Comm.IsOpen)
                ret = false;

            return ret;
        }

        public void CommClose()
        {
            try
            {
                this.Comm.Close();
                isCommOpen = false;
            }
            catch (Exception ex)
            {
                Log.f(this.GetType().FullName,"CommClose() Error: " + ex.Message);
            }
        }

        public bool Write(string Command)
        {

            if (Comm == null) return false;
            Comm.DiscardOutBuffer();
            Comm.DiscardInBuffer();
            char[] ElecsCmd = Command.ToCharArray();
            Comm.Write(ElecsCmd, 0, ElecsCmd.Length);
            return true;
        }

        public bool TouchWriteAndRead(string Command, ref string RetStr)
        {
            bool ret = true;
            if (Write(Command))
            {
                Thread.Sleep(200);
                ret = Read(ref RetStr);
                Thread.Sleep(200);
            }
            else
                ret = false;

            Comm.DiscardInBuffer();
            Comm.DiscardOutBuffer();
            
            return ret;
        }


        public bool WriteAndRead(string Command, ref string RetStr)
        {
            bool ret = true;
            if (Write(Command))
            {
                Thread.Sleep(200);  
                ret = Read(ref RetStr);
                Thread.Sleep(200);  
            }
            else
                ret = false;

            return ret;
        }

        public bool Read(ref string RetStr)
        {
            bool ret = true;
            try
            {
                RetStr = Comm.ReadExisting(); 

            }
            catch (Exception)
            {
                ret = false;
                throw;
            }

            return ret;
        }

        public bool isOpen()
        {
            return isCommOpen;
        }
    }
}
