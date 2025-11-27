using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PTZControl.Enums;
using PTZControl.Common;

namespace PTZControl.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Props

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    List<byte> recvBuffer = new List<byte>();
    private readonly Queue<string> replyQueue = new Queue<string>(3);

    [ObservableProperty]
    private string ipAddress = "192.168.1.100";

    [ObservableProperty]
    private string hexString;

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
    private string iRDigitalZoomValue = "0";

    [ObservableProperty]
    private string tcpReply = "";

    [ObservableProperty]
    private string tcpSent = "";

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

    [ObservableProperty]
    private int pTReplyCount;

    #endregion

    public MainViewModel()
    {

    }

    #region TCP 

    private CancellationTokenSource? _cts;

    [RelayCommand]
    private async Task SendHex()
    {
        try
        {
            TcpReply = "";
            if (string.IsNullOrEmpty(TcpPort))
            {
                TcpPort = "1470";
            }
            int.TryParse(TcpPort, out int port);

            if (_tcpClient == null || !_tcpClient.Connected)
            {
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(IpAddress, port);
                _stream = _tcpClient.GetStream();
                
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                _ = StartReceiving(_cts.Token);
            }

            if (!string.IsNullOrEmpty(HexString))
            {
                var bytes = HexStringTo7Bytes(HexString);

                if (bytes == null || bytes.Length == 0)
                {
                    return;
                }

                try
                {
                    if (_stream != null)
                    {
                        await _stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task StartReceiving(CancellationToken ct)
    {
        recvBuffer.Clear();
        byte[] buffer = new byte[1024];

        try
        {
            while (_tcpClient?.Connected == true && !ct.IsCancellationRequested)
            {
                if (_stream == null) break;

                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
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

                    var reply = $"Receive : {DateTime.Now:HH:mm:ss}  {hex} | {CurrentCommand.ToString()} Value : {combinedDec}";

                    if (replyQueue.Count == 3)
                        replyQueue.Dequeue();

                    replyQueue.Enqueue(reply);

                    TcpReply = string.Join(Environment.NewLine, replyQueue);
                }
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            TcpReply += $"Receive error: {ex.Message}\n" + System.Environment.NewLine;
        }
    }

    #endregion

    #region Pan/Tilt

    [RelayCommand]
    private async Task MoveUp()
    {
        PTReplyCount = 0;
        CurrentCommand = Commands.TiltUp;
        if (string.IsNullOrEmpty(TiltSpeed))
            TiltSpeed = "00";

        HexString = "ff 04 00 08 00 " + int.Parse(TiltSpeed).ToString("D2");
        await SendHex();
    }

    [RelayCommand]
    private async Task MoveDown()
    {
        PTReplyCount = 0;
        CurrentCommand = Commands.TiltDown;
        if (string.IsNullOrEmpty(TiltSpeed))
            TiltSpeed = "00";

        HexString = "ff 04 00 10 00 " + int.Parse(TiltSpeed).ToString("D2");
        await SendHex();
    }

    [RelayCommand]
    private async Task MoveLeft()
    {
        PTReplyCount = 0;
        CurrentCommand = Commands.PanLeft;
        if (string.IsNullOrEmpty(PanSpeed))
            PanSpeed = "00";

        HexString = "ff 04 00 04 " + int.Parse(PanSpeed).ToString("D2") + " 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task MoveRight()
    {
        PTReplyCount = 0;
        CurrentCommand = Commands.PanRight;
        if (string.IsNullOrEmpty(PanSpeed))
            PanSpeed = "00";

        HexString = "ff 04 00 02 " + int.Parse(PanSpeed).ToString("D2") + " 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task StopMotor()
    {
        CurrentCommand = Commands.Stop;
        HexString = "ff 04 00 00 00 00";
        await SendHex();
    }

    #endregion

    #region IR

    [RelayCommand]
    private async Task IRAbsoluteZoom()
    {
        CurrentCommand = Commands.IRManualZoom;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(IRAbsoluteZoomValue);
        currentZoom = step;

        if (currentZoom > Constants.IRMaxZoom)
        {
            currentZoom = Constants.IRMaxZoom;
            TcpReply = $"Error : {DateTime.Now:HH:mm:ss}  Cannot zoom greater than 37,801."
                                + Environment.NewLine;
        }

        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task IRDigitalZoom()
    {
        CurrentCommand = Commands.IRDigitalZoom;
        int step = int.Parse(IRDigitalZoomValue);

        if (step > Constants.IRMaxDigitalZoom)
        {
            step = Constants.IRMaxDigitalZoom;
        }

        if (step < Constants.IRMinDigitalZoom)
        {
            step = Constants.IRMinDigitalZoom;
        }

        string hex = step.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 E0 35 {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task ZoomInIR()
    {
        //await CheckIRZoomPosition();
        CurrentCommand = Commands.IRZoomIn;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(ZoomIRValue);
        currentZoom += step;

        if (currentZoom > Constants.IRMaxZoom)
        {
            currentZoom = Constants.IRMaxZoom;
            TcpReply = $"Error : {DateTime.Now:HH:mm:ss}  Cannot zoom greater than 37,801."
                                + Environment.NewLine;
        }

        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task ZoomOutIR()
    {
        CurrentCommand = Commands.IRZoomOut;
        int currentZoom = ZoomDefaultIRValue;
        int step = int.Parse(ZoomIRValue);
        currentZoom -= step;
        if (currentZoom < Constants.IRMinZoom)
        {
            currentZoom = Constants.IRMinZoom;
        }
        ZoomDefaultIRValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task IRNarrowFOV()
    {
        CurrentCommand = Commands.IRNarrowFOV;
        ZoomDefaultIRValue = Constants.IRMaxZoom;
        HexString = $"ff 02 00 20 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task IRWideFOV()
    {
        CurrentCommand = Commands.IRWideFOV;
        ZoomDefaultIRValue = Constants.IRMinZoom;
        HexString = $"ff 02 00 40 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task FocusIR()
    {
        CurrentCommand = Commands.IRFocus;
        int currentFocus = FocusDefaultIRValue;
        int step = int.Parse(FocusIRValue);
        currentFocus = step;
        FocusDefaultIRValue = currentFocus;
        string hex = step.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 02 00 5f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task AutoFocusIR()
    {
        CurrentCommand = Commands.IRAutoFocus;
        HexString = $"ff 02 00 2b 00 00";
        await SendHex();
    }

    #endregion

    #region DTV

    [RelayCommand]
    private async Task DTVAbsoluteZoom()
    {
        CurrentCommand = Commands.DTVManualZoom;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(DTVAbsoluteZoomValue);
        currentZoom = step;

        if (currentZoom > Constants.DTVMaxZoom)
        {
            currentZoom = Constants.DTVMaxZoom;
            TcpReply = $"Info : {DateTime.Now:HH:mm:ss}  Maximum zoom is 980."
                                + Environment.NewLine;
        }

        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task ZoomInDTV()
    {
        //await CheckDTVZoomPosition();
        CurrentCommand = Commands.DTVZoomIn;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom += step;

        if (currentZoom > Constants.DTVMaxZoom)
        {
            currentZoom = Constants.DTVMaxZoom;
            TcpReply = $"Info : {DateTime.Now:HH:mm:ss}  Maximum zoom is 980."
                                + Environment.NewLine;
        }

        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task ZoomOutDTV()
    {
        CurrentCommand = Commands.DTVZoomOut;
        int currentZoom = ZoomDefaultDTVValue;
        int step = int.Parse(ZoomDTVValue);
        currentZoom -= step;
        if (currentZoom < Constants.DTVMinZoom)
        {
            currentZoom = Constants.DTVMinZoom;
        }
        ZoomDefaultDTVValue = currentZoom;
        string hex = currentZoom.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 4f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task DTVNarrowFOV()
    {
        CurrentCommand = Commands.DTVWideFOV;
        ZoomDefaultDTVValue = Constants.DTVMaxZoom;
        HexString = $"ff 01 00 20 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task DTVWideFOV()
    {
        CurrentCommand = Commands.DTVNarrowFOV;
        ZoomDefaultDTVValue = Constants.DTVMinZoom;
        HexString = $"ff 01 00 40 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task FocusDTV()
    {
        CurrentCommand = Commands.DTVFocus;
        int currentFocus = FocusDefaultDTVValue;
        int step = int.Parse(FocusDTVValue);
        currentFocus = step;
        FocusDefaultDTVValue = currentFocus;
        string hex = step.ToString("X4");
        string result = hex.Insert(2, " ");
        HexString = $"ff 01 00 5f {result}";
        await SendHex();
    }

    [RelayCommand]
    private async Task AutoFocusDTV()
    {
        CurrentCommand = Commands.DTVAutoFocus;
        HexString = $"ff 01 00 2b 00 00";
        await SendHex();
    }

    #endregion

    #region Common

    private async Task CheckDTVZoomPosition()
    {
        CurrentCommand = Commands.IRZoomPos;
        HexString = $"ff 02 00 55 00 00";
        await SendHex();
    }

    private async Task CheckIRZoomPosition()
    {
        CurrentCommand = Commands.IRZoomPos;
        HexString = $"ff 02 00 55 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task EnableFilter()
    {
        CurrentCommand = Commands.EnableFilters;

        HexString = $"ff 02 E0 37 00 0A";
        await SendHex();

        HexString = $"ff 02 E0 38 00 0A";
        await SendHex();

        HexString = $"ff 02 E0 39 00 32";
        await SendHex();

        HexString = $"ff 02 E0 3A 00 0A";
        await SendHex();
    }

    [RelayCommand]
    private async Task DisableFilter()
    {
        CurrentCommand = Commands.DisableFilters;

        HexString = $"ff 02 E0 37 00 00";
        await SendHex();

        HexString = $"ff 02 E0 38 00 00";
        await SendHex();

        HexString = $"ff 02 E0 39 00 00";
        await SendHex();

        HexString = $"ff 02 E0 3A 00 00";
        await SendHex();
    }

    [RelayCommand]
    private async Task DefaultFilter()
    {
        CurrentCommand = Commands.DisableFilters;

        HexString = $"ff 02 E0 37 00 00";
        await SendHex();

        HexString = $"ff 02 E0 38 00 00";
        await SendHex();

        HexString = $"ff 02 E0 39 00 05";
        await SendHex();

        HexString = $"ff 02 E0 3A 00 03";
        await SendHex();
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

            TcpSent = $"Sent : {DateTime.Now:HH:mm:ss}  {hexInput + " " + checksum.ToString("X2")}\n" + System.Environment.NewLine;

            return packet;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
