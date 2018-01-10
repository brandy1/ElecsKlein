using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    class SL_Visa_Util: VisaInstrument
    {

        public string SimpleDoQuery(string visaEquitAddr, string strCommand)
        {
            int status = 0,ret =0;
            StringBuilder strResults = new StringBuilder(1000);
            byte[] StrtoBytes = Encoding.ASCII.GetBytes(strCommand);
            string RdStr = null;
            m_strVisaAddress = visaEquitAddr;
            OpenSimpleSession();
            /* Set the timeout for message-based communication*/
            SetSimpleTimeOut(5);
            /* Ask the device for identification */
            visa32.viWrite(m_nSession, StrtoBytes, StrtoBytes.Length, out ret);           
            visa32.viRead(m_nSession, out RdStr, 256);
            /* Your code should process the data */
            CloseSession();
            return RdStr;
        }

        public void CloseSession()
        {
            if (m_nSession != 0)
                visa32.viClose(m_nSession);
        }

        public string SimpleDoCommand(string visaEquitAddr, string strCommand)
        {
            int status = 0;
            StringBuilder strResults = new StringBuilder(1000);
            m_strVisaAddress = visaEquitAddr;
            OpenSimpleSession();
            /* Set the timeout for message-based communication*/
            SetSimpleTimeOut(5);
            /* Ask the device for identification */
            status = visa32.viPrintf(this.m_nSession, strCommand);
            status = visa32.viScanf(this.m_nSession, "%1000t", strResults);
            /* Your code should process the data */
            Close();
            return strResults.ToString();
         }

        public void SetSimpleTimeOut(int nSeconds)
        {
            int nViStatus;
            nViStatus = visa32.viSetAttribute(this.m_nSession,
                                              visa32.VI_ATTR_TMO_VALUE, nSeconds * 1000);
        }

        private void OpenSimpleSession()
        {
            int nViStatus;
            nViStatus = visa32.viOpen(this.m_nResourceManager,
                                      this.m_strVisaAddress, visa32.VI_NULL,
                                      visa32.VI_NULL, out this.m_nSession);
        }
    }
}
