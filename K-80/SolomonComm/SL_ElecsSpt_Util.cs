using System.Collections;
using System.Collections.Generic;

namespace SL_Tek_Studio_Pro
{
    /*
       class ElecsCmd
        {
             string Cmd : 
                ELSCS E7422 Command
             byte Type :  
                R/W/WR/Other: 0/1/2/3
             byte Category :
                0:  No Parameter
                1:  Examine Parameter as String
                2:  Examine Parameter as Numeric
                3:  Examine Parameter as Numeric and Parameter Num Equal
                4:  Examine the last One Item (String)
                5:  Examine Parameter as Float
                6.  Examine Parameter as String and Parameter Num Equal
                Appendix: Parameters are bigger then zero (all of above)
             int  MaxParm : 
                R/W Paramenter Number  Range: 0~65535
             int MaxValue :  
                0~65535
             byte Class:
                0:  ELECS Command
				1:  System Command
                2:  Instrument Command
                3.  Solomon Command
            byte Reg:
                Register Address
             ArrayList Item :
                Special Compare Item
        }
     */

    public class ScriptInfo
    {
        public string Command { get; set; }
        public bool Result { get; set; }
        public string Message { get; set; }

    }

    public class ElecsCmd
    {
        public string Cmd { get; set; }
        public byte Type { get; set; }
        public int MaxParm { get; set; }
        public int MaxValue { get; set; }
        public byte Category { get; set; }
        public byte Class { get; set; }
        public byte Reg { get; set; }
        public ArrayList Item { get; set; }
    }

    public class ElecsResult
    {
        public string ElecsCmd { get; set; }
        public int Line { get; set; }
        public string Result { get; set; }

        public ElecsResult(string ElecsCmd, int Line, string Result)
        {
            this.ElecsCmd = ElecsCmd;
            this.Line = Line;
            this.Result = Result;
        }
    }

