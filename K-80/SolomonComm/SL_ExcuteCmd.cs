using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace SL_Tek_Studio_Pro
{

    public class InstruDefine
    {
        public string MainName { get; set; }
        public string NickName { get; set; }
        public InstruDefine(string MainName, string NickName)
        {
            this.MainName = MainName;
            this.NickName = NickName;
        }
    }

    public class SL_ExcuteCmd
    {
        SL_Comm_Util ElecsComm = null, EspecComm = null;
        string InstruAddr = null, CommAddr = null;
        enum CmdType { Read, Write, WrAndRd }
        private char[] DelimiterChars = { ' ', ',', '\t' };
        private char EndTokenChars = '\r';
        private string DELAYCMD = "delay";
        private string ERR_INSTRUOPEN = "Open Instrument Error";
        private bool bPauseCmd = false;
        private bool bScopeCmd = false;

        SL_Equip_Util[] EquipUtil = new SL_Equip_Util[10];
        SL_ElecsSpt_Util ElecsSpt = new SL_ElecsSpt_Util();
        SL_WhiskySpt_Util WhiskySpt = new SL_WhiskySpt_Util();

        ~SL_ExcuteCmd()
        {
            foreach (SL_Equip_Util InstruUtil in EquipUtil)
            {
#if INSTRUSUPPORT
                if (InstruUtil != null && InstruUtil.isOpen()) InstruUtil.Close();
#endif
            }
        }


        public string ExamScript(string[] Commands, ref List<ScriptInfo> ScriptInfo)
        {
            return ElecsSpt.ExamScript(Commands, ref ScriptInfo);
        }

        public bool ExeScriptFile(string FileName, ref string RdStr)
        {
            string SptFilePath = null, ErrInfo = null,CleanCmd = null;
            int ErrCode = 0;
            SL_IO_Util IOUtil = new SL_IO_Util();
            List<ScriptInfo> lScriptInfo = new List<ScriptInfo>();
            if (!IOUtil.FileExist(FileName, ref SptFilePath)) { RdStr += "Script not Exist"; return false; }
            ErrInfo = ElecsSpt.ExamScript(IOUtil.ReadFile(SptFilePath), ref lScriptInfo);
            if (!String.IsNullOrEmpty(ErrInfo)) { RdStr = ErrInfo; return false; }

            for (int i = 0; i < lScriptInfo.Count; i++)
            {
                if (ElecsSpt.ExamCmd(lScriptInfo[i].Command.Trim(),ref CleanCmd, ref ErrCode))
                {
                    if (ElecsSpt.GetCmdClass() == 0 || ElecsSpt.GetCmdClass() == 2 || ElecsSpt.GetCmdClass() == 3)
                    {
                        SetDevices(ElecsSpt.getCommAddr(), ref ElecsComm, ElecsSpt.getInstruAddr());
                        ProcessCmd(ElecsSpt.GetElecsCmd(), ElecsSpt.GetElecsClass(), ref RdStr);
                        Thread.Sleep(20);
                    }
                }
            }
            return true;
        }

        public bool ExeScriptFile(string FileName, string Times, ref string RdStr)
        {
            string SptFilePath = null, ErrInfo = null, CleanCmd = null ;
            int ErrCode = 0, Count = 0;
            Count = int.TryParse(Times, out Count) ? Count : 1;
            SL_IO_Util IOUtil = new SL_IO_Util();
            List<ScriptInfo> lScriptInfo = new List<ScriptInfo>();

            if (!IOUtil.FileExist(FileName, ref SptFilePath)) { RdStr += "Script not Exist"; return false; }
            ErrInfo = ElecsSpt.ExamScript(IOUtil.ReadFile(SptFilePath), ref lScriptInfo);
            if (!String.IsNullOrEmpty(ErrInfo)) { RdStr = ErrInfo; return false; }

            for (int i = 0; i < Count; i++)
            {
                for (int j = 0; j < lScriptInfo.Count; j++)
                {
                    if (ElecsSpt.ExamCmd(lScriptInfo[j].Command.Trim(),ref CleanCmd, ref ErrCode))
                    {
                        if (ElecsSpt.GetCmdClass() == 0 || ElecsSpt.GetCmdClass() == 2 || ElecsSpt.GetCmdClass() == 3)
                        {
                            SetDevices(ElecsSpt.getCommAddr(), ref ElecsComm, ElecsSpt.getInstruAddr());
                            ProcessCmd(ElecsSpt.GetElecsCmd(), ElecsSpt.GetElecsClass(), ref RdStr);
                            Thread.Sleep(20);
                        }
                    }
                }
            }
            return true;
        }



        public bool ExamCmd(ScriptInfo ScriptCmd)
        {
            byte ret = ElecsSpt.ExamCmd(ScriptCmd);
            return (ret == 0 || ret == 1 ) ? true : false;
        }

        public bool SetCommDevice(ref SL_Comm_Util Comm)
        {
            this.ElecsComm = Comm;
            return true;
        }

        public bool SetInstruDevice(string IntruAddr)
        {
            this.InstruAddr = IntruAddr;
            return true;
        }

        public bool SetDevices(string CommPort, string IntruAddr, ref SL_Comm_Util Comm)
        {
            this.CommAddr = CommPort;
            this.ElecsComm = Comm;
            this.InstruAddr = IntruAddr;
            return true;
        }

        public bool SetDevices(string CommPort, string IntruAddr)
        {
            this.CommAddr = CommPort;
            this.InstruAddr = IntruAddr;
            return true;
        }

        public bool SetDevices(string CommPort, ref SL_Comm_Util Comm, string IntruAddr)
        {
            this.CommAddr = CommPort;
            this.ElecsComm = Comm;
            this.InstruAddr = IntruAddr;
            return true;
        }

        public bool RunSingleCmd(string Command, ref string rdStr)
        {
            string[] ElecsCmd = new string[1] { Command };
            List<ScriptInfo> lScriptInfo = new List<ScriptInfo>();
            string Msg =  ElecsSpt.ExamScript(ElecsCmd, ref lScriptInfo);
            byte ret = ElecsSpt.ExamCmd(lScriptInfo[0]);
            if (ret == 0 || ret == 1)
                return ProcessCmd(getElecsCmd(), getElecsClass(), ref rdStr);
            else
                return false;
        }

        public bool ProcessCmd(string Command, ElecsCmd Elecs, ref string RdStr)
        {
            return ProcessCmd(Command, Elecs.Type, Elecs.Class,Elecs.Delay, ref RdStr);
        }

        public bool ProcessCmd(string Command, byte Type, byte Class,int Delay, ref string RdStr)
        {
            bool ret = true;
            if (Class == 0) ret = DealWithComm(Command, Type, Delay,ref RdStr);
            if (Class == 1) ret = DealWithSystem(Command, Type, ref RdStr);
#if (INSTRUSUPPORT)
            if (Class == 2) ret = DealWithInstr(Command, Type, ref RdStr);
#endif
            if (Class == 3) ret = DealWithWhisky(Command, Type, ref RdStr);

            return ret;
        }


        public bool Open(string CommAddr)
        {
            ElecsComm = new SL_Comm_Util(CommAddr, "115200", "8","None", "1");  //20170724
            if (ElecsComm.CommOpen())
                return true;
            else
                return false;
        }

        public bool Open(string CommAddr, string Baudrate, string Parity, string DataBit, string StopBit)
        {
            ElecsComm = new SL_Comm_Util(CommAddr, Baudrate, DataBit, Parity, StopBit);
            if (ElecsComm.CommOpen())
                return true;
            else
                return false;
        }

        public bool Close()
        {
            ElecsComm.CommClose();
            ElecsComm = null;
            return true;
        }

        public bool Write(string Command)
        {
            bool ret = true;
            int delaytime = 0;
            string[] Token = Command.Trim().Split(DelimiterChars);
            if (Command.LastIndexOf(EndTokenChars) < 0)
                Command = Command + EndTokenChars;

            if (Token[0].CompareTo(DELAYCMD) == 0)
            {
                ret = ElecsComm.Write(Command);
                ret = int.TryParse(Token[1], out delaytime);
                if (ret) Thread.Sleep(delaytime);
            }
            else
                ret = ElecsComm.Write(Command);
            return ret;
        }

        public bool Read(ref string RdCmd)
        {
            return ElecsComm.Read(ref RdCmd);
        }

        public bool WriteRead(string Command, ref string RdCmd)
        {
            if (Command.LastIndexOf(EndTokenChars) < 0) Command = Command + EndTokenChars;
            return ElecsComm.WriteAndRead(Command, ref RdCmd);
        }

        public bool Status()
        {
            if (ElecsComm == null) return false;
            return ElecsComm.isOpen();
        }

        public bool ComSetting(string CommAddr, ref SL_Comm_Util Comm)
        {
            bool ret = false;
            ElecsComm = new SL_Comm_Util(CommAddr, "115200", "8", "None", "1");
            if (ElecsComm.CommOpen()) { Comm = ElecsComm; ret = true; }
            return ret;
        }

        public bool getScopeMode() {return this.bScopeCmd; }
        public void setScopeMode(bool Mode) { this.bScopeCmd = Mode; }
        public bool getPauseMode() { return this.bPauseCmd; }
        public void setPauseMode(bool Mode) { this.bPauseCmd = Mode; }
        public string getCommName() { return ElecsSpt.getCommAddr(); }
        public string getInstruName() { return ElecsSpt.getInstruAddr(); }
        public string getElecsCmd() { return ElecsSpt.GetElecsCmd(); }
        public byte getCmdType() { return ElecsSpt.GetCmdType(); }
        public byte getCmdClass() { return ElecsSpt.GetCmdClass(); }
        public int getCmdDelay() { return ElecsSpt.GetCmdClass(); }
        public byte getCmdReg() { return ElecsSpt.GetCmdReg();}
        public string ReadInfo(int Line, string Result) { return ElecsSpt.ReadInfo(Line, Result); }
        public string ErrResult(string Info, int Line) { return ElecsSpt.ErrResult(Info, Line); }
        public ElecsCmd getElecsClass() { return ElecsSpt.GetElecsClass(); }

#if (INSTRUSUPPORT)

        private bool InstrRead(int Count, ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughRead(ref RdStr);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool InstrSend(int Count, string instruCmd, ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughSend(instruCmd);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool InstrSendRead(int Count, string instruCmd, ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughSendRead(instruCmd, ref RdStr);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool InstrScopeImage(int Count, string instruCmd, ref string RdStr)
        {
            bool ret = true;
            string strPath = null, FileName = null;
            SL_IO_Util IOUtil = new SL_IO_Util();
            byte[] ScopeScreenResultsArray; // Screen Results array.
            int g_ScreenLength;
            VisaInstrument visaScope = new VisaInstrument(EquipUtil[Count].getInstrName());
            //ScopeList.SimpleScopeGraph(devices[1], ":DISPLAY:DATA? PNG, SCREEN, COLOR\r\n");
            // Download the screen image.
            // -----------------------------------------------------------
            visaScope.SetTimeoutSeconds(30000);
            visaScope.DoCommand(":STOP");
            visaScope.DoCommand(":HARDcopy:INKSaver OFF");
            // Get the screen data.
            g_ScreenLength = visaScope.DoQueryIEEEBlock(":DISPlay:DATA? PNG, COLor", out ScopeScreenResultsArray);

            if (instruCmd.CompareTo("auto") == 0)
                FileName = "Scope_" + DateTime.Now.ToLocalTime().ToString("yyyyMMdd-HHmmss") + ".png";
            else
                FileName = instruCmd;

            strPath = Setting.ExeScopeDirPath + "\\" + FileName;
            if (IOUtil.isFileExist(strPath)) IOUtil.FileDelete(strPath);
            if (IOUtil.GetExtName(strPath).CompareTo("png") != 0) strPath += ".png";
            FileStream fStream = File.Open(strPath, FileMode.Create);
            fStream.Write(ScopeScreenResultsArray, 0, g_ScreenLength);
            fStream.Close();

            visaScope.DoCommand(":RUN");
            visaScope.Close();
            return ret;
        }

        private bool SearchInstruName(ref int Count)
        {
            string InstrName = this.InstruAddr;
            int Match = -1;

            for (int i = 0; i < EquipUtil.Length; i++)
            {
                if (String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && String.IsNullOrEmpty(EquipUtil[i].getInstruNickName())) continue;
                if ((EquipUtil[i].getInstrName().CompareTo(InstrName) == 0) || EquipUtil[i].getInstruNickName().CompareTo(InstrName) == 0)
                {
                    Count = Match = i;
                    break;
                }
            }

            if (Match < 0) SearchAndAddList(InstrName, ref Count);

            return (Count >= 0) ? true : false;
        }

        private bool SearchAndAddList(string InstruName, ref int Count)
        {
            bool ret = true;
            string[] Token = InstruName.Split(DelimiterChars);
            int Match = -1;
            string NickName = null;

            for (int i = 0; i < EquipUtil.Length; i++)
            {
                if (!String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && EquipUtil[i].getInstrName().CompareTo(Token[0]) == 0)
                {
                    EquipUtil[i].SetInstruName(Token[0], Token[1]);
                    Count = Match = i;
                    break;
                }
            }

            if (Match < 0)
            {
                for (int i = 0; i < EquipUtil.Length; i++)
                {
                    if (String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && String.IsNullOrEmpty(EquipUtil[i].getInstruNickName()))
                    {
                        NickName = (Token.Length > 1) ? Token[1] : Token[0];
                        EquipUtil[i].SetInstruName(Token[0], NickName);
                        Count = i;
                        break;
                    }
                }
            }
            return ret;
        }

        private bool DealWithInstr(string InstruCmd, byte InstruType, ref string RdStr)
        {
            bool ret = true;
            int Count = -1;
            for (int i = 0; i < EquipUtil.Length; i++) { if (EquipUtil[i] == null) EquipUtil[i] = new SL_Equip_Util(); }
            switch (InstruType)
            {
                case 0:
                    if (SearchInstruName(ref Count)) ret = InstrRead(Count, ref RdStr);
                    break;
                case 1:
                    if (SearchInstruName(ref Count)) ret = InstrSend(Count, InstruCmd, ref RdStr);
                    break;
                case 2:
                    if (SearchInstruName(ref Count)) ret = InstrSendRead(Count, InstruCmd, ref RdStr);
                    break;
                case 3:
                    ret = SearchAndAddList(InstruCmd, ref Count);
                    break;
                case 4:
                    if (SearchInstruName(ref Count)) ret = InstrScopeImage(Count, InstruCmd, ref RdStr);
                    break;
                default:
                    break;
            }

            return ret;
        }
#endif

        private string SystemInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        private string SystemLoad(string SystemCmd)
        {
            string Msg = null;
            string[] Parameter = SystemCmd.Split(DelimiterChars);
            if (Parameter.Length == 2) ExeScriptFile(Parameter[1], ref Msg);
            if (Parameter.Length == 3) ExeScriptFile(Parameter[1], Parameter[2], ref Msg);
            return Msg;
        }

        private bool DealWithSystem(string SystemCmd, byte ElecsType, ref string RdStr)
        {
            if (ElecsType == (byte)CmdType.Write)
            {
                string[] Parameter = SystemCmd.Split(DelimiterChars);
                if (Parameter[0].CompareTo("scope.show") == 0) { bScopeCmd = true; }       
                if (Parameter[0].CompareTo("system") == 0) { RdStr = SystemInfo(); }
                if (Parameter[0].CompareTo("pause") == 0) { RdStr = "Pause"; bPauseCmd = true; }
                if (Parameter[0].CompareTo("txt") == 0) { RdStr = SystemLoad(SystemCmd); }
            }
            return true;
        }

        private bool DealWithComm(string ElecsCmd, byte ElecsType,int Delay, ref string RdStr)
        {
            bool ret = true;
            int Times = 0;
            string[] Token = ElecsCmd.Split(DelimiterChars);

            if (ElecsComm == null && Token[0].ToLower().CompareTo(DELAYCMD) == 0 && int.TryParse(Token[1], out Times))  Thread.Sleep(Times);

            if (ElecsComm == null) { RdStr = "Comm Err"; return false; }

            ElecsCmd = ElecsCmd + EndTokenChars;

            if (ElecsType == (byte)CmdType.Write)
                ret = ElecsComm.Write(ElecsCmd);
            else if (ElecsType == (byte)CmdType.WrAndRd)
                ret = ElecsComm.WriteAndRead(ElecsCmd, ref RdStr);
            else
                ret = ElecsComm.Read(ref RdStr);

            if (Token[0].ToLower().CompareTo(DELAYCMD) == 0 && int.TryParse(Token[1], out Times)) Thread.Sleep(Times);

            Thread.Sleep(Delay);

            return ret;
        }

        private bool DealWithWhisky(string ElecsCmd, byte ElecsType, ref string RdStr)
        {
            bool ret = true;
            ret = WhiskySpt.ExcuteCmd(ElecsCmd, ElecsType, ref RdStr);
            return ret;
        }

        private int Temper_SetTemp(string setTemp, ref string TmpStr)  //important #3
        {
            string RdStr = null;
            int Temp_flag = 0, ErrCode = 0, SET = 0, Temp = 0;
            float set_temp = 0, f_value = 0;
            SET = int.TryParse(setTemp, out Temp) ? Temp : 0;
            while (Temp_flag == 0)
            {
                EspecComm.WriteAndRead("1, MON?", ref RdStr);
                //ShowBox1.Text;
                string[] substrings = Regex.Split(RdStr, ",");
                if (float.TryParse(substrings[0], out f_value))  //setting temperature , every step is less than 2 degree c to change to target temperature.
                {
                    //set_temp = SET[i, 1];
                    set_temp = SET;
                    if (Math.Abs(set_temp - f_value) <= 2)
                    {
                        string s = set_temp.ToString("#.0");
                        EspecComm.Write("1, TEMP, S" + s);
                        if (Math.Abs(set_temp - f_value) <= 0.5)
                        {
                            Temp_flag = 1;
                        }
                    }
                    else
                    {
                        if (set_temp - f_value > 2)
                        {
                            set_temp = f_value + 2;
                            string s = set_temp.ToString("#.0");
                            EspecComm.Write("1, TEMP, S" + s);
                        }
                        if (set_temp - f_value < -2)
                        {
                            set_temp = f_value - 2;
                            string s = set_temp.ToString("#.0");
                            EspecComm.Write("1, TEMP, S" + s);
                        }
                    }
                    //ErrCnt++;
                }
                else
                {
                    ErrCode = ErrCode | 1;  //if SU241 response error format. Record it!
                }

                if (substrings[3] == "0\r")  //check if SU241 has alarm
                {
                    if (substrings[2] == "OFF" || substrings[2] == "STANDBY")
                    {
                        EspecComm.Write("1, MODE, CONSTANT"); //if no alarm , start SU241.
                    }
                }
                else
                {
                    EspecComm.Write("1, MODE, STANDBY");
                    TmpStr = "SU241 has alarm!";
                    ErrCode = ErrCode | 2;  //if SU241 response alarm. Record it!
                }
                if (ErrCode == 0)
                {
                    //Thread.Sleep(50000);  //delay 50s for temperature change
                    for (int k = 0; k < 40; k++)
                    {
                        Thread.Sleep(1000);
                    }

                }
                else
                {
                    Temp_flag = 1;
                }

                if (Temp_flag == 1 && ErrCode == 0)
                {
                    for (int k = 0; k < 180; k++)
                    {
                        Thread.Sleep(1000);  //delay 180s for temperature stable

                    }
                }
            }

            return ErrCode;
        }

        private bool DealWithEspec(string EspecCmd, byte ElecsType, ref string RdStr)
        {
            bool ret = true;
            string[] Token = EspecCmd.Split(DelimiterChars);
            string Command = null;

            if (EspecComm == null) { RdStr = "ERROR Open Device Err"; return false; }
            if (Token.Length > 1 && Token[0].CompareTo("temper.set") == 0) Temper_SetTemp(Token[1], ref RdStr);
            if (Token.Length > 1 && Token[0].CompareTo("temper.send") == 0) Command = Token[1] + EndTokenChars;
            if (Token.Length > 1 && Token[0].CompareTo("temper.sendread") == 0) Command = Token[1] + EndTokenChars;
            if (Token.Length > 1 && Token[1].CompareTo("on") == 0) Command = "1, POWER, ON" + EndTokenChars;
            if (Token.Length > 1 && Token[1].CompareTo("off") == 0) Command = "1, POWER, OFF" + EndTokenChars;
            if (Token.Length > 1 && Token[1].CompareTo("standby") == 0) Command = "1, MODE, STANDBY" + EndTokenChars;
            if (String.IsNullOrEmpty(Command)) Command = EspecCmd + EndTokenChars;


            if (ElecsType == (byte)CmdType.Write)
                ret = EspecComm.Write(Command);
            else if (ElecsType == (byte)CmdType.WrAndRd)
                ret = EspecComm.WriteAndRead(Command, ref RdStr);
            else
                ret = EspecComm.Read(ref RdStr);

            return ret;
        }
    }
}
