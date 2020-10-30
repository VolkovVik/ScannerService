using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace ScannerService {
    class Worker
    {
        private const string TestNamePort = "COM10";
        private const string TerminalTopic = "terminal";
        private const string ScannerTopic = "scanner";
        private const int Delay = 1000;

        private MyMqttClient _mqttClient;
        private PortRs232 _terminalPort;
        private PortRs232 _scannerPort;

        private bool _isExit;

        public async Task Start()
        {
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File("C:\\ASPU\\myapp\\myapp1.txt", rollingInterval:RollingInterval.Day)
                        .CreateLogger();
            Log.Information("Running!");

            _isExit = false;

            _scannerPort = new PortRs232();
            _scannerPort.Notify += ScannerPort_Notify;

            _terminalPort = new PortRs232();
            _terminalPort.Notify += TerminalPort_Notify;

            _mqttClient = new MyMqttClient("ping", "127.0.0.1", 1883, "/server/#");
            _mqttClient.Message += _mqttClient_Message;
            _mqttClient.ReceivedCallback += _mqttClient_ReceivedCallback;
            
            try
            {
                var name = ManagementDevice.GetScannerPort();
                _scannerPort.Open(name);
                _terminalPort.Open(TestNamePort);
                await _mqttClient.StartAsync().ConfigureAwait(false);

                while (!_isExit)
                {
                    var array = new byte[] {16};
                    _terminalPort?.Write(array, 0, array.Length);
                    await Task.Delay(Delay);
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
                if (_terminalPort != null)
                {
                    _terminalPort.Notify -= TerminalPort_Notify;
                    _terminalPort?.Close();
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
            await Task.Delay(1000);
        }

        private Task _mqttClient_ReceivedCallback(string topic, string body)
        {
            Log.Information($"[MQTT] Received topic {topic} data {body}");
            return Task.CompletedTask;
        }

        private void _mqttClient_Message(string content) => Log.Information($"{content}");

        private async void TerminalPort_Notify(string message) =>
            await _mqttClient.PublishingAsync(TerminalTopic, message);

        private async void ScannerPort_Notify(string message) =>
            await _mqttClient.PublishingAsync(ScannerTopic, message);
    }
}