    class SL_ElecsSpt_Util
    {
        private string SplitLine = "\r\n", SendCommand = null;
        private char[] DelimiterChars = { ' ', ',', '\t' };
        private string Comment = "#";
        private string CommentSlash = "//";
        private const int MAXCMDS = 130;
        public ElecsCmd[] E7422Cmds = new ElecsCmd[MAXCMDS];
        private int Count = 0;
        private int ElecsOpt = -1;
        private const string READMSG = "READ";
        private const string ERRMSG = "ERROR";
        private const int ERRELECSCMD = 1;
        private const int ERRELECSPARM = 2;
        public SL_ElecsSpt_Util()
        {

            for (int i = 0; i < MAXCMDS; i++)
            {
                E7422Cmds[i] = new ElecsCmd();
                E7422Cmds[i].Item = new ArrayList();
            }

            //id
            E7422Cmds[Count].Cmd = "id";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //version
            E7422Cmds[Count].Cmd = "version";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Delay
            E7422Cmds[Count].Cmd = "delay";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 10000;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //bridge.reset
            E7422Cmds[Count].Cmd = "bridge.reset";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi.host.reset
            E7422Cmds[Count].Cmd = "mipi.host.reset";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI Write
            E7422Cmds[Count].Cmd = "spi.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI1 Write
            E7422Cmds[Count].Cmd = "spi1.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI2 Write
            E7422Cmds[Count].Cmd = "spi2.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge Write
            E7422Cmds[Count].Cmd = "bridge.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI Read
            E7422Cmds[Count].Cmd = "spi.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI1 Read
            E7422Cmds[Count].Cmd = "spi1.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI2 Read
            E7422Cmds[Count].Cmd = "spi2.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge Read
            E7422Cmds[Count].Cmd = "bridge.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI Write Set
            E7422Cmds[Count].Cmd = "spi.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge Write Set
            E7422Cmds[Count].Cmd = "bridge.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI1 Write Set
            E7422Cmds[Count].Cmd = "spi1.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge1 Write Set
            E7422Cmds[Count].Cmd = "bridge1.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI2 Write Set
            E7422Cmds[Count].Cmd = "spi2.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge2 Write Set
            E7422Cmds[Count].Cmd = "bridge2.write.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI Write Clear
            E7422Cmds[Count].Cmd = "spi.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge Write Set
            E7422Cmds[Count].Cmd = "bridge.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI1 Write Clear
            E7422Cmds[Count].Cmd = "spi1.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge1 Write Set
            E7422Cmds[Count].Cmd = "bridge1.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //SPI2 Write Clear
            E7422Cmds[Count].Cmd = "spi2.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Bridge2 Write Set
            E7422Cmds[Count].Cmd = "bridge2.write.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Write
            E7422Cmds[Count].Cmd = "mipi.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1 Write
            E7422Cmds[Count].Cmd = "mipi1.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2 Write
            E7422Cmds[Count].Cmd = "mipi2.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Write Hs
            E7422Cmds[Count].Cmd = "mipi.write.hs";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1 Write Hs
            E7422Cmds[Count].Cmd = "mipi1.write.hs";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2 Write Hs
            E7422Cmds[Count].Cmd = "mipi2.write.hs";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 65535;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Read
            E7422Cmds[Count].Cmd = "mipi.read";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1 Read
            E7422Cmds[Count].Cmd = "mipi1.read";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2 Read
            E7422Cmds[Count].Cmd = "mipi2.read";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi.dsi
            E7422Cmds[Count].Cmd = "mipi.dsi";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 4;
            E7422Cmds[Count].MaxParm = 4;
            E7422Cmds[Count].MaxValue = 4096;
            E7422Cmds[Count].Class = 0;
            E7422Cmds[Count].Item.Add("command");
            E7422Cmds[Count].Item.Add("burst");
            E7422Cmds[Count].Item.Add("nonburst");
            Count = Count + 1;

            //Mipi.mode
            E7422Cmds[Count].Cmd = "mipi.mode";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 4;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            E7422Cmds[Count].Item.Add("single");
            E7422Cmds[Count].Item.Add("twin");
            Count = Count + 1;

            //mipi.video
            E7422Cmds[Count].Cmd = "mipi.video";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 10;
            E7422Cmds[Count].MaxValue = 4096;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi.clock.enable
            E7422Cmds[Count].Cmd = "mipi.clock.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi1.clock.enable
            E7422Cmds[Count].Cmd = "mipi1.clock.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi2.clock.enable
            E7422Cmds[Count].Cmd = "mipi2.clock.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi.clock.disable
            E7422Cmds[Count].Cmd = "mipi.clock.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi1.clock.disable
            E7422Cmds[Count].Cmd = "mipi1.clock.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //mipi2.clock.disable
            E7422Cmds[Count].Cmd = "mipi2.clock.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi.lane.enable
            E7422Cmds[Count].Cmd = "mipi.lane.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1.lane.enable
            E7422Cmds[Count].Cmd = "mipi1.lane.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2.lane.enable
            E7422Cmds[Count].Cmd = "mipi2.lane.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi.lane.disable
            E7422Cmds[Count].Cmd = "mipi.lane.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1.lane.disable
            E7422Cmds[Count].Cmd = "mipi1.lane.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2.lane.disable
            E7422Cmds[Count].Cmd = "mipi12.lane.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi.video.enable
            E7422Cmds[Count].Cmd = "mipi.video.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1.video.enable
            E7422Cmds[Count].Cmd = "mipi1.video.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2.video.enable
            E7422Cmds[Count].Cmd = "mipi2.video.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi.video.disable
            E7422Cmds[Count].Cmd = "mipi.video.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi1.video.disable
            E7422Cmds[Count].Cmd = "mipi1.video.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi2.video.disable
            E7422Cmds[Count].Cmd = "mipi2.video.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Timing LP
            E7422Cmds[Count].Cmd = "mipi.timing.lp";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 5;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Timing Data
            E7422Cmds[Count].Cmd = "mipi.timing.data";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 5;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Mipi Timing Clk
            E7422Cmds[Count].Cmd = "mipi.timing.clk";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 5;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power.on
            E7422Cmds[Count].Cmd = "power.on";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1 on
            E7422Cmds[Count].Cmd = "power1.on";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2 on
            E7422Cmds[Count].Cmd = "power2.on";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power.off
            E7422Cmds[Count].Cmd = "power.off";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 256;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1.off
            E7422Cmds[Count].Cmd = "power1.off";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 256;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2.off
            E7422Cmds[Count].Cmd = "power2.off";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 256;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power LED
            E7422Cmds[Count].Cmd = "power.led";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1 LED
            E7422Cmds[Count].Cmd = "power1.led";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2 LED
            E7422Cmds[Count].Cmd = "power2.led";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power Current
            E7422Cmds[Count].Cmd = "power.current";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1 Current
            E7422Cmds[Count].Cmd = "power1.current";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2 Current
            E7422Cmds[Count].Cmd = "power2.current";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power Voltage
            E7422Cmds[Count].Cmd = "power.voltage";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power Level
            E7422Cmds[Count].Cmd = "power.level";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 5;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1 Voltage
            E7422Cmds[Count].Cmd = "power1.voltage";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power1 Level
            E7422Cmds[Count].Cmd = "power1.level";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 5;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2 Voltage
            E7422Cmds[Count].Cmd = "power2.voltage";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Power2 Level
            E7422Cmds[Count].Cmd = "power2.level";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 5;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 10;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio Set
            E7422Cmds[Count].Cmd = "gpio.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio1 Set
            E7422Cmds[Count].Cmd = "gpio1.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio2 Set
            E7422Cmds[Count].Cmd = "gpio2.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio Clr
            E7422Cmds[Count].Cmd = "gpio.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio1 Clr
            E7422Cmds[Count].Cmd = "gpio1.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio2 Clr
            E7422Cmds[Count].Cmd = "gpio2.clr";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio Write
            E7422Cmds[Count].Cmd = "gpio.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio1 Write
            E7422Cmds[Count].Cmd = "gpio1.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio2 Write
            E7422Cmds[Count].Cmd = "gpio2.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 5;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 5;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio.dir
            E7422Cmds[Count].Cmd = "gpio.dir";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio.Level
            E7422Cmds[Count].Cmd = "gpio.level";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 5;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio.i2c.set
            E7422Cmds[Count].Cmd = "gpio.i2c.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio.i2c.write
            E7422Cmds[Count].Cmd = "gpio.i2c.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65536;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Gpio.output.enable
            E7422Cmds[Count].Cmd = "gpio.output.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Fill
            E7422Cmds[Count].Cmd = "image.fill";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 4;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Display
            E7422Cmds[Count].Cmd = "image.display";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Show
            E7422Cmds[Count].Cmd = "image.show";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image First
            E7422Cmds[Count].Cmd = "image.first";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Next
            E7422Cmds[Count].Cmd = "image.next";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Prev
            E7422Cmds[Count].Cmd = "image.prev";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Repeat
            E7422Cmds[Count].Cmd = "image.repeat";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Enable
            E7422Cmds[Count].Cmd = "image.enable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Image Disable
            E7422Cmds[Count].Cmd = "image.disable";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 0;
            Count = Count + 1;

            //Elecs Comm Open
            E7422Cmds[Count].Cmd = "load";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //Elecs Comm Open
            E7422Cmds[Count].Cmd = "elecs.open";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //Elecs Comm Close
            E7422Cmds[Count].Cmd = "elecs.close";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //Elecs Comm Close
            E7422Cmds[Count].Cmd = "pause";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //Sleep
            E7422Cmds[Count].Cmd = "sleep";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 10000;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //System
            E7422Cmds[Count].Cmd = "system";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 1;
            Count = Count + 1;

            //Instrument Read
            E7422Cmds[Count].Cmd = "instr.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 2;
            Count = Count + 1;

            //Instrument Write
            E7422Cmds[Count].Cmd = "instr.send";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 2;
            Count = Count + 1;

            //Instrument Send and Read
            E7422Cmds[Count].Cmd = "instr.sendread";
            E7422Cmds[Count].Type = 2;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 2;
            Count = Count + 1;

            //Instrument Define
            E7422Cmds[Count].Cmd = "instr.define";
            E7422Cmds[Count].Type = 3;
            E7422Cmds[Count].Category = 6;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 2;
            Count = Count + 1;

            //Solomon Whiskey Setting
            E7422Cmds[Count].Cmd = "ssl.fpga.set";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 7;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Gpio Direction
            E7422Cmds[Count].Cmd = "ssl.gpio.dir";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Reg = 0xfa;
            Count = Count + 1;

            //Solomon GpioH Write
            E7422Cmds[Count].Cmd = "ssl.gpioh.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Reg = 0xfb;
            Count = Count + 1;

            //Solomon Gpiol Write
            E7422Cmds[Count].Cmd = "ssl.gpiol.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Reg = 0xfc;
            Count = Count + 1;

            //Solomon GpioH Read
            E7422Cmds[Count].Cmd = "ssl.gpioh.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Reg = 0xfb;
            Count = Count + 1;

            //Solomon GpioL Read
            E7422Cmds[Count].Cmd = "ssl.gpiol.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Reg = 0xfc;
            Count = Count + 1;

            //Solomon Gpio Clr
            E7422Cmds[Count].Cmd = "ssl.gpio.clr";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 0;
            E7422Cmds[Count].MaxParm = 1;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon mipi.video
            E7422Cmds[Count].Cmd = "ssl.mipi.video";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 10;
            E7422Cmds[Count].MaxValue = 4096;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Bridge Write
            E7422Cmds[Count].Cmd = "ssl.bridge.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon MIPI Write
            E7422Cmds[Count].Cmd = "ssl.mipi.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65535;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Mipi Read
            E7422Cmds[Count].Cmd = "ssl.mipi.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon I2C Write
            E7422Cmds[Count].Cmd = "ssl.i2c.write";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 2;
            E7422Cmds[Count].MaxParm = 65535;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Bridge Read
            E7422Cmds[Count].Cmd = "ssl.bridge.read";
            E7422Cmds[Count].Type = 0;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 3;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Bridge Select
            E7422Cmds[Count].Cmd = "ssl.bridge.select";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Image Fill
            E7422Cmds[Count].Cmd = "ssl.image.fill";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 3;
            E7422Cmds[Count].MaxParm = 4;
            E7422Cmds[Count].MaxValue = 255;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Image Show
            E7422Cmds[Count].Cmd = "ssl.image.show";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 1;
            E7422Cmds[Count].MaxParm = 2;
            E7422Cmds[Count].MaxValue = 0;
            E7422Cmds[Count].Class = 3;
            Count = Count + 1;

            //Solomon Mipi.dsi
            E7422Cmds[Count].Cmd = "ssl.mipi.dsi";
            E7422Cmds[Count].Type = 1;
            E7422Cmds[Count].Category = 4;
            E7422Cmds[Count].MaxParm = 4;
            E7422Cmds[Count].MaxValue = 4096;
            E7422Cmds[Count].Class = 3;
            E7422Cmds[Count].Item.Add("syncpulse");
            E7422Cmds[Count].Item.Add("burst");
            E7422Cmds[Count].Item.Add("syncevent");
            Count = Count + 1;
        }

