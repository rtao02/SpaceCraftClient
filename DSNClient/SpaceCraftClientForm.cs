using System;
using System.Windows.Forms;
using System.ServiceModel;
using DSNInterfaces;
using System.ComponentModel;
using System.Threading;

namespace SpaceCraftsClient
{
    public partial class SpaceCraftsClientForm : Form
    {
        static int _totalNumber = 9;
        static BindingList<SpaceCraft> spacecraftList_client = new BindingList<SpaceCraft>();
        static Thread[] launchThread = new Thread[_totalNumber];
        static Thread[] lvTelemetry = new Thread[_totalNumber];
        static Thread[] plData = new Thread[_totalNumber];
        static Thread[] plTelemetry = new Thread[_totalNumber];
        IDSNService service;
        public SpaceCraftsClientForm()
        {
            InitializeComponent();
        }
        public void SpaceCraftsClientForm_Load(object sender, EventArgs e)
        {
            //BindingList<SpaceCraft> spacecraftList_client = new BindingList<SpaceCraft>();
            for (int i = 1; i <= _totalNumber; i++)
            {
                SpaceCraft temp = SpaceCraft.CreateNewSC(i);
                spacecraftList_client.Add(temp);
                //launchThread[i - 1] = new Thread(() => LaunchSC(i));
                //lvTelemetry[i - 1] = new Thread(()=> LvStartTele(i));
            }
            var callback = new DSNCallback();
            var context = new InstanceContext(callback);
            var pipeFactory =
                 new DuplexChannelFactory<IDSNService>(context,
                 new NetNamedPipeBinding(),
                 new EndpointAddress("net.pipe://localhost/DSN"));

            service = pipeFactory.CreateChannel();

            service.Connect();
        }
        
