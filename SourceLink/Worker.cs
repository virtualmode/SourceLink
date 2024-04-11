using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SourceLink
{
    public class Worker : BackgroundService
    {
        private const string HTTP_CONTENT_SEPARATOR = "\r\n\r\n";
        private const int CHUNK_SIZE = 256;

        private static readonly string CONFIG_DIRECTORY = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.config";
        private static readonly string SETTINGS_FILE = $"{CONFIG_DIRECTORY}/SourceLink.json";

        private static int _threads;

        private readonly ILogger<Worker> _logger;
        private readonly TcpListener _tcpListener;
        private readonly Settings _settings;

        static Worker()
        {
            _threads = 0;
        }

        /// <summary>
        /// Worker initialization.
        /// </summary>
        /// <param name="logger"></param>
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            // Prepare settings.
            Directory.CreateDirectory(CONFIG_DIRECTORY);
            if (File.Exists(SETTINGS_FILE))
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SETTINGS_FILE));
            }
            else
            {
                _settings = JsonConvert.DeserializeObject<Settings>("{}");
                File.WriteAllText(SETTINGS_FILE, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            }

            // Prepare listener.
            if (string.IsNullOrEmpty(_settings.ListenerIpAddress))
                _tcpListener = new TcpListener(IPAddress.Any, _settings.ListenerPort);
            else
                _tcpListener = new TcpListener(IPAddress.Parse(_settings.ListenerIpAddress), _settings.ListenerPort);

            _tcpListener.Start();
        }

        /// <summary>
        /// Inbound connection listener.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation($"{DateTimeOffset.Now} Inbound connection: {client.Client.RemoteEndPoint}");
                    Task.Run(() => ProcessConnection(client), stoppingToken);
                    //await Task.Delay(1000, stoppingToken);
                }
                catch(Exception ex)
                {
                }
            }
        }

        private async Task ProcessConnection(TcpClient client)
        {
            _threads++;

            try
            {
                int i = 0;
                string request = string.Empty;
                byte[] buffer = new byte[CHUNK_SIZE];
                NetworkStream stream = client.GetStream();

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    // Translate data bytes to a UTF-8 string (maybe ASCII are used in protocol).
                    request += Encoding.UTF8.GetString(buffer, 0, i);

                    // Client awaits our response.
                    if (request.EndsWith(HTTP_CONTENT_SEPARATOR))
                    {
                        int count = GetResponse(
                            SetValue(
                                "Cookie",
                                _settings.Cookies,
                                SetValue("Host",
                                    $"{_settings.DestinationAddress}{(_settings.DestinationPort == 80 ? string.Empty : $":{_settings.DestinationPort}")}",
                                    request)),
                            out byte[] result);

                        stream.Write(result, 0, count);
                        client.Close();
                    }
                }

                Console.Write(request);
            }
            catch(Exception ex)
            {
            }
            finally
            {
                client.Close();
                _threads--;
            }
        }

        /// <summary>
        /// Get response from Source Link provider.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <param name="buffer">HTTP response in bytes.</param>
        /// <returns>Byte count of response.</returns>
        private int GetResponse(string request, out byte[] buffer)
        {
            buffer = new byte[_settings.MaxResponseLength];
            TcpClient host = new TcpClient(_settings.DestinationAddress, _settings.DestinationPort);
            host.Client.Send(Encoding.UTF8.GetBytes(request));
            return host.Client.Receive(buffer);
        }

        /// <summary>
        /// Sets value to HTTP header.
        /// </summary>
        private string SetValue(string key, string value, string request)
        {
            int i = request.IndexOf(key), j;
            if (i < 0)
            {
                i = request.IndexOf(HTTP_CONTENT_SEPARATOR);
                if (i < 0)
                    return request;

                value += "\r\n";
                j = i = i + 2;
            }
            else
            {
                j = request.IndexOfAny(new char[] { '\r', '\n' }, i);
            }

            if (j < 0)
                return request;

            StringBuilder result = new StringBuilder(request.Substring(0, i));
            result.Append($"{key}: ");
            result.Append(value);
            result.Append(request.Substring(j));

            return result.ToString();
        }
    }
}
