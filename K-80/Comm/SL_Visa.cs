/*
* Agilent VISA Example in C#
* -------------------------------------------------------------------
* This program illustrates a few commonly used programming
* features of your Agilent oscilloscope.
* -------------------------------------------------------------------
*/
using System;
using System.IO;
using System.Text;

namespace SL_Tek_Studio_Pro
{    
    class VisaInstrument
    {
	    public int m_nResourceManager;
        public int m_nSession;
        public string m_strVisaAddress;
	    // Constructor.
	    public VisaInstrument(string strVisaAddress)
	    {
		    // Save VISA addres in member variable.
		    m_strVisaAddress = strVisaAddress;
		    // Open the default VISA resource manager.
		    OpenResourceManager();
		    // Open a VISA resource session.
		    OpenSession();
		    // Clear the interface.
		    int nViStatus;
		    nViStatus = visa32.viClear(m_nSession);
	    }

        public VisaInstrument()
        {
            // Open the default VISA resource manager.
            OpenResourceManager();
        }

	    public void DoCommand(string strCommand)
	    {
		    // Send the command.
		    VisaSendCommandOrQuery(strCommand);
		    // Check for inst errors.
		    CheckInstrumentErrors(strCommand);
	    }

	    public int DoCommandIEEEBlock(string strCommand,
	                                  byte[] DataArray)
	    {
		    // Send the command to the device.
		    string strCommandAndLength;
		    int nViStatus, nLength, nBytesWritten;
		    nLength = DataArray.Length;
		    strCommandAndLength = String.Format("{0} #8%08d",
		                                        strCommand);
		    // Write first part of command to formatted I/O write buffer.

		    nViStatus = visa32.viPrintf(m_nSession, strCommandAndLength,
		                                nLength);
		    CheckVisaStatus(nViStatus);
		    // Write the data to the formatted I/O write buffer.
		    nViStatus = visa32.viBufWrite(m_nSession, DataArray, nLength,
		                                  out nBytesWritten);
		    CheckVisaStatus(nViStatus);
		    // Check for inst errors.
		    CheckInstrumentErrors(strCommand);
		    return nBytesWritten;
	    }

	    public StringBuilder DoQueryString(string strQuery)
	    {
		    // Send the query.
		    VisaSendCommandOrQuery(strQuery);
		    // Get the result string.
		    StringBuilder strResults = new StringBuilder(1000);
		    strResults = VisaGetResultString();
		    // Check for inst errors.
		    CheckInstrumentErrors(strQuery);
		    // Return string results.
		    return strResults;
	    }

	    public double DoQueryNumber(string strQuery)
	    {
		    // Send the query.
		    VisaSendCommandOrQuery(strQuery);
		    // Get the result string.
		    double fResults;
		    fResults = VisaGetResultNumber();
		    // Check for inst errors.
		    CheckInstrumentErrors(strQuery);
		    // Return string results.
		    return fResults;
	    }

	    public double[] DoQueryNumbers(string strQuery)
	    {
		    // Send the query.
		    VisaSendCommandOrQuery(strQuery);
		    // Get the result string.
		    double[] fResultsArray;
		    fResultsArray = VisaGetResultNumbers();
		    // Check for inst errors.

		    CheckInstrumentErrors(strQuery);
		    // Return string results.
		    return fResultsArray;
	    }

	    public int DoQueryIEEEBlock(string strQuery,
	                                out byte[] ResultsArray)
	    {
		    // Send the query.
		    VisaSendCommandOrQuery(strQuery);
		    // Get the result string.
		    int length; // Number of bytes returned from instrument.
		    length = VisaGetResultIEEEBlock(out ResultsArray);
		    // Check for inst errors.
		    CheckInstrumentErrors(strQuery);
		    // Return string results.
		    return length;
	    }