        static void LaunchSC(int i)
        {
            if (spacecraftList_client[i - 1].Vehicle_Stat == "Waiting for launching")
            {
                spacecraftList_client[i - 1].Vehicle_Stat = "Raising";
                int t = (spacecraftList_client[i - 1].Orbit_Info / 3600) + 10;//time in s
                int v = spacecraftList_client[i - 1].Orbit_Info / t;//speed in km/s
                while (spacecraftList_client[i - 1].Vehicle_Altitude1 < spacecraftList_client[i - 1].Orbit_Info && spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1 > 0) 
                {
                    Thread.Sleep(1000); //sleep 1s
                    Random rnd = new Random();
                    int tempNum = rnd.Next(-90, 90);
                    int tempNum1 = rnd.Next(-180, 180);
                    //updata real altitudes
                    spacecraftList_client[i - 1].Vehicle_Altitude1 += v;
                    spacecraftList_client[i - 1].Payload_Altitude1 = spacecraftList_client[i - 1].Vehicle_Altitude1;
                    //update real longtitude and latitude(randomly update)
                    spacecraftList_client[i - 1].Vehicle_Latitude1 = tempNum;
                    spacecraftList_client[i - 1].Payload_Latitude1 = spacecraftList_client[i - 1].Vehicle_Latitude1;
                    spacecraftList_client[i - 1].Vehicle_Longitude1 = tempNum1;
                    spacecraftList_client[i - 1].Payload_Longitude1 = spacecraftList_client[i - 1].Vehicle_Longitude1;
                    //update temp(reduce 10F per second)
                    spacecraftList_client[i - 1].Vehicle_Temperature1 -= 10;
                    spacecraftList_client[i - 1].Payload_Temperature1 = spacecraftList_client[i - 1].Vehicle_Temperature1;
                    //updata time to orbit
                    spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1 -= 1;
                    spacecraftList_client[i - 1].Payload_Time_to_Orbit1 -= 1;
                }
                //spacecraft final ajust(assume current latitude and longtitude will not changed in ordit)
                spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1 = 0;
                spacecraftList_client[i - 1].Payload_Time_to_Orbit1 = 0;
                spacecraftList_client[i - 1].Vehicle_Altitude1 = spacecraftList_client[i - 1].Orbit_Info;
                spacecraftList_client[i - 1].Payload_Altitude1 = spacecraftList_client[i - 1].Orbit_Info;
                spacecraftList_client[i - 1].Vehicle_Stat = "In orbit(with Payload)";
                spacecraftList_client[i - 1].Payload_Stat = "Ready to deploy";
            }
            else
            {
                MessageBox.Show("Invalid CMD");
                return;
            }
            return;
        }
        static void LvStartTele(int i)
        {
            while (true)//update telemetry every 1s
            {
                if (spacecraftList_client[i - 1].Vehicle_Stat == "Waiting for launching" || spacecraftList_client[i - 1].Vehicle_Stat == "Raising") {
                    spacecraftList_client[i - 1].Vehicle_Altitude = spacecraftList_client[i - 1].Vehicle_Altitude1;
                    spacecraftList_client[i - 1].Vehicle_Longitude = spacecraftList_client[i - 1].Vehicle_Longitude1;
                    spacecraftList_client[i - 1].Vehicle_Latitude = spacecraftList_client[i - 1].Vehicle_Latitude1;
                    spacecraftList_client[i - 1].Vehicle_Temperature = spacecraftList_client[i - 1].Vehicle_Temperature1;
                    spacecraftList_client[i - 1].Vehicle_Time_to_Orbit = spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1;
                }
                else if(spacecraftList_client[i - 1].Vehicle_Stat == "In orbit(with Payload)" || spacecraftList_client[i - 1].Vehicle_Stat == "In orbit(No Payload)")
                {
                    Random rnd = new Random();
                    int tempNum = rnd.Next(-90, 90);
                    int tempNum1 = rnd.Next(-180, 180);
                    spacecraftList_client[i - 1].Vehicle_Altitude = spacecraftList_client[i - 1].Vehicle_Altitude1;
                    spacecraftList_client[i - 1].Vehicle_Latitude = tempNum1;
                    spacecraftList_client[i - 1].Vehicle_Longitude = tempNum;
                    spacecraftList_client[i - 1].Vehicle_Temperature = spacecraftList_client[i - 1].Vehicle_Temperature1;
                    spacecraftList_client[i - 1].Vehicle_Time_to_Orbit = spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1;
                }
                else//{de-orbited}
                {
                    MessageBox.Show("Invalid CMD(Vehicle finished its job, Thank you!)");
                    return;
                }
                Thread.Sleep(1000);
            }
        }
        static void PlStartData(int i)
        {
            if(spacecraftList_client[i - 1].Payload_Stat == "Undeployed")
            {
                MessageBox.Show("Payload - Undeployed");
                return;
            }
            else if (spacecraftList_client[i - 1].Payload_Stat == "Ended")
            {
                MessageBox.Show("Invalid CMD(Payload finished its job, Thank you!)");
                return;
            }
            if (spacecraftList_client[i - 1].PayloadType == "Scientific")//1min
            {
                while (true)
                {
                    Random rnd1 = new Random();
                    spacecraftList_client[i - 1].PayloadData = "%Rain: " + rnd1.Next(100).ToString() + " %Humidity: " + rnd1.Next(100).ToString() + " %Snow: " + rnd1.Next(100).ToString();
                    spacecraftList_client[i - 1].PayloadData1 = spacecraftList_client[i - 1].PayloadData;
                    Thread.Sleep(60000);
                }
            }
            else if (spacecraftList_client[i - 1].PayloadType == "Communication")//5s
            {
                while (true)
                {
                    Random rnd1 = new Random();
                    spacecraftList_client[i - 1].PayloadData = "Uplink: " + rnd1.Next(100).ToString() + "Mbps" + " Downlink: " + rnd1.Next(50).ToString() + "Mbps";
                    spacecraftList_client[i - 1].PayloadData1 = spacecraftList_client[i - 1].PayloadData;
                    Thread.Sleep(5000);
                }
            }
            else if (spacecraftList_client[i - 1].PayloadType == "Spy")//10s
            {
                while (true)
                {
                    Random rnd1 = new Random();
                    spacecraftList_client[i - 1].PayloadData = "Image" + rnd1.Next(200).ToString() + ".jpg";
                    spacecraftList_client[i - 1].PayloadData1 = spacecraftList_client[i - 1].PayloadData;
                    Thread.Sleep(10000);
                }
            }
            return;
        }
        static void PlStartTele(int i)
        {
            while (true)
            {
                if (spacecraftList_client[i - 1].Payload_Stat == "Deployed" || spacecraftList_client[i - 1].Payload_Stat == "Undeployed")
                {
                    if (spacecraftList_client[i - 1].Vehicle_Stat == "Waiting for launching" || spacecraftList_client[i - 1].Vehicle_Stat == "Raising")
                    {
                        spacecraftList_client[i - 1].Payload_Altitude = spacecraftList_client[i - 1].Vehicle_Altitude1;
                        spacecraftList_client[i - 1].Payload_Longitude = spacecraftList_client[i - 1].Vehicle_Longitude1;
                        spacecraftList_client[i - 1].Payload_Latitude = spacecraftList_client[i - 1].Vehicle_Latitude1;
                        spacecraftList_client[i - 1].Payload_Temperature = spacecraftList_client[i - 1].Vehicle_Temperature1;
                        spacecraftList_client[i - 1].Payload_Time_to_Orbit = spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1;

                        spacecraftList_client[i - 1].Payload_Altitude1 = spacecraftList_client[i - 1].Vehicle_Altitude1;
                        spacecraftList_client[i - 1].Payload_Longitude1 = spacecraftList_client[i - 1].Vehicle_Longitude1;
                        spacecraftList_client[i - 1].Payload_Latitude1 = spacecraftList_client[i - 1].Vehicle_Latitude1;
                        spacecraftList_client[i - 1].Payload_Temperature1 = spacecraftList_client[i - 1].Vehicle_Temperature1;
                        spacecraftList_client[i - 1].Payload_Time_to_Orbit1 = spacecraftList_client[i - 1].Vehicle_Time_to_Orbit1;
                    }
                    else
                    {
                        Random rnd = new Random();
                        int tempNum = rnd.Next(-90, 90);
                        int tempNum1 = rnd.Next(-180, 180);
                        spacecraftList_client[i - 1].Payload_Altitude = spacecraftList_client[i - 1].Payload_Altitude1;
                        spacecraftList_client[i - 1].Payload_Latitude = tempNum1;
                        spacecraftList_client[i - 1].Payload_Longitude = tempNum;
                        spacecraftList_client[i - 1].Payload_Latitude1 = tempNum1;
                        spacecraftList_client[i - 1].Payload_Longitude1 = tempNum;
                        spacecraftList_client[i - 1].Payload_Temperature = spacecraftList_client[i - 1].Payload_Temperature1;
                        spacecraftList_client[i - 1].Payload_Time_to_Orbit = spacecraftList_client[i - 1].Payload_Time_to_Orbit1;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid CMD(Payload finished its job, Thank you!)");
                    return;
                }
            }
        }
        static public void GetCMD(string inputCMD) {
            string[] phrase = inputCMD.Split('_');
            if(phrase.Length != 2 && phrase.Length != 3)
            {
                MessageBox.Show("Invalid CMD");
                return;
            }
            int SC_index = 0;
            if(phrase.Length == 2)
            {
                if (phrase[0].Length == 3 && phrase[0][2] - '0' >= 1 && phrase[0][2] - '0' <= _totalNumber)
                {
                    SC_index = phrase[0][2] - '0';
                }
                else
                {
                    MessageBox.Show("Invalid CMD");
                    return;
                }
                if(phrase[1] == "Launch")
                {
                    launchThread[SC_index - 1] = new Thread(() => LaunchSC(SC_index));
                    launchThread[SC_index - 1].Start();
                }
                else
                {
                    MessageBox.Show("Invalid CMD");
                    return;
                }
            }
            else if(phrase.Length == 3)
            {
                if (phrase[0].Length == 3 && phrase[0][2] - '0' >= 1 && phrase[0][2] - '0' <= _totalNumber)
                {
                    SC_index = phrase[0][2] - '0';
                }
                else
                {
                    MessageBox.Show("Invalid CMD");
                    return;
                }
                if(phrase[1] == "LV")
                {
                    if(phrase[2] == "DeployPayload")
                    {
                        if (spacecraftList_client[SC_index - 1].Payload_Stat == "Ready to deploy")
                        {
                            spacecraftList_client[SC_index - 1].Vehicle_Stat = "In orbit(No Payload)";
                            spacecraftList_client[SC_index - 1].Payload_Stat = "Deployed";
                        }
                        else
                        {
                            MessageBox.Show("Invalid CMD(Can't deploy currently)");
                            return;
                        }
                    }
                    else if(phrase[2] == "Deorbit")
                    {
                        if(spacecraftList_client[SC_index - 1].Vehicle_Stat == "In orbit(No Payload)")
                        {
                            spacecraftList_client[SC_index - 1].Vehicle_Stat = "De-orbited";
                            spacecraftList_client[SC_index - 1].Vehicle_Altitude1 = -1;
                            spacecraftList_client[SC_index - 1].Vehicle_Longitude1 = -1;
                            spacecraftList_client[SC_index - 1].Vehicle_Latitude1 = -1;
                            spacecraftList_client[SC_index - 1].Vehicle_Temperature1 = -1;
                            spacecraftList_client[SC_index - 1].Vehicle_Time_to_Orbit1 = -1;
                        }
                        else
                        {
                            MessageBox.Show("Invalid CMD(Can't deorbit currently)");
                            return;
                        }
                    }
                    else if(phrase[2] == "StartTelemetry")
                    {
                        lvTelemetry[SC_index - 1] = new Thread(() => LvStartTele(SC_index));
                        lvTelemetry[SC_index - 1].Start();
                    }
                    else if(phrase[2] == "StopTelemetry")
                    {
                        lvTelemetry[SC_index - 1].Abort();
                        spacecraftList_client[SC_index - 1].Vehicle_Altitude = -1;
                        spacecraftList_client[SC_index - 1].Vehicle_Longitude = -1;
                        spacecraftList_client[SC_index - 1].Vehicle_Latitude = -1;
                        spacecraftList_client[SC_index - 1].Vehicle_Temperature = -1;
                        spacecraftList_client[SC_index - 1].Vehicle_Time_to_Orbit = -1;
                    }
                    else
                    {
                        MessageBox.Show("Invalid CMD");
                        return;
                    }
                }
                else if(phrase[1] == "P")
                {
                    if (phrase[2] == "StartData") {
                        plData[SC_index - 1] = new Thread(() => PlStartData(SC_index));
                        plData[SC_index - 1].Start();
                    }
                    else if(phrase[2] == "StopData")
                    {
                        plData[SC_index - 1].Abort();
                        spacecraftList_client[SC_index - 1].PayloadData = String.Empty;
                    }
                    else if(phrase[2] == "Decommission")
                    {
                        if(spacecraftList_client[SC_index - 1].Payload_Stat == "Deployed")
                        {
                            spacecraftList_client[SC_index - 1].Payload_Stat = "Ended";
                            spacecraftList_client[SC_index - 1].PayloadData = String.Empty;
                            spacecraftList_client[SC_index - 1].PayloadData1 = String.Empty;

                            spacecraftList_client[SC_index - 1].Payload_Altitude1 = -1;
                            spacecraftList_client[SC_index - 1].Payload_Longitude1 = -1;
                            spacecraftList_client[SC_index - 1].Payload_Latitude1 = -1;
                            spacecraftList_client[SC_index - 1].Payload_Temperature1 = -1;
                            spacecraftList_client[SC_index - 1].Payload_Time_to_Orbit1 = -1;

                            spacecraftList_client[SC_index - 1].Payload_Altitude = -1;
                            spacecraftList_client[SC_index - 1].Payload_Longitude = -1;
                            spacecraftList_client[SC_index - 1].Payload_Latitude = -1;
                            spacecraftList_client[SC_index - 1].Payload_Temperature = -1;
                            spacecraftList_client[SC_index - 1].Payload_Time_to_Orbit = -1;
                        }
                        else
                        {
                            MessageBox.Show("Invalid CMD");
                            return;
                        }
                    }
                    else if(phrase[2] == "StartTelemetry")
                    {
                        plTelemetry[SC_index - 1] = new Thread(() => PlStartTele(SC_index));
                        plTelemetry[SC_index - 1].Start();
                    }
                    else if(phrase[2] == "StopTelemetry")
                    {
                        plTelemetry[SC_index - 1].Abort();
                        spacecraftList_client[SC_index - 1].Payload_Altitude = -1;
                        spacecraftList_client[SC_index - 1].Payload_Longitude = -1;
                        spacecraftList_client[SC_index - 1].Payload_Latitude = -1;
                        spacecraftList_client[SC_index - 1].Payload_Temperature = -1;
                        spacecraftList_client[SC_index - 1].Payload_Time_to_Orbit = -1;
                    }
                    else
                    {
                        MessageBox.Show("Invalid CMD");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid CMD");
                    return;
                }
            }
        }
        public void KeepSending()
        {
            while (true)
            {
                Thread T = new Thread(() => service.SendMessage(spacecraftList_client));
                T.Start();
                Thread.Sleep(1000);
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            //service.SendMessage("Hi, I'm the client");
            this.btnSend.Enabled = false;
            MessageBox.Show("Start Sync");
            Thread TT = new Thread(KeepSending);
            TT.Start();
        }
    }
}
