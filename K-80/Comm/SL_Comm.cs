using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    class SL_Comm
    {
        public bool Device_Open(ushort Vid, ushort Pid) { return true; }
        public bool Device_Close(ushort Vid, ushort Pid) { return true; }
        public virtual void SetInterfaceParm(int index) { }
        public virtual void Comm_RegWrite() { Console.WriteLine("SC_Comm Reg Write"); }
        public virtual void Comm_RegRead() { Console.WriteLine("SC_Comm Reg Read"); }
        public virtual void Comm_IO() { Console.WriteLine("SC_Comm Reg IO"); }
    }
}