	    private void VisaSendCommandOrQuery(string strCommandOrQuery)
	    {
		    // Send command or query to the device.
		    string strWithNewline;
		    strWithNewline = String.Format("{0}\n", strCommandOrQuery);
		    int nViStatus;
		    nViStatus = visa32.viPrintf(m_nSession, strWithNewline);
		    CheckVisaStatus(nViStatus);
	    }

	    private StringBuilder VisaGetResultString()
	    {
		    StringBuilder strResults = new StringBuilder(1000);
		    // Read return value string from the device.
		    int nViStatus;
		    nViStatus = visa32.viScanf(m_nSession, "%1000t", strResults);
		    CheckVisaStatus(nViStatus);
		    return strResults;
	    }

	    private double VisaGetResultNumber()
	    {
		    double fResults = 0;
		    // Read return value string from the device.
		    int nViStatus;
		    nViStatus = visa32.viScanf(m_nSession, "%lf", out fResults);
		    CheckVisaStatus(nViStatus);
		    return fResults;
	    }

	    private double[] VisaGetResultNumbers()
	    {
		    double[] fResultsArray;
		    fResultsArray = new double[10];
		    // Read return value string from the device.
		    int nViStatus;
		    nViStatus = visa32.viScanf(m_nSession, "%,10lf\n",
		                               fResultsArray);
		    CheckVisaStatus(nViStatus);
		    return fResultsArray;
	    }

	    private int VisaGetResultIEEEBlock(out byte[] ResultsArray)
	    {
		    // Results array, big enough to hold a PNG.
		    ResultsArray = new byte[300000];
		    int length; // Number of bytes returned from instrument.
		    // Set the default number of bytes that will be contained in
		    // the ResultsArray to 300,000 (300kB).
		    length = 300000;
		    // Read return value string from the device.
		    int nViStatus;
		    nViStatus = visa32.viScanf(m_nSession, "%#b", ref length,
		                               ResultsArray);
		    CheckVisaStatus(nViStatus);
		    // Write and read buffers need to be flushed after IEEE block?
		    nViStatus = visa32.viFlush(m_nSession, visa32.VI_WRITE_BUF);
		    CheckVisaStatus(nViStatus);
		    nViStatus = visa32.viFlush(m_nSession, visa32.VI_READ_BUF);
		    CheckVisaStatus(nViStatus);
		    return length;
	    }

	    private void CheckInstrumentErrors(string strCommand)
	    {
		    // Check for instrument errors.
		    StringBuilder strInstrumentError = new StringBuilder(1000);
		    bool bFirstError = true;
		    do // While not "0,No error"
		    {
			    VisaSendCommandOrQuery(":SYSTem:ERRor?");
			    strInstrumentError = VisaGetResultString();
			    if (!strInstrumentError.ToString().StartsWith("+0,"))
			    {
				    if (bFirstError)
				    {
					    Console.WriteLine("ERROR(s) for command '{0}': ",
					                      strCommand);

					    bFirstError = false;
				    }
				    Console.Write(strInstrumentError);
			    }
		    }
		    while (!strInstrumentError.ToString().StartsWith("+0,"));
	    }

	    private void OpenResourceManager()
	    {
		    int nViStatus;
		    nViStatus =
		        visa32.viOpenDefaultRM(out this.m_nResourceManager);
		    if (nViStatus < visa32.VI_SUCCESS)
			    throw new
			    ApplicationException("Failed to open Resource Manager");
	    }

	    private void OpenSession()
	    {
		    int nViStatus;
		    nViStatus = visa32.viOpen(this.m_nResourceManager,
		                              this.m_strVisaAddress, visa32.VI_NO_LOCK,
		                              visa32.VI_TMO_IMMEDIATE, out this.m_nSession);
		    CheckVisaStatus(nViStatus);
	    }

        public void SetTimeoutSeconds(int nSeconds)
	    {
		    int nViStatus;
		    nViStatus = visa32.viSetAttribute(this.m_nSession,
		                                      visa32.VI_ATTR_TMO_VALUE, nSeconds * 1000);
		    CheckVisaStatus(nViStatus);
	    }

