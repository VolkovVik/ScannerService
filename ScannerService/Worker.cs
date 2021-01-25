using System;
using System.Threading.Tasks;
using Serilog;

namespace ScannerService {
    class Worker
    {
        private PortRs232 _scannerPort;
        private MyMqttClient _mqttClient;
        private const string ScannerTopic = "scanner";
        private const string NewScannerTopic = "newscan";
        private int _flag = 0;

        private bool _isExit;

        public async Task Start()
        {
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File("C:\\ASPU\\logs\\log.txt", rollingInterval:RollingInterval.Day)
                        .CreateLogger();
            Log.Information("Running!");

            _isExit = false;

            _scannerPort = new PortRs232();
            _scannerPort.Notify += ScannerPort_Notify;

            _mqttClient = new MyMqttClient("ping", "127.0.0.1", 1883, "/server/#");
            _mqttClient.Message += _mqttClient_Message;
            _mqttClient.ReceivedCallback += _mqttClient_ReceivedCallback;
            
            try
            {
                var name = ManagementDevice.GetScannerPort();
                _scannerPort.Open(name);
                await _mqttClient.StartAsync().ConfigureAwait(false);

                while (!_isExit)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc, exc.Message);
            }
            finally
            {
                if (_mqttClient != null)
                {
                    _mqttClient.Message -= _mqttClient_Message;
                    _mqttClient.ReceivedCallback -= _mqttClient_ReceivedCallback;
                    await _mqttClient.StopAsync().ConfigureAwait(false);
                }
                if (_scannerPort != null)
                {
                    _scannerPort.Notify -= ScannerPort_Notify;
                    _scannerPort?.Close();
                }
                Log.CloseAndFlush();
            }
        }

        public async Task Stop()
        {
            _isExit = true;
            await Task.Delay(1100);
        }

        private Task _mqttClient_ReceivedCallback(string topic, string body)
        {
            Log.Information($"[MQTT] Received topic {topic} data {body}");
            return Task.CompletedTask;
        }

        private void _mqttClient_Message(string content) => Log.Information($"{content}");

        private async void ScannerPort_Notify(string message)
        {
            await _mqttClient.PublishingAsync(ScannerTopic, message);
            await Task.Delay(TimeSpan.FromSeconds(.5));
            await _mqttClient.PublishingAsync(NewScannerTopic, _flag.ToString());
            _flag = _flag == 0 ? 1 : 0;
        }
    }
}
