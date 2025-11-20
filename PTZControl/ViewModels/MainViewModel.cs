using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PTZControl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;

    public ObservableCollection<string> ComPorts { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new();

    [ObservableProperty]
    private string ipAddress = "192.168.1.100";

    [ObservableProperty]
    private string hexString;

    [ObservableProperty]
    private int selectedBaudRate;

    [ObservableProperty]
    private string connectionStatus = "Connect";

    [ObservableProperty]
    private bool isConnected = false;

    [ObservableProperty]
    private string panSpeed = "00";

    [ObservableProperty]
    private string tiltSpeed = "00";

    [ObservableProperty]
    private string port = "1470";

    [ObservableProperty]
    private string tcpPort = "1470";

    [ObservableProperty]
    private string zoomDTVValue = "0";

    [ObservableProperty]
    private string zoomIRValue = "0";

    [ObservableProperty]
    private int zoomDefaultDTVValue = 0;

    [ObservableProperty]
    private int zoomDefaultIRValue = 0;

    public MainViewModel()
    {
        LoadComPorts();
        LoadBaudRates();
    }

    public void LoadComPorts()
    {
        try
        {
            ComPorts.Clear();
            string[] ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                foreach (var port in ports)
                    ComPorts.Add(port);

                Port = "";
            }
            else
            {
                ComPorts.Add("No COM ports found");
                Port = ComPorts.First();
            }
        }
        catch (Exception ex)
        {
           
        }
    }

    private void LoadBaudRates()
    {
        try
        {
            BaudRates.Clear();
            var listOfBaudRates = new List<int> { 110, 300, 600, 1200, 2400, 4800, 9600, 14400,
                19200, 38400, 57600, 115200, 128000, 256000 };

            foreach (var baudrate in listOfBaudRates)
                BaudRates.Add(baudrate);

            SelectedBaudRate = BaudRates[6];
        }
        catch (Exception ex)
        {
            
        }
    }

    [RelayCommand]
    private async void SendHex()
    {
        try
        {
            if (string.IsNullOrEmpty(TcpPort))
            {
                TcpPort = "1470";
            }
            int.TryParse(TcpPort, out int port);

            if (_tcpClient == null)
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(IpAddress, port);
                _stream = _tcpClient.GetStream();
            }

            if (!string.IsNullOrEmpty(HexString))
            {
                var bytes = HexStringTo7Bytes(HexString);

                if (bytes == null || bytes.Length == 0)
                {
                    ConnectionStatus = "Invalid HEX";
                    return;
                }

                try
                {
                    await _stream.WriteAsync(bytes, 0, bytes.Length);
                    ConnectionStatus = $"Sent {bytes.Length} bytes";
                }
                catch (Exception ex)
                {
                    ConnectionStatus = $"Send failed: {ex.Message}";
                }
            }
        }
        catch (Exception ex)
        {

        }
    }

    [RelayCommand]
    private void MoveUp()
    {
        HexString = "ff 04 00 08 " + PanSpeed + " " + TiltSpeed;
        SendHex();
    }

    [RelayCommand]
    private void MoveDown()
    {
        HexString = "ff 04 00 10 " + PanSpeed + " " + TiltSpeed;
        SendHex();
    }

    [RelayCommand]
    private void MoveLeft()
    {
        HexString = "ff 04 00 04 " + PanSpeed + " " + TiltSpeed;
        SendHex();
    }

    [RelayCommand]
    private void MoveRight()
    {
        HexString = "ff 04 00 02 " + PanSpeed + " " + TiltSpeed;
        SendHex();
    }

    [RelayCommand]
    private void StopMotor()
    {
        HexString = "ff 04 00 00 " + PanSpeed + " " + TiltSpeed;
        SendHex();
    }

    [RelayCommand]
    private void ZoomInIR()
    {
        //int currentZoom = ZoomDefaultIRValue;
        //int step = int.Parse(ZoomIRValue);
        //currentZoom += step;
        //ZoomDefaultIRValue = currentZoom;
        //string hex = currentZoom.ToString("X4");
        //string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 20 00 00";
        SendHex();
    }

    [RelayCommand]
    private void ZoomOutIR()
    {
        //int currentZoom = ZoomDefaultIRValue;
        //int step = int.Parse(ZoomIRValue);
        //currentZoom -= step;
        //if (currentZoom < 0) currentZoom = 0;
        //ZoomDefaultIRValue = currentZoom;
        //string hex = currentZoom.ToString("X4");
        //string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 40 00 00";
        SendHex();
    }

    [RelayCommand]
    private void ZoomInDTV()
    {
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom += step;
        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void ZoomOutDTV()
    {
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom -= step;
        if (currentZoom < 0) currentZoom = 0;
        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        SendHex();
    }

    //[RelayCommand]
    //private void Connect()
    //{

    //}

    public void OnValueChange(object sender)
    {
        
    }

    private byte[]? HexStringTo7Bytes(string hexInput)
    {
        if (string.IsNullOrWhiteSpace(hexInput)) return null;

        var cleaned = Regex.Replace(hexInput, @"[^0-9A-Fa-f ]", " ").Trim();
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5 && parts.Length != 6) return null;

        try
        {
            byte[] bytes = parts.Select(p => Convert.ToByte(p, 16)).ToArray();

            byte[] packet = new byte[7];
            int startIndex = 0;

            if (bytes.Length == 6 && bytes[0] == 0xFF)
            {
                Array.Copy(bytes, packet, 6);
                startIndex = 1;
            }
            else
            {
                packet[0] = 0xFF;
                Array.Copy(bytes, 0, packet, 1, 5);
                startIndex = 1;
            }

            byte checksum = 0;
            for (int i = 1; i <= 5; i++)
                checksum += packet[i];

            packet[6] = checksum;

            return packet;
        }
        catch
        {
            return null;
        }
    }
}
