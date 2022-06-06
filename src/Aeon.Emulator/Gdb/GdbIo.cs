using Aeon.Emulator.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aeon.Emulator.Gdb;

public class GdbIo : IDisposable {
    private readonly GdbFormatter _gdbFormatter = new();
    private readonly List<byte> _rawCommand = new();
    private readonly Socket _serverSocket;
    private readonly Socket _socket;
    private readonly TcpListener _tcpListener;
    private bool _disposedValue;
    private readonly NetworkStream _stream;

    public GdbIo(int port) {
        IPHostEntry host = Dns.GetHostEntry("localhost");
        var ip = new IPAddress(host.AddressList.First().GetAddressBytes());
        _tcpListener = new TcpListener(ip, port);
        _tcpListener.Start();
        _serverSocket = _tcpListener.Server;
        _socket = _tcpListener.AcceptSocket();
        System.Diagnostics.Debug.WriteLine($"GDB Server listening on port {port}");
        System.Diagnostics.Debug.WriteLine($"Client connected: {_socket.RemoteEndPoint}");
 
        _stream = new NetworkStream(_socket);
    }

    public bool IsClientConnected => !((_socket.Poll(1000, SelectMode.SelectRead) && (_socket.Available == 0)) || !_socket.Connected);

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public string GenerateMessageToDisplayResponse(string message) {
        string toSend = $"{message}\n";
        return this.GenerateResponse(ConvertUtils.ByteArrayToHexString(Encoding.UTF8.GetBytes(toSend)));
    }

    public string GenerateResponse(string data) {
        byte checksum = 0;
        byte[] array = Encoding.UTF8.GetBytes(data);
        for (int i = 0; i < array.Length; i++) {
            byte b = array[i];
            checksum += b;
        }

        return $"+${data}#{_gdbFormatter.FormatValueAsHex8(checksum)}";
    }

    public string GenerateUnsupportedResponse() {
        return "";
    }

    public List<byte> RawCommand => _rawCommand;

    public string ReadCommand() {
        _rawCommand.Clear();
        int chr = _stream.ReadByte();
        var resBuilder = new StringBuilder();
        while (chr >= 0) {
            _rawCommand.Add((byte)chr);
            if ((char)chr == '#') {
                // Ignore checksum
                _stream.ReadByte();
                _stream.ReadByte();
                break;
            }
            resBuilder.Append((char)chr);
            chr = _stream.ReadByte();
        }
        String payload = GetPayload(resBuilder);
        System.Diagnostics.Debug.WriteLine($"Received command from GDB {@payload}");
        return payload;
    }

    public void SendResponse(string? data) {
        if (data != null) {
            System.Diagnostics.Debug.WriteLine($"Sending response {@data}");
            _stream.Write(Encoding.UTF8.GetBytes(data));
        }
    }

    protected void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                // dispose managed state (managed objects)
                _tcpListener.Stop();
                _serverSocket.Close();
                _socket.Close();
            }

            _disposedValue = true;
        }
    }

    private string GetPayload(StringBuilder resBuilder) {
        string res = resBuilder.ToString();
        int beginning = res.IndexOf('$');
        if (beginning != -1) {
            return res[(beginning + 1)..];
        }

        beginning = res.IndexOf('+');
        if (beginning != -1) {
            return res[(beginning + 1)..];
        }

        return res;
    }
}