        public string ExamScript(string[] Commands, ref List<ScriptInfo> ScriptInfo)
        {
            string ErrResult = null;

            int ErrCode = 0;
            for (int i = 0; i < Commands.Length; i++)
            {
                ScriptInfo Info = new ScriptInfo();
                Info.Command = Commands[i].Trim();
                if (!ExamCmd(Commands[i].Trim(), ref ErrCode))
                {
                    if (ErrCode == ERRELECSCMD)
                    {
                        Info.Message = this.ErrResult(Commands[i].Trim(), i);
                        ErrResult += Info.Message;
                        Info.Result = false;
                    }else if(ErrCode == ERRELECSPARM)
                    {
                        Info.Message = this.ErrResult(Commands[i].Trim(), i);
                        ErrResult += Info.Message;
                        Info.Result = false;
                    }
                    else
                        Info.Result = true;
                }
                else
                    Info.Result = true;


                ScriptInfo.Add(Info);
            }
            return ErrResult;
        }

        public bool ExamCmd(string Cmd, ref int ErrCode)
        {
            bool ret = true;
            string tmpCmd = null;
            if (CleanComment(Cmd, ref ErrCode, ref tmpCmd))
            {
                if (!VerifyToken(tmpCmd)) { ErrCode = 1; return false; }
                if (!VerifyParameter(tmpCmd)) {ErrCode = 2;return false;}
                if (!VerifyWarning(tmpCmd)) { ErrCode = 3; }
                SendCommand = tmpCmd + SplitLine;
                ErrCode = 0;
            }
            else
                return false;

            return ret;
        }

