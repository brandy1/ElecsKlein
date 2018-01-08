using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace SL_Tek_Studio_Pro
{
    /*
     * List Usb Device and get the usb devices according to my settings.
     * Date:2016/10/04
     */
    class SL_Device_Util
    {
        private  List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
        private const string DEVICE_3R = "3R";
        private const string DEVICE_SC = "SC";
        private const string USBVID = "VID_";
        private const string USBPID = "PID_";
        private string Vid = null, Pid = null;
        public int  GetUSBDevices()
        {
            ManagementObjectCollection collection;

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity "))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices.Count;
        }

        public List<ScDeviceInfo> FindScDevice(string UserDevice)
        {
            List<ScDeviceInfo> Devices = new List<ScDeviceInfo>();

            foreach (USBDeviceInfo deviceinfo in devices)
            {
                if (deviceinfo.Description != null &&
                    (deviceinfo.Description.Contains(DEVICE_3R) || 
                     deviceinfo.Description.Contains(DEVICE_SC) ||
                     deviceinfo.Description.Contains(UserDevice)))
                {
                    Devices.Add(new ScDeviceInfo(deviceinfo.Description, deviceinfo.DeviceID));
                }
            }
            return Devices;
        }

        public List<ScDeviceInfo>  FindScDevice()
        {
            List<ScDeviceInfo> Devices = new List<ScDeviceInfo>();

            foreach(USBDeviceInfo deviceinfo in devices)
            {
                if(deviceinfo.Description!= null &&
                    (deviceinfo.Description.Contains(DEVICE_3R) ||
                    deviceinfo.Description.Contains(DEVICE_SC)))
                {
                    Devices.Add(new ScDeviceInfo(deviceinfo.Description,deviceinfo.DeviceID));
                }
            }         
            return Devices;
        }

        public bool DeviceCompare(ScDeviceInfo[] SysDevice , ScDeviceInfo[] TimerDevice)
        {
            if (SysDevice.Length != TimerDevice.Length) return false;
            for(int i = 0;i<SysDevice.Length;i++)
            {
                if (SysDevice[i].Description != TimerDevice[i].Description) return false;
                if (SysDevice[i].DeviceID != TimerDevice[i].DeviceID) return false;
            }
            return true;
        }


        public bool getDeviceItem(string devStr)
        {
            int VidAddr = devStr.IndexOf(USBVID, 0);
            int PidAddr = devStr.IndexOf(USBPID, 0);
            if(VidAddr > 0 && PidAddr >0)
            {
                this.Vid = devStr.Substring(VidAddr+4, 4);
                this.Pid = devStr.Substring(PidAddr+4, 4);
                return true;
            }
            return false;
        }

        public int getShortVid() { return ushort.Parse(this.Vid,System.Globalization.NumberStyles.HexNumber);  }
        public int getShortPid() { return ushort.Parse(this.Pid, System.Globalization.NumberStyles.HexNumber); }
        public string getStrVid() { return this.Vid; }
        public string getStrPid() { return this.Pid; }
        public string getRootDevInfo(string devInfo) { return devInfo.Substring(10, devInfo.Length - 10); }

        public class ScDeviceInfo
        {
            public ScDeviceInfo(string deviceID, string description)
            {
                this.DeviceID = deviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string Description { get; private set; }
        }

        private class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
        }
    }
}
