using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;

namespace ScannerService
{
    public class MyMqttClient
    {
        public bool IsConnect => _client?.IsConnected ?? false;

        /// <summary>
        /// Клиент
        /// </summary>
        private readonly IMqttClient _client;

        /// <summary>
        /// Конфигурация
        /// </summary>
        private readonly IMqttClientOptions _config;

        /// <summary>
        /// Идентификатор клиента
        /// </summary>
        private readonly string _clientId;

        /// <summary>
        /// Хост
        /// </summary>
        private readonly string _host;

        /// <summary>
        /// Порт
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Список топиков
        /// </summary>
        private readonly List<string> _subscribedTopics = new List<string>();

        private MqttTopicFilter _topics;

        public delegate Task ReceivedCallbackHandler(string topic, string body);

        public event ReceivedCallbackHandler ReceivedCallback;

        public delegate void MessageHandler(string content);

        public event MessageHandler Message;

        public MyMqttClient(string clientId, string host, int port)
        {
            _clientId = clientId;
            _host = host;
            _port = port;

            // Конфигурация MQTT клиента
            // Use TCP connection.
            _config = new MqttClientOptionsBuilder()
                     .WithClientId(_clientId)
                     .WithTcpServer(_host, _port) // Port is optional
                      //.WithCredentials("bud", "%spencer%")
                      //.WithTls()
                     .WithCleanSession()
                     .Build();

            // Create a new MQTT client.
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            // Соединение
            _client.UseConnectedHandler(OnСonnectedAsync);
            // Отключение от сервера
            _client.UseDisconnectedHandler(OnDisconnectedAsync);
            // Приём входящих сообщений
            _client.UseApplicationMessageReceivedHandler(OnMessageReceived);
        }

        public MyMqttClient(string clientId, string host, int port, string topic)
            : this(clientId, host, port)
        {
            if (!string.IsNullOrWhiteSpace(topic))
                _topics = new MqttTopicFilterBuilder()
                         .WithTopic(topic)
                         .Build();
        }

        private async Task OnСonnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Message?.Invoke("[MQTT client] сonnected from server");
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (_topics != null)
                await _client.SubscribeAsync(_topics);
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            Message?.Invoke("[MQTT client] disсonnected from server");
            await Task.Delay(TimeSpan.FromSeconds(1));

            try
            {
                await _client.ConnectAsync(_config, CancellationToken.None);
            }
            catch
            {
                Console.WriteLine("[ MQTT client ] reconnected to server failed");
            }
        }

        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;
            var body = Encoding.UTF8.GetString(args.ApplicationMessage.Payload ?? new byte [0]);

            Message?.Invoke("[MQTT client] received message. " +
                            $"client = {args.ClientId}  path = {topic}  payload = {body}");

            ReceivedCallback?.Invoke(topic, body);
        }

        public async Task StartAsync()
        {
            Message?.Invoke("[MQTT server] connect");
            try
            {
                await _client.ConnectAsync(_config);
            }
            catch (Exception exc)
            {
                Message?.Invoke($"[MQTT server] connect exception : {exc.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (!_client.IsConnected) return;
            Message?.Invoke("[MQTT server] disconnect");
            await _client.DisconnectAsync();
        }

        public async Task SubscribeTopicAsync(string topic)
        {
            if (!_client.IsConnected || _subscribedTopics.Contains(topic)) return;

            Message?.Invoke($"[MQTT server] subscribed topic : {topic}");

            _subscribedTopics.Add(topic);
            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public async Task UnsubscribeAllTopicAsync()
        {
            if (!_client.IsConnected) return;

            Message?.Invoke("[MQTT server] unsubscribed all topics");
            var array = _subscribedTopics.ToArray();
            _subscribedTopics.Clear();
            await _client.UnsubscribeAsync(array);
        }

        public async Task UnsubscribeTopicAsync(string topic)
        {
            if (!_client.IsConnected) return;

            Message?.Invoke($"[MQTT server] unsubscribed topic : {topic}");
            _subscribedTopics.Remove(topic);
            await _client.UnsubscribeAsync(topic);
        }

        public async Task PublishingAsync(string topic, string payload)
        {
            if (!_client.IsConnected) return;

            Message?.Invoke($"[MQTT server] publishing {topic} : {payload}");
            var message = new MqttApplicationMessageBuilder()
                         .WithTopic(topic)
                         .WithPayload(payload)
                          //.WithAtMostOnceQoS()
                          //.WithAtLeastOnceQoS()
                         .WithExactlyOnceQoS()
                         .WithRetainFlag(false)
                         .Build();

            // Флаги QoS содержат следующие значения
            // MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce
            //     0 = не более одного раза: сервер срабатывает и забывает. 
            //     Сообщения могут быть потеряны или продублированы
            // MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce
            //     1 = по крайней мере один раз: получатель подтверждает доставку. 
            //     Сообщения могут дублироваться, но доставка гарантирована
            // MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce
            //     2 = ровно один раз: сервер обеспечивает доставку. 
            //     Сообщения поступают точно один раз без потери или дублирования

            await _client.PublishAsync(message);
        }
    }
}