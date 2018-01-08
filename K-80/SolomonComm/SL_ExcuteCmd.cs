  
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        SL_Comm_Util ElecsComm = null;
        string InstruAddr = null, CommAddr = null;
        enum CmdType { Read, Write, WrAndRd }
        private char[] DelimiterChars = { ' ', ',', '\t' };
        private char EndTokenChars = '\r';
        private string DELAYCMD = "delay";
        private string ERR_INSTRUOPEN = "Open Instrument Error";

        SL_Equip_Util[] EquipUtil = new SL_Equip_Util[10];
        SL_ElecsSpt_Util ElecsSpt = new SL_ElecsSpt_Util();
        SL_WhiskySpt_Util WhiskySpt = new SL_WhiskySpt_Util();

        ~SL_ExcuteCmd()
        {
#if (INSTRUSUPPORT)
            foreach (SL_Equip_Util InstruUtil in EquipUtil)
            {
                if (InstruUtil != null && InstruUtil.isOpen()) InstruUtil.Close();
            }
#endif
        }

        public string ExamScript(string[] Commands, ref List<ScriptInfo> ScriptInfo)
        {
            return ElecsSpt.ExamScript(Commands, ref ScriptInfo);
        }

        public bool ExeScriptFile(string FileName,ref string RdStr)
        {
            string SptFilePath = null, ErrInfo = null;
            int ErrCode = 0;
            SL_IO_Util IOUtil = new SL_IO_Util();
            List<ScriptInfo> lScriptInfo = new List<ScriptInfo>();
            if (!IOUtil.FileExist(FileName,ref SptFilePath)) { RdStr += "Script not Exist"; return false; }
            ErrInfo = ElecsSpt.ExamScript(IOUtil.ReadFile(SptFilePath), ref lScriptInfo);
            if (!String.IsNullOrEmpty(ErrInfo)) { RdStr = ErrInfo; return false; }

            for (int i = 0; i < lScriptInfo.Count; i++)
            {
                if (ElecsSpt.ExamCmd(lScriptInfo[i].Command.Trim(), ref ErrCode))
                {
                    if(ElecsSpt.GetCmdClass() == 0  || ElecsSpt.GetCmdClass() == 3)
                    { 
                        SetDevices(ElecsSpt.getCommAddr(), ref ElecsComm, ElecsSpt.getInstruAddr());
                        ProcessCmd(ElecsSpt.GetElecsCmd(), ElecsSpt.GetElecsType(), ref RdStr);
                        Thread.Sleep(20);
                    }
                }
            }
            return true;
        }


        public bool ExamCmd(string Command,ref int ret)
        {
            return ElecsSpt.ExamCmd(Command, ref ret);
        }

        public bool RunCmdWithoutExam(string Command, ref string retStr)
        {
            int Err = 0;
            bool ret = true;
            ret = ElecsSpt.ExamCmd(Command, ref Err);
            ret = ProcessCmd(Command, ElecsSpt.GetCmdType(), ElecsSpt.GetCmdClass(), ElecsSpt.GetCmdReg(), ref retStr);
            return ret;
        }

        public bool RunCmd(string Command,ref string retStr)
        {
            int Err = 0;
            bool ret = true;
            if (ElecsSpt.ExamCmd(Command, ref Err)) ret = ProcessCmd(Command, ElecsSpt.GetCmdType(), ElecsSpt.GetCmdClass(), ElecsSpt.GetCmdReg(), ref retStr);
            return ret;
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

        public bool SetDevices(string CommPort, ref SL_Comm_Util Comm,string IntruAddr)
        {
            this.CommAddr = CommPort;
            this.ElecsComm = Comm;
            this.InstruAddr = IntruAddr;
            return true;         
        }

        public bool ProcessCmd(string Command ,ElecsCmd Elecs,ref string RdStr)
        {
            return ProcessCmd(Command, Elecs.Type, Elecs.Class,Elecs.Reg ,ref RdStr);
        }

        public bool Open(string CommAddr)
        {
            ElecsComm = new SL_Comm_Util(CommAddr, "115200", "None", "8", "1");  //20170724
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
                if (ret)Thread.Sleep(delaytime);
            }else
                ret = ElecsComm.Write(Command);
            return  ret ;
        }

        public bool Read(ref string  RdCmd)
        {
            return ElecsComm.Read(ref RdCmd);
        }

        public bool WriteRead(string Command, ref string RdCmd)
        {
            if (Command.LastIndexOf(EndTokenChars) < 0) Command = Command + EndTokenChars;
            return  ElecsComm.WriteAndRead(Command, ref RdCmd);
        }

        public bool Status()
        {
            return ElecsComm.isOpen();
        }

        public bool ComSetting(string CommAddr, ref SL_Comm_Util Comm)
        {
            bool ret = false;
            ElecsComm = new SL_Comm_Util(CommAddr, "115200", "8", "None", "1");
            if (ElecsComm.CommOpen()) { Comm = ElecsComm; ret = true; }
            return ret;
        }

        public string getCommName() { return ElecsSpt.getCommAddr(); }
        public string getInstruName() { return ElecsSpt.getInstruAddr(); }
        public string getElecsCmd() { return ElecsSpt.GetElecsCmd(); }
        public byte getCmdClass() { return ElecsSpt.GetCmdClass(); }
        public string ReadInfo(int Line, string Result) { return ElecsSpt.ReadInfo(Line, Result); }       
        public string ErrResult(string Info, int Line) { return ElecsSpt.ErrResult(Info, Line); }
        public ElecsCmd getElecsType() { return ElecsSpt.GetElecsType(); }

