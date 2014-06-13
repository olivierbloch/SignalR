using System.Diagnostics;
using Microsoft.AspNet.SignalR.Redis;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Scaleout
{
    public class RedisMessageBusFacts
    {
        [Fact]
        public void OpenCalledOnConnectionRestored()
        {
            bool openInvoked = false;

            var dr = new DefaultDependencyResolver();

            var redisMessageBus = new Mock<RedisMessageBus>(dr, new RedisScaleoutConfiguration("", ""), new TraceSource("RedisTest")) { CallBase = true };

            redisMessageBus.Setup(m => m.OpenStream(It.IsAny<int>())).Callback(() => { openInvoked = true; });

            redisMessageBus.Object.InvokeConnectionRestored();

            Assert.True(openInvoked, "");
        }

        [Fact]
        public void ShutdownChangesStateToDisposed()
        {
            //TBD
        }

        [Fact]
        public void ConnectionFailedChangesStateToClosed()
        {
            //TBD
        }
    }
}
