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
using PTZControl.Enums;

namespace PTZControl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    byte[] buffer = new byte[1024];
    List<byte> recvBuffer = new List<byte>();

    public ObservableCollection<string> ComPorts { get; } = new();
    public ObservableCollection<int> BaudRates { get; } = new();

    private readonly int IRMaxZoom = 37801;
    private readonly int IRMinZoom = 24194;
    private readonly int DTVMaxZoom = 980;
    private readonly int DTVMinZoom = 28;

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
    private string dTVAbsoluteZoomValue = "0";

    [ObservableProperty]
    private string zoomIRValue = "0";

    [ObservableProperty]
    private int zoomDefaultDTVValue = 28;

    [ObservableProperty]
    private int zoomDefaultIRValue = 24194;

    [ObservableProperty]
    private string iRAbsoluteZoomValue = "0";

    [ObservableProperty]
    private string tcpReply = "";

    [ObservableProperty]
    private string focusDTVValue = "0";

    [ObservableProperty]
    private string focusIRValue = "0";

    [ObservableProperty]
    private int focusDefaultDTVValue = 0;

    [ObservableProperty]
    private int focusDefaultIRValue = 0;

    [ObservableProperty]
    private Commands currentCommand;

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
            TcpReply = "";
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
                StartReceiving();
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

    private async void StartReceiving()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (_tcpClient?.Connected == true)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                recvBuffer.AddRange(buffer.Take(bytesRead));

                while (recvBuffer.Count >= 7)
                {
                    byte[] packet = recvBuffer.Take(7).ToArray();   
                    recvBuffer.RemoveRange(0, 7);

                    byte b5 = packet[4];
                    byte b6 = packet[5];
                    int decB5 = b5;
                    int decB6 = b6;
                    int combinedDec = (b5 << 8) | b6;

                    string hex = BitConverter.ToString(packet)
                                             .Replace("-", " ")
                                             .ToLower();

                    TcpReply += $"Receive : {DateTime.Now:HH:mm:ss}  {hex} | {CurrentCommand.ToString()} Value : {combinedDec}"
                                + Environment.NewLine;
                }
            }
        }
        catch (Exception ex)
        {
            TcpReply += $"Receive error: {ex.Message}\n" + System.Environment.NewLine;
        }
    }

    #region Pan/Tilt

    [RelayCommand]
    private void MoveUp()
    {
        CurrentCommand = Commands.TiltUp;
        if (string.IsNullOrEmpty(TiltSpeed))
            TiltSpeed = "00";

        HexString = "ff 04 00 08 00 " + int.Parse(TiltSpeed).ToString("D2");
        SendHex();
    }

    [RelayCommand]
    private void MoveDown()
    {
        CurrentCommand = Commands.TiltDown;
        if (string.IsNullOrEmpty(TiltSpeed))
            TiltSpeed = "00";

        HexString = "ff 04 00 10 00 " + int.Parse(TiltSpeed).ToString("D2");
        SendHex();
    }

    [RelayCommand]
    private void MoveLeft()
    {
        CurrentCommand = Commands.PanLeft;
        if (string.IsNullOrEmpty(PanSpeed))
            PanSpeed = "00";

        HexString = "ff 04 00 04 " + int.Parse(PanSpeed).ToString("D2") + " 00";
        SendHex();
    }

    [RelayCommand]
    private void MoveRight()
    {
        CurrentCommand = Commands.PanRight;
        if (string.IsNullOrEmpty(PanSpeed))
            PanSpeed = "00";

        HexString = "ff 04 00 02 " + int.Parse(PanSpeed).ToString("D2") + " 00";
        SendHex();
    }

    [RelayCommand]
    private void StopMotor()
    {
        CurrentCommand = Commands.Stop;
        HexString = "ff 04 00 00 00 00";
        SendHex();
    }

    #endregion

    #region IR

    [RelayCommand]
    private void IRAbsoluteZoom()
    {
        CurrentCommand = Commands.IRZoomIn;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(IRAbsoluteZoomValue);
        currentZoom = step;

        if (currentZoom > IRMaxZoom)
        {
            currentZoom = IRMaxZoom;
            TcpReply = $"Error : {DateTime.Now:HH:mm:ss}  Cannot zoom greater than 37,801."
                                + Environment.NewLine;
        }

        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void ZoomInIR()
    {
        CurrentCommand = Commands.IRZoomIn;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(ZoomIRValue);
        currentZoom += step;

        if (currentZoom > IRMaxZoom)
        {
            currentZoom = IRMaxZoom;
            TcpReply = $"Error : {DateTime.Now:HH:mm:ss}  Cannot zoom greater than 37,801."
                                + Environment.NewLine;
        }

        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void ZoomOutIR()
    {
        CurrentCommand = Commands.IRZoomOut;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(ZoomIRValue);
        currentZoom -= step;
        if (currentZoom < IRMinZoom) currentZoom = IRMinZoom;
        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void IRNarrowFOV()
    {
        CurrentCommand = Commands.IRNarrowFOV;
        ZoomDefaultIRValue = IRMaxZoom;
        HexString = $"ff 02 00 20 00 00";
        SendHex();
    }

    [RelayCommand]
    private void IRWideFOV()
    {
        CurrentCommand = Commands.IRWideFOV;
        ZoomDefaultIRValue = IRMinZoom;
        HexString = $"ff 02 00 40 00 00";
        SendHex();
    }

    [RelayCommand]
    private void FocusIR()
    {
        CurrentCommand = Commands.IRFocus;
        int currentFocus = FocusDefaultIRValue;
        int step = int.Parse(FocusIRValue);
        currentFocus = step;
        FocusDefaultIRValue = currentFocus;
        string hex = step.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 5f {result}";
        SendHex();
    }

    [RelayCommand]
    private void AutoFocusIR()
    {
        CurrentCommand = Commands.IRAutoFocus;
        HexString = $"ff 02 00 2b 00 00";
        SendHex();
    }

    #endregion

    #region

    [RelayCommand]
    private void DTVAbsoluteZoom()
    {
        CurrentCommand = Commands.DTVManualZoom;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(DTVAbsoluteZoomValue);
        currentZoom = step;

        if (currentZoom > DTVMaxZoom)
        {
            currentZoom = DTVMaxZoom;
            TcpReply = $"Info : {DateTime.Now:HH:mm:ss}  Maximum zoom is 980."
                                + Environment.NewLine;
        }

        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void ZoomInDTV()
    {
        CurrentCommand = Commands.DTVZoomIn;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom += step;

        if (currentZoom > DTVMaxZoom)
        {
            currentZoom = DTVMaxZoom;
            TcpReply = $"Info : {DateTime.Now:HH:mm:ss}  Maximum zoom is 980."
                                + Environment.NewLine;
        }

        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void ZoomOutDTV()
    {
        CurrentCommand = Commands.DTVZoomOut;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom -= step;
        if (currentZoom < DTVMinZoom) currentZoom = DTVMinZoom;
        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        SendHex();
    }

    [RelayCommand]
    private void DTVNarrowFOV()
    {
        CurrentCommand = Commands.DTVWideFOV;
        ZoomDefaultDTVValue = DTVMaxZoom;
        HexString = $"ff 01 00 20 00 00";
        SendHex();
    }

    [RelayCommand]
    private void DTVWideFOV()
    {
        CurrentCommand = Commands.DTVNarrowFOV;
        ZoomDefaultDTVValue = DTVMinZoom;
        HexString = $"ff 01 00 40 00 00";
        SendHex();
    }

    [RelayCommand]
    private void FocusDTV()
    {
        CurrentCommand = Commands.DTVFocus;
        int currentFocus = FocusDefaultDTVValue;
        int step = int.Parse(FocusDTVValue);
        currentFocus = step;
        FocusDefaultDTVValue = currentFocus;
        string hex = step.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 5f {result}";
        SendHex();
    }

    [RelayCommand]
    private void AutoFocusDTV()
    {
        CurrentCommand = Commands.DTVAutoFocus;
        HexString = $"ff 01 00 2b 00 00";
        SendHex();
    }

    #endregion

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

            TcpReply += $"Sent : {DateTime.Now:HH:mm:ss}  {hexInput + " " + checksum.ToString("X2")}\n" + System.Environment.NewLine;

            return packet;
        }
        catch
        {
            return null;
        }
    }
}
