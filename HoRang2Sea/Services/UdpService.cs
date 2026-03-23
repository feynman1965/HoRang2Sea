using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HoRang2Sea.Services
{
    public class UdpService
    {
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        public bool IsConnected { get; private set; }

        public UdpService(int port)
        {
            IsConnected = false;
            Task.Run(async () =>
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                using (var udpClient = new UdpClient(port))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var receivedResults = await udpClient.ReceiveAsync();
                        string buffer = Encoding.ASCII.GetString(receivedResults.Buffer);
                        OnDataReceived(buffer);
                        IsConnected = true;
                    }
                }
            }, cancellationToken);
        }
        public void Disconnect()
        {
            IsConnected = false;
            cancellationTokenSource.Cancel();
        }

        protected virtual void OnDataReceived(string data) => DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
    }
}