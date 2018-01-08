using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    class SL_Cpu : SL_Comm
    {
        public override void SetInterfaceParm(int index) { }
        public override void Comm_RegWrite() { Console.WriteLine("SC_Cpu Reg Write"); }
        public override void Comm_RegRead() { Console.WriteLine("SC_Cpu Reg Read"); }
        public override void Comm_IO() { Console.WriteLine("SC_Cpu Comm IO"); }
    }
}
