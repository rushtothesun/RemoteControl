using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DreamPoeBot.Loki.Common;
using log4net;

namespace RemoteControl
{
    /// <summary>
    /// Background TCP server that accepts HTTP-formatted requests and dispatches commands.
    /// Runs on its own thread, independent of DPB's bot lifecycle.
    /// </summary>
    public class TcpCommandServer
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _isRunning;

        public event Action<string> OnCommandReceived;

        public void Start(int port)
        {
            if (_isRunning) return;

            try
            {
                var lanIp = FindPrivateLanIp();
                _listener = new TcpListener(lanIp, port);
                _listener.Start();
                _isRunning = true;

                _thread = new Thread(ListenLoop);
                _thread.IsBackground = true;
                _thread.Name = "RemoteControl_TCPServer";
                _thread.Start();

                Log.WarnFormat("[RemoteControl] TCP listener started on {0}:{1} (LAN only).", lanIp, port);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[RemoteControl] Failed to start TCP listener on port {0}: {1}", port, ex.Message);
            }
        }

        /// Finds the first private IPv4 address (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
        /// from an active network adapter. Falls back to IPAddress.Loopback if none found.
        /// </summary>
        private static IPAddress FindPrivateLanIp()
        {
            try
            {
                foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (iface.OperationalStatus != OperationalStatus.Up) continue;
                    if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var props = iface.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                        var bytes = addr.Address.GetAddressBytes();
                        // 10.x.x.x
                        if (bytes[0] == 10) return addr.Address;
                        // 172.16.x.x - 172.31.x.x
                        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return addr.Address;
                        // 192.168.x.x
                        if (bytes[0] == 192 && bytes[1] == 168) return addr.Address;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[RemoteControl] Error detecting LAN IP, falling back to Loopback: {0}", ex.Message);
            }

            Log.WarnFormat("[RemoteControl] No private LAN IP found, falling back to IPAddress.Loopback (127.0.0.1).");
            return IPAddress.Loopback;
        }

        public void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            _listener = null;
            Log.WarnFormat("[RemoteControl] TCP listener stopped.");
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    using (var client = _listener.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = 3000;
                        HandleClient(client);
                    }
                }
                catch (SocketException)
                {
                    // Expected when listener is stopped
                    if (!_isRunning) break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Log.ErrorFormat("[RemoteControl] Listener error: {0}", ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    var remoteIp = client.Client.RemoteEndPoint.ToString();

                    // Read headers + possibly partial body
                    var sb = new StringBuilder();
                    var buffer = new byte[4096];
                    int bytesRead;

                    // Keep reading until we have the full header block (\r\n\r\n)
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        if (sb.ToString().Contains("\r\n\r\n"))
                            break;
                    }

                    var rawRequest = sb.ToString();
                    if (rawRequest.Length == 0) return;

                    // Parse Content-Length and read remaining body if needed
                    var headerEnd = rawRequest.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd >= 0)
                    {
                        var headers = rawRequest.Substring(0, headerEnd);
                        var bodyReceived = rawRequest.Substring(headerEnd + 4);

                        // Extract Content-Length
                        int contentLength = 0;
                        foreach (var line in headers.Split(new[] { "\r\n" }, StringSplitOptions.None))
                        {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            {
                                int.TryParse(line.Substring(15).Trim(), out contentLength);
                                break;
                            }
                        }

                        // Read remaining body bytes if we haven't received them all
                        while (Encoding.UTF8.GetByteCount(bodyReceived) < contentLength)
                        {
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break;
                            bodyReceived += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        }

                        // Reconstruct the full request for ExtractCommand
                        rawRequest = headers + "\r\n\r\n" + bodyReceived;
                    }

                    // Extract the command
                    var command = ExtractCommand(rawRequest);
                    Log.InfoFormat("[RemoteControl] Command from {0}: {1}", remoteIp, command);

                    // Dispatch the command
                    string resultJson;
                    try
                    {
                        OnCommandReceived?.Invoke(command);
                        resultJson = "{\"status\":\"ok\",\"command\":\"" + EscapeJson(command) + "\"}";
                    }
                    catch (Exception ex)
                    {
                        resultJson = "{\"status\":\"error\",\"message\":\"" + EscapeJson(ex.Message) + "\"}";
                    }

                    // Send HTTP response
                    var responseBody = Encoding.UTF8.GetBytes(resultJson);
                    var header2 = "HTTP/1.1 200 OK\r\n" +
                                 "Content-Type: application/json\r\n" +
                                 "Content-Length: " + responseBody.Length + "\r\n" +
                                 "Access-Control-Allow-Origin: *\r\n" +
                                 "Connection: close\r\n" +
                                 "\r\n";
                    var headerBytes = Encoding.UTF8.GetBytes(header2);
                    stream.Write(headerBytes, 0, headerBytes.Length);
                    stream.Write(responseBody, 0, responseBody.Length);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[RemoteControl] Error handling client: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Extracts the command from an HTTP request body, or from the URL path as fallback.
        /// Supports: POST with JSON body {"command":"X"}, or GET /command/X
        /// </summary>
        private static string ExtractCommand(string rawRequest)
        {
            // Try to extract from JSON body first
            var bodyStart = rawRequest.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (bodyStart >= 0)
            {
                var body = rawRequest.Substring(bodyStart + 4).Trim();
                if (body.Length > 0)
                {
                    // Simple JSON parsing — find "command":"value"
                    var cmdKey = "\"command\"";
                    var idx = body.IndexOf(cmdKey, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var colonIdx = body.IndexOf(':', idx + cmdKey.Length);
                        if (colonIdx >= 0)
                        {
                            var valueStart = body.IndexOf('"', colonIdx + 1);
                            if (valueStart >= 0)
                            {
                                var valueEnd = body.IndexOf('"', valueStart + 1);
                                if (valueEnd > valueStart)
                                {
                                    return body.Substring(valueStart + 1, valueEnd - valueStart - 1);
                                }
                            }
                        }
                    }
                    // If no "command" key, treat the whole body as the command
                    return body;
                }
            }

            // Fallback: extract from URL path (GET /command/StartFollow HTTP/1.1)
            var firstLine = rawRequest.Split('\n')[0];
            var parts = firstLine.Split(' ');
            if (parts.Length >= 2)
            {
                var path = parts[1].TrimStart('/');
                // Strip "command/" prefix if present
                if (path.StartsWith("command/", StringComparison.OrdinalIgnoreCase))
                    path = path.Substring(8);
                return path;
            }

            return "";
        }

        private static string EscapeJson(string value)
        {
            if (value == null) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