	    public void CheckVisaStatus(int nViStatus)
	    {
		    // If VISA error, throw exception.
		    if (nViStatus < visa32.VI_SUCCESS)
		    {
			    StringBuilder strError = new StringBuilder(256);
			    visa32.viStatusDesc(this.m_nResourceManager, nViStatus,
			                        strError);
			    throw new ApplicationException(strError.ToString());
		    }
	    }

	    public void Close()
	    {
		    if (m_nSession != 0)
			    visa32.viClose(m_nSession);
		    if (m_nResourceManager != 0)
			    visa32.viClose(m_nResourceManager);
	    }

        public void SetVisAddress(string strVisaAddress)
	    {
		    // Save VISA addres in member variable.
		    m_strVisaAddress = strVisaAddress;
		    // Open a VISA resource session.
		    OpenSession();
		    // Clear the interface.
		    int nViStatus;
		    nViStatus = visa32.viClear(m_nSession);
	    }

        public void Find_MeasureResource(string[] RsrcName, out uint num)
        {
            StringBuilder buffer = new StringBuilder("", 100);
            int nmatches=0;
            int list=0;
            int idx = 0;

            visa32.viFindRsrc(this.m_nResourceManager, "?*INSTR", out list, out nmatches, buffer);
            num = (uint)nmatches;
            for (idx=0; idx<nmatches; idx++)
            {
                RsrcName[idx] = buffer.ToString();
                visa32.viFindNext(list, buffer);                
            }
        }
    }

    class VisaInstrumentApp
    {
        private static VisaInstrument myScope;

