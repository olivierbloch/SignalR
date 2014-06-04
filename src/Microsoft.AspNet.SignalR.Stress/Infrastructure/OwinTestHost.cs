using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class OwinTestHost : ITestHost
    {
        private static Random _random = new Random();

        private readonly TransportType _transportType;
        private readonly string _url;
        private IDisposable _server;
        private bool _disposed;

        public OwinTestHost(TransportType transportType)
        {
            _transportType = transportType;
            _url = "http://localhost:" + _random.Next(8000, 9000);
            _disposed = false;
        }

        string ITestHost.Url { get { return _url; } }

        IDependencyResolver ITestHost.Resolver { get; set; }

        void ITestHost.Initialize(int? keepAlive,
            int? connectionTimeout,
            int? disconnectTimeout,
            int? transportConnectTimeout,
            int? maxIncomingWebSocketMessageSize,
            bool enableAutoRejoiningGroups)
        {
            _server = WebApp.Start<Startup>(_url);

            (this as ITestHost).TransportFactory = () =>
            {
                switch (_transportType)
                {
                    case TransportType.Websockets:
                        return new WebSocketTransport(new DefaultHttpClient());
                    case TransportType.ServerSentEvents:
                        return new ServerSentEventsTransport(new DefaultHttpClient());
                    case TransportType.ForeverFrame:
                        break;
                    case TransportType.LongPolling:
                        return new LongPollingTransport(new DefaultHttpClient());
                    default:
                        return new AutoTransport(new DefaultHttpClient());
                }

                throw new NotSupportedException("Transport not supported");
            };
        }

        Func<IClientTransport> ITestHost.TransportFactory { get; set; }

        Task ITestHost.Get(string uri)
        {
            var client = new HttpClient();
            return client.GetAsync(uri);
        }

        Task ITestHost.Post(string uri, IDictionary<string, string> data)
        {
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(data);
            return client.PostAsync(uri, content);
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                _server.Dispose();
                _disposed = true;
            }
        }
    }
}
