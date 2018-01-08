using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    class SL_Color_Util
    {
        enum ChipTypeList { SSL2X30, SSL2X25};
        SL_Color_Base ColorUtil = null;

        bool SSL_Color_SetChipType(byte ChipType)
        {
            switch (ChipType)
            {
                case 0:
                    ColorUtil = new SL_Color_SSL2X30();
                    break;
                case 1:
                    ColorUtil = new SL_Color_SSL2X25();
                    break;
                default:
                    break;
            }
            return true; 
        }

    }
}