        public VisaInstrumentApp()
        {
            try
            {
                myScope = new VisaInstrument("USB0::0x0957::0x17A6::MY52010925::0::INSTR");
                myScope.SetTimeoutSeconds(10);
                // Initialize - start from a known state.
                Initialize();
                // Capture data.
                Capture();
                // Analyze the captured waveform.
                Analyze();
            }
            catch (System.ApplicationException err)
            {
                Console.WriteLine("*** VISA Error Message : " + err.Message);
            }
            catch (System.SystemException err)
            {
                Console.WriteLine("*** System Error Message : " + err.Message);
            }
            catch (System.Exception err)
            {
                System.Diagnostics.Debug.Fail("Unexpected Error");
                Console.WriteLine("*** Unexpected Error : " + err.Message);
            }
            finally
            {
                myScope.Close();
            }
        }
        /*
        * Initialize the oscilloscope to a known state.
        * --------------------------------------------------------------
        */
        private static void Initialize()
        {
            StringBuilder strResults;
            // Get and display the device's *IDN? string.
            strResults = myScope.DoQueryString("*IDN?");
            Console.WriteLine("*IDN? result is: {0}", strResults);

            // Clear status and load the default setup.
            myScope.DoCommand("*CLS");
            myScope.DoCommand("*RST");
        }
        /*
        * Capture the waveform.
        * --------------------------------------------------------------
        */
        private static void Capture()
        {
            // Use auto-scale to automatically configure oscilloscope.
            myScope.DoCommand(":AUToscale");
            // Set trigger mode (EDGE, PULSe, PATTern, etc., and input source.
            myScope.DoCommand(":TRIGger:MODE EDGE");
            Console.WriteLine("Trigger mode: {0}",
                              myScope.DoQueryString(":TRIGger:MODE?"));
            // Set EDGE trigger parameters.
            myScope.DoCommand(":TRIGger:EDGE:SOURCe CHANnel1");
            Console.WriteLine("Trigger edge source: {0}",
                              myScope.DoQueryString(":TRIGger:EDGE:SOURce?"));
            myScope.DoCommand(":TRIGger:EDGE:LEVel 1.5");
            Console.WriteLine("Trigger edge level: {0}",
                              myScope.DoQueryString(":TRIGger:EDGE:LEVel?"));
            myScope.DoCommand(":TRIGger:EDGE:SLOPe POSitive");
            Console.WriteLine("Trigger edge slope: {0}",
                              myScope.DoQueryString(":TRIGger:EDGE:SLOPe?"));
            // Save oscilloscope configuration.
            byte[] ResultsArray; // Results array.
            int nLength; // Number of bytes returned from instrument.
            string strPath;
            // Query and read setup string.
            nLength = myScope.DoQueryIEEEBlock(":SYSTem:SETup?",
                                               out ResultsArray);
            // Write setup string to file.
            strPath = "c:\\setup.stp";
            FileStream fStream = File.Open(strPath, FileMode.Create);
            fStream.Write(ResultsArray, 0, nLength);
            fStream.Close();
            Console.WriteLine("Setup bytes saved: {0}", nLength);
            // Change settings with individual commands:
            // Set vertical scale and offset.
            myScope.DoCommand(":CHANnel1:SCALe 0.05");
            Console.WriteLine("Channel 1 vertical scale: {0}",
                              myScope.DoQueryString(":CHANnel1:SCALe?"));
            myScope.DoCommand(":CHANnel1:OFFSet -1.5");
            Console.WriteLine("Channel 1 vertical offset: {0}",

                              myScope.DoQueryString(":CHANnel1:OFFSet?"));
            // Set horizontal scale and position.
            myScope.DoCommand(":TIMebase:SCALe 0.0002");
            Console.WriteLine("Timebase scale: {0}",
                              myScope.DoQueryString(":TIMebase:SCALe?"));
            myScope.DoCommand(":TIMebase:POSition 0.0");
            Console.WriteLine("Timebase position: {0}",
                              myScope.DoQueryString(":TIMebase:POSition?"));
            // Set the acquisition type (NORMal, PEAK, AVERage, or HRESolution

            myScope.DoCommand(":ACQuire:TYPE NORMal");
            Console.WriteLine("Acquire type: {0}",
                              myScope.DoQueryString(":ACQuire:TYPE?"));
            // Or, configure by loading a previously saved setup.
            byte[] DataArray;
            int nBytesWritten;
            // Read setup string from file.
            strPath = "c:\\setup.stp";
            DataArray = File.ReadAllBytes(strPath);
            // Restore setup string.
            nBytesWritten = myScope.DoCommandIEEEBlock(":SYSTem:SETup",
                            DataArray);
            Console.WriteLine("Setup bytes restored: {0}", nBytesWritten);
            // Capture an acquisition using :DIGitize.
            myScope.DoCommand(":DIGitize CHANnel1");
        }
        /*
        * Analyze the captured waveform.
        * --------------------------------------------------------------
        */
        private static void Analyze()
        {
            byte[] ResultsArray; // Results array.
            int nLength; // Number of bytes returned from instrument.
            string strPath;
            // Make a couple of measurements.
            // -----------------------------------------------------------
            myScope.DoCommand(":MEASure:SOURce CHANnel1");
            Console.WriteLine("Measure source: {0}",
                              myScope.DoQueryString(":MEASure:SOURce?"));
            double fResult;
            myScope.DoCommand(":MEASure:FREQuency");
            fResult = myScope.DoQueryNumber(":MEASure:FREQuency?");
            Console.WriteLine("Frequency: {0:F4} kHz", fResult / 1000);
            myScope.DoCommand(":MEASure:VAMPlitude");
            fResult = myScope.DoQueryNumber(":MEASure:VAMPlitude?");

            Console.WriteLine("Vertial amplitude: {0:F2} V", fResult);
            // Download the screen image.
            // -----------------------------------------------------------
            myScope.DoCommand(":HARDcopy:INKSaver OFF");
            // Get the screen data.
            nLength = myScope.DoQueryIEEEBlock(":DISPlay:DATA? PNG, COLor",
                                               out ResultsArray);
            // Store the screen data to a file.
            strPath = "c:\\screen.png";
            FileStream fStream = File.Open(strPath, FileMode.Create);
            fStream.Write(ResultsArray, 0, nLength);            
            fStream.Close();
            Console.WriteLine("Screen image ({0} bytes) written to {1}",
                              nLength, strPath);
            // Download waveform data.
            // -----------------------------------------------------------
            // Set the waveform points mode.
            myScope.DoCommand(":WAVeform:POINts:MODE RAW");
            Console.WriteLine("Waveform points mode: {0}",
                              myScope.DoQueryString(":WAVeform:POINts:MODE?"));
            // Get the number of waveform points available.
            myScope.DoCommand(":WAVeform:POINts 10240");
            Console.WriteLine("Waveform points available: {0}",
                              myScope.DoQueryString(":WAVeform:POINts?"));
            // Set the waveform source.
            myScope.DoCommand(":WAVeform:SOURce CHANnel1");
            Console.WriteLine("Waveform source: {0}",
                              myScope.DoQueryString(":WAVeform:SOURce?"));
            // Choose the format of the data returned (WORD, BYTE, ASCII):
            myScope.DoCommand(":WAVeform:FORMat BYTE");
            Console.WriteLine("Waveform format: {0}",
                              myScope.DoQueryString(":WAVeform:FORMat?"));
            // Display the waveform settings:
            double[] fResultsArray;
            fResultsArray = myScope.DoQueryNumbers(":WAVeform:PREamble?");
            double fFormat = fResultsArray[0];
            if (fFormat == 0.0)
            {
                Console.WriteLine("Waveform format: BYTE");
            }
            else if (fFormat == 1.0)
            {
                Console.WriteLine("Waveform format: WORD");
            }
            else if (fFormat == 2.0)
            {
                Console.WriteLine("Waveform format: ASCii");

            }
            double fType = fResultsArray[1];
            if (fType == 0.0)
            {
                Console.WriteLine("Acquire type: NORMal");
            }
            else if (fType == 1.0)
            {
                Console.WriteLine("Acquire type: PEAK");
            }
            else if (fType == 2.0)
            {
                Console.WriteLine("Acquire type: AVERage");
            }
            else if (fType == 3.0)
            {
                Console.WriteLine("Acquire type: HRESolution");
            }
            double fPoints = fResultsArray[2];
            Console.WriteLine("Waveform points: {0:e}", fPoints);
            double fCount = fResultsArray[3];
            Console.WriteLine("Waveform average count: {0:e}", fCount);
            double fXincrement = fResultsArray[4];
            Console.WriteLine("Waveform X increment: {0:e}", fXincrement);
            double fXorigin = fResultsArray[5];
            Console.WriteLine("Waveform X origin: {0:e}", fXorigin);
            double fXreference = fResultsArray[6];
            Console.WriteLine("Waveform X reference: {0:e}", fXreference);
            double fYincrement = fResultsArray[7];
            Console.WriteLine("Waveform Y increment: {0:e}", fYincrement);
            double fYorigin = fResultsArray[8];
            Console.WriteLine("Waveform Y origin: {0:e}", fYorigin);
            double fYreference = fResultsArray[9];
            Console.WriteLine("Waveform Y reference: {0:e}", fYreference);
            // Read waveform data.
            nLength = myScope.DoQueryIEEEBlock(":WAVeform:DATA?",
                                               out ResultsArray);
            Console.WriteLine("Number of data values: {0}", nLength);
            // Set up output file:
            strPath = "c:\\waveform_data.csv";
            if (File.Exists(strPath)) File.Delete(strPath);
            // Open file for output.
            StreamWriter writer = File.CreateText(strPath);
            // Output waveform data in CSV format.

            for (int i = 0; i < nLength - 1; i++)
                writer.WriteLine("{0:f9}, {1:f6}",
                                 fXorigin + ((float)i * fXincrement),
                                 (((float)ResultsArray[i] - fYreference) *
                                  fYincrement) + fYorigin);
            // Close output file.
            writer.Close();
            Console.WriteLine("Waveform format BYTE data written to {0}",
                              strPath);
        }
    }
}