        public string getCommAddr()
        {
            string CommAddr = null;
            if (this.E7422Cmds[ElecsOpt].Class == 1)
            {
                string[] Cmds = this.SendCommand.Split(DelimiterChars);
                if (Cmds.Length == 2) CommAddr = Cmds[1].Trim();
            }
            return CommAddr;
        }

        public string getInstruAddr()
        {
            string DeviceAddr = null;
            if(this.E7422Cmds[ElecsOpt].Class == 2)
            {
                ArrayList Cmds = MergeElecsCmds(this.SendCommand);
                if (Cmds.Count > 1) DeviceAddr = Cmds[1].ToString();
            }
            return DeviceAddr;
        }

        public string GetElecsCmd()
        {
            string[] Cmds = (string[]) MergeElecsCmds(SendCommand.Trim()).ToArray(typeof(string)); 
            int ClassId = GetCmdClass();
            string Command = null;
            switch (ClassId)
            {
                case 0:
                case 1:
                    Command = this.SendCommand.Trim();
                    break;
                case 2:
                    if (Cmds.Length == 3)
                    {
                        if (GetCmdType() == 1 || GetCmdType() == 2)
                            Command = Cmds[2].Trim();
                        if (GetCmdType() == 3)
                            Command = string.Concat(Cmds[1], " ", Cmds[2]).Trim();
                    }
                    break;
                default:
                    Command = this.SendCommand.Trim();
                    break;
            }
            return Command;
        }

