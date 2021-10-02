using NVLenovoController.Features;
using OpenHardwareMonitor.Hardware;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;

namespace NVLenovoController
{
    public partial class main_app : Form
    {
        public main_app()
        {

            InitializeComponent();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

        }
        private readonly RadioButton[] _alwaysOnUsbButtons;
        private readonly AlwaysOnUsbFeature _alwaysOnUsbFeature = new AlwaysOnUsbFeature();
        private readonly RadioButton[] _batteryButtons;
        private readonly BatteryFeature _batteryFeature = new BatteryFeature();
        private readonly RadioButton[] _powerModeButtons;
        private readonly PowerModeFeature _powerModeFeature = new PowerModeFeature();
        private readonly FnLockFeature _fnLockFeature = new FnLockFeature();
        private readonly OverDriveFeature _overDriveFeature = new OverDriveFeature();
        private readonly TouchpadLockFeature _touchpadLockFeature = new TouchpadLockFeature();
        private class FeatureCheck
        {
            private readonly Action _check;
            private readonly Action _disable;

            internal FeatureCheck(Action check, Action disable)
            {
                _check = check;
                _disable = disable;
            }

            internal void Check() => _check();
            internal void Disable() => _disable();
        }

        int lastbrightness = power_manager.beforeBrightness();
        UpdateVisitor updateVisitor = new UpdateVisitor();
        Computer computer = new Computer();


        private void main_app_Load(object sender, EventArgs e)
        {
            if (!Program.IsAdministrator())
            {
                // Restart and run as admin
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                startInfo.Arguments = "restart";
                Process.Start(startInfo);
                Application.Exit();
            }

            computer.Open();
            computer.CPUEnabled = true;
            computer.GPUEnabled = true;
            computer.RAMEnabled = true;
            computer.Accept(updateVisitor);

            this.FormClosing += Main_app_FormClosing;
            DisplayDnsAddresses();
            var guidPlans = power_manager.GetAll();

            foreach (Guid guidPlan in guidPlans)
            {
                comboBox1.Items.Add(power_manager.ReadFriendlyName(guidPlan));
                comboBox2.Items.Add(guidPlan);
            }
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            mainbrightnesslevel.Value = lastbrightness;

            foreach (var item in Power_tab.Controls)
            {
                if (item is Panel)
                {
                    ((Panel)item).Left = ((tabControl1.Width - ((Panel)item).Width) / 2) + 5;
                }
            }
            try
            {
                foreach (var item in Properties.Settings.Default.dnsList.Split('|'))
                {
                    if (item != "")
                    {
                        RadioButton dns = new RadioButton();
                        dns.Text = item.Split('>')[1];
                        dns.Tag = item.Split('_')[0] + "," + item.Split('_')[1].Substring(0, item.Split('_')[1].IndexOf('>'));
                        dns.CheckedChanged += radioButton1_CheckedChanged;
                        dns.AutoSize = true;
                        panel_dnsholder.Controls.Add(dns);
                    }
                }
            }
            catch (Exception)
            {
            }
            minimize_Click(null,null);
            Control[] toRound = { panel_powerplans, panel_powermode, panel_thermalmode, keyboard_panel, panel_dnssettings, panel2};
            helper.roundAll(toRound, 25);
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    rk.SetValue("NVLenovo Controller", Application.ExecutablePath);
        }
        bool is_userClicked = false;
        private void Main_app_FormClosing(object sender, FormClosingEventArgs e)
        {
            computer.Close();
            if (is_userClicked == false)
            {
                var exeName = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                startInfo.Arguments = "restart";
                Process.Start(startInfo);
                Application.Exit();
            }
        }

