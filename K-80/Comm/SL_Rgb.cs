using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    class SL_Rgb:SL_Comm
    {
        public override void SetInterfaceParm(int index) { }
        public override void Comm_RegWrite() { Console.WriteLine("SC_Rgb Write"); }
        public override void Comm_RegRead() { Console.WriteLine("SC_Rgb Read"); }
        public override void Comm_IO() { Console.WriteLine("SC_Rgb Comm_IO"); }
    }
}
