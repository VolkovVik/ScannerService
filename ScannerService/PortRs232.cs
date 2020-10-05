using System.IO.Ports;
using System.Linq;
using System.Threading;
using Serilog;

namespace ScannerService {
    public class PortRs232
    {
        private SerialPort _serialPort;
        private string _description;

        public delegate void ComHandler( string message);
        public event ComHandler Notify;

        ~PortRs232() {
            Close();
        }

        public bool Open(string comPortName)
        {
            _description = $"[COM port {comPortName}] ";
            var list = SerialPort.GetPortNames();
            if (!list.Contains(comPortName))
            {
                Log.Information($"{_description} Com port {comPortName} not found");
                return false;
            }
            _serialPort = new SerialPort(comPortName)
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };
            if (_serialPort.IsOpen) return true;
            _serialPort.Open();
            _serialPort.DataReceived += _serialPort_DataReceived;
            return true;
        }

        public void Close()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Write(buffer, offset, count);
        }

        public void Write(string message)
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Write(message);
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(20);
            var str = _serialPort.ReadExisting().Trim('\r', '\n');
            Log.Information( $"{_description} Received data {str}" );
            Notify?.Invoke(str);
        }
    }
}