        public byte GetCmdClass() { return this.E7422Cmds[ElecsOpt].Class; }
        public byte GetCmdType() { return this.E7422Cmds[ElecsOpt].Type; }
        public byte GetCmdCategory() { return this.E7422Cmds[ElecsOpt].Category; }
        public byte GetCmdReg() { return this.E7422Cmds[ElecsOpt].Reg; }
        public ElecsCmd GetElecsType() { return E7422Cmds[this.ElecsOpt]; }
        public string ReadInfo(int Line, string Result){return READMSG + "[" + Line.ToString() + "]: " + Result.Trim() + SplitLine;}
        public string ReadInfo(ElecsResult Result){ return READMSG + "[" + Result.Line.ToString() + "]: " + Result.Result + SplitLine; }
        public string ErrResult(string Info, int Line){return ERRMSG + " : " + " " + Info + " (Line: " + Line.ToString() + " )" + SplitLine;}
        
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

        private bool CleanComment(string Cmd, ref int ErrCode, ref string mCmd)
        {
            bool ret = true;
            int Addr = 0;
            if (string.IsNullOrEmpty(Cmd)) {  return false; }
            int WellheadAddr = Cmd.IndexOf(Comment);
            int SlashAddr = Cmd.IndexOf(CommentSlash);

            if (WellheadAddr > 0 && SlashAddr > 0) ret =  false;
            if (WellheadAddr > 0 && SlashAddr < 0) Addr = WellheadAddr;
            if (WellheadAddr < 0 && SlashAddr > 0) Addr = SlashAddr;

            if (WellheadAddr == 0 || SlashAddr == 0) { ret = false; }
            if (WellheadAddr < 0 && SlashAddr < 0) { mCmd = Cmd.Trim(); }
            if (Addr > 0) { mCmd = Cmd.Substring(0, Addr).Trim(); }

            return ret;
        }

