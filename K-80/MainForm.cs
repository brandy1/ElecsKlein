using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KClmtrBase;
using KClmtrBase.KClmtrWrapper;
using System.IO;
using System.Threading; //Thread.Sleep 需要引用它
using System.IO.Ports;
using SL_Tek_Studio_Pro;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace K_80
{
    public partial class MainForm : Form
    {
        //↓↓↓↓↓↓↓↓↓↓全域變數區域↓↓↓↓↓↓↓↓↓↓
        private KClmtrWrap kClmtr;
        private SL_ExcuteCmd Device = null;
        private string ELECSBOARD = "0x7422\r";
        SL_Device_Util.ScDeviceInfo[] UsbDeviceInfo;

        private uint VCOM_Setting = 0;

        private double[] VRA_mapping_Brightness = new double[1024];//將1024組電壓分壓 轉換成亮度表現 提供查表Mapping

        private int[] VP_index = new int[29]  { 0, 1, 3, 5, 7, 9, 11, 13, 15,
                    24, 32, 48, 64, 96, 128, 160, 192, 208, 224, 232,
                    240, 242, 244, 246, 248, 250, 252, 254, 255};



        private double[] EstimateBrightness = new double[256];//推算符合Gamma曲線設定的亮度表現
        private double[] Actual_Brightness = new double[256];//實測亮度表現
        private double[] EstimateBrightness_toleranceP = new double[256];//推算符合Gamma曲線設定的亮度表現
        private double[] EstimateBrightness_toleranceN = new double[256];//推算符合Gamma曲線設定的亮度表現
        private double[] Tie_Estimate_Brightness = new double[29];//推算的亮度 計算的斜率
        private double[] Tie_Actual_Brightness = new double[29];//實測的亮度 計算的斜率


        private double[] Actual_Brightness_Slope = new double[29];//實測的亮度 計算的斜率
        private uint[] Tie_Index_CalGet = new uint[29];//推算的亮度 去找出的綁點的設定值
        private uint[] Tie_ParameterSetting_MSB = new uint[29];
        private uint[] Tie_ParameterSetting_LSB = new uint[29];

        private uint[] TieRegisterSetting = new uint[29]; //目前讀出的的綁點設定值使用 或是用於讀取控制盤上的設定值 準備寫給IC用
        private uint[] DigGma_TieRegisterSetting = new uint[30];
        private uint[] GP_OTM1911A = new uint[30]; //GP1~GP29 OTM1911 Gamma1綁點設定值存放處

        private double[] RGB_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(RGB灰階用)
        private double[] R_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(R灰階用)
        private double[] G_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(G灰階用)
        private double[] B_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(B灰階用)

        private double[] RGB_Tie_Projection = new double[29];//推算符合Gamma綁點亮度值(RGB灰階用)
        private double[] R_Tie_Projection = new double[29];//推算符合Gamma綁點亮度值(R灰階用)
        private double[] G_Tie_Projection = new double[29];//推算符合Gamma綁點亮度值(G灰階用)
        private double[] B_Tie_Projection = new double[29];//推算符合Gamma綁點亮度值(B灰階用)

        private uint[] Index_RGB_Tie_Projection = new uint[29];//推算符合Gamma綁點設定值(RGB灰階用)
        private uint[] Index_R_Tie_Projection = new uint[29];//推算符合Gamma綁點設定值(R灰階用)
        private uint[] Index_G_Tie_Projection = new uint[29];//推算符合Gamma綁點設定值(G灰階用)
        private uint[] Index_B_Tie_Projection = new uint[29];//推算符合Gamma綁點設定值(B灰階用)

        public BrightnessTie_struct[] EstimateBrightnessTie_struct = new BrightnessTie_struct[29];
        public BrightnessTie_struct[] ActualBrightnessTie_struct = new BrightnessTie_struct[29];



        public class BrightnessTie_struct
        {
            public int Start_Tie_Index;
            public int End_Tie_Index;
            public double Start_Tie_Brightness;
            public double End_Tie_Brightness;
        }


        //↑↑↑↑↑↑↑↑↑↑全域變數區域↑↑↑↑↑↑↑↑↑↑


        public MainForm()
        {
            InitializeComponent();
            kClmtr = new KClmtrWrap();
            InitialSetting();

        }

        //↓↓↓↓↓↓↓↓↓↓公用副程式區域↓↓↓↓↓↓↓↓↓↓

        private void InitialSetting()
        {

            //Create Copy Dll
            string eppdll = Application.StartupPath + "\\EPP2USB_DLL_V12.dll";
            if (!File.Exists(eppdll))
            {
                Assembly aObj = Assembly.GetExecutingAssembly();
                Stream sStream = aObj.GetManifestResourceStream("SL_Tek_Studio_Pro.Resources.EPP2USB_DLL_V12.dll");

                if (sStream == null)
                {
                    MessageBox.Show("read file error....");
                }
                else
                {
                    byte[] bySave = new byte[sStream.Length];
                    sStream.Read(bySave, 0, bySave.Length);
                    FileStream fsObj = new FileStream(eppdll, FileMode.CreateNew);
                    fsObj.Write(bySave, 0, bySave.Length);
                    fsObj.Close();
                }
            }

            UsbDeviceList();

            GMA_Set_comboBox.SelectedIndex = 0;
            AGma_tolerance_comboBox.SelectedIndex = 1;
        }


        private bool errorCheck(int error)
        {
            String stringError = "";
            //Avergring, just needs to display it
            error &= ~(int)KleinsErrorCodes.AVERAGING_LOW_LIGHT;
            //Resetting the FFT data
            error &= ~(int)KleinsErrorCodes.FFT_PREVIOUS_RANGE;
            //The data isn't ready to display yet
            error &= ~(int)KleinsErrorCodes.FFT_INSUFFICIENT_DATA;
            if (false)
            {
                error &= ~(int)KleinsErrorCodes.AIMING_LIGHTS;
            }
            if (true)
            {
                error &= ~(int)KleinsErrorCodes.BOTTOM_UNDER_RANGE;
                error &= ~(int)KleinsErrorCodes.TOP_OVER_RANGE;
                error &= ~(int)KleinsErrorCodes.OVER_HIGH_RANGE;

                error &= ~(int)KleinsErrorCodes.CONVERTED_NM;
                error &= ~(int)KleinsErrorCodes.KELVINS;
            }
            if (error > 0)
            {
                kClmtr.stopMeasuring();

                if ((error & (int)KleinsErrorCodes.CONVERTED_NM) > 0)
                {
                    stringError += "There was an error when coverting to NM with the measurement.\n";
                    error &= ~(int)KleinsErrorCodes.CONVERTED_NM;
                }
                if ((error & (int)KleinsErrorCodes.KELVINS) > 0)
                {
                    stringError += "There was an error when coverting to Kelvins with the measurement.\n";
                    error &= ~(int)KleinsErrorCodes.KELVINS;
                }
                if ((error & (int)KleinsErrorCodes.AIMING_LIGHTS) > 0)
                {
                    stringError += "The Aiming lights are on.\n";
                    error &= ~(int)KleinsErrorCodes.AIMING_LIGHTS;
                }
                if ((error & (int)(KleinsErrorCodes.BOTTOM_UNDER_RANGE
                    | KleinsErrorCodes.TOP_OVER_RANGE | KleinsErrorCodes.OVER_HIGH_RANGE)) > 0)
                {
                    stringError += "There was an error from the Klein device due to the Range switching with the measurement.\n";
                    error &= ~(int)KleinsErrorCodes.BOTTOM_UNDER_RANGE;
                    error &= ~(int)KleinsErrorCodes.TOP_OVER_RANGE;
                    error &= ~(int)KleinsErrorCodes.OVER_HIGH_RANGE;
                }
                if ((error & (int)KleinsErrorCodes.FFT_BAD_STRING) > 0)
                {
                    stringError += "The Flicker string from the Klein device was bad.\n";
                    error &= ~(int)KleinsErrorCodes.FFT_BAD_STRING;
                }
                if ((error & (int)(KleinsErrorCodes.NOT_OPEN
                    | KleinsErrorCodes.TIMED_OUT
                    | KleinsErrorCodes.LOST_CONNECTION)) > 0)
                {

                    kClmtr.closePort();
                    stringError += "The the Klein device as been unplugged\n";
                    error &= ~(int)(KleinsErrorCodes.NOT_OPEN
                        | KleinsErrorCodes.TIMED_OUT
                        | KleinsErrorCodes.LOST_CONNECTION);
                }
                if (error > 0)
                {
                    stringError += "There was an error with the measurement. Error code: " + error + "\n";
                }

                MessageBox.Show(stringError);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void SetSeriesPort()
        {
            string[] Ports = SerialPort.GetPortNames();
            int PortCount = Ports.Length;
            if (PortCount > 0)
            {
                foreach (string port in Ports)
                {
                    ComPortSel_comboBox.Items.Add(port);
                    cbo_elecsport.Items.Add(port);
                }
            }
            else
            {
                ComPortSel_comboBox.Items.Add("Null");
                cbo_elecsport.Items.Add("Null");
            }
            ComPortSel_comboBox.SelectedIndex = 0;
            cbo_elecsport.SelectedIndex = 0;

        }

        //↑↑↑↑↑↑↑↑↑↑公用副程式區域↑↑↑↑↑↑↑↑↑↑

        private void Form1_Load(object sender, EventArgs e)
        {
            SetSeriesPort();
        }

        private void ComPortCheck_Button_Click(object sender, EventArgs e)
        {
            string SelStr = ComPortSel_comboBox.Text;
            if (String.IsNullOrEmpty(SelStr) || SelStr.CompareTo("Null") == 0) { ComPortState_label.Text = "No Deviice"; return; }

            Int32 ComPortNumber = Convert.ToInt32(SelStr.Substring(3));
            string[] test;

            ComPortState_label.Text = "Please wait for the connection!!";
            ComPortState_label.ForeColor = Color.Gray;

            kClmtr.connect(ComPortNumber);
            test = kClmtr.CalFileList;
            if (test[0] == "0: Factory Cal File")//土砲判斷K-80連線機制
            {
                ComPortState_label.Text = "K80 Already Connection!!";
                ComPortState_label.ForeColor = Color.Green;
                ComPortCheck_Button.BackColor = SystemColors.Control;
                //GetEstimateBrightness_button.Enabled = true;
                button1.Enabled = true;
                button6.Enabled = true;
            }
            else
            {
                ComPortState_label.Text = "Please Check Again K-80 State";
                ComPortState_label.ForeColor = Color.Red;
                ComPortCheck_Button.BackColor = Color.GreenYellow;
            }



        }






        private void btn_oepnelecs_Click(object sender, EventArgs e)
        {
            Device = new SL_ExcuteCmd();
            string RdStr = null;
            string SelStr = cbo_elecsport.SelectedItem.ToString();
            if (String.IsNullOrEmpty(SelStr) || SelStr.CompareTo("Null") == 0) { lbl_elecs_status.Text = "No Deviice"; return; }

            if (Device.Open(SelStr))
            {
                Device.WriteRead("id", ref RdStr);
                if (RdStr.Trim().CompareTo(ELECSBOARD) == 0)
                {
                    lbl_elecs_status.Text = "E7422 Connect";
                    btn_oepnelecs.BackColor = Color.GreenYellow;
                }
            }
            else
            {
                lbl_elecs_status.Text = "E7422 Not Connect";
                btn_oepnelecs.BackColor = SystemColors.Control;
                Device.Close();
                Device = null;
            }
        }

        private void btn_write_Click(object sender, EventArgs e)
        {




            if (Device != null || !Device.Status())
            {
                Device.Write("mipi.write 0x05 0x28");
                Device.Write("mipi.write 0x05 0x10");
                Thread.Sleep(500);
                Device.Write("mipi.video.disable");
                Device.Write("mipi.clock.disable");
                Device.Write("gpio.write 0x0F");
                Thread.Sleep(100);
                Device.Write("gpio.write 0x07");
                Thread.Sleep(100);
                Device.Write("gpio.write 0x00 ");
                Thread.Sleep(100);
                Device.Write("power.off all");
            }
        }


        //private void GetEstimateBrightness_button_Click(object sender, EventArgs e)
        //{
        //    string textdata = null;


        //    this.GetEstimateBrightness_button.ForeColor = Color.Green;
        //    Application.DoEvents();
        //    int dive = comboBox1.SelectedIndex + 1;
        //    SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

        //    Info_textBox.Text = "";

        //    /*推算標準Gamma亮度前 先用K80量測面板表現最暗與最亮灰階的亮度值*/

        //    EstimateBrightness_Min = Actual_Brightness[0];// 最暗
        //    EstimateBrightness_Max = Actual_Brightness[255];// 最亮

        //    YMax_label.Text = "YMin=" + Convert.ToString(EstimateBrightness_Min);
        //    YMin_label.Text = "YMax=" + Convert.ToString(EstimateBrightness_Max);


        //    /*套用設定的Gamma值 推算出符合標準Gamma曲線答案的亮度表現*/

        //    double Gamma_set;
        //    double Gamma_set_tolerance;
        //    double temp;

        //    double.TryParse(GammaSet_textBox.Text, out Gamma_set);

        //    //EstimateBrightness_Max = 50;
        //    //EstimateBrightness_Min = 0.5;

        //    chart1.Series[0].Points.Clear();
        //    chart1.Series[1].Points.Clear();
        //    chart1.Series[2].Points.Clear();
        //    uint tie = 0;

        //    for (int Brightness = 0; Brightness < 256; Brightness++)
        //    {
        //        temp = (double)(Brightness) / 256;

        //        EstimateBrightness[Brightness] = Math.Round(EstimateBrightness_Max * (float)Math.Pow(temp, Gamma_set), 4) + EstimateBrightness_Min;

        //        chart1.Series[0].Points.AddXY(Brightness, EstimateBrightness[Brightness]);

        //        if (VP_index[tie] == Brightness)
        //        {
        //            textdata = "VP" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(EstimateBrightness[Brightness]) + "\r\n";
        //            Info_textBox.AppendText(textdata);
        //            tie++;
        //        }

        //    }


        //    //計算誤差上界 並繪出圖
        //    double.TryParse(Gamma_set_tolerance_textBox.Text, out Gamma_set_tolerance);
        //    Gamma_set_tolerance = Gamma_set + Gamma_set_tolerance;
        //    //Gamma_set_tolerance = Gamma_set + Convert.ToDouble(Gamma_set_tolerance_textBox.Text);
        //    for (int Brightness = 0; Brightness < 256; Brightness++)
        //    {
        //        temp = (double)(Brightness) / 256;

        //        EstimateBrightness_toleranceP[Brightness] = Math.Round(EstimateBrightness_Max * (float)Math.Pow(temp, Gamma_set_tolerance), 4) + EstimateBrightness_Min;

        //        chart1.Series[1].Points.AddXY(Brightness, EstimateBrightness_toleranceP[Brightness]);
        //    }

        //    //計算誤差下界 並繪出圖
        //    double.TryParse(Gamma_set_tolerance_textBox.Text, out Gamma_set_tolerance);
        //    Gamma_set_tolerance = Gamma_set - Gamma_set_tolerance;
        //    for (int Brightness = 0; Brightness < 256; Brightness++)
        //    {
        //        temp = (double)(Brightness) / 256;

        //        EstimateBrightness_toleranceN[Brightness] = Math.Round(EstimateBrightness_Max * (float)Math.Pow(temp, Gamma_set_tolerance), 4) + EstimateBrightness_Min;

        //        chart1.Series[2].Points.AddXY(Brightness, EstimateBrightness_toleranceN[Brightness]);
        //    }


        //    //透過上面的步驟可以計算出 符合Gamma曲線的亮度表現 
        //    //因為我們都是用亮度去做計算與評估 因此要把1024階電阻分壓選擇 
        //    //轉換成 這些分壓可以轉變成1024階亮度表現
        //    //因此 下面運算 取最大亮度與最小亮度表現
        //    //套入分壓階層 模擬出1024個Source電壓階層能產生出的相對應1024階亮度表現
        //    for (int num = 0; num < 1024; num++)
        //    {
        //        VRA_mapping_Brightness[num] = Math.Round(EstimateBrightness_Max * ((double)(1024 - num) / 1024), 5) + EstimateBrightness_Min;//取到小數點第5位
        //    }

        //    textdata = "Brigheness Toleranc Analyse" + "\r\n";
        //    Info_textBox.AppendText(textdata);
        //    for (int Brightness = 0; Brightness < 256; Brightness++)
        //    {
        //        textdata = "Gary=" + Convert.ToString(Brightness) + " Brightness:" + Convert.ToString(Actual_Brightness[Brightness]) + ":" + Convert.ToString(EstimateBrightness_toleranceP[Brightness]) + ":" + Convert.ToString(EstimateBrightness_toleranceN[Brightness]) + "\r\n";
        //        Info_textBox.AppendText(textdata);

        //        //if(Actual_Brightness[Brightness] > EstimateBrightness_toleranceP[Brightness])
        //        //{
        //        //    textdata = "Gary=" + Convert.ToString(Brightness) + " Brightness:" + Convert.ToString(Actual_Brightness[Brightness]) + " > tolance Spec:" + Convert.ToString(EstimateBrightness_toleranceP[Brightness])+"\r\n";
        //        //    Info_textBox.AppendText(textdata);
        //        //}
        //        //else if(Actual_Brightness[Brightness] < EstimateBrightness_toleranceN[Brightness])
        //        //{
        //        //    textdata = "Gary=" + Convert.ToString(Brightness) + " Brightness:" + Convert.ToString(Actual_Brightness[Brightness]) + " < tolance Spec:" + Convert.ToString(EstimateBrightness_toleranceN[Brightness]) + "\r\n";
        //        //    Info_textBox.AppendText(textdata);

        //        //}
        //        //else
        //        //{
        //        //    textdata = "Gary=" + Convert.ToString(Brightness) + " Brightness:" + Convert.ToString(Actual_Brightness[Brightness]) + " < Qualified Spec!! \r\n";
        //        //    Info_textBox.AppendText(textdata);
        //        //}
        //    }

        //    button13.Enabled = true;
        //    this.GetEstimateBrightness_button.ForeColor = Color.Black;
        //}


        private double K80_Trigger_Measurement(int testcount)
        {
            double Y_Data = 0;
            double Big_Y_total = 0;
            double Big_Y_avg = 0;


            for (int i = 0; i < testcount; i++)
            {
                //↓↓↓↓↓K80 量測↓↓↓↓↓//
                Application.DoEvents();
                wMeasurement measure = kClmtr.getNextMeasurement(-1);
                Application.DoEvents();

                if (errorCheck(measure.errorcode))
                {
                    Y_Data = measure.BigY;
                    Big_Y_total = Y_Data + Big_Y_total;
                }
            }

            if (Big_Y_total > 0)
            {
                Big_Y_avg = Big_Y_total / testcount;
            }
            else
            {
                Big_Y_avg = 0;
            }


            Application.DoEvents();
            //↑↑↑↑↑K80量測↑↑↑↑↑//
            return Big_Y_avg;
        }



        //從Tie_ParameterSetting的內容顯示於Form上的Text
        private void Tie_ParameterSetting_to_LoadVP_TextData(uint[] registersetting)
        {
            string textdata = null;
            int cnt = 0;

            textdata = "Gamma Register Setting\r\n";
            Info_textBox.AppendText(textdata);

            //VP0
            textdata = "VP__0 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP1
            textdata = "VP__1 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP3
            textdata = "VP__3 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP5
            textdata = "VP__5 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP7
            textdata = "VP__7 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP9
            textdata = "VP__9 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP11
            textdata = "VP_11 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP13
            textdata = "VP_13 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP15
            textdata = "VP_15 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP24
            textdata = "VP_24 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP32
            textdata = "VP_32 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP48
            textdata = "VP_48 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP64
            textdata = "VP_64 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP96
            textdata = "VP_96 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP128
            textdata = "VP128 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP160
            textdata = "VP160 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP192
            textdata = "VP192 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP208
            textdata = "VP208 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP224
            textdata = "VP224 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP232
            textdata = "VP232 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP240
            textdata = "VP240 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP242
            textdata = "VP242 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP244
            textdata = "VP244 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP246
            textdata = "VP246 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP248
            textdata = "VP248 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP250
            textdata = "VP250 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP252
            textdata = "VP252 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP254
            textdata = "VP254 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
            cnt++;

            //VP255
            textdata = "VP255 Setting=" + Convert.ToString(registersetting[cnt]) + "\r\n";
            Info_textBox.AppendText(textdata);
        }


        //從Form上的Text擷取Data去Tie_ParameterSetting
        private bool LoadVP_TextData_to_Tie_ParameterSetting(uint[] registersetting)
        {

            string infotxt = null;
            string Title = null, Value = null;
            int test = 0;

            infotxt = Info_textBox.Lines[0];

            test = Info_textBox.Text.Length;

            if (infotxt.CompareTo("Gamma Register Setting") != 0)
            {
                return false;
            }
            else
            {
                for (int i = 1; i < (Info_textBox.Lines.Length - 1); i++)
                {
                    infotxt = Info_textBox.Lines[i];
                    string[] innerTxt = infotxt.Split('=');
                    Title = innerTxt[0].Substring(4, 1);
                    Value = innerTxt[1];

                    uint.TryParse(Value, out registersetting[(i - 1)]);


                }
                return true;
            }
        }

        //寫
        private void WriteGammaSettingAlltheSame_to_SSD2130(uint[] gammasetting)
        {
            byte TieCnt = 0;
            byte page = 0x00;
            uint temp = 0;
            byte RegisterSetting = 0x00;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            string textdata = null;


            //切換SSD2130 PassWord & page
            //Page31 R+   Page32 R-   Page33 G+   Page34 G-   Page35 B+   Page36 B-
            for (page = 0x31; page <= 0x36; page++)
            {
                //textdata = "Set Page:0x"+Convert.ToString(page, 16) +"START!"+ Environment.NewLine;
                //Info_textBox.AppendText(textdata);


                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, page);


                for (TieCnt = 0; TieCnt < 29; TieCnt++)
                {
                    uint temp2 = TieCnt;
                    byte addr = 0;

                    textdata = ".";
                    //Info_textBox.AppendText(textdata);

                    temp2 = temp2 * 2;
                    addr = Convert.ToByte(temp2);

                    temp = gammasetting[TieCnt];
                    temp >>= 8;
                    temp = temp & 0x03;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);


                    temp2 = TieCnt;
                    temp2 = (temp2 * 2) + 1;
                    addr = Convert.ToByte(temp2);

                    temp = gammasetting[TieCnt];
                    temp = temp & 0xFF;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);
                }
                //textdata = Environment.NewLine + "Set Page:0x" + Convert.ToString(page, 16) + "Done!" + Environment.NewLine;
                //Info_textBox.AppendText(textdata);
            }
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }

        private void WriteGammaSettingAlltheSame_to_SSD2130_SetPage(uint[] gammasetting, byte Page)
        {
            byte TieCnt = 0;
            byte page = 0x00;
            uint temp = 0;
            byte RegisterSetting = 0x00;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            //切換SSD2130 PassWord & page
            //Page31 R+   Page32 R-   Page33 G+   Page34 G-   Page35 B+   Page36 B-
            if(true)
            {
                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, Page);


                for (TieCnt = 0; TieCnt < 29; TieCnt++)
                {
                    uint temp2 = TieCnt;
                    byte addr = 0;

                    temp2 = temp2 * 2;
                    addr = Convert.ToByte(temp2);

                    temp = gammasetting[TieCnt];
                    temp >>= 8;
                    temp = temp & 0x03;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);


                    temp2 = TieCnt;
                    temp2 = (temp2 * 2) + 1;
                    addr = Convert.ToByte(temp2);

                    temp = gammasetting[TieCnt];
                    temp = temp & 0xFF;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);
                }
            }
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }



        //Read Gamma Parameter Setting from Gamma Register to Tie_ParameterSettingt[0~28]
        private void ReadGammaSettingAll_from_SSD2130(uint[] gammasetting)
        {
            byte TieCnt = 0;
            byte[] RdVal_page = new byte[3];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31); //Page31 R+ 僅讀回一筆及代表所有Gamma
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x35); //Page36 B+ 僅讀回一筆及代表所有Gamma
            //Application.DoEvents();
            //WhiskeyUtil.MipiRead(0xFF, 3, ref RdVal_page);


            for (TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                uint temp2 = TieCnt;
                byte addr = 0;
                uint RegisterRead = 0x00;
                byte[] RdVal = new byte[1];

                temp2 = temp2 * 2;
                addr = Convert.ToByte(temp2);
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);
                RegisterRead = RdVal[0];
                RegisterRead <<= 8;

                temp2 = TieCnt;

                temp2 = (temp2 * 2) + 1;
                addr = Convert.ToByte(temp2);
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);
                RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
                gammasetting[TieCnt] = RegisterRead;
            }
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }



        private void button2_Click(object sender, EventArgs e)
        {
            bool status = false;
            this.WrText2GammaRegister_but.ForeColor = Color.Green;
            Application.DoEvents();

            //從Form上的Text擷取Data去Tie_ParameterSetting
            status = LoadVP_TextData_to_Tie_ParameterSetting(TieRegisterSetting);
            if (status == false)//判斷是否為Gamma寫入允許的格式
            {
                MessageBox.Show("目前textbox內容並非用於設定Gamma使用! 將重新載入Gamma目前設定值(從變數TieRegisterSetting中取得)");
                Info_textBox.Text = "";
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x35);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x36);
                //Tie_ParameterSetting_to_LoadVP_TextData(TieRegisterSetting);
            }
            else
            {
                //Load Gamma Parameter Setting from Tie_ParameterSettingt[0~28] to Gamma Register 
                WriteGammaSettingAlltheSame_to_SSD2130(TieRegisterSetting);
                Info_textBox.AppendText("從控制盤載入IC暫存器完畢!");
            }

            this.WrText2GammaRegister_but.ForeColor = Color.Black;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.RdRegisteer2Text_but.ForeColor = Color.Green;
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            Info_textBox.Text = "";

            //Read Gamma Parameter Setting from Gamma Register to Tie_ParameterSettingt[0~28]
            ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);


            //從Tie_ParameterSetting的內容顯示於Form上的Text
            Tie_ParameterSetting_to_LoadVP_TextData(TieRegisterSetting);

            this.RdRegisteer2Text_but.ForeColor = Color.Black;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.ForeColor = Color.Green;
            Application.DoEvents();

            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            byte tie_gray = 0;
            byte[] track_flag = new byte[2];
            uint[] Brighter_GammaRegister = new uint[29];
            uint[] Darker_GammaRegister = new uint[29];
            uint[] temp = new uint[29];


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();


            //★步驟1:讀回目前Gamma設定值 存放於TieRegisterSetting[]變數之中
            ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);
            Tie_ParameterSetting_to_LoadVP_TextData(TieRegisterSetting);
            for (uint tie = 0; tie < 29; tie++)
            { temp[tie] = TieRegisterSetting[tie]; }


            //★步驟2:實際點根據綁點所在 套用設定值去點面板 
            //透過K80量測出實測亮度 之後存放於Tie_Actual_Brightness[] 
            //再與Gamma2.2曲線推估的標準亮度 EstimateBrightness[] 進行比較

            //TieRegisterSetting[0] = 0; // 直接把綁點0(最亮)位置 直接寫入亮度最大值
            //Tie_Actual_Brightness[0] = EstimateBrightness_Max;//實測綁點位置0 亮度直接設定亮度最大值


            //★步驟3:先把綁點推算的亮度從EstimateBrightness DataBase放到變數Tie_Estimate_Brightness去
            //因為EstimateBrightness[]裡面是存放推算出的256階灰階亮度對應表現
            //Tie_Estimate_Brightness[]也是存放推算的值 但是是專門存放幾個綁點 提供自動調整比較使用
            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            { Tie_Estimate_Brightness[tie] = EstimateBrightness[VP_index[tie]]; }





            //★步驟4:依序點綁點處的灰階 並且用K80量測
            for (uint tie = 5; tie < 23; tie++) //tir=0 時亮度最亮
            {
                track_flag[0] = 0x00;//本次測試的Flag狀態 清除
                track_flag[1] = 0x00;//上次測試的Flag狀態 清除

                RETRY:

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(255 - VP_index[tie]);
                WhiskeyUtil.ImageFill(tie_gray, tie_gray, tie_gray);
                Thread.Sleep(100);
                WhiskeyUtil.ImageFill(tie_gray, tie_gray, tie_gray);
                Thread.Sleep(100);
                //K80量測亮度表現
                Tie_Actual_Brightness[tie] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位


                if (Tie_Actual_Brightness[tie] > Tie_Estimate_Brightness[tie])
                {//本次實際量測亮度比推算的亮度較亮 處置~暫存器-- 讓亮度降低
                    track_flag[0] = 0x01;   //實測較推算的亮 給予Flag = 0x01

                    if (track_flag[1] == 0x10)
                    {//表示上一次為實測亮度較暗 為反折點
                        track_flag[1] = track_flag[0];
                        TieRegisterSetting[tie] = temp[tie];
                        //保存目前設定值與上次設定值 備用 並跳出這個綁點的測試
                        goto TieTestDone;

                    }
                    else if (track_flag[1] == 0x01)
                    {
                        //表示連續兩次調整暫存器後亮度測試都偏亮
                        //處置方式 請持續將暫存器設置變大以 降低亮度 
                        track_flag[1] = track_flag[0];
                        if (temp[tie] >= 1023)
                        {
                            TieRegisterSetting[tie] = temp[tie];
                            goto TieTestDone;
                        }
                        temp[tie]++;
                        //單獨針對想改變的Gamma暫存器設定副程式
                        WriteGammaPartialSetting_to_SSD2130(tie, temp[tie], TieRegisterSetting);

                        goto RETRY;
                    }
                    else
                    {
                        track_flag[1] = track_flag[0];
                        goto RETRY;
                    }

                }
                else if (Tie_Actual_Brightness[tie] < Tie_Estimate_Brightness[tie])
                {//本次實際量測亮度比推算的亮度較暗 處置~暫存器++ 讓亮度提高
                    track_flag[0] = 0x10;   //實測較推算的暗 給予Flag = 0x10

                    if (track_flag[1] == 0x01)
                    {//表示上一次為實測亮度較亮 為反折點

                        track_flag[1] = track_flag[0];
                        TieRegisterSetting[tie] = temp[tie];
                        //保存目前設定值與上次設定值 備用 並跳出這個綁點的測試
                        goto TieTestDone;
                    }
                    else if (track_flag[1] == 0x10)
                    {   //表示連續兩次調整暫存器後亮度測試都偏暗
                        //處置方式 請持續將暫存器設置變小以 提高亮度 
                        track_flag[1] = track_flag[0];
                        if (temp[tie] <= 0)
                        {
                            TieRegisterSetting[tie] = temp[tie];
                            goto TieTestDone;
                        }
                        temp[tie]--;
                        //單獨針對想改變的Gamma暫存器設定副程式
                        WriteGammaPartialSetting_to_SSD2130(tie, temp[tie], TieRegisterSetting);


                        goto RETRY;
                    }
                    else
                    {
                        track_flag[1] = track_flag[0];
                        goto RETRY;
                    }

                }
                else
                {//本次實際量測亮度與推算的亮度兩者一致
                    TieRegisterSetting[tie] = temp[tie];
                    track_flag[0] = 0x00;
                    track_flag[1] = 0x00;
                }
                TieTestDone:
                track_flag[0] = 0x00;//本次測試的Flag狀態 清除
                track_flag[1] = 0x00;//上次測試的Flag狀態 清除
            }


            //★步驟5:根據GMDarker_checkBox & GMBrighter_checkBox 進行判斷曲線應表現如何
            //if (GMBrighter_checkBox.Checked == true )
            //{
            //    GMDarker_checkBox.Checked = false;

            //    for (uint tie = 0; tie < 29; tie++)
            //    {//使用者亮度表現希望在標準線之上
            //        if(TieRegisterSetting[tie] == 0)
            //        {   TieRegisterSetting[tie] = 0;    }
            //        else
            //        {   TieRegisterSetting[tie] = TieRegisterSetting[tie] - 1;  }

            //    }
            //}
            //else if(GMDarker_checkBox.Checked == true)
            //{
            //    GMBrighter_checkBox.Checked = false;
            //    for (uint tie = 0; tie < 29; tie++)
            //    {//使用者亮度表現希望在標準線之下
            //        if (TieRegisterSetting[tie] >= 1023)
            //        { TieRegisterSetting[tie] = 1023; }
            //        else
            //        { TieRegisterSetting[tie] = TieRegisterSetting[tie] + 1; }
            //    }
            //}


            //★步驟6:將選定的值 寫入暫存器中
            //Load Gamma Parameter Setting from Tie_ParameterSettingt[0~28] to Gamma Register 
            WriteGammaSettingAlltheSame_to_SSD2130(TieRegisterSetting);

            //從Tie_ParameterSetting的內容顯示於Form上的Text
            Tie_ParameterSetting_to_LoadVP_TextData(TieRegisterSetting);
            Info_textBox.AppendText("從控制盤載入IC暫存器完畢!");


            this.button4.ForeColor = Color.Black;
        }


        private void OTM1911A_GammaSetRegisterMapping(uint StartAddr, uint TieNum, uint GammaValueSet)
        {
            byte[] RegData = new byte[37];
            uint StartAddress = (StartAddr & 0xFF00);
            byte[] receiver = new byte[37];
            byte addr_MSB = 0;


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);

            //STEP3: 針對想設定的值去設定
            GP_OTM1911A[TieNum] = Convert.ToUInt16(GammaValueSet);


            //STEP4: 把GP[1]~GP[29] 填到預備要寫入暫存器的空間(20170830驗證功能正確)
            uint cnt = 1;
            uint temp1 = 0, temp2 = 0;

            uint MSB = 0;
            for (uint i = 0; i < 7; i++)
            {
                MSB = 0;
                temp1 = GP_OTM1911A[cnt] & 0x00FF;
                temp2 = GP_OTM1911A[cnt] & 0x0F00;
                temp2 >>= 8;
                MSB = temp2 & 0x03;
                RegData[(4 + (i * 5))] = Convert.ToByte(MSB);
                RegData[(0 + (i * 5))] = Convert.ToByte(temp1);
                cnt++;

                temp1 = GP_OTM1911A[cnt] & 0x00FF;
                temp2 = GP_OTM1911A[cnt] & 0x0F00;
                temp2 >>= 8;
                MSB = MSB + ((temp2 & 0x03) << 2);
                RegData[(4 + (i * 5))] = Convert.ToByte(MSB);
                RegData[(1 + (i * 5))] = Convert.ToByte(temp1);
                cnt++;

                temp1 = GP_OTM1911A[cnt] & 0x00FF;
                temp2 = GP_OTM1911A[cnt] & 0x0F00;
                temp2 >>= 8;
                MSB = MSB + ((temp2 & 0x03) << 4);
                RegData[(4 + (i * 5))] = Convert.ToByte(MSB);
                RegData[(2 + (i * 5))] = Convert.ToByte(temp1);
                cnt++;

                temp1 = GP_OTM1911A[cnt] & 0x00FF;
                temp2 = GP_OTM1911A[cnt] & 0x0F00;
                temp2 >>= 8;
                MSB = MSB + ((temp2 & 0x03) << 6);
                RegData[(4 + (i * 5))] = Convert.ToByte(MSB);
                RegData[(3 + (i * 5))] = Convert.ToByte(temp1);
                cnt++;
            }

            temp1 = GP_OTM1911A[29] & 0x00FF;
            temp2 = GP_OTM1911A[29] & 0x0F00;
            temp2 >>= 8;
            MSB = temp2 & 0x03;
            RegData[36] = Convert.ToByte(MSB);
            RegData[35] = Convert.ToByte(temp1);

            //STEP5: 將預備要寫入暫存器的空間 填入IC中的暫存器
            for (byte reg = 0x00; reg < 0x25; reg++)
            {
                WhiskeyUtil.MipiWrite(0x23, 0x00, reg);
                WhiskeyUtil.MipiWrite(0x23, addr_MSB, RegData[reg]);
            }
        }



        private void deviceTimer_Tick(object sender, EventArgs e)
        {
            FindUsb();
        }

        private void UsbDeviceList()
        {
            SL_Device_Util deviceUtil = new SL_Device_Util();
            if (deviceUtil.GetUSBDevices() > 0)
            {
                List<SL_Device_Util.ScDeviceInfo> UsbDevice = deviceUtil.FindScDevice();
                UsbDeviceInfo = UsbDevice.ToArray();
                if (UsbDeviceInfo.Length > 1)
                    txtbox_info.Text = "Much Device,First Connected";
                else
                {
                    if (UsbDeviceInfo.Length == 1)
                    {
                        deviceUtil.getDeviceItem(UsbDeviceInfo[0].Description);
                        txtbox_info.Text = UsbDeviceInfo[0].DeviceID;
                        txtbox_vid.Text = "0x" + deviceUtil.getStrVid();
                        txtbox_pid.Text = "0x" + deviceUtil.getStrPid();
                        SL_Comm_Base.Device_Open((ushort)deviceUtil.getShortVid(), (ushort)deviceUtil.getShortPid());
                    }
                }
            }

        }

        private void FindUsb()
        {
            SL_Device_Util deviceUtil = new SL_Device_Util();
            if (deviceUtil.GetUSBDevices() > 0)
            {
                List<SL_Device_Util.ScDeviceInfo> UsbDevice = deviceUtil.FindScDevice();
                this.UsbDeviceInfo = UsbDevice.ToArray();

                if (UsbDeviceInfo.Length > 1)
                    txtbox_info.Text = "Much Device,First Connected";
                else
                {
                    if (UsbDeviceInfo.Length == 1)
                    {
                        deviceUtil.getDeviceItem(UsbDeviceInfo[0].Description);
                        txtbox_info.Text = UsbDeviceInfo[0].DeviceID;
                        txtbox_vid.Text = "0x" + deviceUtil.getStrVid();
                        txtbox_pid.Text = "0x" + deviceUtil.getStrPid();
                        SL_Comm_Base.Device_Open((ushort)deviceUtil.getShortVid(), (ushort)deviceUtil.getShortPid());
                    }
                }

            }
        }




        private void DSV_Setting(ref SL_WhiskyComm_Util WhiskeyUtil)
        {
            // SPLC095A 
            //Set External DSV Power
            //ssl.i2c.write 0x22 0x0c 0x48
            //ssl.i2c.write 0x22 0x09 0x0a
            //ssl.i2c.write 0x22 0x0d 0x67
            //ssl.i2c.write 0x22 0x0e 0x5e                          #Set AVDD = 5.5V
            //ssl.i2c.write 0x22 0x0f 0x5e                          #Set AVEE = -5.5V
            //ssl.i2c.write 0x22 0x02 0x50
            //ssl.i2c.write 0x22 0x0a 0x11

            //CSOT
            //WhiskeyUtil.i2cWrite(0x22, 0x0c, 0x48);
            //WhiskeyUtil.i2cWrite(0x22, 0x09, 0x0a);
            //WhiskeyUtil.i2cWrite(0x22, 0x0d, 0x67);
            //WhiskeyUtil.i2cWrite(0x22, 0x0e, 0xDD);
            //WhiskeyUtil.i2cWrite(0x22, 0x0f, 0xDD);
            //WhiskeyUtil.i2cWrite(0x22, 0x02, 0x50);
            //WhiskeyUtil.i2cWrite(0x22, 0x0A, 0x11);

            
            WhiskeyUtil.i2cWrite(0x22, 0x0c, 0x48);
            WhiskeyUtil.i2cWrite(0x22, 0x09, 0x0a);
            WhiskeyUtil.i2cWrite(0x22, 0x0d, 0x67);
            WhiskeyUtil.i2cWrite(0x22, 0x0e, 0x5e);
            WhiskeyUtil.i2cWrite(0x22, 0x0f, 0x5e);
            //WhiskeyUtil.i2cWrite(0x22, 0x02, 0x50);//50h Bright Voltage Out 25V
            WhiskeyUtil.i2cWrite(0x22, 0x02, 0x10);//10h Bright Voltage Out 18V
            WhiskeyUtil.i2cWrite(0x22, 0x0A, 0x11);



            /*SL_Comm_Base.SL_CommBase_WriteReg(0xa0, 0x20);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0c);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x48);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x09);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0a);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0d);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x67);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0e);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x55);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0f);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x55);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x50);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);

            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x02);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x22);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x0a);
            SL_Comm_Base.SL_CommBase_WriteReg(0x80, 0x11);
            SL_Comm_Base.SL_CommBase_WriteReg(0x9b, 0x01);*/
        }

        private void OTT1911A_CMD2_and_PassWord_Enable()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte[] RdVal = new byte[6];
            string rdstr = null;

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x19, 0x11, 0x01);

            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x80);
            WhiskeyUtil.MipiWrite(0x23, 0xFF, 0x19);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x81);
            WhiskeyUtil.MipiWrite(0x23, 0xFF, 0x11);




            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x78);

            WhiskeyUtil.MipiWrite(0x00, 0x00);
            WhiskeyUtil.MipiRead(0xF8, 6, ref RdVal); // rdstr: ID1: 0x40h
            //Info_textBox.Text += rdstr + "\r\n"; rdstr = null;

            WhiskeyUtil.MipiRead(0x0A, 1, ref rdstr); // rdstr: ID1: 0x40h


            WhiskeyUtil.MipiWrite(0x00, 0x00);
            WhiskeyUtil.MipiRead(0xDA, 1, ref rdstr); // rdstr: ID1: 0x40h
            //Info_textBox.Text +=  rdstr + "\r\n"; 
        }

        private void Vset_but_Click(object sender, EventArgs e)
        {

        }


        private void WHISKY_FPGA_InitialSetting(ref SL_WhiskyComm_Util WhiskeyUtil)
        {
            //SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank (Mipi Lane在SD卡那邊)
            //WhiskeyUtil.MipiBridgeSelect(0x01); //Select 2828 Bank (Mipi Lane在USB那邊)

            WhiskeyUtil.SetFpgaTiming(0x33, 0x11, 0x13, 0xff, 0x21, 0x0B);

            WhiskeyUtil.SetMipiVideo(1920, 1080, 60, 16, 16, 10, 10, 4, 4);

            WhiskeyUtil.SetMipiDsi(4, 820, "syncpulse");
            ///WhiskeyUtil.SetMipiDsi(4, 700, "burst"); 
            uint data = 0;
            SL_Comm_Base.SPI_ReadReg(0xbb, ref data, 2);

            DSV_Setting(ref WhiskeyUtil);

            data = 1;


        }



        private void LoadInitialCode ()
        {

            this.button3.ForeColor = Color.Green;
            byte[] RdVal = new byte[1];

            string textdata = null;

            Application.DoEvents();

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            Info_textBox.Text = "";

            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xff); //GPIO RESET
            Thread.Sleep(20);
            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xfe);
            Thread.Sleep(5);
            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xff);


            //OTT1911A_CMD2_and_PassWord_Enable();
            //SD2123_InitialCode_forAUO_nmosTypeA();
            //SSD2123_InitialCode_forCSOT_ES1p1_59p6HZ();
            //SSD2123_InitialCode_for_CSOT55_initial_code_v1P0_20171016(ref WhiskeyUtil);//FOR PANEL NO.1 FINE TUNE

            //SSD2123_GammaSetting_for_AUO_nmos_TypeA_v1_0_20170921();
            //SSD2123_GammaSetting_for_CSOT_v1_0_20171103();
            //SSD2123_GammaSetting_for_AUO_nmos_TypeA_R_Only_forGM2p2();




            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x04);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x23);
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0F);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x18, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x17, 0x03);




            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            WhiskeyUtil.MipiWrite(0x05, 0x11);//Sleep-Out
            Thread.Sleep(100);
            WhiskeyUtil.MipiWrite(0x05, 0x29);//Display-On




            WhiskeyUtil.MipiRead(0x0A, 1, ref RdVal);
            textdata = "Sleep-Out Display-On Power Status:" + Convert.ToString(RdVal[0]) + "\r\n";
            Info_textBox.AppendText(textdata);

            uint[] gammasetting = new uint[29];

            gammasetting[0] = 0;
            gammasetting[1] = 50;
            gammasetting[2] = 115;
            gammasetting[3] = 155;
            gammasetting[4] = 187;
            gammasetting[5] = 214;
            gammasetting[6] = 236;
            gammasetting[7] = 254;
            gammasetting[8] = 271;
            gammasetting[9] = 331;
            gammasetting[10] = 369;
            gammasetting[11] = 425;
            gammasetting[12] = 466;
            gammasetting[13] = 526;
            gammasetting[14] = 572;
            gammasetting[15] = 615;
            gammasetting[16] = 669;
            gammasetting[17] = 706;
            gammasetting[18] = 758;
            gammasetting[19] = 793;
            gammasetting[20] = 837;
            gammasetting[21] = 851;
            gammasetting[22] = 865;
            gammasetting[23] = 882;
            gammasetting[24] = 900;
            gammasetting[25] = 922;
            gammasetting[26] = 951;
            gammasetting[27] = 976;
            gammasetting[28] = 1022;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);


            gammasetting[0] = 0;
            gammasetting[1] = 51;
            gammasetting[2] = 119;
            gammasetting[3] = 163;
            gammasetting[4] = 195;
            gammasetting[5] = 221;
            gammasetting[6] = 242;
            gammasetting[7] = 262;
            gammasetting[8] = 279;
            gammasetting[9] = 337;
            gammasetting[10] = 375;
            gammasetting[11] = 430;
            gammasetting[12] = 470;
            gammasetting[13] = 529;
            gammasetting[14] = 575;
            gammasetting[15] = 617;
            gammasetting[16] = 672;
            gammasetting[17] = 707;
            gammasetting[18] = 760;
            gammasetting[19] = 794;
            gammasetting[20] = 838;
            gammasetting[21] = 851;
            gammasetting[22] = 865;
            gammasetting[23] = 882;
            gammasetting[24] = 901;
            gammasetting[25] = 922;
            gammasetting[26] = 947;
            gammasetting[27] = 978;
            gammasetting[28] = 1022;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);

            gammasetting[0] = 0;
            gammasetting[1] = 27;
            gammasetting[2] = 145;
            gammasetting[3] = 186;
            gammasetting[4] = 215;
            gammasetting[5] = 240;
            gammasetting[6] = 259;
            gammasetting[7] = 278;
            gammasetting[8] = 292;
            gammasetting[9] = 348;
            gammasetting[10] = 382;
            gammasetting[11] = 436;
            gammasetting[12] = 474;
            gammasetting[13] = 531;
            gammasetting[14] = 575;
            gammasetting[15] = 618;
            gammasetting[16] = 671;
            gammasetting[17] = 707;
            gammasetting[18] = 759;
            gammasetting[19] = 793;
            gammasetting[20] = 837;
            gammasetting[21] = 851;
            gammasetting[22] = 866;
            gammasetting[23] = 881;
            gammasetting[24] = 902;
            gammasetting[25] = 927;
            gammasetting[26] = 939;
            gammasetting[27] = 947;
            gammasetting[28] = 1020;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);




            this.button3.ForeColor = Color.Black;

            textdata = "Initial Code Done!! \n\r ";
            Info_textBox.AppendText(textdata);

            //WhiskeyUtil.ImageFill(255, 255, 255);
            WhiskeyUtil.ImageShow("11.bmp");

        }


        private void SSD2123_GammaSetting_for_AUO_nmos_TypeA_R_Only_forGM2p2()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 49;
            TieRegisterSetting[2] = 110;
            TieRegisterSetting[3] = 152;
            TieRegisterSetting[4] = 184;
            TieRegisterSetting[5] = 210;
            TieRegisterSetting[6] = 233;
            TieRegisterSetting[7] = 252;
            TieRegisterSetting[8] = 269;
            TieRegisterSetting[9] = 329;
            TieRegisterSetting[10] = 368;
            TieRegisterSetting[11] = 425;
            TieRegisterSetting[12] = 466;
            TieRegisterSetting[13] = 525;
            TieRegisterSetting[14] = 571;
            TieRegisterSetting[15] = 615;
            TieRegisterSetting[16] = 669;
            TieRegisterSetting[17] = 706;
            TieRegisterSetting[18] = 759;
            TieRegisterSetting[19] = 793;
            TieRegisterSetting[20] = 838;
            TieRegisterSetting[21] = 850;
            TieRegisterSetting[22] = 865;
            TieRegisterSetting[23] = 880;
            TieRegisterSetting[24] = 898;
            TieRegisterSetting[25] = 920;
            TieRegisterSetting[26] = 948;
            TieRegisterSetting[27] = 964;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x35);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x36);




            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 51;
            TieRegisterSetting[2] = 119;
            TieRegisterSetting[3] = 163;
            TieRegisterSetting[4] = 195;
            TieRegisterSetting[5] = 221;
            TieRegisterSetting[6] = 242;
            TieRegisterSetting[7] = 262;
            TieRegisterSetting[8] = 279;
            TieRegisterSetting[9] = 337;
            TieRegisterSetting[10] = 375;
            TieRegisterSetting[11] = 430;
            TieRegisterSetting[12] = 470;
            TieRegisterSetting[13] = 529;
            TieRegisterSetting[14] = 575;
            TieRegisterSetting[15] = 617;
            TieRegisterSetting[16] = 672;
            TieRegisterSetting[17] = 707;
            TieRegisterSetting[18] = 760;
            TieRegisterSetting[19] = 794;
            TieRegisterSetting[20] = 838;
            TieRegisterSetting[21] = 851;
            TieRegisterSetting[22] = 865;
            TieRegisterSetting[23] = 882;
            TieRegisterSetting[24] = 901;
            TieRegisterSetting[25] = 922;
            TieRegisterSetting[26] = 947;
            TieRegisterSetting[27] = 978;
            TieRegisterSetting[28] = 1022;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x33);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x34);

            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 70;
            TieRegisterSetting[2] = 141;
            TieRegisterSetting[3] = 183;
            TieRegisterSetting[4] = 214;
            TieRegisterSetting[5] = 238;
            TieRegisterSetting[6] = 259;
            TieRegisterSetting[7] = 277;
            TieRegisterSetting[8] = 292;
            TieRegisterSetting[9] = 348;
            TieRegisterSetting[10] = 384;
            TieRegisterSetting[11] = 436;
            TieRegisterSetting[12] = 475;
            TieRegisterSetting[13] = 531;
            TieRegisterSetting[14] = 575;
            TieRegisterSetting[15] = 618;
            TieRegisterSetting[16] = 671;
            TieRegisterSetting[17] = 707;
            TieRegisterSetting[18] = 758;
            TieRegisterSetting[19] = 792;
            TieRegisterSetting[20] = 837;
            TieRegisterSetting[21] = 850;
            TieRegisterSetting[22] = 865;
            TieRegisterSetting[23] = 880;
            TieRegisterSetting[24] = 894;
            TieRegisterSetting[25] = 921;
            TieRegisterSetting[26] = 930;
            TieRegisterSetting[27] = 944;
            TieRegisterSetting[28] = 1021;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x31);
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x32);

        }


        private void SSD2123_GammaSetting_for_CSOT_v1_0_20171103()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 112;
            TieRegisterSetting[2] = 152;
            TieRegisterSetting[3] = 178;
            TieRegisterSetting[4] = 199;
            TieRegisterSetting[5] = 216;
            TieRegisterSetting[6] = 231;
            TieRegisterSetting[7] = 244;
            TieRegisterSetting[8] = 256;
            TieRegisterSetting[9] = 301;
            TieRegisterSetting[10] = 332;
            TieRegisterSetting[11] = 383;
            TieRegisterSetting[12] = 423;
            TieRegisterSetting[13] = 488;
            TieRegisterSetting[14] = 541;
            TieRegisterSetting[15] = 589;
            TieRegisterSetting[16] = 639;
            TieRegisterSetting[17] = 668;
            TieRegisterSetting[18] = 708;
            TieRegisterSetting[19] = 736;
            TieRegisterSetting[20] = 774;
            TieRegisterSetting[21] = 787;
            TieRegisterSetting[22] = 801;
            TieRegisterSetting[23] = 817;
            TieRegisterSetting[24] = 837;
            TieRegisterSetting[25] = 861;
            TieRegisterSetting[26] = 894;
            TieRegisterSetting[27] = 949;
            TieRegisterSetting[28] = 1023;











            WriteGammaSettingAlltheSame_to_SSD2130(TieRegisterSetting);
        }


        private void SSD2123_GammaSetting_for_AUO_nmos_TypeA_v1_0_20170921()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            /*TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 150;
            TieRegisterSetting[2] = 195;
            TieRegisterSetting[3] = 225;
            TieRegisterSetting[4] = 245;
            TieRegisterSetting[5] = 265;
            TieRegisterSetting[6] = 285;
            TieRegisterSetting[7] = 300;
            TieRegisterSetting[8] = 312;
            TieRegisterSetting[9] = 362;
            TieRegisterSetting[10] = 398;
            TieRegisterSetting[11] = 452;
            TieRegisterSetting[12] = 492;
            TieRegisterSetting[13] = 559;
            TieRegisterSetting[14] = 610;
            TieRegisterSetting[15] = 655;
            TieRegisterSetting[16] = 705;
            TieRegisterSetting[17] = 735;
            TieRegisterSetting[18] = 781;
            TieRegisterSetting[19] = 818;
            TieRegisterSetting[20] = 867;
            TieRegisterSetting[21] = 888;
            TieRegisterSetting[22] = 935;
            TieRegisterSetting[23] = 1023;
            TieRegisterSetting[24] = 1023;
            TieRegisterSetting[25] = 1023;
            TieRegisterSetting[26] = 1023;
            TieRegisterSetting[27] = 1023;
            TieRegisterSetting[28] = 1023;*/

            //Gamma 1.8 Setting
            /*
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 165;
            TieRegisterSetting[2] = 201;
            TieRegisterSetting[3] = 222;
            TieRegisterSetting[4] = 243;
            TieRegisterSetting[5] = 261;
            TieRegisterSetting[6] = 276;
            TieRegisterSetting[7] = 288;
            TieRegisterSetting[8] = 303;
            TieRegisterSetting[9] = 345;
            TieRegisterSetting[10] = 378;
            TieRegisterSetting[11] = 429;
            TieRegisterSetting[12] = 471;
            TieRegisterSetting[13] = 534;
            TieRegisterSetting[14] = 585;
            TieRegisterSetting[15] = 630;
            TieRegisterSetting[16] = 675;
            TieRegisterSetting[17] = 702;
            TieRegisterSetting[18] = 738;
            TieRegisterSetting[19] = 762;
            TieRegisterSetting[20] = 801;
            TieRegisterSetting[21] = 810;
            TieRegisterSetting[22] = 825;
            TieRegisterSetting[23] = 837;
            TieRegisterSetting[24] = 855;
            TieRegisterSetting[25] = 876;
            TieRegisterSetting[26] = 900;
            TieRegisterSetting[27] = 942;
            TieRegisterSetting[28] = 1023;*/



            //Gamma 2.2 Setting
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 152;
            TieRegisterSetting[2] = 197;
            TieRegisterSetting[3] = 227;
            TieRegisterSetting[4] = 247;
            TieRegisterSetting[5] = 267;
            TieRegisterSetting[6] = 287;
            TieRegisterSetting[7] = 302;
            TieRegisterSetting[8] = 314;
            TieRegisterSetting[9] = 364;
            TieRegisterSetting[10] = 400;
            TieRegisterSetting[11] = 454;
            TieRegisterSetting[12] = 494;
            TieRegisterSetting[13] = 566;
            TieRegisterSetting[14] = 617;
            TieRegisterSetting[15] = 657;
            TieRegisterSetting[16] = 707;
            TieRegisterSetting[17] = 739;
            TieRegisterSetting[18] = 785;
            TieRegisterSetting[19] = 820;
            TieRegisterSetting[20] = 872;
            TieRegisterSetting[21] = 890;
            TieRegisterSetting[22] = 911;
            TieRegisterSetting[23] = 931;
            TieRegisterSetting[24] = 952;
            TieRegisterSetting[25] = 969;
            TieRegisterSetting[26] = 990;
            TieRegisterSetting[27] = 1011;
            TieRegisterSetting[28] = 1023;


            WriteGammaSettingAlltheSame_to_SSD2130(TieRegisterSetting);
        }

        private void SSD2123_InitialCode_for_CSOT55_initial_code_v1P0_20171016(ref SL_WhiskyComm_Util WhiskeyUtil)
        {
            byte[] RdVal = new byte[1];

            string textdata = null;
            int cnt = 0;

            textdata = "MiPi Read IC Power Status= ";

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0C);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0C);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x0D, 0x02);

            //WhiskeyUtil.MipiWrite(0x23 ,0x20 ,0x42);  
            //WhiskeyUtil.MipiWrite(0x23 ,0x21 ,0x3A);  
            //WhiskeyUtil.MipiWrite(0x23 ,0x22 ,0x22);  
            //WhiskeyUtil.MipiWrite(0x23 ,0x23 ,0x09);  
            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x42);
            WhiskeyUtil.MipiWrite(0x23, 0x21, 0x3a);
            WhiskeyUtil.MipiWrite(0x23, 0x22, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x23, 0x09);


            WhiskeyUtil.MipiWrite(0x23, 0x25, 0xC3);
            WhiskeyUtil.MipiWrite(0x23, 0x26, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x27, 0x0D);
            WhiskeyUtil.MipiWrite(0x23, 0x28, 0x65);
            WhiskeyUtil.MipiWrite(0x23, 0x2A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2D, 0x00);

            WhiskeyUtil.MipiWrite(0x23, 0x30, 0x81);
            WhiskeyUtil.MipiWrite(0x23, 0x31, 0x02);
            WhiskeyUtil.MipiWrite(0x23, 0x32, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x33, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x34, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x35, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x36, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x37, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x38, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x39, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3D, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3E, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3F, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x40, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x41, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x42, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x43, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x44, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x45, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x46, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x47, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x48, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x49, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4D, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4E, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4F, 0x00);

            WhiskeyUtil.MipiWrite(0x23, 0x70, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x71, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x72, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x73, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x74, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x75, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x76, 0x07);
            WhiskeyUtil.MipiWrite(0x23, 0x77, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x78, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x79, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x7E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x7F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x80, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x81, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x82, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x83, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x84, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x85, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x86, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x88, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x89, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x8E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x8F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x90, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x91, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x92, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x93, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x94, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x95, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x96, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x97, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x98, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x99, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x9E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x9F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0xA0, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA1, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0xA2, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0xA3, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0xA4, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0xA5, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0xA6, 0x07);
            WhiskeyUtil.MipiWrite(0x23, 0xA7, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA8, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA9, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAD, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0xAE, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0xAF, 0x30);

            WhiskeyUtil.MipiWrite(0x23, 0xC7, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0xC8, 0x57);
            WhiskeyUtil.MipiWrite(0x23, 0xCB, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0xD2, 0x79);
            WhiskeyUtil.MipiWrite(0x23, 0xD3, 0x19);
            WhiskeyUtil.MipiWrite(0x23, 0xD4, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0xD6, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD7, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD8, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xDA, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xDB, 0x18);
            WhiskeyUtil.MipiWrite(0x23, 0xE0, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xE1, 0x3F);
            WhiskeyUtil.MipiWrite(0x23, 0xE2, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xE3, 0x0F);
            WhiskeyUtil.MipiWrite(0x23, 0xE4, 0xAA);
            WhiskeyUtil.MipiWrite(0x23, 0xE5, 0xAA);
            WhiskeyUtil.MipiWrite(0x23, 0xE6, 0xBA);
            WhiskeyUtil.MipiWrite(0x23, 0xE7, 0x75);
            WhiskeyUtil.MipiWrite(0x23, 0xEA, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0xEB, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0xEC, 0x50);
            WhiskeyUtil.MipiWrite(0x23, 0xF2, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xF5, 0x43);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x11);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x11, 0x8F);
            WhiskeyUtil.MipiWrite(0x23, 0x12, 0x0A);





            //Iphone OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x61, 0x92);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x0f);
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0a);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0a);


            //Iphone function
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x44);
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x09, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x0A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x0B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x01);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xB1);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

            //IPhone MY
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x45);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x04);

            //VCOM , GVDDP/N ,AVDDREF/AVEEREF
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);                          //VCOM 1
            WhiskeyUtil.MipiWrite(0x23, 0x04, 0x18);                          //VCOM 1 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);                          //VCOM 2
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x18);                          //VCOM 2 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x74);                          //GVDDP = 5V
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x74);                          //GVDDN = -5V
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x77);                          //AVDDREF/AVEEREF=+5V/-5V


            //MIPI OPT                  
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x16);                          //di_mipi_sel_clk[4:0] skew
            WhiskeyUtil.MipiWrite(0x23, 0x63, 0x00);                          //di_mipi_sel_D0[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x64, 0x18);                          //di_mipi_sel_D1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x18);                          //di_mipi_sel_D2[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x66, 0x18);                         //di_mipi_sel_D3[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x69, 0x74);                         //di_mipi_swihsrx2[2:0]  RX bias
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x04);                         //d2a_mipi_gb_sw[2:0]

            //MIPI CD disable
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x6b, 0xfe);

            //VDD OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x28);                         //VDD REG slew rate 28 [1:0]BGIR

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);

            //Tuning test code

            //OSC trim target=90.4M
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x8B);                         //OSC trim code

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x7a, 0x02);                         //0 frame 2 line  source chop

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);


        }


        private void SSD2123_InitialCode_for_AUO_nmos_TypeA_initial_code_v3_2_20170920(ref SL_WhiskyComm_Util WhiskeyUtil)
        {
            //SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte[] RdVal = new byte[1];

            string textdata = null;
            int cnt = 0;

            textdata = "MiPi Read IC Power Status= ";



            //ssl.mipi.read,0x0a 1
            //pause stop
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //AUO_nmos TypeA_initial_code v1.2 from Johnny Tsai
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x14);  //[5:0]t8_de
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);  //[5:0]t7p_de
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0C);  //[7:4]t9p_de, [3:0]t9_de
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x2B);  //[5:0]t7_de
            WhiskeyUtil.MipiWrite(0x23, 0x0e, 0x80);  // CKH_3TO1

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0C);  //[5:0]SD-CKH  Setup time, refer to t9_de

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x11);  //[7:4]ckh_vbp, [3:0]ckh_vfp
            WhiskeyUtil.MipiWrite(0x23, 0x0D, 0x82);  //[7]CKH_VP_Full, [6:5]CKH2_RGB_Sel, [4]CKH_VP_REG_EN, [3]CKH_RGB_Zigazg, [2]CKH_321_Frame, [1]CKH_321_Line, [0]CKH_321

            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x41);  //[7:6]STV_A_Rise[6:5], [4:0]STV_A_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x21, 0x29);  //[5]FTI_A_Rise_mode, [4]FTI_A_Fall_mode, [3:2]Phase_STV_A, [1:0]Overlap_STV_A
            WhiskeyUtil.MipiWrite(0x23, 0x22, 0x62);  //[7:0]FTI_A_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x23, 0x62);  //[7:0]FTI_A_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x25, 0x02);  //[7:6]STV_B_Rise[6:5], [4:0]STV_B_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x26, 0x1E);  //[5]FTI_B_Rise_mode, [4]FTI_B_Fall_mode, [3:2]Phase_STV_B, [1:0]Overlap_STV_B
            WhiskeyUtil.MipiWrite(0x23, 0x27, 0x0C);  //[7:0]FTI_B_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x28, 0x0C);  //[7:0]FTI_B_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x2A, 0x02);  //[7:6]STV_C_Rise[6:5], [4:0]STV_C_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x2B, 0x19);  //[5]FTI_C_Rise_mode, [4]FTI_C_Fall_mode, [3:2]Phase_STV_C, [1:0]Overlap_STV_C
            WhiskeyUtil.MipiWrite(0x23, 0x2C, 0x0C);  //[7:0]FTI_C_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x2D, 0x0C);  //[7:0]FTI_C_Fall

            WhiskeyUtil.MipiWrite(0x23, 0x30, 0x81);  //[7]CLK_A_Rise[5], [4:0]CLK_A_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x31, 0x01);  //[7]CLK_A_Fall[5], [4:0]CLK_A_Fall[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x32, 0x11);  //[7:4]Phase_CLK_A, [3:0]Overlap_CLK_A
            WhiskeyUtil.MipiWrite(0x23, 0x33, 0x31);  //[7]CLK_A_inv, [6]CLK_A_stop_level, [5] CLK_A_ct_mode, [4] CLK_A_Keep, [1]CLW_A_Rise_mode, [0]CLW_A_Fall_mode
            WhiskeyUtil.MipiWrite(0x23, 0x34, 0x0C);  //[7:0]CLW_A1_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x35, 0x0C);  //[7:0]CLW_A2_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x36, 0x00);  //[7:0]CLW_A1_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x37, 0x00);  //[7:0]CLW_A2_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x38, 0x00);  //[7:0]CLK_A_Rise_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x39, 0x00);  //[7:0]CLK_A_Rise_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x3A, 0x00);  //[7:0]CLK_A_Fall_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x3B, 0x00);  //[7:0]CLK_A_Fall_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x3C, 0x20);  //[5]CLK_A_VBP_Keep_gs_Chg, [4]CLK_A_VFP_Keep_gs_Chg, [3:2]CLK_A_Keep_Pos2_gs_Chg, [1:0]CLK_A_Keep_Pos1_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x3D, 0x08);  //[7:6]CLK_A4_Stop_Level_gs_Chg, [5:4] CLK_A3_Stop_Level_gs_Chg, [3:2]CLK_A2_Stop_Level_gs_Chg, [1:0]CLK_A1_Stop_Level_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x3E, 0x00);  //[7]CLK_A_Keep_Pos1[5], [4:0]CLK_A_Keep_Pos1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x3F, 0x00);  //[7]CLK_A_Keep_Pos2[5], [4:0]CLK_A_Keep_Pos2[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x40, 0x00);  //[7]CLK_B_Rise[5], [4:0]CLK_B_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x41, 0x00);  //[7]CLK_B_Fall[5], [4:0]CLK_B_Fall[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x42, 0x00);  //[7:4]Phase_CLK_B, [3:0]Overlap_CLK_B
            WhiskeyUtil.MipiWrite(0x23, 0x43, 0x00);  //[7]CLK_B_inv, [6]CLK_B_stop_level, [5] CLK_B_ct_mode, [4] CLK_B_Keep, [1]CLW_B_Rise_mode, [0]CLW_B_Fall_mode
            WhiskeyUtil.MipiWrite(0x23, 0x44, 0x00);  //[7:0]CLW_B1_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x45, 0x00);  //[7:0]CLW_B2_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x46, 0x00);  //[7:0]CLW_B1_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x47, 0x00);  //[7:0]CLW_B2_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x48, 0x00);  //[7:0]CLK_B_Rise_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x49, 0x00);  //[7:0]CLK_B_Rise_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x4A, 0x00);  //[7:0]CLK_B_Fall_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x4B, 0x00);  //[7:0]CLK_B_Fall_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x4C, 0x00);  //[5]CLK_B_VBP_Keep_gs_Chg, [4]CLK_B_VFP_Keep_gs_Chg, [3:2]CLK_B_Keep_Pos2_gs_Chg, [1:0]CLK_B_Keep_Pos1_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x4D, 0x00);  //[7:6]CLK_B4_Stop_Level_gs_Chg, [5:4] CLK_B3_Stop_Level_gs_Chg, [3:2]CLK_B2_Stop_Level_gs_Chg, [1:0]CLK_B1_Stop_Level_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x4E, 0x00);  //[7]CLK_B_Keep_Pos1[5], [4:0]CLK_B_Keep_Pos1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x4F, 0x00);  //[7]CLK_B_Keep_Pos2[5], [4:0]CLK_B_Keep_Pos2[4:0]

            WhiskeyUtil.MipiWrite(0x23, 0x70, 0x06);  //GOUT_R_01_FW
            WhiskeyUtil.MipiWrite(0x23, 0x71, 0x37);  //GOUT_R_02_FW
            WhiskeyUtil.MipiWrite(0x23, 0x72, 0x36);  //GOUT_R_03_FW
            WhiskeyUtil.MipiWrite(0x23, 0x73, 0x10);  //GOUT_R_04_FW
            WhiskeyUtil.MipiWrite(0x23, 0x74, 0x11);  //GOUT_R_05_FW
            WhiskeyUtil.MipiWrite(0x23, 0x75, 0x0A);  //GOUT_R_06_FW
            WhiskeyUtil.MipiWrite(0x23, 0x76, 0x2A);  //GOUT_R_07_FW
            WhiskeyUtil.MipiWrite(0x23, 0x77, 0x2A);  //GOUT_R_08_FW
            WhiskeyUtil.MipiWrite(0x23, 0x78, 0x0E);  //GOUT_R_09_FW
            WhiskeyUtil.MipiWrite(0x23, 0x79, 0x00);  //GOUT_R_10_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x00);  //GOUT_R_11_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x00);  //GOUT_R_12_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x00);  //GOUT_R_13_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7D, 0x30);  //GOUT_R_14_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7E, 0x31);  //GOUT_R_15_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7F, 0x32);  //GOUT_R_16_FW
            WhiskeyUtil.MipiWrite(0x23, 0x80, 0x06);  //GOUT_L_01_FW
            WhiskeyUtil.MipiWrite(0x23, 0x81, 0x37);  //GOUT_L_02_FW
            WhiskeyUtil.MipiWrite(0x23, 0x82, 0x36);  //GOUT_L_03_FW
            WhiskeyUtil.MipiWrite(0x23, 0x83, 0x10);  //GOUT_L_04_FW
            WhiskeyUtil.MipiWrite(0x23, 0x84, 0x11);  //GOUT_L_05_FW
            WhiskeyUtil.MipiWrite(0x23, 0x85, 0x0A);  //GOUT_L_06_FW
            WhiskeyUtil.MipiWrite(0x23, 0x86, 0x2A);  //GOUT_L_07_FW
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x2A);  //GOUT_L_08_FW
            WhiskeyUtil.MipiWrite(0x23, 0x88, 0x0E);  //GOUT_L_09_FW
            WhiskeyUtil.MipiWrite(0x23, 0x89, 0x00);  //GOUT_L_10_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x00);  //GOUT_L_11_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x00);  //GOUT_L_12_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x00);  //GOUT_L_13_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8D, 0x30);  //GOUT_L_14_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8E, 0x31);  //GOUT_L_15_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8F, 0x32);  //GOUT_L_16_FW
            WhiskeyUtil.MipiWrite(0x23, 0x90, 0x0E);  //GOUT_R_01_BW
            WhiskeyUtil.MipiWrite(0x23, 0x91, 0x37);  //GOUT_R_02_BW
            WhiskeyUtil.MipiWrite(0x23, 0x92, 0x36);  //GOUT_R_03_BW
            WhiskeyUtil.MipiWrite(0x23, 0x93, 0x11);  //GOUT_R_04_BW
            WhiskeyUtil.MipiWrite(0x23, 0x94, 0x10);  //GOUT_R_05_BW
            WhiskeyUtil.MipiWrite(0x23, 0x95, 0x0A);  //GOUT_R_06_BW
            WhiskeyUtil.MipiWrite(0x23, 0x96, 0x2A);  //GOUT_R_07_BW
            WhiskeyUtil.MipiWrite(0x23, 0x97, 0x2A);  //GOUT_R_08_BW
            WhiskeyUtil.MipiWrite(0x23, 0x98, 0x06);  //GOUT_R_09_BW
            WhiskeyUtil.MipiWrite(0x23, 0x99, 0x00);  //GOUT_R_10_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x00);  //GOUT_R_11_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x00);  //GOUT_R_12_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x00);  //GOUT_R_13_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9D, 0x30);  //GOUT_R_14_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9E, 0x31);  //GOUT_R_15_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9F, 0x32);  //GOUT_R_16_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA0, 0x0E);  //GOUT_L_01_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA1, 0x37);  //GOUT_L_02_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA2, 0x36);  //GOUT_L_03_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA3, 0x11);  //GOUT_L_04_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA4, 0x10);  //GOUT_L_05_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA5, 0x0A);  //GOUT_L_06_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA6, 0x2A);  //GOUT_L_07_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA7, 0x2A);  //GOUT_L_08_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA8, 0x06);  //GOUT_L_09_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA9, 0x00);  //GOUT_L_10_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x00);  //GOUT_L_11_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x00);  //GOUT_L_12_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x00);  //GOUT_L_13_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAD, 0x30);  //GOUT_L_14_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAE, 0x31);  //GOUT_L_15_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAF, 0x32);  //GOUT_L_16_BW

            WhiskeyUtil.MipiWrite(0x23, 0xC7, 0x22);  //[7:4]Blank_Frame_OPT1[3:0], [3:0]Blank_Frame_OPT2[3:0]
            WhiskeyUtil.MipiWrite(0x23, 0xC8, 0x57);  //[7:6]SRC_Front_Blank_Sel, [5:4]SRC_Mid_Blank_Sel, [3:2]SRC_Back_Blank_Sel
            WhiskeyUtil.MipiWrite(0x23, 0xCB, 0x00);  //[5:4]GOUT_LVD, [3:2]GOUT_SO, [1:0]GOUT_SI
            WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x11);  //[5:4]ONSeq_Ext, [2:0] OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD2, 0x79);  //[7:6]CLK_B_ONSeq_Ext, [5]CLK_A_ONSeq_Ext, [4] STV_C_ONSeq_Ext, [3:2]STV_B_ONSeq_Ext, STV_A_ONSeq_Ext, 00:ori, 01:VGL, 10:VGH, 11:GND
            WhiskeyUtil.MipiWrite(0x23, 0xD3, 0x19);  //[5:4]CKH_ONSeq_Ext, [3]CLK_D_ONSeq_Ext, [1:0]CLK_C_ONSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD4, 0x10);  //[6]RESET_ONSeq_Ext, [4]CLK_E_ONSeq_Ext, [2]GAS_ONSeq_Ext, [1:0]FWBW_ONSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD6, 0x00);  //[7:6]CLK_A_OFFSeq_Ext, [5:4]STV_B_OFFSeq_Ext, [3:2]STV_B_OFFSeq_Ext, [1:0]STV_A_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD7, 0x00);  //[7:6]CKH_OFFSeq_Ext, [5:4]CLK_D_OFFSeq_Ext, [3:2]CLK_C_OFFSeq_Ext, [1:0]CLK_B_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD8, 0x00);  //[7:6]CLK_E_OFFSeq_Ext, [4]Reset_OFFSet_Ext, [2]GAS_OFFSeq_Ext, [1:0]FWBW_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xDA, 0xFF);  //[7]CKH_AbnSeq, [6]CLK_D_AbnSeq, [5]CLK_C_AbnSeq, [4]CLK_B_AbnSeq, [3]CLK_A_AbnSeq, [2]STV_C_AbnSeq, [1]STV_B_AbnSeq, [0]STV_A_AbnSeq, 0:VGL, 1:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xDB, 0x1A);  //[4]CLK_E_AbnSeq, [3]Reset_AbnSeq, [2]GAS_AbnSeq, [1:0]FWBW_AbnSeq, 00:norm, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE0, 0x54);  //[7:6]STV_A_ONSeq, [5:4]STV_B_ONSeq, [3:2]STV_C_ONSeq, 00:ori, 01:VGL, 10:VGH, 11:GND
            WhiskeyUtil.MipiWrite(0x23, 0xE1, 0x15);  //[6:4]CLK_A_ONSeq, [4:3]CLK_B_ONSeq, [1:0]CLK_C_ONSeq, 00:ori, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE2, 0x19);  //[7:6]CLK_D_ONSeq, [5:4]CLK_E_ONSeq, [3:2]CKH_ONSeq, [1:0]FWBW_ONSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE3, 0x00);  //[3:2]GAS_ONSeq, [1:0]Reset_ONSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE4, 0x00);  //[7:6]STV_A_OFFSeq, [5:4]STV_B_OFFSeq, [3:2]STV_C_OFFSeq, [1:0]CLK_A_OFFSeq, 00:ori, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE5, 0x00);  //[7:6]CLK_B_OFFSeq, [5:4]CLK_C_OFFSeq, [3:2]CLK_D_OFFSeq, [1:0]CLK_E_OFFSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE6, 0x10);  //[7:6]CKH_OFFSeq, [5:4]FWBW_OFFSeq, [3:2]Reset_OFFSet, [1:0]GAS_OFFSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE7, 0x75);  //[6]SRC_ONSeq_OPT, [5:4]VCM_ONSeq_OPT, [2]SRC_OFFSeq_OPT, [1:0]VCM_OFFSeq_OPT
            WhiskeyUtil.MipiWrite(0x23, 0xEA, 0x00);  //[7:4]STV_Onoff_Seq_dly, [3:0]VCK_A_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xEB, 0x00);  //[7:4]VCK_B_Onoff_Seq_dly, [3:0]VCK_C_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xEC, 0x00);  //[7:4]CKH_Onoff_Seq_dly, [3:0]GAS_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xF2, 0x00);  //[7]GS_Sync_2frm_opt
            WhiskeyUtil.MipiWrite(0x23, 0xF5, 0x43);  //[7:6]RST_Each_Frame, [5]GIP_RST_INV, [4]PWRON_RST_OPT, [3:0]GRST_WID_ONSeq_EXT[11:8]

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x60, 0x00);  // Panel Scheme Selection
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x20);  // Column Inversion
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x11);  //[6:4]VFP_CKH_DUM, [2:0]VBP_CKH_DUM


            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x44); //OSC Bias Always On
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x03);


            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x11, 0x8D);  //[7]VGH_ratio, [5:0]VGHS, default:12v; AUO:8.5v; CSOT:9v  change,0x8D -->0x 0D to 2x
            WhiskeyUtil.MipiWrite(0x23, 0x12, 0x0F);  //[7]VGL_ratio, [5:0]VGLS, default:-8v; AUO:-8v; CSOT:-7v

            //Iphone OPT
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x12);
            //WhiskeyUtil.MipiWrite(0x23,0x61,0x92);                         //OSC frequency, default 90.43Mhz; For iphone+ MIPI 1.2Ghz case, OSC= 95.78Mhz
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x10);
            //WhiskeyUtil.MipiWrite(0x23,0x00,0x0f);                          //[5:0]t8_de
            //WhiskeyUtil.MipiWrite(0x23,0x01,0x00);                          //[5:0]t7p_de
            //WhiskeyUtil.MipiWrite(0x23,0x02,0x0a);                          //[7:4]t9p_de, [3:0]t9_de
            //WhiskeyUtil.MipiWrite(0x23,0x03,0x22);                          //[5:0]t7_de
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x11); 
            //WhiskeyUtil.MipiWrite(0x23,0x54,0x0a);                          //[5:0]SD-CKH  Setup time, refer to t9_de

            //Iphone function
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x44); 
            //WhiskeyUtil.MipiWrite(0x23,0x08,0x00);                          //FTE1_SEL
            //WhiskeyUtil.MipiWrite(0x23,0x09,0x06);                          //FTE_SEL = HIFA
            //WhiskeyUtil.MipiWrite(0x23,0x0A,0x00);                          //VSOUT_SEL
            //WhiskeyUtil.MipiWrite(0x23,0x0B,0x00);                          //HSOUT_SEL
            //WhiskeyUtil.MipiWrite(0x23,0x20,0x01);                          //SDO_SEL = MSYNC

            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0xB1); 
            //WhiskeyUtil.MipiWrite(0x23,0x00,0x01);                          //IPhone Func. Enable

            //IPhone MY
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x45); 
            //WhiskeyUtil.MipiWrite(0x23,0x03,0x05); 

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);                         //VCOM 1
            WhiskeyUtil.MipiWrite(0x23, 0x04, 0x1D);                         //VCOM 1 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);                         //VCOM 2
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x1D);                         //VCOM 2 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x74);                         //GVDDP = 5V
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x74);                         //GVDDN = -5V


            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Tuning test code

            //MX
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x45);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x06);

            //OSC trim target=90.4M
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x8A);                         //OSC trim code
                                                                             //ssl.mipi.read,0x62 1

            //TEST MODE for OSC trim 1/(period/780)
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x44);
            //WhiskeyUtil.MipiWrite(0x23,0x09,0x0c);

            //BIST Manual mode
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x11);
            //WhiskeyUtil.MipiWrite(0x23,0x41,0x0E);                         //BIST pattern select all color
            //WhiskeyUtil.MipiWrite(0x23,0x31,0x2F);                         //BIST pattern select all color
            //WhiskeyUtil.MipiWrite(0x23,0x30,0x01);                         //BIST enable

            //MIPI CD disable
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x6b, 0xfe);

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //IPhone ID check
            //ssl.mipi.read,0xb1 15


            /*Andy Kang Add xrgb*/
            /*WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x33);
            WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x35);
            WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x33);
            WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x35);

            WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x33);
            WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x35);
            WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x33);
            WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x35);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);*/

        }

        private void SSD2123_InitialCode_forCSOT_ES1p1_59p6HZ()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte[] RdVal = new byte[1];

            string textdata = null;
            int cnt = 0;

            textdata = "MiPi Read IC Power Status= ";


            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0C);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0C);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x0D, 0x02);

            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x42);
            WhiskeyUtil.MipiWrite(0x23, 0x21, 0x3a);
            WhiskeyUtil.MipiWrite(0x23, 0x22, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x23, 0x09);


            WhiskeyUtil.MipiWrite(0x23, 0x25, 0xC3);
            WhiskeyUtil.MipiWrite(0x23, 0x26, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x27, 0x0D);
            WhiskeyUtil.MipiWrite(0x23, 0x28, 0x65);
            WhiskeyUtil.MipiWrite(0x23, 0x2A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x2D, 0x00);

            WhiskeyUtil.MipiWrite(0x23, 0x30, 0x81);
            WhiskeyUtil.MipiWrite(0x23, 0x31, 0x02);
            WhiskeyUtil.MipiWrite(0x23, 0x32, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x33, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x34, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x35, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x36, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x37, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x38, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x39, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3D, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3E, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x3F, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x40, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x41, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x42, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x43, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x44, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x45, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x46, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x47, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x48, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x49, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4C, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4D, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4E, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x4F, 0x00);

            WhiskeyUtil.MipiWrite(0x23, 0x70, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x71, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x72, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0x73, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x74, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x75, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x76, 0x07);
            WhiskeyUtil.MipiWrite(0x23, 0x77, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x78, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x79, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x7D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x7E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x7F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x80, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x81, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x82, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x83, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x84, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x85, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x86, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x88, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x89, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x8D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x8E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x8F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0x90, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x91, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x92, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x93, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0x94, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0x95, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0x96, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x97, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x98, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x99, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0x9D, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0x9E, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0x9F, 0x30);
            WhiskeyUtil.MipiWrite(0x23, 0xA0, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA1, 0x13);
            WhiskeyUtil.MipiWrite(0x23, 0xA2, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0xA3, 0x2F);
            WhiskeyUtil.MipiWrite(0x23, 0xA4, 0x37);
            WhiskeyUtil.MipiWrite(0x23, 0xA5, 0x36);
            WhiskeyUtil.MipiWrite(0x23, 0xA6, 0x07);
            WhiskeyUtil.MipiWrite(0x23, 0xA7, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA8, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xA9, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x05);
            WhiskeyUtil.MipiWrite(0x23, 0xAD, 0x32);
            WhiskeyUtil.MipiWrite(0x23, 0xAE, 0x31);
            WhiskeyUtil.MipiWrite(0x23, 0xAF, 0x30);

            WhiskeyUtil.MipiWrite(0x23, 0xC7, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0xC8, 0x57);
            WhiskeyUtil.MipiWrite(0x23, 0xCB, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0xD2, 0x79);
            WhiskeyUtil.MipiWrite(0x23, 0xD3, 0x19);
            WhiskeyUtil.MipiWrite(0x23, 0xD4, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0xD6, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD7, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xD8, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xDA, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xDB, 0x18);
            WhiskeyUtil.MipiWrite(0x23, 0xE0, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xE1, 0x3F);
            WhiskeyUtil.MipiWrite(0x23, 0xE2, 0xFF);
            WhiskeyUtil.MipiWrite(0x23, 0xE3, 0x0F);
            WhiskeyUtil.MipiWrite(0x23, 0xE4, 0xAA);
            WhiskeyUtil.MipiWrite(0x23, 0xE5, 0xAA);
            WhiskeyUtil.MipiWrite(0x23, 0xE6, 0xBA);
            WhiskeyUtil.MipiWrite(0x23, 0xE7, 0x75);
            WhiskeyUtil.MipiWrite(0x23, 0xEA, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0xEB, 0x34);
            WhiskeyUtil.MipiWrite(0x23, 0xEC, 0x50);
            WhiskeyUtil.MipiWrite(0x23, 0xF2, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0xF5, 0x43);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x11);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x11, 0x8F);
            WhiskeyUtil.MipiWrite(0x23, 0x12, 0x0A);





            //Iphone OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x22);
            WhiskeyUtil.MipiWrite(0x23, 0x61, 0x94);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x0f);
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0a);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0a);

            //Iphone function
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x44);
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x09, 0x06);
            WhiskeyUtil.MipiWrite(0x23, 0x0A, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x0B, 0x00);
            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x01);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xB1);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

            //IPhone MY
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x45);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x04);


            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);
            WhiskeyUtil.MipiWrite(0x23, 0x04, 0x18);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x18);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x74);
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x74);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x77);

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x04);
            WhiskeyUtil.MipiWrite(0x23, 0x69, 0x74);



            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x28);

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x7a, 0x02);

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);
            WhiskeyUtil.MipiWrite(0x23, 0x04, 0x21);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x21);


            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);

            //==================================================================
            // 5.5" AUO NMOS panel
            // SSD2130 ES1.1 Initial Code
            // MIPI 4 Lanes
            // Created 2017.12.05
            // IOVCC=1.8V, DDVDH=5.45V, DDVDL=-5.45V

            //===================================================================

            // Revision History :
            // v1.0 First Release 2017-12-05

            //==============================================================================
            //AUO NMOS TYPE A initial code
            //==============================================================================

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x00, 0x14);
            //WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x03, 0x2B);

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0C);

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x05, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x0D, 0x82);

            //WhiskeyUtil.MipiWrite(0x23, 0x20, 0x41);
            //WhiskeyUtil.MipiWrite(0x23, 0x21, 0x29);
            //WhiskeyUtil.MipiWrite(0x23, 0x22, 0x62);
            //WhiskeyUtil.MipiWrite(0x23, 0x23, 0x62);
            //WhiskeyUtil.MipiWrite(0x23, 0x25, 0x02);
            //WhiskeyUtil.MipiWrite(0x23, 0x26, 0x1E);
            //WhiskeyUtil.MipiWrite(0x23, 0x27, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x28, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x2A, 0x02);
            //WhiskeyUtil.MipiWrite(0x23, 0x2B, 0x19);
            //WhiskeyUtil.MipiWrite(0x23, 0x2C, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x2D, 0x0C);

            //WhiskeyUtil.MipiWrite(0x23, 0x30, 0x81);
            //WhiskeyUtil.MipiWrite(0x23, 0x31, 0x01);
            //WhiskeyUtil.MipiWrite(0x23, 0x32, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x33, 0x31);
            //WhiskeyUtil.MipiWrite(0x23, 0x34, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x35, 0x0C);
            //WhiskeyUtil.MipiWrite(0x23, 0x36, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x37, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x38, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x39, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x3A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x3B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x3C, 0x20);
            //WhiskeyUtil.MipiWrite(0x23, 0x3D, 0x08);
            //WhiskeyUtil.MipiWrite(0x23, 0x3E, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x3F, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x40, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x41, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x42, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x43, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x44, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x45, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x46, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x47, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x48, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x49, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4C, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4D, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4E, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x4F, 0x00);

            //WhiskeyUtil.MipiWrite(0x23, 0x70, 0x06);
            //WhiskeyUtil.MipiWrite(0x23, 0x71, 0x37);
            //WhiskeyUtil.MipiWrite(0x23, 0x72, 0x36);
            //WhiskeyUtil.MipiWrite(0x23, 0x73, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x74, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x75, 0x0A);
            //WhiskeyUtil.MipiWrite(0x23, 0x76, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x77, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x78, 0x0E);
            //WhiskeyUtil.MipiWrite(0x23, 0x79, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x7D, 0x30);
            //WhiskeyUtil.MipiWrite(0x23, 0x7E, 0x31);
            //WhiskeyUtil.MipiWrite(0x23, 0x7F, 0x32);
            //WhiskeyUtil.MipiWrite(0x23, 0x80, 0x06);
            //WhiskeyUtil.MipiWrite(0x23, 0x81, 0x37);
            //WhiskeyUtil.MipiWrite(0x23, 0x82, 0x36);
            //WhiskeyUtil.MipiWrite(0x23, 0x83, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x84, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x85, 0x0A);
            //WhiskeyUtil.MipiWrite(0x23, 0x86, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x87, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x88, 0x0E);
            //WhiskeyUtil.MipiWrite(0x23, 0x89, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x8D, 0x30);
            //WhiskeyUtil.MipiWrite(0x23, 0x8E, 0x31);
            //WhiskeyUtil.MipiWrite(0x23, 0x8F, 0x32);
            //WhiskeyUtil.MipiWrite(0x23, 0x90, 0x0E);
            //WhiskeyUtil.MipiWrite(0x23, 0x91, 0x37);
            //WhiskeyUtil.MipiWrite(0x23, 0x92, 0x36);
            //WhiskeyUtil.MipiWrite(0x23, 0x93, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x94, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x95, 0x0A);
            //WhiskeyUtil.MipiWrite(0x23, 0x96, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x97, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0x98, 0x06);
            //WhiskeyUtil.MipiWrite(0x23, 0x99, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x9D, 0x30);
            //WhiskeyUtil.MipiWrite(0x23, 0x9E, 0x31);
            //WhiskeyUtil.MipiWrite(0x23, 0x9F, 0x32);
            //WhiskeyUtil.MipiWrite(0x23, 0xA0, 0x0E);
            //WhiskeyUtil.MipiWrite(0x23, 0xA1, 0x37);
            //WhiskeyUtil.MipiWrite(0x23, 0xA2, 0x36);
            //WhiskeyUtil.MipiWrite(0x23, 0xA3, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0xA4, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0xA5, 0x0A);
            //WhiskeyUtil.MipiWrite(0x23, 0xA6, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0xA7, 0x2A);
            //WhiskeyUtil.MipiWrite(0x23, 0xA8, 0x06);
            //WhiskeyUtil.MipiWrite(0x23, 0xA9, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xAD, 0x30);
            //WhiskeyUtil.MipiWrite(0x23, 0xAE, 0x31);
            //WhiskeyUtil.MipiWrite(0x23, 0xAF, 0x32);

            //WhiskeyUtil.MipiWrite(0x23, 0xC7, 0x22);
            //WhiskeyUtil.MipiWrite(0x23, 0xC8, 0x57);
            //WhiskeyUtil.MipiWrite(0x23, 0xCB, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0xD2, 0x79);
            //WhiskeyUtil.MipiWrite(0x23, 0xD3, 0x19);
            //WhiskeyUtil.MipiWrite(0x23, 0xD4, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0xD6, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xD7, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xD8, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xDA, 0xFF);
            //WhiskeyUtil.MipiWrite(0x23, 0xDB, 0x1A);
            //WhiskeyUtil.MipiWrite(0x23, 0xE0, 0x54);
            //WhiskeyUtil.MipiWrite(0x23, 0xE1, 0x15);
            //WhiskeyUtil.MipiWrite(0x23, 0xE2, 0x19);
            //WhiskeyUtil.MipiWrite(0x23, 0xE3, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xE4, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xE5, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xE6, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0xE7, 0x75);
            //WhiskeyUtil.MipiWrite(0x23, 0xEA, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xEB, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xEC, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xF2, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0xF5, 0x43);

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x60, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x62, 0x20);
            //WhiskeyUtil.MipiWrite(0x23, 0x65, 0x11);

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            //WhiskeyUtil.MipiWrite(0x23, 0x11, 0x0D);
            //WhiskeyUtil.MipiWrite(0x23, 0x12, 0x0F);

            ////Iphone OPT
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            //WhiskeyUtil.MipiWrite(0x23, 0x07, 0x22);
            //WhiskeyUtil.MipiWrite(0x23, 0x61, 0x94);
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x10);
            //WhiskeyUtil.MipiWrite(0x23, 0x00, 0x0f);
            //WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0a);
            //WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0a);


            ////Iphone function
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x44);
            //WhiskeyUtil.MipiWrite(0x23, 0x08, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x09, 0x06);
            //WhiskeyUtil.MipiWrite(0x23, 0x0A, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x0B, 0x00);
            //WhiskeyUtil.MipiWrite(0x23, 0x20, 0x01);
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xB1);
            //WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

            ////IPhone MY
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x45);
            //WhiskeyUtil.MipiWrite(0x23, 0x03, 0x05);


            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            //WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);
            //WhiskeyUtil.MipiWrite(0x23, 0x04, 0x18);
            //WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);
            //WhiskeyUtil.MipiWrite(0x23, 0x06, 0x18);
            //WhiskeyUtil.MipiWrite(0x23, 0x07, 0x74);
            //WhiskeyUtil.MipiWrite(0x23, 0x08, 0x74);
            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            //WhiskeyUtil.MipiWrite(0x23, 0x00, 0x77);


            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            //WhiskeyUtil.MipiWrite(0x23, 0x62, 0x04);
            //WhiskeyUtil.MipiWrite(0x23, 0x69, 0x74);

            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            //WhiskeyUtil.MipiWrite(0x23, 0x07, 0x28);

            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            //WhiskeyUtil.MipiWrite(0x23, 0x7a, 0x02);

            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            //WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);
            //WhiskeyUtil.MipiWrite(0x23, 0x04, 0x1D);
            //WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);
            //WhiskeyUtil.MipiWrite(0x23, 0x06, 0x0D);

            //WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);

        }

        private void SSD2123_InitialCode_forAUO_nmosTypeA()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte[] RdVal = new byte[1];

            string textdata = null;
            int cnt = 0;

            textdata = "MiPi Read IC Power Status= ";






            //AUO_nmos TypeA_initial_code v1.2 from Johnny Tsai
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x14);  //[5:0]t8_de
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);  //[5:0]t7p_de
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0C);  //[7:4]t9p_de, [3:0]t9_de
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x2B);  //[5:0]t7_de

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0C);  //[5:0]SD-CKH  Setup time, refer to t9_de

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x11);  //[7:4]ckh_vbp, [3:0]ckh_vfp
            WhiskeyUtil.MipiWrite(0x23, 0x0D, 0x82);  //[7]CKH_VP_Full, [6:5]CKH2_RGB_Sel, [4]CKH_VP_REG_EN, [3]CKH_RGB_Zigazg, [2]CKH_321_Frame, [1]CKH_321_Line, [0]CKH_321

            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x41);  //[7:6]STV_A_Rise[6:5], [4:0]STV_A_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x21, 0x29);  //[5]FTI_A_Rise_mode, [4]FTI_A_Fall_mode, [3:2]Phase_STV_A, [1:0]Overlap_STV_A
            WhiskeyUtil.MipiWrite(0x23, 0x22, 0x62);  //[7:0]FTI_A_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x23, 0x62);  //[7:0]FTI_A_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x25, 0x02);  //[7:6]STV_B_Rise[6:5], [4:0]STV_B_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x26, 0x1E);  //[5]FTI_B_Rise_mode, [4]FTI_B_Fall_mode, [3:2]Phase_STV_B, [1:0]Overlap_STV_B
            WhiskeyUtil.MipiWrite(0x23, 0x27, 0x0C);  //[7:0]FTI_B_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x28, 0x0C);  //[7:0]FTI_B_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x2A, 0x02);  //[7:6]STV_C_Rise[6:5], [4:0]STV_C_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x2B, 0x19);  //[5]FTI_C_Rise_mode, [4]FTI_C_Fall_mode, [3:2]Phase_STV_C, [1:0]Overlap_STV_C
            WhiskeyUtil.MipiWrite(0x23, 0x2C, 0x0C);  //[7:0]FTI_C_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x2D, 0x0C);  //[7:0]FTI_C_Fall

            WhiskeyUtil.MipiWrite(0x23, 0x30, 0x81);  //[7]CLK_A_Rise[5], [4:0]CLK_A_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x31, 0x01);  //[7]CLK_A_Fall[5], [4:0]CLK_A_Fall[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x32, 0x11);  //[7:4]Phase_CLK_A, [3:0]Overlap_CLK_A
            WhiskeyUtil.MipiWrite(0x23, 0x33, 0x31);  //[7]CLK_A_inv, [6]CLK_A_stop_level, [5] CLK_A_ct_mode, [4] CLK_A_Keep, [1]CLW_A_Rise_mode, [0]CLW_A_Fall_mode
            WhiskeyUtil.MipiWrite(0x23, 0x34, 0x0C);  //[7:0]CLW_A1_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x35, 0x0C);  //[7:0]CLW_A2_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x36, 0x00);  //[7:0]CLW_A1_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x37, 0x00);  //[7:0]CLW_A2_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x38, 0x00);  //[7:0]CLK_A_Rise_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x39, 0x00);  //[7:0]CLK_A_Rise_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x3A, 0x00);  //[7:0]CLK_A_Fall_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x3B, 0x00);  //[7:0]CLK_A_Fall_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x3C, 0x20);  //[5]CLK_A_VBP_Keep_gs_Chg, [4]CLK_A_VFP_Keep_gs_Chg, [3:2]CLK_A_Keep_Pos2_gs_Chg, [1:0]CLK_A_Keep_Pos1_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x3D, 0x08);  //[7:6]CLK_A4_Stop_Level_gs_Chg, [5:4] CLK_A3_Stop_Level_gs_Chg, [3:2]CLK_A2_Stop_Level_gs_Chg, [1:0]CLK_A1_Stop_Level_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x3E, 0x00);  //[7]CLK_A_Keep_Pos1[5], [4:0]CLK_A_Keep_Pos1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x3F, 0x00);  //[7]CLK_A_Keep_Pos2[5], [4:0]CLK_A_Keep_Pos2[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x40, 0x00);  //[7]CLK_B_Rise[5], [4:0]CLK_B_Rise[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x41, 0x00);  //[7]CLK_B_Fall[5], [4:0]CLK_B_Fall[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x42, 0x00);  //[7:4]Phase_CLK_B, [3:0]Overlap_CLK_B
            WhiskeyUtil.MipiWrite(0x23, 0x43, 0x00);  //[7]CLK_B_inv, [6]CLK_B_stop_level, [5] CLK_B_ct_mode, [4] CLK_B_Keep, [1]CLW_B_Rise_mode, [0]CLW_B_Fall_mode
            WhiskeyUtil.MipiWrite(0x23, 0x44, 0x00);  //[7:0]CLW_B1_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x45, 0x00);  //[7:0]CLW_B2_Rise
            WhiskeyUtil.MipiWrite(0x23, 0x46, 0x00);  //[7:0]CLW_B1_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x47, 0x00);  //[7:0]CLW_B2_Fall
            WhiskeyUtil.MipiWrite(0x23, 0x48, 0x00);  //[7:0]CLK_B_Rise_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x49, 0x00);  //[7:0]CLK_B_Rise_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x4A, 0x00);  //[7:0]CLK_B_Fall_eqt1
            WhiskeyUtil.MipiWrite(0x23, 0x4B, 0x00);  //[7:0]CLK_B_Fall_eqt2
            WhiskeyUtil.MipiWrite(0x23, 0x4C, 0x00);  //[5]CLK_B_VBP_Keep_gs_Chg, [4]CLK_B_VFP_Keep_gs_Chg, [3:2]CLK_B_Keep_Pos2_gs_Chg, [1:0]CLK_B_Keep_Pos1_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x4D, 0x00);  //[7:6]CLK_B4_Stop_Level_gs_Chg, [5:4] CLK_B3_Stop_Level_gs_Chg, [3:2]CLK_B2_Stop_Level_gs_Chg, [1:0]CLK_B1_Stop_Level_gs_Chg
            WhiskeyUtil.MipiWrite(0x23, 0x4E, 0x00);  //[7]CLK_B_Keep_Pos1[5], [4:0]CLK_B_Keep_Pos1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x4F, 0x00);  //[7]CLK_B_Keep_Pos2[5], [4:0]CLK_B_Keep_Pos2[4:0]

            WhiskeyUtil.MipiWrite(0x23, 0x70, 0x06);  //GOUT_R_01_FW
            WhiskeyUtil.MipiWrite(0x23, 0x71, 0x37);  //GOUT_R_02_FW
            WhiskeyUtil.MipiWrite(0x23, 0x72, 0x36);  //GOUT_R_03_FW
            WhiskeyUtil.MipiWrite(0x23, 0x73, 0x10);  //GOUT_R_04_FW
            WhiskeyUtil.MipiWrite(0x23, 0x74, 0x11);  //GOUT_R_05_FW
            WhiskeyUtil.MipiWrite(0x23, 0x75, 0x0A);  //GOUT_R_06_FW
            WhiskeyUtil.MipiWrite(0x23, 0x76, 0x2A);  //GOUT_R_07_FW
            WhiskeyUtil.MipiWrite(0x23, 0x77, 0x2A);  //GOUT_R_08_FW
            WhiskeyUtil.MipiWrite(0x23, 0x78, 0x0E);  //GOUT_R_09_FW
            WhiskeyUtil.MipiWrite(0x23, 0x79, 0x00);  //GOUT_R_10_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7A, 0x00);  //GOUT_R_11_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7B, 0x00);  //GOUT_R_12_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7C, 0x00);  //GOUT_R_13_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7D, 0x30);  //GOUT_R_14_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7E, 0x31);  //GOUT_R_15_FW
            WhiskeyUtil.MipiWrite(0x23, 0x7F, 0x32);  //GOUT_R_16_FW
            WhiskeyUtil.MipiWrite(0x23, 0x80, 0x06);  //GOUT_L_01_FW
            WhiskeyUtil.MipiWrite(0x23, 0x81, 0x37);  //GOUT_L_02_FW
            WhiskeyUtil.MipiWrite(0x23, 0x82, 0x36);  //GOUT_L_03_FW
            WhiskeyUtil.MipiWrite(0x23, 0x83, 0x10);  //GOUT_L_04_FW
            WhiskeyUtil.MipiWrite(0x23, 0x84, 0x11);  //GOUT_L_05_FW
            WhiskeyUtil.MipiWrite(0x23, 0x85, 0x0A);  //GOUT_L_06_FW
            WhiskeyUtil.MipiWrite(0x23, 0x86, 0x2A);  //GOUT_L_07_FW
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x2A);  //GOUT_L_08_FW
            WhiskeyUtil.MipiWrite(0x23, 0x88, 0x0E);  //GOUT_L_09_FW
            WhiskeyUtil.MipiWrite(0x23, 0x89, 0x00);  //GOUT_L_10_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8A, 0x00);  //GOUT_L_11_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8B, 0x00);  //GOUT_L_12_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8C, 0x00);  //GOUT_L_13_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8D, 0x30);  //GOUT_L_14_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8E, 0x31);  //GOUT_L_15_FW
            WhiskeyUtil.MipiWrite(0x23, 0x8F, 0x32);  //GOUT_L_16_FW
            WhiskeyUtil.MipiWrite(0x23, 0x90, 0x0E);  //GOUT_R_01_BW
            WhiskeyUtil.MipiWrite(0x23, 0x91, 0x37);  //GOUT_R_02_BW
            WhiskeyUtil.MipiWrite(0x23, 0x92, 0x36);  //GOUT_R_03_BW
            WhiskeyUtil.MipiWrite(0x23, 0x93, 0x11);  //GOUT_R_04_BW
            WhiskeyUtil.MipiWrite(0x23, 0x94, 0x10);  //GOUT_R_05_BW
            WhiskeyUtil.MipiWrite(0x23, 0x95, 0x0A);  //GOUT_R_06_BW
            WhiskeyUtil.MipiWrite(0x23, 0x96, 0x2A);  //GOUT_R_07_BW
            WhiskeyUtil.MipiWrite(0x23, 0x97, 0x2A);  //GOUT_R_08_BW
            WhiskeyUtil.MipiWrite(0x23, 0x98, 0x06);  //GOUT_R_09_BW
            WhiskeyUtil.MipiWrite(0x23, 0x99, 0x00);  //GOUT_R_10_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9A, 0x00);  //GOUT_R_11_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9B, 0x00);  //GOUT_R_12_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9C, 0x00);  //GOUT_R_13_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9D, 0x30);  //GOUT_R_14_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9E, 0x31);  //GOUT_R_15_BW
            WhiskeyUtil.MipiWrite(0x23, 0x9F, 0x32);  //GOUT_R_16_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA0, 0x0E);  //GOUT_L_01_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA1, 0x37);  //GOUT_L_02_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA2, 0x36);  //GOUT_L_03_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA3, 0x11);  //GOUT_L_04_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA4, 0x10);  //GOUT_L_05_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA5, 0x0A);  //GOUT_L_06_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA6, 0x2A);  //GOUT_L_07_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA7, 0x2A);  //GOUT_L_08_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA8, 0x06);  //GOUT_L_09_BW
            WhiskeyUtil.MipiWrite(0x23, 0xA9, 0x00);  //GOUT_L_10_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAA, 0x00);  //GOUT_L_11_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAB, 0x00);  //GOUT_L_12_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAC, 0x00);  //GOUT_L_13_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAD, 0x30);  //GOUT_L_14_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAE, 0x31);  //GOUT_L_15_BW
            WhiskeyUtil.MipiWrite(0x23, 0xAF, 0x32);  //GOUT_L_16_BW

            WhiskeyUtil.MipiWrite(0x23, 0xC7, 0x22);  //[7:4]Blank_Frame_OPT1[3:0], [3:0]Blank_Frame_OPT2[3:0]
            WhiskeyUtil.MipiWrite(0x23, 0xC8, 0x57);  //[7:6]SRC_Front_Blank_Sel, [5:4]SRC_Mid_Blank_Sel, [3:2]SRC_Back_Blank_Sel
            WhiskeyUtil.MipiWrite(0x23, 0xCB, 0x00);  //[5:4]GOUT_LVD, [3:2]GOUT_SO, [1:0]GOUT_SI
            WhiskeyUtil.MipiWrite(0x23, 0xD0, 0x11);  //[5:4]ONSeq_Ext, [2:0] OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD2, 0x79);  //[7:6]CLK_B_ONSeq_Ext, [5]CLK_A_ONSeq_Ext, [4] STV_C_ONSeq_Ext, [3:2]STV_B_ONSeq_Ext, STV_A_ONSeq_Ext, 00:ori, 01:VGL, 10:VGH, 11:GND
            WhiskeyUtil.MipiWrite(0x23, 0xD3, 0x19);  //[5:4]CKH_ONSeq_Ext, [3]CLK_D_ONSeq_Ext, [1:0]CLK_C_ONSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD4, 0x10);  //[6]RESET_ONSeq_Ext, [4]CLK_E_ONSeq_Ext, [2]GAS_ONSeq_Ext, [1:0]FWBW_ONSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD6, 0x00);  //[7:6]CLK_A_OFFSeq_Ext, [5:4]STV_B_OFFSeq_Ext, [3:2]STV_B_OFFSeq_Ext, [1:0]STV_A_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD7, 0x00);  //[7:6]CKH_OFFSeq_Ext, [5:4]CLK_D_OFFSeq_Ext, [3:2]CLK_C_OFFSeq_Ext, [1:0]CLK_B_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xD8, 0x00);  //[7:6]CLK_E_OFFSeq_Ext, [4]Reset_OFFSet_Ext, [2]GAS_OFFSeq_Ext, [1:0]FWBW_OFFSeq_Ext
            WhiskeyUtil.MipiWrite(0x23, 0xDA, 0xFF);  //[7]CKH_AbnSeq, [6]CLK_D_AbnSeq, [5]CLK_C_AbnSeq, [4]CLK_B_AbnSeq, [3]CLK_A_AbnSeq, [2]STV_C_AbnSeq, [1]STV_B_AbnSeq, [0]STV_A_AbnSeq, 0:VGL, 1:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xDB, 0x1A);  //[4]CLK_E_AbnSeq, [3]Reset_AbnSeq, [2]GAS_AbnSeq, [1:0]FWBW_AbnSeq, 00:norm, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE0, 0x54);  //[7:6]STV_A_ONSeq, [5:4]STV_B_ONSeq, [3:2]STV_C_ONSeq, 00:ori, 01:VGL, 10:VGH, 11:GND
            WhiskeyUtil.MipiWrite(0x23, 0xE1, 0x15);  //[6:4]CLK_A_ONSeq, [4:3]CLK_B_ONSeq, [1:0]CLK_C_ONSeq, 00:ori, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE2, 0x19);  //[7:6]CLK_D_ONSeq, [5:4]CLK_E_ONSeq, [3:2]CKH_ONSeq, [1:0]FWBW_ONSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE3, 0x00);  //[3:2]GAS_ONSeq, [1:0]Reset_ONSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE4, 0x00);  //[7:6]STV_A_OFFSeq, [5:4]STV_B_OFFSeq, [3:2]STV_C_OFFSeq, [1:0]CLK_A_OFFSeq, 00:ori, 01:VGL, 10:VGH
            WhiskeyUtil.MipiWrite(0x23, 0xE5, 0x00);  //[7:6]CLK_B_OFFSeq, [5:4]CLK_C_OFFSeq, [3:2]CLK_D_OFFSeq, [1:0]CLK_E_OFFSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE6, 0x10);  //[7:6]CKH_OFFSeq, [5:4]FWBW_OFFSeq, [3:2]Reset_OFFSet, [1:0]GAS_OFFSeq
            WhiskeyUtil.MipiWrite(0x23, 0xE7, 0x75);  //[6]SRC_ONSeq_OPT, [5:4]VCM_ONSeq_OPT, [2]SRC_OFFSeq_OPT, [1:0]VCM_OFFSeq_OPT
            WhiskeyUtil.MipiWrite(0x23, 0xEA, 0x00);  //[7:4]STV_Onoff_Seq_dly, [3:0]VCK_A_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xEB, 0x00);  //[7:4]VCK_B_Onoff_Seq_dly, [3:0]VCK_C_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xEC, 0x00);  //[7:4]CKH_Onoff_Seq_dly, [3:0]GAS_Onoff_Seq_dly
            WhiskeyUtil.MipiWrite(0x23, 0xF2, 0x00);  //[7]GS_Sync_2frm_opt
            WhiskeyUtil.MipiWrite(0x23, 0xF5, 0x43);  //[7:6]RST_Each_Frame, [5]GIP_RST_INV, [4]PWRON_RST_OPT, [3:0]GRST_WID_ONSeq_EXT[11:8]

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x60, 0x00);  // Panel Scheme Selection
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x20);  // Column Inversion
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x11);  //[6:4]VFP_CKH_DUM, [2:0]VBP_CKH_DUM

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x11, 0x0D); //[7]VGH_ratio, [5:0]VGHS, default:12v; AUO:8.5v; CSOT:9v  change,0x8D -->0x 0D to 2x
            WhiskeyUtil.MipiWrite(0x23, 0x12, 0x0F);  //[7]VGL_ratio, [5:0]VGLS, default:-8v; AUO:-8v; CSOT:-7v

            //Iphone OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x22);                         //
            WhiskeyUtil.MipiWrite(0x23, 0x61, 0x92);                         //OSC frequency, default 90.43Mhz; For iphone+ MIPI 1.2Ghz case, OSC= 95.78Mhz
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x10);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x0f);                         //[5:0]t8_de
            WhiskeyUtil.MipiWrite(0x23, 0x01, 0x00);                         //[5:0]t7p_de
            WhiskeyUtil.MipiWrite(0x23, 0x02, 0x0a);                         //[7:4]t9p_de, [3:0]t9_de
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x22);                         //[5:0]t7_de
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x54, 0x0a);                         //[5:0]SD-CKH  Setup time, refer to t9_de

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Iphone function
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x44);
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x00);                         //FTE1_SEL
            WhiskeyUtil.MipiWrite(0x23, 0x09, 0x06);                         //FTE_SEL = HIFA
            WhiskeyUtil.MipiWrite(0x23, 0x0A, 0x00);                         //VSOUT_SEL
            WhiskeyUtil.MipiWrite(0x23, 0x0B, 0x00);                         //HSOUT_SEL
            WhiskeyUtil.MipiWrite(0x23, 0x20, 0x01);                         //SDO_SEL = MSYNC
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xB1);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);                         //IPhone Func. Enable

            //IPhone MY
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x45);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x05);

            //VCOM , GVDDP/N ,AVDDREF/AVEEREF
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x01);                         //VCOM 1
            WhiskeyUtil.MipiWrite(0x23, 0x04, 0x18);                         //VCOM 1 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x01);                         //VCOM 2
            WhiskeyUtil.MipiWrite(0x23, 0x06, 0x18);                         //VCOM 2 =-0.25V
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x74);                         //GVDDP = 5V
            WhiskeyUtil.MipiWrite(0x23, 0x08, 0x74);                         //GVDDN = -5V
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x77);                         //AVDDREF/AVEEREF=+5V/-5V

            //CKH mapping XSWR XSWG XSWB
            //WhiskeyUtil.MipiWrite(0x29,0xff,0x21,0x30,0x10);
            //WhiskeyUtil.MipiWrite(0x23,0x7A,0x33); 
            //WhiskeyUtil.MipiWrite(0x23,0x7B,0x34); 
            //WhiskeyUtil.MipiWrite(0x23,0x7C,0x35); 
            //WhiskeyUtil.MipiWrite(0x23,0x8A,0x33); 
            //WhiskeyUtil.MipiWrite(0x23,0x8B,0x34); 
            //WhiskeyUtil.MipiWrite(0x23,0x8C,0x35); 
            //WhiskeyUtil.MipiWrite(0x23,0x9A,0x33); 
            //WhiskeyUtil.MipiWrite(0x23,0x9B,0x34); 
            //WhiskeyUtil.MipiWrite(0x23,0x9C,0x35); 
            //WhiskeyUtil.MipiWrite(0x23,0xAA,0x33); 
            //WhiskeyUtil.MipiWrite(0x23,0xAB,0x34); 
            //WhiskeyUtil.MipiWrite(0x23,0xAC,0x35); 

            //MIPI OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x16);                         //di_mipi_sel_clk[4:0] skew
            WhiskeyUtil.MipiWrite(0x23, 0x63, 0x00);                         //di_mipi_sel_D0[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x64, 0x18);                         //di_mipi_sel_D1[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x65, 0x18);                         //di_mipi_sel_D2[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x66, 0x18);                         //di_mipi_sel_D3[4:0]
            WhiskeyUtil.MipiWrite(0x23, 0x69, 0x74);                         //di_mipi_swihsrx2[2:0]  RX bias
            WhiskeyUtil.MipiWrite(0x23, 0x87, 0x04);                         //d2a_mipi_gb_sw[2:0]  

            //MIPI CD disable
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x40);
            WhiskeyUtil.MipiWrite(0x23, 0x6b, 0xfe);

            //VDD OPT
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x07, 0x28);                         //VDD REG slew rate 28 [1:0]BGIR

            //OSC bias always on
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x44);
            WhiskeyUtil.MipiWrite(0x23, 0x03, 0x03);

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Tuning test code

            //OSC trim target=90.4M
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x12);
            WhiskeyUtil.MipiWrite(0x23, 0x62, 0x8B);                         //OSC trim code

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x11);
            WhiskeyUtil.MipiWrite(0x23, 0x7a, 0x02);                         //0 frame 2 line  source chop                     //source mc

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        }




        /*
         * WriteGammaPartialSetting_to_SSD2130 說明
         * 功能:使用者能針對自己想設定的Gamma進行改變 並且寫入暫存器所有Page31 R+   Page32 R-   Page33 G+   Page34 G-   Page35 B+   Page36 B-
         * uint TieValue:0~29個綁點 看要想改變哪個
         * uint GammaWantSetting:想改變的綁點 想填入多少的Gamma設定值
         * uint[] GammaNowSetting:目前Gamma全部0~28的設定值為多少
         * 請注意! SSD2130 寫入Gamma設定值 應該是0~28個綁點所有設定都寫入後 IC會判斷最後一個暫存器寫入值後才會Trigger一次整個Page套用新值
        */
        private void WriteGammaPartialSetting_to_SSD2130(uint TieValue, uint GammaWantSetting, uint[] GammaNowSetting)
        {
            byte TieCnt = 0;
            byte page = 0x00;
            uint temp = 0;
            byte RegisterSetting = 0x00;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            //切換SSD2130 PassWord & page
            //Page31 R+   Page32 R-   Page33 G+   Page34 G-   Page35 B+   Page36 B-
            for (page = 31; page <= 36; page++)
            {
                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, page);


                for (TieCnt = 0; TieCnt < 29; TieCnt++)
                {
                    uint temp2 = TieCnt;
                    byte addr = 0;

                    if (TieCnt == TieValue)
                    { addr = 0; }


                    temp2 = temp2 * 2;
                    addr = Convert.ToByte(temp2);

                    if (TieValue == TieCnt)
                    { temp = GammaWantSetting; }
                    else
                    { temp = GammaNowSetting[TieCnt]; }
                    temp >>= 8;
                    temp = temp & 0x03;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);



                    temp2 = TieCnt;
                    temp2 = (temp2 * 2) + 1;
                    addr = Convert.ToByte(temp2);

                    if (TieValue == TieCnt)
                    { temp = GammaWantSetting; }
                    else
                    { temp = GammaNowSetting[TieCnt]; }
                    temp = temp & 0xFF;
                    RegisterSetting = Convert.ToByte(temp);
                    WhiskeyUtil.MipiWrite(0x23, addr, RegisterSetting);
                }
            }
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }


        private void button4_Click(object sender, EventArgs e)
        {
            uint[] gammasetting = new uint[29];
            double[] All_Brightness_save = new double[1024];
            int dive = GMA_Set_comboBox.SelectedIndex + 1;

            string textdata = null;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            textdata = "Gamma All Brighness Sacn!!\r\n";
            Info_textBox.AppendText(textdata);


            WhiskeyUtil.ImageFill(127, 0, 0);
            Thread.Sleep(500);

            Application.DoEvents();

            for (uint scal = 0; scal < 1024; scal = scal+2)
            {
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = scal;
                }

                WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);
                    //Page31 R+   Page32 R-   Page33 G+   Page34 G-   Page35 B+   Page36 B-


                WhiskeyUtil.ImageFill(127, 0, 0);
                Thread.Sleep(300);
                All_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(dive), 4);



                byte[] RdVal_page = new byte[3];

                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31); //Page31 R+ 僅讀回一筆及代表所有Gamma




                uint RegisterRead = 0x00;
                if (true)
                {

                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;

                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
                }

                textdata = "RegGet =" + Convert.ToString(RegisterRead) + " Gamma:" + Convert.ToString(scal) + "= ";
                textdata = textdata + Convert.ToString(All_Brightness_save[scal]) + "\r\n";


                Info_textBox.AppendText(textdata);
            }
        }

        private void display_off()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x05, 0x28); Thread.Sleep(100);
        }

        private void display_on()
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x05, 0x29); Thread.Sleep(100);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.button6.ForeColor = Color.Green;
            Application.DoEvents();
            string textdata = null;

            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            uint[] gammasetting = new uint[29];
            textdata = "256 Gray Brightness Get\r\n";
            Info_textBox.AppendText(textdata);

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte tie_gray = 0x00;
            chart1.Series[3].Points.Clear();

            //gammasetting[0] = 0;
            //gammasetting[1] = 48;
            //gammasetting[2] = 110;
            //gammasetting[3] = 153;
            //gammasetting[4] = 185;
            //gammasetting[5] = 211;
            //gammasetting[6] = 235;
            //gammasetting[7] = 254;
            //gammasetting[8] = 270;
            //gammasetting[9] = 329;
            //gammasetting[10] = 369;
            //gammasetting[11] = 425;
            //gammasetting[12] = 466;
            //gammasetting[13] = 525;
            //gammasetting[14] = 571;
            //gammasetting[15] = 615;
            //gammasetting[16] = 669;
            //gammasetting[17] = 707;
            //gammasetting[18] = 758;
            //gammasetting[19] = 793;
            //gammasetting[20] = 838;
            //gammasetting[21] = 852;
            //gammasetting[22] = 866;
            //gammasetting[23] = 882;
            //gammasetting[24] = 902;
            //gammasetting[25] = 925;
            //gammasetting[26] = 962;
            //gammasetting[27] = 997;
            //gammasetting[28] = 1013;
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);


            //gammasetting[0] = 0;
            //gammasetting[1] = 55;
            //gammasetting[2] = 125;
            //gammasetting[3] = 169;
            //gammasetting[4] = 200;
            //gammasetting[5] = 228;
            //gammasetting[6] = 250;
            //gammasetting[7] = 270;
            //gammasetting[8] = 286;
            //gammasetting[9] = 342;
            //gammasetting[10] = 381;
            //gammasetting[11] = 436;
            //gammasetting[12] = 476;
            //gammasetting[13] = 536;
            //gammasetting[14] = 581;
            //gammasetting[15] = 621;
            //gammasetting[16] = 677;
            //gammasetting[17] = 710;
            //gammasetting[18] = 763;
            //gammasetting[19] = 797;
            //gammasetting[20] = 841;
            //gammasetting[21] = 854;
            //gammasetting[22] = 868;
            //gammasetting[23] = 886;
            //gammasetting[24] = 904;
            //gammasetting[25] = 929;
            //gammasetting[26] = 956;
            //gammasetting[27] = 990;
            //gammasetting[28] = 1012;
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);

            ////WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
            //gammasetting[0] = 1;
            //gammasetting[1] = 73;
            //gammasetting[2] = 138;
            //gammasetting[3] = 182;
            //gammasetting[4] = 210;
            //gammasetting[5] = 236;
            //gammasetting[6] = 256;
            //gammasetting[7] = 275;
            //gammasetting[8] = 290;
            //gammasetting[9] = 345;
            //gammasetting[10] = 381;
            //gammasetting[11] = 433;
            //gammasetting[12] = 472;
            //gammasetting[13] = 528;
            //gammasetting[14] = 573;
            //gammasetting[15] = 615;
            //gammasetting[16] = 668;
            //gammasetting[17] = 706;
            //gammasetting[18] = 757;
            //gammasetting[19] = 792;
            //gammasetting[20] = 837;
            //gammasetting[21] = 849;
            //gammasetting[22] = 864;
            //gammasetting[23] = 880;
            //gammasetting[24] = 898;
            //gammasetting[25] = 922;
            //gammasetting[26] = 958;
            //gammasetting[27] = 969;
            //gammasetting[28] = 1016;
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
            //WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);



            //依序點綁點處的灰階 並且用K80量測
            for (int gary = 0; gary < 256; gary++) //tir=0 時亮度最亮
            {
                //tie_gray = Convert.ToByte(255 - gary);
                tie_gray = Convert.ToByte(gary);

                WhiskeyUtil.ImageFill(tie_gray, tie_gray, tie_gray);
                Thread.Sleep(300);

                Actual_Brightness[gary] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(gary, Actual_Brightness[gary]);

                textdata = "Gray=" + Convert.ToString(gary) + "=" + Convert.ToString(Actual_Brightness[gary]) + "\r\n";
                Info_textBox.AppendText(textdata);

            }

            for (int gary = 0; gary < 256; gary++) //tir=0 時亮度最亮
            {
                //tie_gray = Convert.ToByte(255 - gary);
                tie_gray = Convert.ToByte(gary);

                WhiskeyUtil.ImageFill(tie_gray, 0, 0);
                Thread.Sleep(300);

                Actual_Brightness[gary] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(gary, Actual_Brightness[gary]);

                textdata = "R=" + Convert.ToString(gary) + "=" + Convert.ToString(Actual_Brightness[gary]) + "\r\n";
                Info_textBox.AppendText(textdata);

            }

            for (int gary = 0; gary < 256; gary++) //tir=0 時亮度最亮
            {
                //tie_gray = Convert.ToByte(255 - gary);
                tie_gray = Convert.ToByte(gary);

                WhiskeyUtil.ImageFill(0, tie_gray, 0);
                Thread.Sleep(300);

                Actual_Brightness[gary] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(gary, Actual_Brightness[gary]);

                textdata = "G=" + Convert.ToString(gary) + "=" + Convert.ToString(Actual_Brightness[gary]) + "\r\n";
                Info_textBox.AppendText(textdata);

            }

            for (int gary = 0; gary < 256; gary++) //tir=0 時亮度最亮
            {
                //tie_gray = Convert.ToByte(255 - gary);
                tie_gray = Convert.ToByte(gary);

                WhiskeyUtil.ImageFill(0, 0, tie_gray);
                Thread.Sleep(300);

                Actual_Brightness[gary] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(gary, Actual_Brightness[gary]);

                textdata = "B=" + Convert.ToString(gary) + "=" + Convert.ToString(Actual_Brightness[gary]) + "\r\n";
                Info_textBox.AppendText(textdata);

            }
            this.button6.ForeColor = Color.Black;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            this.button7.ForeColor = Color.Green;
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            //WhiskeyUtil.ImageShow("VG.bmp");
            WhiskeyUtil.ImageShow("VG.bmp");
            
            this.button4.ForeColor = Color.Black;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            byte[] receiver_cmp = new byte[38];
            byte[] receiver = new byte[38];
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            for (int time = 0; time < 5000; time++)
            {
                WhiskeyUtil.MipiWrite(0x23, 0x00, 0x00);
                WhiskeyUtil.MipiRead(0xE1, 38, ref receiver);

                receiver_cmp[0] = 0;
                receiver_cmp[1] = 115;
                receiver_cmp[2] = 207;
                receiver_cmp[3] = 16;
                receiver_cmp[4] = 64;
                receiver_cmp[5] = 53;
                receiver_cmp[6] = 91;
                receiver_cmp[7] = 156;
                receiver_cmp[8] = 204;
                receiver_cmp[9] = 85;
                receiver_cmp[10] = 184;
                receiver_cmp[11] = 229;
                receiver_cmp[12] = 19;
                receiver_cmp[13] = 53;
                receiver_cmp[14] = 165;
                receiver_cmp[15] = 96;
                receiver_cmp[16] = 130;
                receiver_cmp[17] = 244;
                receiver_cmp[18] = 25;
                receiver_cmp[19] = 154;
                receiver_cmp[20] = 56;
                receiver_cmp[21] = 89;
                receiver_cmp[22] = 120;
                receiver_cmp[23] = 152;
                receiver_cmp[24] = 170;
                receiver_cmp[25] = 192;
                receiver_cmp[26] = 193;
                receiver_cmp[27] = 243;
                receiver_cmp[28] = 45;
                receiver_cmp[29] = 234;
                receiver_cmp[30] = 78;
                receiver_cmp[31] = 116;
                receiver_cmp[32] = 175;
                receiver_cmp[33] = 232;
                receiver_cmp[34] = 255;
                receiver_cmp[35] = 255;
                receiver_cmp[36] = 3;
                receiver_cmp[37] = 3;

                for (int i = 0; i < 38; i++)
                {
                    if (receiver_cmp[i] != receiver[i])
                    {
                        Info_textBox.Text = "READ ERROR !!!";

                    }
                }
                Info_textBox.Text = "測試次數=" + Convert.ToString(time);
                Application.DoEvents();
            }
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xff); //GPIO RESET
            WHISKY_FPGA_InitialSetting(ref WhiskeyUtil);//Include FPGA initial、2828 initial、DSV initial and Driver reset setting 
            button3.Enabled = true;

            LoadInitialCode();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            byte[] RdVal = new byte[1];

            string textdata = null;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            //WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            WhiskeyUtil.MipiWrite(0x05, 0x11);//Sleep-Out

        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xff); //GPIO RESET
            Thread.Sleep(20);
            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xfe);
            Thread.Sleep(5);
            WhiskeyUtil.GpioCtrl(0x11, 0xff, 0xff);
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.ImageFill(100, 100, 100);
            Thread.Sleep(1000);
        }

        private void button5_Click_2(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            WhiskeyUtil.SetMipiVideo(1920, 1080, 60, 16, 16, 30, 30, 4, 4);

            WhiskeyUtil.SetMipiDsi(4, 700, "syncpulse");
            //WhiskeyUtil.SetMipiDsi(4, 700, "burst");
            uint data = 0;
            SL_Comm_Base.SPI_ReadReg(0xbb, ref data, 2);

            DSV_Setting(ref WhiskeyUtil);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(100);
            WhiskeyUtil.MipiWrite(0x05, 0x29);//Display-On
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(100);
            WhiskeyUtil.MipiWrite(0x05, 0x11);//Sleep Out
        }

        private void button5_Click_3(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(100);
            WhiskeyUtil.MipiWrite(0x05, 0x10);//Sleep In
        }

        private void button11_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(100);
            WhiskeyUtil.MipiWrite(0x05, 0x28);//Display-OFF
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Info_textBox.Text = "";
        }

        private void button13_Click(object sender, EventArgs e)
        {
            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            string textdata = null;
            int cnt = 0;


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte tie_gray = 0x00;

            chart1.Series[3].Points.Clear();

            textdata = "R \r\n";
            Info_textBox.AppendText(textdata);
            
            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(tie_gray, 0, 0);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP=" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            textdata = "G \r\n";
            Info_textBox.AppendText(textdata);

            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(0, tie_gray, 0);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP=" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            textdata = "B \r\n";
            Info_textBox.AppendText(textdata);

            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(0, 0, tie_gray);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP=" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            textdata = "Gary \r\n";
            Info_textBox.AppendText(textdata);

            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(tie_gray, tie_gray, tie_gray);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP=" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }


        }


        private void VCOM_Increase_BUT_Click_1(object sender, EventArgs e)
        {
            uint RegisterWrite = 0x00;
            uint RegisterRead = 0x00;
            byte Cmd = 0;
            byte[] RdVal = new byte[1];

            VCOM_Increase_BUT.Enabled = false;
            VCOM_Decrease_BUT.Enabled = false;

            Thread.Sleep(100);
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            VCOM_Setting++;
            if (VCOM_Setting >= 455)
            { VCOM_Setting = 455; }

            RegisterWrite = VCOM_Setting;
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            RegisterWrite >>= 8;
            Cmd = Convert.ToByte(RegisterWrite);
            WhiskeyUtil.MipiWrite(0x23, 0x03, Cmd);
            WhiskeyUtil.MipiWrite(0x23, 0x05, Cmd);


            RegisterWrite = VCOM_Setting;
            RegisterWrite = RegisterWrite & 0x00FF;
            Cmd = Convert.ToByte(RegisterWrite);
            WhiskeyUtil.MipiWrite(0x23, 0x04, Cmd);
            WhiskeyUtil.MipiWrite(0x23, 0x06, Cmd);


            WhiskeyUtil.MipiRead(0x03, 1, ref RdVal);
            RegisterRead = RdVal[0];
            RegisterRead <<= 8;

            WhiskeyUtil.MipiRead(0x04, 1, ref RdVal);
            RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
            VCOM_Setting = RegisterRead;

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(300);
            WhiskeyUtil.ImageShow("Column_inv_187_1080x1920.bmp");

            VCOM_Increase_BUT.Enabled = true;
            VCOM_Decrease_BUT.Enabled = true;

            Show_VCom_textBox.Text = Convert.ToString(VCOM_Setting, 16);
            VCom_SettingDoneCheck_BUT.Enabled = true;
        }


        private void VCOM_Decrease_BUT_Click_1(object sender, EventArgs e)
        {
            uint RegisterWrite = 0x00;
            uint RegisterRead = 0x00;
            byte Cmd = 0;
            byte[] RdVal = new byte[1];

            VCOM_Increase_BUT.Enabled = false;
            VCOM_Decrease_BUT.Enabled = false;

            Thread.Sleep(100);
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            VCOM_Setting--;
            if (VCOM_Setting <= 0)
            { VCOM_Setting = 0; }

            RegisterWrite = VCOM_Setting;
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);
            RegisterWrite >>= 8;
            Cmd = Convert.ToByte(RegisterWrite);
            WhiskeyUtil.MipiWrite(0x23, 0x03, Cmd);
            WhiskeyUtil.MipiWrite(0x23, 0x05, Cmd);


            RegisterWrite = VCOM_Setting;
            RegisterWrite = RegisterWrite & 0x00FF;
            Cmd = Convert.ToByte(RegisterWrite);
            WhiskeyUtil.MipiWrite(0x23, 0x04, Cmd);
            WhiskeyUtil.MipiWrite(0x23, 0x06, Cmd);


            WhiskeyUtil.MipiRead(0x03, 1, ref RdVal);
            RegisterRead = RdVal[0];
            RegisterRead <<= 8;

            WhiskeyUtil.MipiRead(0x04, 1, ref RdVal);
            RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
            VCOM_Setting = RegisterRead;

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
            Thread.Sleep(300);
            WhiskeyUtil.ImageShow("Column_inv_187_1080x1920.bmp");

            VCOM_Increase_BUT.Enabled = true;
            VCOM_Decrease_BUT.Enabled = true;

            Show_VCom_textBox.Text = Convert.ToString(VCOM_Setting, 16);

            VCom_SettingDoneCheck_BUT.Enabled = true;
        }




        private void TransformRegisterSettingtoScript(uint[] gammasetting)
        {
            uint TieCnt = 0;
            uint temp = 0;
            string textdata = null;

            textdata = "Gamma Register Get\r\n";
            Info_textBox.AppendText(textdata);

            for (TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                uint temp2 = TieCnt;
                byte addr = 0;

                temp2 = temp2 * 2;
                addr = Convert.ToByte(temp2);

                temp = gammasetting[TieCnt];
                temp >>= 8;
                temp = temp & 0x03;
                textdata = "Addr:" + Convert.ToString(addr) + "=" + Convert.ToString(temp) + "\r\n";

                Info_textBox.AppendText(textdata);




                temp2 = TieCnt;
                temp2 = (temp2 * 2) + 1;
                addr = Convert.ToByte(temp2);

                temp = gammasetting[TieCnt];
                temp = temp & 0xFF;
                textdata = "Addr:" + Convert.ToString(addr) + "=" + Convert.ToString(temp) + "\r\n";
                Info_textBox.AppendText(textdata);
            }
        }

        private void button2_Click_3(object sender, EventArgs e)
        {
            TransformRegisterSettingtoScript(TieRegisterSetting);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            WhiskeyUtil.ImageShow("HG.bmp");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            WhiskeyUtil.ImageShow("HG2.bmp");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            WhiskeyUtil.ImageShow("VG2.bmp");
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            uint tie_cnt = 0;

            for (tie_cnt = 0; tie_cnt < 29; tie_cnt++)
            {
                TieRegisterSetting[0] = 0;
                TieRegisterSetting[1] = 4;
                TieRegisterSetting[2] = 12;
                TieRegisterSetting[3] = 20;
                TieRegisterSetting[4] = 28;
                TieRegisterSetting[5] = 36;
                TieRegisterSetting[6] = 44;
                TieRegisterSetting[7] = 52;
                TieRegisterSetting[8] = 60;
                TieRegisterSetting[9] = 96;
                TieRegisterSetting[10] = 128;
                TieRegisterSetting[11] = 193;
                TieRegisterSetting[12] = 257;
                TieRegisterSetting[13] = 385;
                TieRegisterSetting[14] = 514;
                TieRegisterSetting[15] = 642;
                TieRegisterSetting[16] = 770;
                TieRegisterSetting[17] = 834;
                TieRegisterSetting[18] = 899;
                TieRegisterSetting[19] = 931;
                TieRegisterSetting[20] = 963;
                TieRegisterSetting[21] = 971;
                TieRegisterSetting[22] = 979;
                TieRegisterSetting[23] = 987;
                TieRegisterSetting[24] = 995;
                TieRegisterSetting[25] = 1003;
                TieRegisterSetting[26] = 1011;
                TieRegisterSetting[27] = 1019;
                TieRegisterSetting[28] = 1023;
            }

            WriteGammaSettingAlltheSame_to_SSD2130(TieRegisterSetting);


            for (tie_cnt = 0; tie_cnt < 29; tie_cnt++)
            {
                TieRegisterSetting[tie_cnt] = 0;
            }
            ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);
            Tie_ParameterSetting_to_LoadVP_TextData(TieRegisterSetting);
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            uint tie_gamma_setting = 0;
            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            double[] Actual_DigGamma_Brightness = new double[1025];
            string textdata = null;


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();






            for (tie_gamma_setting = 800; tie_gamma_setting < 1024; tie_gamma_setting= tie_gamma_setting+1)
            {
                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x28);
                WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

                //conveter_for_dgm_test_use(tie_gamma_setting);
                conveter_for_dgm_test_use_andSetWhatColorUse(tie_gamma_setting, "B");

                WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);
                WhiskeyUtil.MipiWrite(0x05, 0x11);//Sleep-Out
                Thread.Sleep(100);
                WhiskeyUtil.MipiWrite(0x05, 0x29);//Display-On

                WhiskeyUtil.ImageFill(0, 0, 191);
                Thread.Sleep(100);
                WhiskeyUtil.ImageFill(0, 0, 191);
                Thread.Sleep(300);

                Actual_DigGamma_Brightness[tie_gamma_setting] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                textdata = "DigGammaSet:" + Convert.ToString(tie_gamma_setting) + "  Brightness=" + Convert.ToString(Actual_DigGamma_Brightness[tie_gamma_setting]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }



        }



        private void conveter_for_dgm_test_use_andSetWhatColorUse(uint dgm_setting, string color)
        {
            byte[] Reg_setting = new byte[5];
            uint temp = 0;
            temp = dgm_setting;
            temp >>= 2;


            temp = temp & 0x00FF;

            byte[] testuse = new byte[60];
            byte[] RdVal = new byte[1];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            Reg_setting[0] = Convert.ToByte(temp);
            Reg_setting[1] = Convert.ToByte(temp);
            Reg_setting[2] = Convert.ToByte(temp);
            Reg_setting[3] = Convert.ToByte(temp);


            dgm_setting = dgm_setting & 0x03;
            temp = dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            Reg_setting[4] = Convert.ToByte(temp);

            if (color == "R")
            {
                for (byte addr = 0x10; addr <= 0x32; addr++)
                {

                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
                }

                WhiskeyUtil.MipiWrite(0x23, 0x33, Reg_setting[0]);
                WhiskeyUtil.MipiWrite(0x23, 0x34, Reg_setting[1]);
                WhiskeyUtil.MipiWrite(0x23, 0x35, Reg_setting[4]);
            }
            else if(color == "G")
            {
                for (byte addr = 0x40; addr <= 0x62; addr++)
                {

                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
                }
                WhiskeyUtil.MipiWrite(0x23, 0x63, Reg_setting[0]);
                WhiskeyUtil.MipiWrite(0x23, 0x64, Reg_setting[1]);
                WhiskeyUtil.MipiWrite(0x23, 0x65, Reg_setting[4]);
            }
            else if(color == "B")
            {
                for (byte addr = 0x70; addr <= 0x92; addr++)
                {

                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                    addr++;
                    WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
                }
                WhiskeyUtil.MipiWrite(0x23, 0x93, Reg_setting[0]);
                WhiskeyUtil.MipiWrite(0x23, 0x94, Reg_setting[1]);
                WhiskeyUtil.MipiWrite(0x23, 0x95, Reg_setting[4]);
            }
        }


        private void conveter_for_dgm_test_use(uint dgm_setting)
        {
            byte[] Reg_setting = new byte[5];
            uint temp = 0;
            temp = dgm_setting;
            temp >>= 2;


            temp = temp & 0x00FF;

            byte[] testuse = new byte[60];
            byte[] RdVal = new byte[1];

            //uint RegisterRead = 0x00;




            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();




            Reg_setting[0] = Convert.ToByte(temp);
            Reg_setting[1] = Convert.ToByte(temp);
            Reg_setting[2] = Convert.ToByte(temp);
            Reg_setting[3] = Convert.ToByte(temp);


            dgm_setting = dgm_setting & 0x03;
            temp = dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            temp <<= 2;
            temp = temp + dgm_setting;
            Reg_setting[4] = Convert.ToByte(temp);



            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x28);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

            for (byte addr = 0x10; addr <= 0x32; addr++)
            {

                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
            }

            WhiskeyUtil.MipiWrite(0x23, 0x33, Reg_setting[0]);
            WhiskeyUtil.MipiWrite(0x23, 0x34, Reg_setting[1]);
            WhiskeyUtil.MipiWrite(0x23, 0x35, Reg_setting[4]);

            for (byte addr = 0x40; addr <= 0x62; addr++)
            {

                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
            }
            WhiskeyUtil.MipiWrite(0x23, 0x63, Reg_setting[0]);
            WhiskeyUtil.MipiWrite(0x23, 0x64, Reg_setting[1]);
            WhiskeyUtil.MipiWrite(0x23, 0x65, Reg_setting[4]);



            for (byte addr = 0x70; addr <= 0x92; addr++)
            {

                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[0]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[1]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[2]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[3]);
                addr++;
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[4]);
            }
            WhiskeyUtil.MipiWrite(0x23, 0x93, Reg_setting[0]);
            WhiskeyUtil.MipiWrite(0x23, 0x94, Reg_setting[1]);
            WhiskeyUtil.MipiWrite(0x23, 0x95, Reg_setting[4]);


            //Check Parameter write to Register realdy??
            /*
             string textdata = null;
            for (byte addr = 0x10; addr <= 0x35; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x"+ Convert.ToString(addr,16) +"="+ Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }
            
            for (byte addr = 0x40; addr <= 0x65; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x" + Convert.ToString(addr, 16) + "=" + Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            for (byte addr = 0x70; addr <= 0x95; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x" + Convert.ToString(addr, 16) + "=" + Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }*/


        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            byte[] Reg_setting = new byte[38];
            string textdata = null;
            byte[] RdVal = new byte[1];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            DigGma_TieRegisterSetting[0] = 40;
            DigGma_TieRegisterSetting[1] = 98;
            DigGma_TieRegisterSetting[2] = 134;
            DigGma_TieRegisterSetting[3] = 161;
            DigGma_TieRegisterSetting[4] = 183;
            DigGma_TieRegisterSetting[5] = 203;
            DigGma_TieRegisterSetting[6] = 220;
            DigGma_TieRegisterSetting[7] = 249;
            DigGma_TieRegisterSetting[8] = 272;
            DigGma_TieRegisterSetting[9] = 292;
            DigGma_TieRegisterSetting[10] = 323;
            DigGma_TieRegisterSetting[11] = 348;
            DigGma_TieRegisterSetting[12] = 371;
            DigGma_TieRegisterSetting[13] = 393;
            DigGma_TieRegisterSetting[14] = 414;
            DigGma_TieRegisterSetting[15] = 416;
            DigGma_TieRegisterSetting[16] = 438;
            DigGma_TieRegisterSetting[17] = 465;
            DigGma_TieRegisterSetting[18] = 495;
            DigGma_TieRegisterSetting[19] = 531;
            DigGma_TieRegisterSetting[20] = 574;
            DigGma_TieRegisterSetting[21] = 600;
            DigGma_TieRegisterSetting[22] = 631;
            DigGma_TieRegisterSetting[23] = 668;
            DigGma_TieRegisterSetting[24] = 690;
            DigGma_TieRegisterSetting[25] = 715;
            DigGma_TieRegisterSetting[26] = 745;
            DigGma_TieRegisterSetting[27] = 782;
            DigGma_TieRegisterSetting[28] = 836;
            DigGma_TieRegisterSetting[29] = 1023;


            uint temp = 0;
            uint[] temp_lsb = new uint[4];
            uint j = 0;
            for (uint i = 0; i < 27; i++)
            {
                temp = DigGma_TieRegisterSetting[i];
                temp >>= 2;
                Reg_setting[j] = Convert.ToByte(temp);
                i++; j++;

                temp = DigGma_TieRegisterSetting[i];
                temp >>= 2;
                Reg_setting[j] = Convert.ToByte(temp);
                i++; j++;

                temp = DigGma_TieRegisterSetting[i];
                temp >>= 2;
                Reg_setting[j] = Convert.ToByte(temp);
                i++; j++;

                temp = DigGma_TieRegisterSetting[i];
                temp >>= 2;
                Reg_setting[j] = Convert.ToByte(temp);
                j = j + 2;
            }


            temp_lsb[0] = DigGma_TieRegisterSetting[0];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[1];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[2];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[3];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[4] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[4];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[5];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[6];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[7];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[9] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[8];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[9];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[10];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[11];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[14] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[12];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[13];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[14];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[15];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[19] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[16];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[17];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[18];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[19];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[24] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[20];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[21];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[22];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[23];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[29] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);

            temp_lsb[0] = DigGma_TieRegisterSetting[24];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[25];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            temp_lsb[2] = DigGma_TieRegisterSetting[26];
            temp_lsb[2] = temp_lsb[2] & 0x03;
            temp_lsb[2] <<= 4;
            temp_lsb[3] = DigGma_TieRegisterSetting[27];
            temp_lsb[3] = temp_lsb[3] & 0x03;
            temp_lsb[3] <<= 6;
            Reg_setting[34] = Convert.ToByte(temp_lsb[0] + temp_lsb[1] + temp_lsb[2] + temp_lsb[3]);


            temp = DigGma_TieRegisterSetting[28];
            Reg_setting[35] = Convert.ToByte(temp >>= 2);

            temp = DigGma_TieRegisterSetting[29];
            Reg_setting[36] = Convert.ToByte(temp >>= 2);

            temp_lsb[0] = DigGma_TieRegisterSetting[28];
            temp_lsb[0] = temp_lsb[0] & 0x03;
            temp_lsb[1] = DigGma_TieRegisterSetting[29];
            temp_lsb[1] = temp_lsb[1] & 0x03;
            temp_lsb[1] <<= 2;
            Reg_setting[37] = Convert.ToByte(temp_lsb[0] + temp_lsb[1]);


            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x28);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);

            j = 0;
            for (byte addr = 0x70; addr <= 0x95; addr++)
            {
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[j]);
                j++;
            }

            j = 0;
            for (byte addr = 0x40; addr <= 0x65; addr++)
            {
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[j]);
                j++;
            }

            j = 0;
            for (byte addr = 0x10; addr <= 0x35; addr++)
            {
                WhiskeyUtil.MipiWrite(0x23, addr, Reg_setting[j]);
                j++;
            }

            //Check Parameter write to Register realdy??

            for (byte addr = 0x10; addr <= 0x35; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x" + Convert.ToString(addr, 16) + "=" + Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            for (byte addr = 0x40; addr <= 0x65; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x" + Convert.ToString(addr, 16) + "=" + Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            for (byte addr = 0x70; addr <= 0x95; addr++)
            {
                WhiskeyUtil.MipiRead(addr, 1, ref RdVal);

                textdata = "Addr=0x" + Convert.ToString(addr, 16) + "=" + Convert.ToString(RdVal[0]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x28);
            WhiskeyUtil.MipiWrite(0x23, 0x00, 0x01);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            uint[] gammasetting = new uint[29];
            double[] All_Brightness_save = new double[1024];
            int dive = GMA_Set_comboBox.SelectedIndex + 1;

            string textdata = null;
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            textdata = "Gamma All Brighness Sacn!!\r\n";
            Info_textBox.AppendText(textdata);



            ////////////////////////////////////////////////////////////
            WhiskeyUtil.ImageFill(127, 127, 127);
            Thread.Sleep(500);
            SSD2123_GammaSetting_for_AUO_nmos_TypeA_v1_0_20170921();
            Application.DoEvents();

            ///////////////////////////////////////////////////////////////////////////////////////////
            for (uint scal = 0; scal < 1024; scal = scal + 2)
            {
                for (uint i = 0; i < 29; i++)
                { gammasetting[i] = scal; }

                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);

                WhiskeyUtil.ImageFill(127, 0, 0);
                Thread.Sleep(300);
                All_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(dive), 4);

                byte[] RdVal_page = new byte[3];

                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31); //Page31 R+ 僅讀回一筆及代表所有Gamma

                uint RegisterRead = 0x00;
                if (true)
                {
                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;

                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
                }

                textdata = "RegGet =" + Convert.ToString(RegisterRead) + " Gamma:" + Convert.ToString(scal) + "= ";
                textdata = textdata + Convert.ToString(All_Brightness_save[scal]) + "\r\n";

                Info_textBox.AppendText(textdata);
            }
            ///////////////////////////////////////////////////////////
            WhiskeyUtil.ImageFill(0, 127, 0);
            Thread.Sleep(500);
            SSD2123_GammaSetting_for_AUO_nmos_TypeA_v1_0_20170921();
            Application.DoEvents();

            textdata = "START G Gamma Test" + "\r\n";
            Info_textBox.AppendText(textdata);

            for (uint scal = 0; scal < 1024; scal = scal + 2)
            {
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = scal;
                }

                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);


                WhiskeyUtil.ImageFill(0, 127, 0);
                Thread.Sleep(300);
                All_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(dive), 4);

                byte[] RdVal_page = new byte[3];

                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x33); //Page31 R+ 僅讀回一筆及代表所有Gamma

                uint RegisterRead = 0x00;
                if (true)
                {

                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;

                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
                }

                textdata = "RegGet =" + Convert.ToString(RegisterRead) + " Gamma:" + Convert.ToString(scal) + "= ";
                textdata = textdata + Convert.ToString(All_Brightness_save[scal]) + "\r\n";


                Info_textBox.AppendText(textdata);
            }

            ///////////////////////////////////////////////////////////
            WhiskeyUtil.ImageFill(0, 127, 0);
            Thread.Sleep(500);
            SSD2123_GammaSetting_for_AUO_nmos_TypeA_v1_0_20170921();
            Application.DoEvents();

            textdata = "START B Gamma Test" + "\r\n";
            Info_textBox.AppendText(textdata);

            for (uint scal = 0; scal < 1024; scal = scal + 2)
            {
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = scal;
                }

                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
                WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);


                WhiskeyUtil.ImageFill(0, 0, 127);
                Thread.Sleep(300);
                All_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(dive), 4);

                byte[] RdVal_page = new byte[3];

                WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x35); //Page31 R+ 僅讀回一筆及代表所有Gamma
                uint RegisterRead = 0x00;
                if (true)
                {

                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;

                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
                }
                textdata = "RegGet =" + Convert.ToString(RegisterRead) + " Gamma:" + Convert.ToString(scal) + "= ";
                textdata = textdata + Convert.ToString(All_Brightness_save[scal]) + "\r\n";
                Info_textBox.AppendText(textdata);
            }




        }

        private void button19_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            //WhiskeyUtil.ImageShow("VG.bmp");
            WhiskeyUtil.ImageShow("VGR.bmp");
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            //WhiskeyUtil.ImageShow("VG.bmp");
            WhiskeyUtil.ImageShow("VGG.bmp");
        }

        private void button21_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            //WhiskeyUtil.ImageShow("VG.bmp");
            WhiskeyUtil.ImageShow("VGB.bmp");
        }

        private void button22_Click(object sender, EventArgs e)
        {
            byte[] RdVal = new byte[1];
            string textdata = null;

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();



            //R+ 1.8

            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 165;
            TieRegisterSetting[2] = 194;
            TieRegisterSetting[3] = 217;
            TieRegisterSetting[4] = 237;
            TieRegisterSetting[5] = 254;
            TieRegisterSetting[6] = 267;
            TieRegisterSetting[7] = 280;
            TieRegisterSetting[8] = 293;
            TieRegisterSetting[9] = 348;
            TieRegisterSetting[10] = 382;
            TieRegisterSetting[11] = 435;
            TieRegisterSetting[12] = 477;
            TieRegisterSetting[13] = 537;
            TieRegisterSetting[14] = 588;
            TieRegisterSetting[15] = 633;
            TieRegisterSetting[16] = 679;
            TieRegisterSetting[17] = 706;
            TieRegisterSetting[18] = 742;
            TieRegisterSetting[19] = 767;
            TieRegisterSetting[20] = 804;
            TieRegisterSetting[21] = 815;
            TieRegisterSetting[22] = 828;
            TieRegisterSetting[23] = 842;
            TieRegisterSetting[24] = 859;
            TieRegisterSetting[25] = 879;
            TieRegisterSetting[26] = 905;
            TieRegisterSetting[27] = 944;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x31);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31);
            textdata = "Read Page 0x31" + "\r\n";
            Info_textBox.AppendText(textdata);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr="+ Convert.ToString(TieCnt, 16) +"; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }


            //R- 2.2                                                  
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 170;
            TieRegisterSetting[2] = 204;
            TieRegisterSetting[3] = 229;
            TieRegisterSetting[4] = 251;
            TieRegisterSetting[5] = 267;
            TieRegisterSetting[6] = 283;
            TieRegisterSetting[7] = 297;
            TieRegisterSetting[8] = 311;
            TieRegisterSetting[9] = 365;
            TieRegisterSetting[10] = 401;
            TieRegisterSetting[11] = 457;
            TieRegisterSetting[12] = 497;
            TieRegisterSetting[13] = 563;
            TieRegisterSetting[14] = 614;
            TieRegisterSetting[15] = 660;
            TieRegisterSetting[16] = 708;
            TieRegisterSetting[17] = 739;
            TieRegisterSetting[18] = 780;
            TieRegisterSetting[19] = 808;
            TieRegisterSetting[20] = 850;
            TieRegisterSetting[21] = 862;
            TieRegisterSetting[22] = 874;
            TieRegisterSetting[23] = 888;
            TieRegisterSetting[24] = 903;
            TieRegisterSetting[25] = 922;
            TieRegisterSetting[26] = 944;
            TieRegisterSetting[27] = 1003;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x32);
            textdata = "\r\n"+ "Read Page 0x32" + "\r\n";
            Info_textBox.AppendText(textdata);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x32);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr=" + Convert.ToString(TieCnt, 16) + "; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            //G+ 1.8                              
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 134;
            TieRegisterSetting[2] = 176;
            TieRegisterSetting[3] = 202;
            TieRegisterSetting[4] = 225;
            TieRegisterSetting[5] = 244;
            TieRegisterSetting[6] = 256;
            TieRegisterSetting[7] = 269;
            TieRegisterSetting[8] = 282;
            TieRegisterSetting[9] = 328;
            TieRegisterSetting[10] = 363;
            TieRegisterSetting[11] = 417;
            TieRegisterSetting[12] = 461;
            TieRegisterSetting[13] = 538;
            TieRegisterSetting[14] = 591;
            TieRegisterSetting[15] = 637;
            TieRegisterSetting[16] = 683;
            TieRegisterSetting[17] = 710;
            TieRegisterSetting[18] = 746;
            TieRegisterSetting[19] = 768;
            TieRegisterSetting[20] = 806;
            TieRegisterSetting[21] = 817;
            TieRegisterSetting[22] = 830;
            TieRegisterSetting[23] = 843;
            TieRegisterSetting[24] = 860;
            TieRegisterSetting[25] = 881;
            TieRegisterSetting[26] = 909;
            TieRegisterSetting[27] = 973;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x33);
            textdata = "\r\n" + "Read Page 0x33" + "\r\n";
            Info_textBox.AppendText(textdata);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x33);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr=" + Convert.ToString(TieCnt, 16) + "; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            //G- 2.2                              
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 170;
            TieRegisterSetting[2] = 205;
            TieRegisterSetting[3] = 232;
            TieRegisterSetting[4] = 254;
            TieRegisterSetting[5] = 267;
            TieRegisterSetting[6] = 285;
            TieRegisterSetting[7] = 298;
            TieRegisterSetting[8] = 313;
            TieRegisterSetting[9] = 368;
            TieRegisterSetting[10] = 405;
            TieRegisterSetting[11] = 460;
            TieRegisterSetting[12] = 500;
            TieRegisterSetting[13] = 562;
            TieRegisterSetting[14] = 610;
            TieRegisterSetting[15] = 656;
            TieRegisterSetting[16] = 710;
            TieRegisterSetting[17] = 743;
            TieRegisterSetting[18] = 782;
            TieRegisterSetting[19] = 810;
            TieRegisterSetting[20] = 851;
            TieRegisterSetting[21] = 862;
            TieRegisterSetting[22] = 874;
            TieRegisterSetting[23] = 889;
            TieRegisterSetting[24] = 904;
            TieRegisterSetting[25] = 921;
            TieRegisterSetting[26] = 943;
            TieRegisterSetting[27] = 974;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x34);
            textdata = "\r\n" + "Read Page 0x34" + "\r\n";
            Info_textBox.AppendText(textdata);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x34);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr=" + Convert.ToString(TieCnt, 16) + "; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            //B+ 1.8                                                     
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 165;
            TieRegisterSetting[2] = 201;
            TieRegisterSetting[3] = 222;
            TieRegisterSetting[4] = 243;
            TieRegisterSetting[5] = 261;
            TieRegisterSetting[6] = 276;
            TieRegisterSetting[7] = 288;
            TieRegisterSetting[8] = 303;
            TieRegisterSetting[9] = 345;
            TieRegisterSetting[10] = 378;
            TieRegisterSetting[11] = 429;
            TieRegisterSetting[12] = 471;
            TieRegisterSetting[13] = 534;
            TieRegisterSetting[14] = 585;
            TieRegisterSetting[15] = 630;
            TieRegisterSetting[16] = 675;
            TieRegisterSetting[17] = 702;
            TieRegisterSetting[18] = 738;
            TieRegisterSetting[19] = 762;
            TieRegisterSetting[20] = 801;
            TieRegisterSetting[21] = 810;
            TieRegisterSetting[22] = 825;
            TieRegisterSetting[23] = 837;
            TieRegisterSetting[24] = 855;
            TieRegisterSetting[25] = 876;
            TieRegisterSetting[26] = 900;
            TieRegisterSetting[27] = 942;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x35);
            textdata = "\r\n" + "Read Page 0x35" + "\r\n";
            Info_textBox.AppendText(textdata);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x35);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr=" + Convert.ToString(TieCnt, 16) + "; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }

            //B- 2.2     
            TieRegisterSetting[0] = 0;
            TieRegisterSetting[1] = 152;
            TieRegisterSetting[2] = 197;
            TieRegisterSetting[3] = 227;
            TieRegisterSetting[4] = 247;
            TieRegisterSetting[5] = 267;
            TieRegisterSetting[6] = 287;
            TieRegisterSetting[7] = 302;
            TieRegisterSetting[8] = 314;
            TieRegisterSetting[9] = 364;
            TieRegisterSetting[10] = 400;
            TieRegisterSetting[11] = 454;
            TieRegisterSetting[12] = 494;
            TieRegisterSetting[13] = 566;
            TieRegisterSetting[14] = 617;
            TieRegisterSetting[15] = 657;
            TieRegisterSetting[16] = 707;
            TieRegisterSetting[17] = 739;
            TieRegisterSetting[18] = 785;
            TieRegisterSetting[19] = 820;
            TieRegisterSetting[20] = 872;
            TieRegisterSetting[21] = 890;
            TieRegisterSetting[22] = 911;
            TieRegisterSetting[23] = 931;
            TieRegisterSetting[24] = 952;
            TieRegisterSetting[25] = 969;
            TieRegisterSetting[26] = 990;
            TieRegisterSetting[27] = 1011;
            TieRegisterSetting[28] = 1023;
            WriteGammaSettingAlltheSame_to_SSD2130_SetPage(TieRegisterSetting, 0x36);
            textdata = "\r\n" + "Read Page 0x36" + "\r\n";
            Info_textBox.AppendText(textdata);
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x36);
            for (byte TieCnt = 0; TieCnt < 29; TieCnt++)
            {
                WhiskeyUtil.MipiRead(TieCnt, 1, ref RdVal);
                textdata = "Addr=" + Convert.ToString(TieCnt, 16) + "; Cmd=" + Convert.ToString(RdVal[0], 16) + "\r\n";
                Info_textBox.AppendText(textdata);
            }
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);



        }

        private void button23_Click(object sender, EventArgs e)
        {
            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            string textdata = null;
            int cnt = 0;


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte tie_gray = 0x00;

            chart1.Series[3].Points.Clear();

            textdata = "Now Brightness\r\n";
            Info_textBox.AppendText(textdata);

            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(0, tie_gray, 0);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);


            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            byte[] RdVal = new byte[1];
            string textdata = null;

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x30);

            textdata = "(Before)Read Setting Addr 0x05=" + Convert.ToString(RdVal[0]) + "\r\n";
            Info_textBox.AppendText(textdata);

            WhiskeyUtil.MipiWrite(0x23, 0x05, 0x40);
            WhiskeyUtil.MipiRead(0x05, 1, ref RdVal);

            textdata = "(After)Read Setting Addr 0x05=" + Convert.ToString(RdVal[0],16) + "\r\n";
            Info_textBox.AppendText(textdata);

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            byte[] RdVal = new byte[1];
            string textdata = null;

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x30);
            textdata = "(Before)Read Setting Addr 0x05=" + Convert.ToString(RdVal[0]) + "\r\n";
            Info_textBox.AppendText(textdata);

            WhiskeyUtil.MipiWrite(0x23, 0x05, 0xc0);
            WhiskeyUtil.MipiRead(0x05, 1, ref RdVal);

            textdata = "(After)Read Setting Addr 0x05=" + Convert.ToString(RdVal[0],16) + "\r\n";
            Info_textBox.AppendText(textdata);

            WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x00);
        }

        private void button26_Click(object sender, EventArgs e)
        {
            int dive = GMA_Set_comboBox.SelectedIndex + 1;
            string textdata = null;
            int cnt = 0;


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            byte tie_gray = 0x00;

            chart1.Series[3].Points.Clear();

            textdata = "Now Brightness\r\n";
            Info_textBox.AppendText(textdata);

            for (uint tie = 0; tie < 29; tie++) //tir=0 時亮度最亮
            {

                //面板點目前要測試亮度的灰階
                tie_gray = Convert.ToByte(VP_index[tie]);


                WhiskeyUtil.ImageFill(0, 0, tie_gray);
                Thread.Sleep(300);

                Actual_Brightness[VP_index[tie]] = Math.Round(K80_Trigger_Measurement(dive), 4);//取到小數點第4位

                chart1.Series[3].Points.AddXY(VP_index[tie], Actual_Brightness[VP_index[tie]]);




                textdata = "VP" + Convert.ToString(VP_index[tie]) + " Brightness=" + Convert.ToString(Actual_Brightness[VP_index[tie]]) + "\r\n";
                Info_textBox.AppendText(textdata);


            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            WhiskeyUtil.ImageShow("Column_inv_187_1080x1920.bmp");
        }

        private void R_VCOMSet_BOT_Click(object sender, EventArgs e)
        {
            uint RegisterRead = 0x00;
            byte[] RdVal = new byte[1];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            
            //VCom 調整時 設定暫存器Page為0xA0
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);

            //讀回VCom設定值 Bit8
            WhiskeyUtil.MipiRead(0x03, 1, ref RdVal);
            RegisterRead = RdVal[0];
            RegisterRead <<= 8;

            //讀回VCom設定值 Bit0~Bit7
            WhiskeyUtil.MipiRead(0x04, 1, ref RdVal);
            RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);
            VCOM_Setting = RegisterRead;

            Show_VCom_textBox.Text = Convert.ToString(VCOM_Setting, 16);
            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);

            VCOM_Increase_BUT.Enabled = true;
            VCOM_Decrease_BUT.Enabled = true;
            Wr_VComSetting_BOT.Enabled = true;
            WhiskeyUtil.ImageShow("Column_inv_187_1080x1920.bmp");
        }

        private void Wr_VComSetting_BOT_Click(object sender, EventArgs e)
        {
            string VCom_Setting_String = "";
            uint VCom_Setting_Value = 0;
            byte[] RdVal = new byte[1];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();

            VCom_Setting_String = Show_VCom_textBox.Text;
            //VCOM_Setting


            VCom_Setting_Value = Convert.ToUInt16(VCom_Setting_String,16);
            if(VCom_Setting_Value <= 0)
            { VCom_Setting_Value = 0; }
            else if(VCom_Setting_Value >= 455)
            { VCom_Setting_Value = 455; }


            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0xA0);

            VCOM_Setting = VCom_Setting_Value;
            RdVal[0] = Convert.ToByte(VCom_Setting_Value & 0x00FF);
            WhiskeyUtil.MipiWrite(0x23, 0x04, RdVal[0]);
            WhiskeyUtil.MipiWrite(0x23, 0x06, RdVal[0]);

            VCOM_Setting = VCom_Setting_Value;
            RdVal[0] = Convert.ToByte(VCom_Setting_Value >>= 8);
            WhiskeyUtil.MipiWrite(0x23, 0x03, RdVal[0]);
            WhiskeyUtil.MipiWrite(0x23, 0x05, RdVal[0]);

            WhiskeyUtil.MipiWrite(0x29, 0xff, 0x21, 0x30, 0x00);

            WhiskeyUtil.ImageShow("Column_inv_187_1080x1920.bmp");

            VCom_SettingDoneCheck_BUT.Enabled = true;
        }

        private void Get_AGma_ReadBrightness_BUT_Click(object sender, EventArgs e)
        {
            string textdata = null;
            uint[] gammasetting = new uint[29];

            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            Info_textBox.Text = "";


            //被要求測試灰階Gamma(RGB套用同樣的值測)
            if (Gary_CKBox.Checked == true)
            {
                textdata = "實測灰階 Gamma亮度表現\r\n";
                Info_textBox.AppendText(textdata);

                for (uint scal = 0; scal < 1024; scal++)
                {
                    for (uint i = 0; i < 29; i++)
                    { gammasetting[i] = scal; }
                    Application.DoEvents();

                    //Page31h~Page36h 都套用所有的gammasetting值
                    WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);

                    //讀回Page31h的任一個AnaolgGamma設定 用以代表/判斷目前AnalogGamma設定值
                    WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31); //Page31 R+ 僅讀回一筆及代表所有Gamma
                    uint RegisterRead = 0x00;
                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);//讀回Page 31h  Command 20h的內容 RPGMA16[9:8]
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;
                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);//讀回Page 31h  Command 20h的內容 RPGMA16[7:0]
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);



                    //RGB套同樣值(以灰階圖 RGB 127灰階 去測試量測面板亮度表現) 
                    WhiskeyUtil.ImageFill(127, 127, 127);
                    Thread.Sleep(300);
                    RGB_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    //RGB_Brightness_save[scal] = 1024 - (double)(scal);

                    textdata = "RGB AGamma值=" + Convert.ToString(scal) + "時  實測亮度表現=";
                    textdata = textdata + Convert.ToString(RGB_Brightness_save[scal]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                textdata = "------------------------------\r\n";
                Info_textBox.AppendText(textdata);
            }

            //被要求測試R Gamma(R套用同樣的值測)
            if (R_CKBox.Checked == true)
            {
                
                textdata = "實測R Gamma亮度表現\r\n";
                Info_textBox.AppendText(textdata);

                for (uint scal = 0; scal < 1024; scal++)
                {
                    for (uint i = 0; i < 29; i++)
                    { gammasetting[i] = scal; }
                    Application.DoEvents();

                    //Page31h~Page32h 為R Gamma Setting Register針對這兩個Page填入測試Gamma設定值
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);

                    //讀回Page31h的任一個AnaolgGamma設定 用以代表/判斷目前AnalogGamma設定值
                    WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x31); //Page31 R+ 僅讀回一筆及代表所有Gamma
                    uint RegisterRead = 0x00;
                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);//讀回Page 31h  Command 20h的內容 RPGMA16[9:8]
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;
                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);//讀回Page 31h  Command 20h的內容 RPGMA16[7:0]
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);


                    //RGB套同樣值(以G 127灰階 去測試量測面板亮度表現) 
                    WhiskeyUtil.ImageFill(127, 0, 0);
                    Thread.Sleep(300);
                    R_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    //R_Brightness_save[scal] = (double)(256 - (double)scal / 4);

                    textdata = "R AGamma值=" + Convert.ToString(scal) + "時  實測亮度表現=";
                    textdata = textdata + Convert.ToString(R_Brightness_save[scal]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                textdata = "------------------------------\r\n";
                Info_textBox.AppendText(textdata);
            }

            //被要求測試G Gamma(G套用同樣的值測)
            if (G_CKBox.Checked == true)
            {

                textdata = "實測G Gamma亮度表現\r\n";
                Info_textBox.AppendText(textdata);

                for (uint scal = 0; scal < 1024; scal++)
                {
                    for (uint i = 0; i < 29; i++)
                    { gammasetting[i] = scal; }
                    Application.DoEvents();

                    //Page33h~Page34h 為G Gamma Setting Register針對這兩個Page填入測試Gamma設定值
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);

                    //讀回Page31h的任一個AnaolgGamma設定 用以代表/判斷目前AnalogGamma設定值
                    WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x33); //Page33 G+ 僅讀回一筆及代表所有Gamma
                    uint RegisterRead = 0x00;
                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);//讀回Page 33h  Command 20h的內容 RPGMA16[9:8]
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;
                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);//讀回Page 33h  Command 20h的內容 RPGMA16[7:0]
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);


                    //RGB套同樣值(以G 127灰階 去測試量測面板亮度表現) 
                    WhiskeyUtil.ImageFill(0, 127, 0);
                    Thread.Sleep(300);
                    G_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    //G_Brightness_save[scal] = (double)(512 - (double)(scal / 2));

                    textdata = "G AGamma值=" + Convert.ToString(scal) + "時  實測亮度表現=";
                    textdata = textdata + Convert.ToString(G_Brightness_save[scal]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                textdata = "------------------------------\r\n";
                Info_textBox.AppendText(textdata);
            }

            //被要求測試B Gamma(B套用同樣的值測)
            if (B_CKBox.Checked == true)
            {

                textdata = "實測B Gamma亮度表現\r\n";
                Info_textBox.AppendText(textdata);

                for (uint scal = 0; scal < 1024; scal++)
                {
                    for (uint i = 0; i < 29; i++)
                    { gammasetting[i] = scal; }
                    Application.DoEvents();

                    //Page35h~Page36h 為B Gamma Setting Register針對這兩個Page填入測試Gamma設定值
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);

                    //讀回Page31h的任一個AnaolgGamma設定 用以代表/判斷目前AnalogGamma設定值
                    WhiskeyUtil.MipiWrite(0x29, 0xFF, 0x21, 0x30, 0x35); //Page35 B+ 僅讀回一筆及代表所有Gamma
                    uint RegisterRead = 0x00;
                    byte[] RdVal = new byte[1];

                    WhiskeyUtil.MipiRead(20, 1, ref RdVal);//讀回Page 35h  Command 20h的內容 RPGMA16[9:8]
                    RegisterRead = RdVal[0];
                    RegisterRead <<= 8;
                    WhiskeyUtil.MipiRead(21, 1, ref RdVal);//讀回Page 35h  Command 20h的內容 RPGMA16[7:0]
                    RegisterRead = RegisterRead + Convert.ToUInt16(RdVal[0]);


                    //RGB套同樣值(以G 127灰階 去測試量測面板亮度表現) 
                    WhiskeyUtil.ImageFill(0, 127, 0);
                    Thread.Sleep(300);
                    B_Brightness_save[scal] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    //B_Brightness_save[scal] = (double)(256 - (double)(scal / 4));

                    textdata = "B AGamma值=" + Convert.ToString(scal) + "時  實測亮度表現=";
                    textdata = textdata + Convert.ToString(B_Brightness_save[scal]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                textdata = "------------------------------\r\n";
                Info_textBox.AppendText(textdata);
            }
        }

        private void TieValueProjection_BUT_Click(object sender, EventArgs e)
        {
            //private double[] RGB_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(RGB灰階用)
            //private double[] R_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(R灰階用)
            //private double[] G_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(G灰階用)
            //private double[] B_Brightness_save = new double[1024];//用以儲存K80實測後亮度表現值(B灰階用)
            uint scal = 0;
            double max_brightness = 0;
            double min_brightness = 0;
            double maxmin_diffbrightness = 0;
            double GammaSet = 0;
            double temp = 0, temp2 = 0;
            string textdata = null;

            Info_textBox.Text = "";//清空Info_textBox 內容


            if (GMA_Set_comboBox.SelectedIndex == 0)
            { GammaSet = 2.2; }
            else if (GMA_Set_comboBox.SelectedIndex == 1)
            { GammaSet = 1.8; }


            if (Gary_CKBox.Checked == true)
            {
                max_brightness = 0;
                min_brightness = 1000;
                for (scal = 0; scal < 1024; scal++)
                {
                    if (max_brightness <= RGB_Brightness_save[scal])
                    { max_brightness = RGB_Brightness_save[scal]; }

                    if (min_brightness >= RGB_Brightness_save[scal])
                    { min_brightness = RGB_Brightness_save[scal]; }
                }
                maxmin_diffbrightness = max_brightness - min_brightness;

                //textdata = " Max=" + Convert.ToString(max_brightness) + "Min=" + Convert.ToString(min_brightness) + Environment.NewLine;
                //textdata = textdata + "Gamma Set = " + Convert.ToString(GammaSet) + Environment.NewLine;
                //Info_textBox.AppendText(textdata);

                //計算出綁點推算符合Gamma曲線的標準亮度值
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    temp = ((double)VP_index[tienum] / 255);
                    temp = 1 - temp;
                    temp2 = (float)Math.Pow(temp, GammaSet);

                    RGB_Tie_Projection[tienum] = Math.Round(maxmin_diffbrightness * (float)Math.Pow(temp, GammaSet), 4) + min_brightness;
                }


                //根據推算的標準亮度值 去找尋實測的亮度資料中 媒合綁點設定值
                double min_diff = 1000;
                textdata = "RGB Tie Projection" + Environment.NewLine;
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    min_diff = 1000;
                    for (scal = 0; scal < 1024; scal++)
                    {
                        if (Math.Abs(RGB_Brightness_save[scal] - RGB_Tie_Projection[tienum]) <= min_diff)
                        {
                            min_diff = Math.Abs(RGB_Brightness_save[scal] - RGB_Tie_Projection[tienum]);
                            Index_RGB_Tie_Projection[tienum] = scal;
                        }
                    }
                    textdata = textdata + "Index_RGB_Tie["+ Convert.ToString(tienum) + "]=" + Convert.ToString(Index_RGB_Tie_Projection[tienum]) + Environment.NewLine;
                }
                Info_textBox.AppendText(textdata);
            }
            if (R_CKBox.Checked == true)
            {
                max_brightness = 0;
                min_brightness = 1000;
                for (scal = 0; scal < 1024; scal++)
                {
                    if (max_brightness <= R_Brightness_save[scal])
                    { max_brightness = R_Brightness_save[scal]; }

                    if (min_brightness >= R_Brightness_save[scal])
                    { min_brightness = R_Brightness_save[scal]; }
                }
                maxmin_diffbrightness = max_brightness - min_brightness;


                //計算出綁點推算符合Gamma曲線的標準亮度值
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    temp = ((double)VP_index[tienum] / 255);
                    temp = 1 - temp;
                    temp2 = (float)Math.Pow(temp, GammaSet);

                    R_Tie_Projection[tienum] = Math.Round(maxmin_diffbrightness * (float)Math.Pow(temp, GammaSet), 4) + min_brightness;
                }


                //根據推算的標準亮度值 去找尋實測的亮度資料中 媒合綁點設定值
                double min_diff = 1000;
                textdata = "R Tie Projection" + Environment.NewLine;
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    min_diff = 1000;
                    for (scal = 0; scal < 1024; scal++)
                    {
                            if(scal == 623)
                            {
                                uint i = 0;
                            }
                        if (Math.Abs(R_Brightness_save[scal] - R_Tie_Projection[tienum]) <= min_diff)
                        {
                            min_diff = Math.Abs(R_Brightness_save[scal] - R_Tie_Projection[tienum]);
                            Index_R_Tie_Projection[tienum] = scal;
                        }
                    }
                    textdata = textdata + "Index_R_Tie[" + Convert.ToString(tienum) + "]=" + Convert.ToString(Index_R_Tie_Projection[tienum]) + Environment.NewLine;
                }
                Info_textBox.AppendText(textdata);
            }
            if (G_CKBox.Checked == true)
            {
                max_brightness = 0;
                min_brightness = 1000;

                for (scal = 0; scal < 1024; scal++)
                {
                    if (max_brightness <= G_Brightness_save[scal])
                    { max_brightness = G_Brightness_save[scal]; }

                    if (min_brightness >= G_Brightness_save[scal])
                    { min_brightness = G_Brightness_save[scal]; }
                }
                maxmin_diffbrightness = max_brightness - min_brightness;

                //計算出綁點推算符合Gamma曲線的標準亮度值                                                                              
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    temp = ((double)VP_index[tienum] / 255);
                    temp = 1 - temp;
                    temp2 = (float)Math.Pow(temp, GammaSet);

                    G_Tie_Projection[tienum] = Math.Round(maxmin_diffbrightness * (float)Math.Pow(temp, GammaSet), 4) + min_brightness;
                }                                           


                //根據推算的標準亮度值 去找尋實測的亮度資料中 媒合綁點設定值         
                textdata = "G Tie Projection" + Environment.NewLine;
                double min_diff = 1000;
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    min_diff = 1000;
                    for (scal = 0; scal < 1024; scal++)
                    {
                        if (Math.Abs(G_Brightness_save[scal] - G_Tie_Projection[tienum]) <= min_diff)
                        {
                            min_diff = Math.Abs(G_Brightness_save[scal] - G_Tie_Projection[tienum]);
                            Index_G_Tie_Projection[tienum] = scal;
                        }
                    }
                    textdata = textdata + "Index_G_Tie[" + Convert.ToString(tienum) + "]=" + Convert.ToString(Index_G_Tie_Projection[tienum]) + Environment.NewLine;
                }
                Info_textBox.AppendText(textdata);
            }
            if (B_CKBox.Checked == true)
            {
                max_brightness = 0;
                min_brightness = 1000;
                for (scal = 0; scal < 1024; scal++)
                {
                    if (max_brightness <= B_Brightness_save[scal])
                    { max_brightness = B_Brightness_save[scal]; }

                    if (min_brightness >= B_Brightness_save[scal])
                    { min_brightness = B_Brightness_save[scal]; }
                }
                maxmin_diffbrightness = max_brightness - min_brightness;

                //計算出綁點推算符合Gamma曲線的標準亮度值                                                                              
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    temp = ((double)VP_index[tienum] / 255);
                    temp = 1 - temp;
                    temp2 = (float)Math.Pow(temp, GammaSet);

                    B_Tie_Projection[tienum] = Math.Round(maxmin_diffbrightness * (float)Math.Pow(temp, GammaSet), 4) + min_brightness;
                }                                           


                //根據推算的標準亮度值 去找尋實測的亮度資料中 媒合綁點設定值                                                           
                double min_diff = 1000;
                textdata = "B Tie Projection" + Environment.NewLine;
                for (uint tienum = 0; tienum < 29; tienum++)
                {
                    min_diff = 1000;
                    for (scal = 0; scal < 1024; scal++)
                    {
                        if (Math.Abs(B_Brightness_save[scal] - B_Tie_Projection[tienum]) <= min_diff)
                        {
                            min_diff = Math.Abs(B_Brightness_save[scal] - B_Tie_Projection[tienum]);
                            Index_B_Tie_Projection[tienum] = scal;
                        }
                    }
                    textdata = textdata + "Index_B_Tie[" + Convert.ToString(tienum) + "]=" + Convert.ToString(Index_B_Tie_Projection[tienum]) + Environment.NewLine;
                }
                Info_textBox.AppendText(textdata);
            }
        }

        private void SetAGmaProjectIndex2Register_BUT_Click(object sender, EventArgs e)
        {
            if (Gary_CKBox.Checked == true)
            {
                WriteGammaSettingAlltheSame_to_SSD2130(Index_RGB_Tie_Projection);
            }
            if (R_CKBox.Checked == true)
            {
                WriteGammaSettingAlltheSame_to_SSD2130(Index_R_Tie_Projection);
            }
            if (G_CKBox.Checked == true)
            {
                WriteGammaSettingAlltheSame_to_SSD2130(Index_G_Tie_Projection);
            }
            if (B_CKBox.Checked == true)
            {
                WriteGammaSettingAlltheSame_to_SSD2130(Index_B_Tie_Projection);
            }
        }

        private void OpenFormatforGammaTieSetting_BUT_Click_1(object sender, EventArgs e)
        {
            string textdata = null;


            if (Gary_CKBox.Checked == true)
            {
                Info_textBox.Text = "";//清空Info_textBox 內容
                for (uint i=0; i<29; i++)
                {
                    textdata = textdata + "Index_RGB_Tie[" + Convert.ToString(i) + "]=" + Convert.ToString(Index_RGB_Tie_Projection[i]) + Environment.NewLine;
                }
                textdata = textdata + "------------------------------" + Environment.NewLine;
                Info_textBox.AppendText(textdata);
            }
            if (R_CKBox.Checked == true)
            {
                Info_textBox.Text = "";//清空Info_textBox 內容
                for (uint i = 0; i < 29; i++)
                {
                    textdata = textdata + "Index_R_Tie[" + Convert.ToString(i) + "]=" + Convert.ToString(Index_R_Tie_Projection[i]) + Environment.NewLine;
                }
                textdata = textdata + "------------------------------" + Environment.NewLine;
                Info_textBox.AppendText(textdata);
            }
            if (G_CKBox.Checked == true)
            {
                Info_textBox.Text = "";//清空Info_textBox 內容
                for (uint i = 0; i < 29; i++)
                {
                    textdata = textdata + "Index_G_Tie[" + Convert.ToString(i) + "]=" + Convert.ToString(Index_G_Tie_Projection[i]) + Environment.NewLine;
                }
                textdata = textdata + "------------------------------" + Environment.NewLine;
                Info_textBox.AppendText(textdata);
            }
            if (B_CKBox.Checked == true)
            {
                Info_textBox.Text = "";//清空Info_textBox 內容
                for (uint i = 0; i < 29; i++)
                {
                    textdata = textdata + "Index_B_Tie[" + Convert.ToString(i) + "]=" + Convert.ToString(Index_B_Tie_Projection[i]) + Environment.NewLine;
                }
                textdata = textdata + "------------------------------" + Environment.NewLine;
                Info_textBox.AppendText(textdata);
            }
        }

        private void button34_Click(object sender, EventArgs e)
        {
            uint i;
            i = uint.Parse(Info_textBox.Text);

            Info_textBox.AppendText("Done!");
            //if (Gary_CKBox.Checked == true)
            //{
            //    WriteGammaSettingAlltheSame_to_SSD2130(Index_RGB_Tie_Projection);
            //}
            //if (R_CKBox.Checked == true)
            //{
            //    WriteGammaSettingAlltheSame_to_SSD2130(Index_R_Tie_Projection);
            //}
            //if (G_CKBox.Checked == true)
            //{
            //    WriteGammaSettingAlltheSame_to_SSD2130(Index_G_Tie_Projection);
            //}
            //if (B_CKBox.Checked == true)
            //{
            //    WriteGammaSettingAlltheSame_to_SSD2130(Index_B_Tie_Projection);
            //}
        }

        private void AGma_ToleranceJudgment_BUT_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            string textdata = null;
            double Max_Brightness = 0;
            double Min_Brightness = 0;
            double AGma_tolence = 0;
            bool Error_Flag_RGB = false;
            bool Error_Flag_R = false;
            bool Error_Flag_G = false;
            bool Error_Flag_B = false;

            double[] RGB_Brightness_temp = new double[256];
            double[] R_Brightness_temp = new double[256];
            double[] G_Brightness_temp = new double[256];
            double[] B_Brightness_temp = new double[256];

            double[] RGB_Brightness_STDAns_Pos = new double[256];
            double[] R_Brightness_STDAns_Pos = new double[256];
            double[] G_Brightness_STDAns_Pos = new double[256];
            double[] B_Brightness_STDAns_Pos = new double[256];

            double[] RGB_Brightness_STDAns_Nag = new double[256];
            double[] R_Brightness_STDAns_Nag = new double[256];
            double[] G_Brightness_STDAns_Nag = new double[256];
            double[] B_Brightness_STDAns_Nag = new double[256];

            Info_textBox.Text = "";//清空Info_textBox 內容
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();
            chart1.Series[3].Points.Clear();
            chart1.Series[4].Points.Clear();
            chart1.Series[5].Points.Clear();
            chart1.Series[6].Points.Clear();
            chart1.Series[7].Points.Clear();
            chart1.Series[8].Points.Clear();
            chart1.Series[9].Points.Clear();
            chart1.Series[10].Points.Clear();
            chart1.Series[11].Points.Clear();


            double maxSubmin = 0;
            double GammaValue = 0;
            switch (AGma_tolerance_comboBox.SelectedIndex)
            {
                case 0:
                    AGma_tolence = 0.2;
                    break;
                case 1:
                    AGma_tolence = 0.1;
                    break;
                case 2:
                    AGma_tolence = 0.05;
                    break;
            }

            switch (GMA_Set_comboBox.SelectedIndex)
            {
                case 0:
                    GammaValue = 2.2;
                    break;
                case 1:
                    GammaValue = 1.8;
                    break;
            }



            //STEP1:先實測測試選項256階灰階表現
            if (Gary_CKBox.Checked == true)
            {
                Max_Brightness = 0;
                Min_Brightness = 1000;
                byte color_GrayScale = 0;
                for (uint GrayScale=0; GrayScale<256; GrayScale++)
                {
                    
                    Application.DoEvents();
                    WhiskeyUtil.ImageFill(color_GrayScale, color_GrayScale, color_GrayScale);
                    Thread.Sleep(300);
                    //RGB_Brightness_temp[GrayScale] = Math.Round(1023 * ((double)GrayScale/260),4);
                    RGB_Brightness_temp[GrayScale] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    textdata = "RGB_Brightness[Gary=" + Convert.ToString(GrayScale) + "]=" + Convert.ToString(RGB_Brightness_temp[GrayScale]) + Environment.NewLine;

                    

                    if (RGB_Brightness_temp[GrayScale] > Max_Brightness)
                    { Max_Brightness = RGB_Brightness_temp[GrayScale];      }
                    if(Min_Brightness > RGB_Brightness_temp[GrayScale])
                    { Min_Brightness = RGB_Brightness_temp[GrayScale];      }
                    Info_textBox.AppendText(textdata);

                    color_GrayScale++;
                    chart1.Series[0].Points.AddXY(GrayScale, RGB_Brightness_temp[GrayScale]);//Chart1繪圖RGB_Gma_Curve
                }
                
                maxSubmin = Max_Brightness - Min_Brightness;

                GMA_Set_comboBox.SelectedIndex = 0;

                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {
                    RGB_Brightness_STDAns_Pos[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue + AGma_tolence))),4)+ Min_Brightness;
                    RGB_Brightness_STDAns_Nag[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue - AGma_tolence))),4) + Min_Brightness;

                    chart1.Series[1].Points.AddXY(GrayScale, RGB_Brightness_STDAns_Pos[GrayScale]);//Chart1繪圖RGB_Gma_Upbound
                    chart1.Series[2].Points.AddXY(GrayScale, RGB_Brightness_STDAns_Nag[GrayScale]);//Chart1繪圖RGB_Gma_Lowbound
                    
                    //實測亮度 比Gamma預期值誤差量 還要亮 超過Tolerance Spec.
                    if (RGB_Brightness_temp[GrayScale] > RGB_Brightness_STDAns_Pos[GrayScale])
                    {
                        if(Error_Flag_RGB == false)
                        {   Error_Flag_RGB = true; }

                        textdata = "Fail 大於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(RGB_Brightness_STDAns_Pos[GrayScale]) + "  實測:"+ RGB_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 還要暗 低於過Tolerance Spec.
                    else if (RGB_Brightness_temp[GrayScale] < RGB_Brightness_STDAns_Nag[GrayScale])
                    {
                        if (Error_Flag_RGB == false)
                        { Error_Flag_RGB = true; }

                        textdata = "Fail 小於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(RGB_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + RGB_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 符合預期範圍
                    else
                    {
                        textdata = "Pass 合乎 Tolerance Sprc. @ GaryScal=" + RGB_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    Info_textBox.AppendText(textdata);
                }

                if(Error_Flag_RGB == false)
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Green;
                    AGma_RGBJudge_Label.Text = "灰階Gma:Pass";
                }
                else
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Red;
                    AGma_RGBJudge_Label.Text = "灰階Gma:Fail";
                }
            }
            if (R_CKBox.Checked == true)
            {
                Max_Brightness = 0;
                Min_Brightness = 1000;
                byte color_GrayScale = 0;
                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {

                    Application.DoEvents();
                    WhiskeyUtil.ImageFill(color_GrayScale, 0, 0);
                    Thread.Sleep(300);
                    //R_Brightness_temp[GrayScale] = Math.Round(1023 * ((double)GrayScale / 260), 4);
                    R_Brightness_temp[GrayScale] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    textdata = "R_Brightness[Gary=" + Convert.ToString(GrayScale) + "]=" + Convert.ToString(R_Brightness_temp[GrayScale]) + Environment.NewLine;

                    if (R_Brightness_temp[GrayScale] > Max_Brightness)
                    { Max_Brightness = R_Brightness_temp[GrayScale]; }
                    if (Min_Brightness > R_Brightness_temp[GrayScale])
                    { Min_Brightness = R_Brightness_temp[GrayScale]; }
                    Info_textBox.AppendText(textdata);

                    color_GrayScale++;
                    chart1.Series[3].Points.AddXY(GrayScale, R_Brightness_temp[GrayScale]);//Chart1繪圖R_Gma_Curve
                }

                maxSubmin = Max_Brightness - Min_Brightness;

                GMA_Set_comboBox.SelectedIndex = 0;

                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {
                    R_Brightness_STDAns_Pos[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue + AGma_tolence))), 4) + Min_Brightness;
                    R_Brightness_STDAns_Nag[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue - AGma_tolence))), 4) + Min_Brightness;

                    chart1.Series[4].Points.AddXY(GrayScale, R_Brightness_STDAns_Pos[GrayScale]);//Chart1繪圖R_Gma_Upbound
                    chart1.Series[5].Points.AddXY(GrayScale, R_Brightness_STDAns_Nag[GrayScale]);//Chart1繪圖R_Gma_Lowbound

                    //實測亮度 比Gamma預期值誤差量 還要亮 超過Tolerance Spec.
                    if (R_Brightness_temp[GrayScale] > R_Brightness_STDAns_Pos[GrayScale])
                    {
                        if (Error_Flag_R == false)
                        { Error_Flag_R = true; }

                        textdata = "Fail 大於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(R_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + R_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 還要暗 低於過Tolerance Spec.
                    else if (R_Brightness_temp[GrayScale] < R_Brightness_STDAns_Nag[GrayScale])
                    {
                        if (Error_Flag_R == false)
                        { Error_Flag_R = true; }

                        textdata = "Fail 小於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(R_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + R_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 符合預期範圍
                    else
                    {
                        textdata = "Pass 合乎 Tolerance Sprc. @ GaryScal=" + R_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    Info_textBox.AppendText(textdata);
                }

                if (Error_Flag_R == false)
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Green;
                    AGma_RGBJudge_Label.Text = "R Gma:Pass";
                }
                else
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Red;
                    AGma_RGBJudge_Label.Text = "R Gma:Fail";
                }
            }
            if (G_CKBox.Checked == true)
            {
                Max_Brightness = 0;
                Min_Brightness = 1000;
                byte color_GrayScale = 0;
                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {

                    Application.DoEvents();
                    WhiskeyUtil.ImageFill(0, color_GrayScale, 0);
                    Thread.Sleep(300);
                    //G_Brightness_temp[GrayScale] = Math.Round(1023 * ((double)GrayScale / 260), 4);
                    G_Brightness_temp[GrayScale] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    textdata = "G_Brightness[Gary=" + Convert.ToString(GrayScale) + "]=" + Convert.ToString(G_Brightness_temp[GrayScale]) + Environment.NewLine;

                    if (G_Brightness_temp[GrayScale] > Max_Brightness)
                    { Max_Brightness = G_Brightness_temp[GrayScale]; }
                    if (Min_Brightness > G_Brightness_temp[GrayScale])
                    { Min_Brightness = G_Brightness_temp[GrayScale]; }
                    Info_textBox.AppendText(textdata);

                    color_GrayScale++;
                    chart1.Series[6].Points.AddXY(GrayScale, G_Brightness_temp[GrayScale]);//Chart1繪圖G_Gma_Curve
                }

                maxSubmin = Max_Brightness - Min_Brightness;

                GMA_Set_comboBox.SelectedIndex = 0;

                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {
                    G_Brightness_STDAns_Pos[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue + AGma_tolence))), 4) + Min_Brightness;
                    G_Brightness_STDAns_Nag[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue - AGma_tolence))), 4) + Min_Brightness;

                    chart1.Series[7].Points.AddXY(GrayScale, G_Brightness_STDAns_Pos[GrayScale]);//Chart1繪圖G_Gma_Upbound
                    chart1.Series[8].Points.AddXY(GrayScale, G_Brightness_STDAns_Nag[GrayScale]);//Chart1繪圖G_Gma_Lowbound                    


                    //實測亮度 比Gamma預期值誤差量 還要亮 超過Tolerance Spec.
                    if (G_Brightness_temp[GrayScale] > G_Brightness_STDAns_Pos[GrayScale])
                    {
                        if (Error_Flag_G == false)
                        { Error_Flag_G = true; }

                        textdata = "Fail 大於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(G_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + G_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 還要暗 低於過Tolerance Spec.
                    else if (G_Brightness_temp[GrayScale] < G_Brightness_STDAns_Nag[GrayScale])
                    {
                        if (Error_Flag_G == false)
                        { Error_Flag_G = true; }

                        textdata = "Fail 小於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(G_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + G_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 符合預期範圍
                    else
                    {
                        textdata = "Pass 合乎 Tolerance Sprc. @ GaryScal=" + G_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    Info_textBox.AppendText(textdata);
                }

                if (Error_Flag_G == false)
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Green;
                    AGma_RGBJudge_Label.Text = "G Gma:Pass";
                }
                else
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Red;
                    AGma_RGBJudge_Label.Text = "G Gma:Fail";
                }
            }
            if (B_CKBox.Checked == true)
            {
                Max_Brightness = 0;
                Min_Brightness = 1000;
                byte color_GrayScale = 0;
                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {

                    Application.DoEvents();
                    WhiskeyUtil.ImageFill(0, 0, color_GrayScale);
                    Thread.Sleep(300);
                    //B_Brightness_temp[GrayScale] = Math.Round(1023 * ((double)GrayScale / 260), 4);
                    B_Brightness_temp[GrayScale] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值
                    textdata = "B_Brightness[Gary=" + Convert.ToString(GrayScale) + "]=" + Convert.ToString(B_Brightness_temp[GrayScale]) + Environment.NewLine;

                    if (B_Brightness_temp[GrayScale] > Max_Brightness)
                    { Max_Brightness = B_Brightness_temp[GrayScale]; }
                    if (Min_Brightness > B_Brightness_temp[GrayScale])
                    { Min_Brightness = B_Brightness_temp[GrayScale]; }
                    Info_textBox.AppendText(textdata);

                    color_GrayScale++;
                    chart1.Series[9].Points.AddXY(GrayScale, B_Brightness_temp[GrayScale]);//Chart1繪圖B_Gma_Curve
                }

                maxSubmin = Max_Brightness - Min_Brightness;

                GMA_Set_comboBox.SelectedIndex = 0;

                for (uint GrayScale = 0; GrayScale < 256; GrayScale++)
                {
                    B_Brightness_STDAns_Pos[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue + AGma_tolence))), 4) + Min_Brightness;
                    B_Brightness_STDAns_Nag[GrayScale] = Math.Round(maxSubmin * ((float)Math.Pow(((double)GrayScale / 255), (GammaValue - AGma_tolence))), 4) + Min_Brightness;

                    chart1.Series[10].Points.AddXY(GrayScale, B_Brightness_STDAns_Pos[GrayScale]);//Chart1繪圖B_Gma_Upbound
                    chart1.Series[11].Points.AddXY(GrayScale, B_Brightness_STDAns_Nag[GrayScale]);//Chart1繪圖B_Gma_Lowbound                     

                    //實測亮度 比Gamma預期值誤差量 還要亮 超過Tolerance Spec.
                    if (B_Brightness_temp[GrayScale] > B_Brightness_STDAns_Pos[GrayScale])
                    {
                        if (Error_Flag_B == false)
                        { Error_Flag_B = true; }

                        textdata = "Fail 大於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(B_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + B_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 還要暗 低於過Tolerance Spec.
                    else if (B_Brightness_temp[GrayScale] < B_Brightness_STDAns_Nag[GrayScale])
                    {
                        if (Error_Flag_B == false)
                        { Error_Flag_B = true; }

                        textdata = "Fail 小於 Tolerance Sprc. @ GaryScal=" + Convert.ToString(GrayScale) + "  Spec:" + Convert.ToString(B_Brightness_STDAns_Pos[GrayScale]) + "  實測:" + B_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    //實測亮度 比Gamma預期值誤差量 符合預期範圍
                    else
                    {
                        textdata = "Pass 合乎 Tolerance Sprc. @ GaryScal=" + B_Brightness_temp[GrayScale] + Environment.NewLine;
                    }
                    Info_textBox.AppendText(textdata);
                }

                if (Error_Flag_B == false)
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Green;
                    AGma_RGBJudge_Label.Text = "B Gma:Pass";
                }
                else
                {
                    AGma_RGBJudge_Label.ForeColor = Color.Red;
                    AGma_RGBJudge_Label.Text = "B Gma:Fail";
                }
            }



            //STEP2:根據實測灰階 找出最亮與最暗的亮度值 並計算出標準曲線 與容許誤差範圍值 比較實測值是否超過容許誤差值

            //STEP3:繪圖顯示

            //STEP4:Label顯示量測結果
        }

        private void ShowImage_Reserve_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.MipiBridgeSelect(0x10); //Select 2828 Bank
            //WhiskeyUtil.ImageShow("VG.bmp");
            WhiskeyUtil.ImageShow("11.bmp");
        }

        private void button37_Click(object sender, EventArgs e)
        {
            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            WhiskeyUtil.ImageFill(128, 128, 128);
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void GammaTest1_Click(object sender, EventArgs e)
        {

            //VP_index = new int[29]  { 0, 1, 3, 5, 7, 9, 11, 13, 15,
            //        24, 32, 48, 64, 96, 128, 160, 192, 208, 224, 232,
            //        240, 242, 244, 246, 248, 250, 252, 254, 255};


            SL_WhiskyComm_Util WhiskeyUtil = new SL_WhiskyComm_Util();
            uint[] gammasetting = new uint[29];
            uint max_bright_index = 0;
            uint min_bright_index = 0;
            string textdata = null;
            double max_brightness = 0;
            double min_brightness = 0;
            uint pass_buffer =0;


            //步驟1 先找尋最大亮度與最暗亮度 
            //調整綁點頭尾 去找尋最大亮度 與最暗亮度 其他綁點固定使用初始值
            if (R_CKBox.Checked == true)
            {
                //Read Gamma Parameter Setting from Gamma Register to Tie_ParameterSettingt[0~28]
                ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = TieRegisterSetting[i];
                }

                //找出最亮綁點設定值
                textdata = "找出最亮綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                max_brightness = 0;
                pass_buffer = 0;
                for (uint i = 0; i < 300; i++)
                {
                    gammasetting[0] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(255, 0, 0);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    //textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    //Info_textBox.AppendText(textdata);


                    if (max_brightness < RGB_Brightness_save[i])
                    {
                        max_brightness = RGB_Brightness_save[i];
                        max_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }



                }
                pass_buffer = 0;
                gammasetting[0] = max_bright_index;
                textdata = "最亮綁點設定值為:" + Convert.ToString(max_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);


                //找出最暗綁點設定值
                textdata = "找出最暗綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                min_brightness = 10000;
                for (uint i = 1023; i >= 700; i--)
                {
                    gammasetting[28] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(0, 0, 0);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    //textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    //Info_textBox.AppendText(textdata);

                    if (min_brightness > RGB_Brightness_save[i])
                    {
                        min_brightness = RGB_Brightness_save[i];
                        min_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }


                }
                gammasetting[28] = min_bright_index;
                textdata = "最暗綁點設定值為:" + Convert.ToString(min_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);
                pass_buffer = 0;


                //推算每個綁點應該表現的亮度值
                double[] std_tie_brightness = new double[29]; //扣去最亮與最暗綁點不用調整 29-2=27
                double maxSubmin = max_brightness - min_brightness;

                std_tie_brightness[0] = max_brightness;
                std_tie_brightness[28] = min_brightness;
                for (uint i = 1; i <= 27; i++)
                {
                    std_tie_brightness[i] = Math.Round(maxSubmin * ((float)Math.Pow(((double)(255-VP_index[i]) / 255), 2.2)),4)+ min_brightness;
                }


                //開始針對每個綁點的標準答案亮度 去找最接近的綁點設定值
                uint Last_time_Index = max_bright_index+1;
                byte Gray_scale = 0;

                


                for (uint j = 1; j <= 27; j++)
                {
                    Gray_scale = Convert.ToByte(255 - VP_index[j]);

                    //STEP1:待測綁點 套入所有可能的Gamma電壓設定值 量測亮度表現 存放於 RGB_Brightness_save[i] 之中
                    double min = 10000;
                    uint index = Last_time_Index + 1;

                    for (uint i = Last_time_Index+1; i < min_bright_index; i++)
                    {
                        gammasetting[j] = i;
                        //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x31);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x32);

                        Application.DoEvents();

                        WhiskeyUtil.ImageFill(Gray_scale, 0, 0);
                        Thread.Sleep(300);
                        RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                        //textdata = "綁點" + Convert.ToString(VP_index[j]) + ":亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                        //Info_textBox.AppendText(textdata);

                        if (min > Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]))// && (RGB_Brightness_save[i] >= std_tie_brightness[j]))
                        {
                            min = Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]);
                            index = i;
                            pass_buffer = 0;
                        }
                        else
                        {
                            pass_buffer++;
                        }

                        if(pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                        {
                            break;
                        }
                    }

                    //STEP2:從存放的RGB_Brightness_save[i] 之中 找出最接近綁點標準答案的Gamma電壓設定值
                    //textdata = "綁點" + Convert.ToString(VP_index[j]) + ": 的最接近設定綁點值為:" + Convert.ToString(index) + "STD=" + Convert.ToString(std_tie_brightness[j]) + "實測為:" + Convert.ToString(RGB_Brightness_save[index]) + "\r\n";
                    //Info_textBox.AppendText(textdata);


                    //STEP3: 箝制綁點設定值 讓下次測試在上個亮度設定與最低亮度設定之間(由亮往暗調整)
                    gammasetting[j] = index;
                    Last_time_Index = index;

                }


                //秀出所有的綁點設定值
                //Info_textBox.Text = "";
                for (uint j = 0; j <= 28; j++)
                {
                    textdata = "R綁點"+ Convert.ToString(VP_index[j])+"的設定值="+Convert.ToString(gammasetting[j]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                pass_buffer = 0;
            }
            ///////////////////////////////////////////////
            if (G_CKBox.Checked == true)
            {
                //Read Gamma Parameter Setting from Gamma Register to Tie_ParameterSettingt[0~28]
                ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = TieRegisterSetting[i];
                }

                //找出最亮綁點設定值
                textdata = "找出最亮綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                max_brightness = 0;
                pass_buffer = 0;
                for (uint i = 0; i < 300; i++)
                {
                    gammasetting[0] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(0, 255, 0);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    //textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    //Info_textBox.AppendText(textdata);


                    if (max_brightness < RGB_Brightness_save[i])
                    {
                        max_brightness = RGB_Brightness_save[i];
                        max_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }



                }
                pass_buffer = 0;
                gammasetting[0] = max_bright_index;
                textdata = "最亮綁點設定值為:" + Convert.ToString(max_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);


                //找出最暗綁點設定值
                textdata = "找出最暗綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                min_brightness = 10000;
                for (uint i = 1023; i >= 700; i--)
                {
                    gammasetting[28] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(0, 0, 0);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    //textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    //Info_textBox.AppendText(textdata);

                    if (min_brightness > RGB_Brightness_save[i])
                    {
                        min_brightness = RGB_Brightness_save[i];
                        min_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }


                }
                gammasetting[28] = min_bright_index;
                textdata = "最暗綁點設定值為:" + Convert.ToString(min_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);
                pass_buffer = 0;


                //推算每個綁點應該表現的亮度值
                double[] std_tie_brightness = new double[29]; //扣去最亮與最暗綁點不用調整 29-2=27
                double maxSubmin = max_brightness - min_brightness;

                std_tie_brightness[0] = max_brightness;
                std_tie_brightness[28] = min_brightness;
                for (uint i = 1; i <= 27; i++)
                {
                    std_tie_brightness[i] = Math.Round(maxSubmin * ((float)Math.Pow(((double)(255 - VP_index[i]) / 255), 2.2)), 4) + min_brightness;
                }


                //開始針對每個綁點的標準答案亮度 去找最接近的綁點設定值
                uint Last_time_Index = max_bright_index + 1;
                byte Gray_scale = 0;




                for (uint j = 1; j <= 27; j++)
                {
                    Gray_scale = Convert.ToByte(255 - VP_index[j]);

                    //STEP1:待測綁點 套入所有可能的Gamma電壓設定值 量測亮度表現 存放於 RGB_Brightness_save[i] 之中
                    double min = 10000;
                    uint index = Last_time_Index + 1;

                    for (uint i = Last_time_Index + 1; i < min_bright_index; i++)
                    {
                        gammasetting[j] = i;
                        //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x33);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x34);

                        Application.DoEvents();

                        WhiskeyUtil.ImageFill(0, Gray_scale, 0);
                        Thread.Sleep(300);
                        RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                        //textdata = "綁點" + Convert.ToString(VP_index[j]) + ":亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                        //Info_textBox.AppendText(textdata);

                        if (min > Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]))
                        {
                            min = Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]);
                            index = i;
                            pass_buffer = 0;
                        }
                        else
                        {
                            pass_buffer++;
                        }

                        if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                        {
                            break;
                        }
                    }

                    //STEP2:從存放的RGB_Brightness_save[i] 之中 找出最接近綁點標準答案的Gamma電壓設定值
                    //textdata = "綁點" + Convert.ToString(VP_index[j]) + ": 的最接近設定綁點值為:" + Convert.ToString(index) + "STD=" + Convert.ToString(std_tie_brightness[j]) + "實測為:" + Convert.ToString(RGB_Brightness_save[index]) + "\r\n";
                    //Info_textBox.AppendText(textdata);


                    //STEP3: 箝制綁點設定值 讓下次測試在上個亮度設定與最低亮度設定之間(由亮往暗調整)
                    gammasetting[j] = index;
                    Last_time_Index = index;

                }


                //秀出所有的綁點設定值
                //Info_textBox.Text = "";
                for (uint j = 0; j <= 28; j++)
                {
                    textdata = "G綁點" + Convert.ToString(VP_index[j]) + "的設定值=" + Convert.ToString(gammasetting[j]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                pass_buffer = 0;
            }
            /////////////////////
            if (B_CKBox.Checked == true)
            {
                //Read Gamma Parameter Setting from Gamma Register to Tie_ParameterSettingt[0~28]
                ReadGammaSettingAll_from_SSD2130(TieRegisterSetting);
                for (uint i = 0; i < 29; i++)
                {
                    gammasetting[i] = TieRegisterSetting[i];
                }

                //找出最亮綁點設定值
                textdata = "找出最亮綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                max_brightness = 0;
                pass_buffer = 0;
                for (uint i = 0; i < 300; i++)
                {
                    gammasetting[0] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(0, 0, 255);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    Info_textBox.AppendText(textdata);


                    if (max_brightness < RGB_Brightness_save[i])
                    {
                        max_brightness = RGB_Brightness_save[i];
                        max_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }



                }
                pass_buffer = 0;
                gammasetting[0] = max_bright_index;
                textdata = "最亮綁點設定值為:" + Convert.ToString(max_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);


                //找出最暗綁點設定值
                textdata = "找出最暗綁點設定值" + "\r\n";
                Info_textBox.AppendText(textdata);
                min_brightness = 10000;
                for (uint i = 1023; i >= 700; i--)
                {
                    gammasetting[28] = i;
                    //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
                    WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);
                    Application.DoEvents();

                    WhiskeyUtil.ImageFill(0, 0, 0);
                    Thread.Sleep(300);
                    RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                    textdata = "亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                    Info_textBox.AppendText(textdata);

                    if (min_brightness > RGB_Brightness_save[i])
                    {
                        min_brightness = RGB_Brightness_save[i];
                        min_bright_index = i;
                        pass_buffer = 0;
                    }
                    else
                    {
                        pass_buffer++;
                    }

                    if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                    {
                        break;
                    }


                }
                gammasetting[28] = min_bright_index;
                textdata = "最暗綁點設定值為:" + Convert.ToString(min_bright_index) + "\r\n";
                Info_textBox.AppendText(textdata);
                pass_buffer = 0;


                //推算每個綁點應該表現的亮度值
                double[] std_tie_brightness = new double[29]; //扣去最亮與最暗綁點不用調整 29-2=27
                double maxSubmin = max_brightness - min_brightness;

                std_tie_brightness[0] = max_brightness;
                std_tie_brightness[28] = min_brightness;
                for (uint i = 1; i <= 27; i++)
                {
                    std_tie_brightness[i] = Math.Round(maxSubmin * ((float)Math.Pow(((double)(255 - VP_index[i]) / 255), 2.2)), 4) + min_brightness;
                }


                //開始針對每個綁點的標準答案亮度 去找最接近的綁點設定值
                uint Last_time_Index = max_bright_index + 1;
                byte Gray_scale = 0;




                for (uint j = 1; j <= 27; j++)
                {
                    Gray_scale = Convert.ToByte(255 - VP_index[j]);

                    //STEP1:待測綁點 套入所有可能的Gamma電壓設定值 量測亮度表現 存放於 RGB_Brightness_save[i] 之中
                    double min = 10000;
                    uint index = Last_time_Index + 1;

                    for (uint i = Last_time_Index + 1; i < min_bright_index; i++)
                    {
                        gammasetting[j] = i;
                        //WriteGammaSettingAlltheSame_to_SSD2130(gammasetting);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x35);
                        WriteGammaSettingAlltheSame_to_SSD2130_SetPage(gammasetting, 0x36);

                        Application.DoEvents();

                        WhiskeyUtil.ImageFill(0, 0, Gray_scale);
                        Thread.Sleep(300);
                        RGB_Brightness_save[i] = Math.Round(K80_Trigger_Measurement(1), 4);//K-80取得亮度表現值

                        textdata = "綁點" + Convert.ToString(VP_index[j]) + ":亮度值在" + Convert.ToString(i) + "時為:" + Convert.ToString(RGB_Brightness_save[i]) + "\r\n";
                        Info_textBox.AppendText(textdata);

                        if (min > Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]))
                        {
                            min = Math.Abs(RGB_Brightness_save[i] - std_tie_brightness[j]);
                            index = i;
                            pass_buffer = 0;
                        }
                        else
                        {
                            pass_buffer++;
                        }

                        if (pass_buffer >= 10)//表示連續10個都呈現沒有最靠近標準答案值的狀況發生 表示偏離趨勢 可以跳出迴圈了
                        {
                            break;
                        }
                    }

                    //STEP2:從存放的RGB_Brightness_save[i] 之中 找出最接近綁點標準答案的Gamma電壓設定值
                    textdata = "綁點" + Convert.ToString(VP_index[j]) + ": 的最接近設定綁點值為:" + Convert.ToString(index) + "STD=" + Convert.ToString(std_tie_brightness[j]) + "實測為:" + Convert.ToString(RGB_Brightness_save[index]) + "\r\n";
                    Info_textBox.AppendText(textdata);


                    //STEP3: 箝制綁點設定值 讓下次測試在上個亮度設定與最低亮度設定之間(由亮往暗調整)
                    gammasetting[j] = index;
                    Last_time_Index = index;

                }


                //秀出所有的綁點設定值
                //Info_textBox.Text = "";
                for (uint j = 0; j <= 28; j++)
                {
                    textdata = "B綁點" + Convert.ToString(VP_index[j]) + "的設定值=" + Convert.ToString(gammasetting[j]) + "\r\n";
                    Info_textBox.AppendText(textdata);
                }
                pass_buffer = 0;
            }
        }
    }
}
