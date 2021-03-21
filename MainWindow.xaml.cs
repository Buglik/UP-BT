using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InTheHand;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace Bluetooth
{
    public partial class MainWindow : Window
    {

        private BluetoothRadio[] radios;
        private BluetoothRadio chosenRadio;
        private BluetoothEndPoint localEndpoint;
        private List<BluetoothDeviceInfo> devices = new List<BluetoothDeviceInfo>();
        private BluetoothDeviceInfo chosenDevice;
        private BluetoothClient localClient;
        private BluetoothComponent localComponent;
        BluetoothListener listener;
        BluetoothClient remoteDevice;

        private string filePath;

        public MainWindow()
        {
            InitializeComponent();

            getRadios();

            listener = new BluetoothListener(chosenRadio.LocalAddress, BluetoothService.SerialPort);
            listener.Start(10);
            listener.BeginAcceptBluetoothClient(new AsyncCallback(AcceptConnection), listener);
        }

        void AcceptConnection(IAsyncResult result)
        {
            if (result.IsCompleted)
            {
                remoteDevice = ((BluetoothListener) result.AsyncState).EndAcceptBluetoothClient(result);
            }
        }

        public void getRadios()
        {
            radios = BluetoothRadio.AllRadios;

            foreach(var radio in radios)
            {
                int index = RadiosCombo.Items.Add(radio.Name);
                if (radio.LocalAddress == BluetoothRadio.PrimaryRadio.LocalAddress)
                {
                    chosenRadio = radio;
                    RadiosCombo.SelectedIndex = index;
                }
            }

            if (chosenRadio == null)
            {
                throw new Exception("Brak adaptera BT lub adapter nieobsługiwany.");
            }
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            getDevices();
        }

        private void getDevices()
        {
            devices.Clear();
            PickDevice.Items.Clear();
            // mac is mac address of local bluetooth device
            localEndpoint = new BluetoothEndPoint(chosenRadio.LocalAddress, BluetoothService.SerialPort);
            // client is used to manage connections
            localClient = new BluetoothClient(localEndpoint);
            // component is used to manage device discovery
            localComponent = new BluetoothComponent(localClient);
            // async methods, can be done synchronously too
            localComponent.DiscoverDevicesAsync(255, true, true, true, true, null);
            localComponent.DiscoverDevicesProgress += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesProgress);
            localComponent.DiscoverDevicesComplete += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesComplete);
        }
        

        private void component_DiscoverDevicesProgress(object sender, DiscoverDevicesEventArgs e)
        {
            // log and save all found devices
            for (int i = 0; i < e.Devices.Length; i++)
            {
                devices.Add(e.Devices[i]);

                string t = e.Devices[i].DeviceName + " (" + e.Devices[i].DeviceAddress + "): ";
                t += e.Devices[i].Remembered ? "znane" : "nieznane";
                PickDevice.Items.Add(t);
            }
        }

        private void component_DiscoverDevicesComplete(object sender, DiscoverDevicesEventArgs e)
        {
            MessageBox.Show("Skanowanie zakończone.");
        }

        private void PickDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chosenDevice = devices[PickDevice.SelectedIndex];
        }

        private void Radios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chosenRadio = radios[RadiosCombo.SelectedIndex];
        }

        private void PairingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!chosenDevice.Authenticated)
            {
                if(BluetoothSecurity.PairRequest(chosenDevice.DeviceAddress, null))
                {
                    MessageBox.Show("Sparowano.");
                }
                else
                    MessageBox.Show("Parowanie zakończone niepowodzeniem.");
            }
            else
                MessageBox.Show("Urządzenie jest już sparowane.");
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
             localClient.SetPin(null);
             localClient.BeginConnect(chosenDevice.DeviceAddress, BluetoothService.SerialPort, new AsyncCallback(Connected), chosenDevice);
        }

        private void Connected(IAsyncResult result)
        {
            if (result.IsCompleted)
            {
                MessageBox.Show("Połączono!");
            }
        }

        private void Send_Button(object sender, RoutedEventArgs e)
        {
            BluetoothDeviceInfo dev = (BluetoothDeviceInfo)chosenDevice;
            ObexStatusCode response_status = SendFile(dev.DeviceAddress, filePath);
        }

        private static ObexStatusCode SendFile(BluetoothAddress adr, string path)
        {
            Uri uri = new Uri("obex://" + adr.ToString() + "/" + path);

            ObexWebRequest req = new ObexWebRequest(uri);
            req.ReadFile(path);
            ObexWebResponse response = (ObexWebResponse)req.GetResponse();
            response.Close();
            return response.StatusCode;
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "File To Send";
            dlg.DefaultExt = "*.*";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                filePath = dlg.FileName;
            }
        }
    }
}