        private void battry_timer_Tick(object sender, EventArgs e)
        {
            int battery_level = (power_manager.GetChargeValue());
            new Thread(() =>
            {
                GetSystemInfo();
            }).Start();

            if (power_manager.IsCharging())
            {
                iconPictureBox2.IconChar = FontAwesome.Sharp.IconChar.ChargingStation;
            }
            else
            {
                iconPictureBox2.IconChar = FontAwesome.Sharp.IconChar.None;
            }
            if (battery_level >= 85)
            {
                iconPictureBox1.IconChar = FontAwesome.Sharp.IconChar.BatteryFull;
                return;
            }
            else if (battery_level >= 50)
            {
                iconPictureBox1.IconChar = FontAwesome.Sharp.IconChar.BatteryHalf;
                return;
            }
            else if (battery_level >= 21)
            {
                iconPictureBox1.IconChar = FontAwesome.Sharp.IconChar.BatteryQuarter;
                return;
            }
            else if (battery_level <= 20)
            {
                iconPictureBox1.IconChar = FontAwesome.Sharp.IconChar.BatteryEmpty;
                return;
            }
        }
        public static NetworkInterface GetActiveEthernetOrWifiNetworkInterface()
        {
            var Nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));

            return Nic;
        }
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
        string detectType(string type, string value)
        {
            if (type == "Temperature")
            {
                return value + "C";
            }
            if (type == "Clock")
            {
                try
                {
                    float temp = float.Parse(value);
                    if (temp < 1000)
                    {
                        return (temp.ToString("F1") + " MHZ");
                    }
                    else
                    {
                        temp = temp / 1000;
                        return (temp.ToString("F1") + " GHZ");
                    }
                }
                catch
                {
                }
            }
            if (type == "Control" || type == "Load")
            {
                try
                {
                    float temp = float.Parse(value);

                    return (temp.ToString("F0") + " %");
                }
                catch
                {
                }
            }
            if (type == "Voltage")
            {
                try
                {
                    float temp = float.Parse(value);
                    return (temp.ToString("F1") + " V");
                }
                catch
                {
                }
            }
            if (type == "Fan")
            {
                try
                {
                    float rpm = float.Parse(value);
                    return (rpm.ToString("F0") + " RPM");
                }
                catch
                {
                }
            }
            if (type == "Power")
            {
                try
                {
                    float rpm = float.Parse(value);
                    return (rpm.ToString("F0") + " W");
                }
                catch
                {
                }
            }
            if (type == "Data")
            {
                try
                {
                    float temp = float.Parse(value) * 1024;
                    if (temp < 1024)
                    {
                        return (temp.ToString("F1") + " MB");
                    }
                    else
                    {
                        temp = temp / 1024;
                        return (temp.ToString("F1") + " GB");
                    }
                }
                catch
                {
                }
            }
            if (type == "SmallData")
            {
                try
                {
                    float temp = float.Parse(value);
                    if (temp < 1024)
                    {
                        return (temp.ToString("F1") + " MB");
                    }
                    else
                    {
                        temp = temp / 1024;
                        return (temp.ToString("F1") + " GB");
                    }
                }
                catch
                {
                }
            }
            return value;

        }
        string system_info = "", gpu_info = "";
        private void setlabel()
        {
            label4.Text = system_info;
            label3.Text = gpu_info.Trim();
        }
        public Bitmap ConvertTextToImage(string txt, string fontname, int fontsize, Color bgcolor, Color fcolor, int width, int Height)
        {
            Bitmap bmp = new Bitmap(width, Height);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {

                Font font = new Font(fontname, fontsize);
                graphics.FillRectangle(new SolidBrush(bgcolor), 0, 0, bmp.Width, bmp.Height);
                graphics.DrawString(txt, font, new SolidBrush(fcolor), 0, 0);
                graphics.Flush();
                font.Dispose();
                graphics.Dispose();


            }
            return bmp;
        }
        Icon icon;
        int cpuMaxTemp = 0, cpuMinTemp, gpuMaxTemp = 0, gpuMinTemp;
        float avgCPU = 0, avgGPU = 0;
        public delegate void UpdateControlsDelegate();
        public void GetSystemInfo()
        {
            gpu_info = ""; system_info = "";
            computer.Accept(updateVisitor);

            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    system_info += "------- CPU -------";
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (!(computer.Hardware[i].Sensors[j].SensorType.ToString() == "Power" && computer.Hardware[i].Sensors[j].Name.Contains("Core")))
                        {
                            system_info += "\n\r" + computer.Hardware[i].Sensors[j].Name + ":    " + detectType(computer.Hardware[i].Sensors[j].SensorType.ToString(), (computer.Hardware[i].Sensors[j].Value).ToString());
                            if (computer.Hardware[i].Sensors[j].Name == "CPU Package" && computer.Hardware[i].Sensors[j].SensorType.ToString() == "Temperature")
                            {
                                if (cputrayTemp.Checked)
                                {
                                    icon = Icon.FromHandle(ConvertTextToImage(((int)computer.Hardware[i].Sensors[j].Value).ToString() + "\r\n████", "RDR Lino", 20, Color.Transparent, Color.Red, 45, 45).GetHicon());
                                    cpuTemp.Icon = icon;

                                    if ((int)computer.Hardware[i].Sensors[j].Value > cpuMaxTemp)
                                    {
                                        cpuMaxTemp = (int)computer.Hardware[i].Sensors[j].Value;
                                    }
                                    if (cpuMinTemp == 0 || (int)computer.Hardware[i].Sensors[j].Value < cpuMinTemp)
                                    {
                                        cpuMinTemp = (int)computer.Hardware[i].Sensors[j].Value;
                                    }
                                    if (avgCPU == 0)
                                    {
                                        avgCPU = (int)computer.Hardware[i].Sensors[j].Value;
                                    }
                                    else
                                    {
                                        avgCPU = (avgCPU + (int)computer.Hardware[i].Sensors[j].Value) / 2;
                                    }
                                    cpuTemp.Text = string.Format("CPU Temp: {2}C\r\nCPU Max: {0}C\r\nCPU Min: {1}C\r\nAvg: {3}C", cpuMaxTemp, cpuMinTemp, (int)computer.Hardware[i].Sensors[j].Value, avgCPU.ToString("0.00"));
                                }
                                else
                                {
                                    cpuTemp.Icon = null;
                                }
                            }
                        }

                    }

                }

                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    gpu_info += "\n\r\n\r------- GPU Nvidia -------";
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        gpu_info += "\n\r" + computer.Hardware[i].Sensors[j].Name + ":   " + detectType(computer.Hardware[i].Sensors[j].SensorType.ToString(), (computer.Hardware[i].Sensors[j].Value).ToString());
                        if (computer.Hardware[i].Sensors[j].SensorType.ToString() == "Temperature")
                        {
                            if (gputrayTemp.Checked)
                            {
                                Icon icon = Icon.FromHandle(ConvertTextToImage(((int)computer.Hardware[i].Sensors[j].Value).ToString() + "\r\n████", "RDR Lino", 20, Color.Transparent, Color.Green, 45, 45).GetHicon());
                                gpuTemp.Icon = icon;

                                if ((int)computer.Hardware[i].Sensors[j].Value > gpuMaxTemp)
                                {
                                    gpuMaxTemp = (int)computer.Hardware[i].Sensors[j].Value;
                                }
                                if (gpuMinTemp == 0 || (int)computer.Hardware[i].Sensors[j].Value < gpuMinTemp)
                                {
                                    gpuMinTemp = (int)computer.Hardware[i].Sensors[j].Value;
                                }
                                if (avgGPU == 0)
                                {
                                    avgGPU = (int)computer.Hardware[i].Sensors[j].Value;
                                }
                                else
                                {
                                    avgGPU = (avgGPU + (int)computer.Hardware[i].Sensors[j].Value) / 2;
                                }

                                gpuTemp.Text = string.Format("GPU Temp: {2}C\r\nGPU Max: {0}C\r\nGPU Min: {1}C\r\nAvg: {3}C", gpuMaxTemp, gpuMinTemp, (int)computer.Hardware[i].Sensors[j].Value, avgGPU.ToString("0.00"));
                            }
                            else
                            {
                                gpuTemp.Icon = null;
                            }
                        }

                    }

                }

                if (computer.Hardware[i].HardwareType == HardwareType.GpuAti)
                {
                    gpu_info += "\n\r\n\r------- GPU AMD -------";
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        gpu_info += "\n\r" + computer.Hardware[i].Sensors[j].Name + ":   " + detectType(computer.Hardware[i].Sensors[j].SensorType.ToString(), (computer.Hardware[i].Sensors[j].Value).ToString());
                    }

                }

                if (computer.Hardware[i].HardwareType == HardwareType.RAM)
                {
                    system_info += "\n\r\n\r------- RAM -------";
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        system_info += "\n\r" + computer.Hardware[i].Sensors[j].Name + ":   " + detectType(computer.Hardware[i].Sensors[j].SensorType.ToString(), (computer.Hardware[i].Sensors[j].Value).ToString());
                    }
                }
            }
            if (this.InvokeRequired)
            {
                this.Invoke(new UpdateControlsDelegate(setlabel));
            }
            else
            {
                setlabel();
            }
        }
        public void setDNS(string DNS)
        {
            string[] dnss = DNS.Split(',');
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    excCMD("/C netsh interface ipv4 set dns name=\"" + ni.Name + "\" static " + dnss[0].Trim() + " & " + "netsh interface ipv4 add dns name=\"" + ni.Name + "\" " + dnss[1].Trim() + " index=2 & ipconfig /flushdns & ipconfig /registerdns & ipconfig /release & ipconfig /renew");
                }
            }
        }

        public void DisplayDnsAddresses()
        {
            listBox1.Items.Clear();
            foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
            {
                listBox1.Items.Add(n.Name);
            }
            listBox1.SelectedIndex = 0;
        }

        private void btn_set_plan_Click(object sender, EventArgs e)
        {
            try
            {
                byte selectedPlan = (byte)comboBox1.SelectedIndex;
                Guid selectedplan = new Guid(comboBox2.Items[comboBox1.SelectedIndex].ToString());
                power_manager.PowerSetActiveScheme(IntPtr.Zero, ref selectedplan);

                if (autorestore.Checked)
                    power_manager.SetBrightness((byte)mainbrightnesslevel.Value);
            }
            catch (Exception)
            {


            }

        }

        private void btn_control_Panel_Click(object sender, EventArgs e)
        {
            var root = Environment.GetEnvironmentVariable("SystemRoot");
            Process.Start(root + "\\system32\\control.exe", "/name Microsoft.PowerOptions");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string newDsns = "", DNSNAME = "";
            foreach (var item in panel_dnsholder.Controls)
            {
                if (item is RadioButton)
                {
                    if (((RadioButton)item).Checked)
                    {

                        newDsns = ((RadioButton)item).Tag.ToString();
                        DNSNAME = ((RadioButton)item).Text;
                    }
                }

            }
            setDNS(newDsns);
            MessageBox.Show(DNSNAME + " DNS Applied");
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            string newDsns = ((RadioButton)sender).Tag.ToString();
            textBox1.Text = newDsns.Split(',')[0];
            textBox2.Text = newDsns.Split(',')[1].Trim();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            power_manager.SetBrightness((byte)mainbrightnesslevel.Value);
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            power_manager.SetBrightnessAlt((short)overlaybrightness.Value);

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                overlaybrightness.Enabled = false;
            }
            else
            {
                overlaybrightness.Enabled = true;
            }
        }

        private void set_performance_Click(object sender, EventArgs e)
        {
            _powerModeFeature.SetState(PowerModeState.Performance);

        }

        private void set_balance_Click(object sender, EventArgs e)
        {
            _powerModeFeature.SetState(PowerModeState.Balance);
        }

        private void set_quiet_Click(object sender, EventArgs e)
        {
            _powerModeFeature.SetState(PowerModeState.Quiet);

        }

        private void enb_touch_Click(object sender, EventArgs e)
        {
            _touchpadLockFeature.SetState(TouchpadLockState.Off);
        }

        private void dis_touch_Click(object sender, EventArgs e)
        {
            _touchpadLockFeature.SetState(TouchpadLockState.On);

        }

        private void enb_fnl_Click(object sender, EventArgs e)
        {
            _fnLockFeature.SetState(FnLockState.On);
        }

        private void dis_fnl_Click(object sender, EventArgs e)
        {
            _fnLockFeature.SetState(FnLockState.Off);
        }

        private void conser_Click(object sender, EventArgs e)
        {
            _batteryFeature.SetState(BatteryState.Conservation);
        }

        private void rapidcharge_Click(object sender, EventArgs e)
        {
            _batteryFeature.SetState(BatteryState.RapidCharge);

        }

        private void normal_battry_Click(object sender, EventArgs e)
        {
            _batteryFeature.SetState(BatteryState.Normal);

        }

        private void iconPictureBox4_Click(object sender, EventArgs e)
        {

        }

        static void Enable(string interfaceName)
        {
            System.Diagnostics.ProcessStartInfo psi =
                   new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" ENABLED");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();
        }

        static void Disable(string interfaceName)
        {
            System.Diagnostics.ProcessStartInfo psi =
                new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" DISABLED");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Disable("Wi-Fi");
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Enable("Wi-Fi");
            timer1.Enabled = false;
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            is_userClicked = true;
            this.Close();
            Application.Exit();
        }

        private void mbtn_power_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }

        private void mbtn_internet_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void mbtn_device_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
        }

        private void add_tolist_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                if (textBox2.Text != "")
                {
                    if (dnsName.Text != "")
                    {
                        IPAddress ipAddress;
                        if (IPAddress.TryParse(textBox1.Text, out ipAddress))
                        {
                            if (IPAddress.TryParse(textBox2.Text, out ipAddress))
                            {
                                NVLenovoController.Properties.Settings.Default.dnsList += textBox1.Text + "_" + textBox2.Text + ">" + dnsName.Text + "|";
                                NVLenovoController.Properties.Settings.Default.Save();

                                RadioButton dns = new RadioButton();
                                dns.Text = dnsName.Text;
                                dns.Tag = textBox1.Text + "," + textBox2.Text;
                                dns.CheckedChanged += radioButton1_CheckedChanged;
                                panel_dnsholder.Controls.Add(dns);
                            }
                            else
                                MessageBox.Show("Invalid IP address [box 2]");
                        }
                        else
                            MessageBox.Show("Invalid IP address [box 1]");
                    }
                    else
                        MessageBox.Show("DNS Name is empty");
                }
                else
                    MessageBox.Show("DNS Box 2 is empty");
            }
            else
                MessageBox.Show("DNS Box 1 is empty");
        }

        private void boostsetting_active_Click(object sender, EventArgs e)
        {
            excCMD("/C powercfg.exe -attributes sub_processor perfboostmode -attrib_hide");
            MessageBox.Show("Processor Boost settings activated!");
        }

        private void minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;

                return cp;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void gpulogging_temp_Click(object sender, EventArgs e)
        {
            cputrayTemp.Checked = !cputrayTemp.Checked;
        }

        private void gputemp_logging_Click(object sender, EventArgs e)
        {
            gputrayTemp.Checked = !gputrayTemp.Checked;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start("netsh", "interface set interface \"" + listBox1.SelectedItem.ToString() + "\" enable");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("netsh", "interface set interface \"" + listBox1.SelectedItem.ToString() + "\" disable");
        }
        void excCMD(string commend)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = commend;
            startInfo.Verb = "runas";
            process.StartInfo = startInfo;
            process.Start();
        }
        private void shutdown_in_Click(object sender, EventArgs e)
        {
            excCMD(string.Format("/C shutdown /s /t {0}", Convert.ToInt32(power_min.Text) * 60));
        }
    }
}
