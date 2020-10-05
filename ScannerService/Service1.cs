using System.ServiceProcess;

namespace ScannerService {
    public partial class Service1 : ServiceBase
    {
        private readonly Worker _worker;
        public Service1() {
            InitializeComponent();
            _worker = new Worker();
        }
        protected override async void OnStart(string[] args) => await _worker.Start();
        protected override async void OnStop() => await _worker.Stop();
        protected override async void OnPause() => await _worker.Stop();
        protected override async void OnContinue() => await _worker.Start();
        protected override async void OnShutdown() => await _worker.Stop();
    }
}
