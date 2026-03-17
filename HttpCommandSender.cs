using System;
using System.IO;
using System.Net;
using System.Text;
using DreamPoeBot.Loki.Common;
using log4net;

namespace RemoteControl
{
    /// <summary>
    /// Sends commands to remote bot instances over HTTP.
    /// Used by Commander mode to reach other bot instances.
    /// </summary>
    public static class HttpCommandSender
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        /// <summary>
        /// Sends a command to a remote bot at the given address.
        /// Address format: "IP:Port" (e.g., "127.0.0.1:5200" or "HOSTNAME:5200")
        /// </summary>
        public static bool Send(string address, string command)
        {
            try
            {
                var url = "http://" + address + "/command";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 3000;

                var payload = "{\"command\":\"" + command + "\"}";
                var data = Encoding.UTF8.GetBytes(payload);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Log.InfoFormat("[RemoteControl] Sent '{0}' to {1} => {2}", command, address, response.StatusCode);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException ex)
            {
                Log.ErrorFormat("[RemoteControl] Failed to send '{0}' to {1}: {2}", command, address, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[RemoteControl] Error sending to {0}: {1}", address, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends a command to all configured remote bots.
        /// </summary>
        public static void SendToAll(string command)
        {
            var settings = RemoteControlSettings.Instance;
            if (settings.RemoteBots == null) return;

            foreach (var address in settings.RemoteBots)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    Send(address.Trim(), command);
                }
            }
        }
    }
}
