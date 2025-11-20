using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
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
    private string selectedComPort = "1470";

    [ObservableProperty]
    private int selectedBaudRate;

    [ObservableProperty]
    private string connectionStatus = "Connect";

    [ObservableProperty]
    private bool isConnected = false;

    [ObservableProperty]
    private string panSpeed;

    [ObservableProperty]
    private string tiltSpeed;

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

                SelectedComPort = "";
            }
            else
            {
                ComPorts.Add("No COM ports found");
                SelectedComPort = ComPorts.First();
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
            int.TryParse(SelectedComPort, out int port);

            _tcpClient = new TcpClient();
            //await _tcpClient.ConnectAsync(IpAddress, port);
            //_stream = _tcpClient.GetStream();

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

    //[RelayCommand]
    //private void Connect()
    //{

    //}

    public void OnValueChange(object sender)
    {
        
    }

    private byte[]? HexStringTo7Bytes(string input)
    {
        try
        {
            var cleaned = Regex.Replace(input, @"[^0-9A-Fa-f ]", "").Trim();
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 6 || parts.Length > 6) return null; // Must have exactly 6 bytes before checksum

            byte[] data = new byte[6];
            for (int i = 0; i < 6; i++)
                data[i] = Convert.ToByte(parts[i], 16);

            // Must start with FF 04
            if (data[0] != 0xFF || data[1] != 0x04) return null;

            // Calculate checksum: XOR of bytes 2 to 5
            byte checksum = data[2];
            for (int i = 1; i <= 5; i++)
                checksum ^= data[i];
            checksum ^= data[5];

            // Full 7-byte packet
            byte[] packet = new byte[] { data[0], data[1], data[2], data[3], data[4], data[5], checksum };

            // Update HexString to show full command with checksum
            HexString = string.Join(" ", packet.Select(b => b.ToString("X2")));

            return packet;
        }
        catch
        {
            return null;
        }
    }
}