#if (INSTRUSUPPORT)

        private bool InstrRead(int Count ,ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughRead(ref RdStr);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool InstrSend(int Count,string instruCmd, ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughSend(instruCmd);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool InstrSendRead(int Count,string instruCmd, ref string RdStr)
        {
            bool ret = true;
            if (!EquipUtil[Count].isOpen()) ret = EquipUtil[Count].Open();
            if (ret)
                ret = EquipUtil[Count].ToughSendRead(instruCmd, ref RdStr);
            else
                RdStr = ERR_INSTRUOPEN;
            return ret;
        }

        private bool SearchInstruName(ref int Count)
        {
            string InstrName = this.InstruAddr;
            int Match = -1;
 
            for(int i =0;i< EquipUtil.Length;i++ )
            {
                if (String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && String.IsNullOrEmpty(EquipUtil[i].getInstruNickName()) ) continue;
                if ((EquipUtil[i].getInstrName().CompareTo(InstrName) == 0) || EquipUtil[i].getInstruNickName().CompareTo(InstrName) == 0)
                {
                    Count = Match = i;
                    break;
                }
            }
      
            if(Match < 0) SearchAndAddList(InstrName,ref Count); 

            return (Count >= 0) ? true : false ;
        }

        private bool SearchAndAddList(string InstruName,ref int Count)
        {
            bool ret = true;
            string[] Token = InstruName.Split(DelimiterChars);
            int Match = -1;
            string NickName =null;

            for(int i=0;i< EquipUtil.Length;i++)
            {
                if (!String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && EquipUtil[i].getInstrName().CompareTo(Token[0]) == 0)
                {
                    EquipUtil[i].SetInstruName(Token[0], Token[1]);
                    Count =  Match = i;
                    break;
                }   
            }

            if(Match < 0)
            {
                for (int i = 0; i < EquipUtil.Length; i++)
                {
                    if(String.IsNullOrEmpty(EquipUtil[i].getInstrName()) && String.IsNullOrEmpty(EquipUtil[i].getInstruNickName()))
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
            for (int i = 0; i < EquipUtil.Length; i++){ if (EquipUtil[i] == null) EquipUtil[i] = new SL_Equip_Util();}
            switch (InstruType)
            {
                case 0:
                    if(SearchInstruName(ref Count)) ret = InstrRead(Count,ref RdStr);
                    break;
                case 1:
                    if (SearchInstruName(ref Count)) ret = InstrSend(Count,InstruCmd, ref RdStr);
                    break;
                case 2:
                    if (SearchInstruName(ref Count)) ret = InstrSendRead(Count,InstruCmd, ref RdStr);
                    break;
                case 3:
                    ret = SearchAndAddList(InstruCmd,ref Count);
                    break;
                default:
                    break;
            }

            return ret;
        }
#endif

        private bool ProcessCmd(string Command, byte Type, byte Class,byte Reg, ref string RdStr)
        {
            bool ret = true;
            if (Class == 0) ret = DealWithComm(Command, Type, ref RdStr);
            if (Class == 1) ret = DealWithSystem(Command, Type, ref RdStr);
#if (INSTRUSUPPORT)
            if (Class == 2) ret = DealWithInstr(Command, Type, ref RdStr);
#endif
            if (Class == 3) ret = DealWithWhisky(Command, Type,Reg, ref RdStr);
            return ret;
        }

        private string SystemInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        private bool DealWithSystem(string SystemCmd, byte ElecsType, ref string RdStr)
        {
            int Times = 0;
            if (ElecsType == (byte)CmdType.Write)
            {
                string[] Parameter = SystemCmd.Split(DelimiterChars);
                if (ElecsComm != null && Parameter[0].CompareTo("elecs.close") == 0) { ElecsComm.CommClose(); ElecsComm = null; }
                if (Parameter[0].CompareTo("system") == 0) { RdStr = SystemInfo(); }
                if (Parameter[0].CompareTo("pause") == 0) { RdStr = "Pause"; }
                if (Parameter[0].CompareTo("sleep") == 0){ if (Parameter.Length > 1 && int.TryParse(Parameter[1], out Times))Thread.Sleep(Times);}
                if (Parameter[0].CompareTo("load") == 0) { if (Parameter.Length > 1) ExeScriptFile(Parameter[1], ref RdStr); }
            }
            return true;
        }

        private bool DealWithComm(string ElecsCmd, byte ElecsType,ref string RdStr)
        {
            bool ret = true;
            int Times = 0;
            string[] Token = ElecsCmd.Split(DelimiterChars);

            if (Token[0].ToLower().CompareTo(DELAYCMD) == 0)
            {
                int.TryParse(Token[1], out Times);
                Thread.Sleep(Times);
                return true;
            }

            if (ElecsComm == null) { RdStr = "ERROR Open Device Err"; return false; }

            ElecsCmd = ElecsCmd + EndTokenChars;

            if (ElecsType == (byte)CmdType.Write)
                ret = ElecsComm.Write(ElecsCmd);
            else if (ElecsType == (byte)CmdType.WrAndRd)
                ret = ElecsComm.WriteAndRead(ElecsCmd , ref RdStr);
            else
                ret = ElecsComm.Read(ref RdStr);

            return ret;
        }

        private bool DealWithWhisky(string ElecsCmd, byte ElecsType,byte ElecsReg, ref string RdStr)
        {
            bool ret = true;
            ret = WhiskySpt.ExcuteCmd(ElecsCmd, ElecsType,ElecsReg, ref RdStr);
            return ret;
        }
    }
}