        private bool VerifyToken(string Command)
        {
            bool ret = false;
            string[] CmdToken = Command.Split(DelimiterChars);

            for (int i = 0; i < E7422Cmds.Length; i++)
            {
                if (CmdToken[0].CompareTo(E7422Cmds[i].Cmd) == 0)
                {
                    ElecsOpt = i;
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        private bool VerifyWarning(string Command)
        {
            ArrayList eCmdList = MergeElecsCmds(Command);
            ElecsCmd optElecs = E7422Cmds[ElecsOpt];
            SL_IO_Util SlUtil = new SL_IO_Util();
            int Value = 0;
            int Count = eCmdList.Count;

            if (optElecs.Category == 0 || optElecs.Category == 1 || optElecs.Category == 5) return true;
            if (optElecs.Category == 4) Count = Count - 1;

            for (int i = 1; i < Count; i++)
            {
                if (SlUtil.isStrtoInt(eCmdList[i].ToString(), ref Value))
                    if (!SlUtil.VerifyStrLength(eCmdList[i].ToString()))
                        return false;
            }
            return true;
        }

        private bool VerifyParameter(string Command)
        {
            bool VerifyParm = true, VerifyItem = true ;
            ArrayList eCmdList = MergeElecsCmds(Command);
            ElecsCmd optElecs = E7422Cmds[ElecsOpt];
            SL_IO_Util SlUtil = new SL_IO_Util();
            int Value = 0;
            float fValue = 0;
            switch (optElecs.Category)
            {
                case 0:
                    if (eCmdList.Count > 1) VerifyParm = false;
                    break;
                case 1:
                    if (eCmdList.Count < 1 || eCmdList.Count > optElecs.MaxParm) VerifyParm = false;
                    break;
                case 2:
                    if (eCmdList.Count < 1 || eCmdList.Count > optElecs.MaxParm) VerifyParm = false;
                    for (int i = 1; i < eCmdList.Count; i++)
                    {
                        if (!SlUtil.isStrtoInt(eCmdList[i].ToString(), ref Value))
                            VerifyParm = false;
                    }
                    break;
                case 3:
                    if (eCmdList.Count != optElecs.MaxParm) VerifyParm = false;
                    for (int i = 1; i < eCmdList.Count; i++)
                    {
                        if (!SlUtil.ExamStrAndWithin(eCmdList[i].ToString(), 0, optElecs.MaxValue, ref Value))
                            VerifyParm = false;
                    }
                    break;
                case 4:
                    VerifyItem = false;
                    if (eCmdList.Count < 1 || eCmdList.Count > optElecs.MaxParm) VerifyParm = false;
                    for (int i = 1; i < eCmdList.Count - 1; i++)
                    {
                        if (!SlUtil.ExamStrAndWithin(eCmdList[i].ToString(), 0, optElecs.MaxValue, ref Value))
                            VerifyParm = false;
                    }
                    for (int i = 0; i < optElecs.Item.Count; i++)
                    {
                        if (optElecs.Item[i].ToString().CompareTo(eCmdList[eCmdList.Count - 1].ToString()) == 0)
                            VerifyItem = true;        
                    }
                    break;
                case 5:
                    if (eCmdList.Count < 1 || eCmdList.Count > optElecs.MaxParm) VerifyParm = false;
                    for (int i = 1; i < eCmdList.Count; i++)
                    {
                        if (!SlUtil.isStrtoFloat(eCmdList[i].ToString(), ref fValue))
                            VerifyParm = false;
                    }
                    break;
                case 6:
                    if (eCmdList.Count != optElecs.MaxParm) VerifyParm = false;
                    break;
                default:
                    break;
            }

            return VerifyParm == true && VerifyItem == true;
        }

    }
}
