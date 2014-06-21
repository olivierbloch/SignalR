using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
            var wh = new ManualResetEventSlim();

            var redisConnection = new Mock<FakeRedisConnection>() { CallBase = true };

            redisConnection.Setup(m => m.SubscribeAsync(It.IsAny<string>(), It.IsAny<Action<int, RedisMessage>>())).
                Returns<string, Action<int, RedisMessage>>((id, callback) =>
                {
                    wh.Set();
                    return TaskAsyncHelper.Empty;
                });

            var redisMessageBus = new Mock<RedisMessageBus>(new DefaultDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object, new TraceSource("RedisTest")) { CallBase = true };

            redisMessageBus.Setup(m => m.OpenStream(It.IsAny<int>())).Callback(() => { openInvoked = true; });

            var instance = redisMessageBus.Object;

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));

            redisConnection.Raise(mock => mock.ConnectionRestored += (sender, ex) => { }, new Exception());

            Assert.True(openInvoked, "Open method not invoked");
        }
        [Fact]
        public async Task RestoreLatestValueForKeyCalledOnConnectionRestored()
        {
            bool restoreLatestValueForKey = false;

            var redisConnection = new Mock<FakeRedisConnection>() { CallBase = true };

            redisConnection.Setup(m => m.RestoreLatestValueForKey(It.IsAny<TraceSource>())).Returns(TaskAsyncHelper.Empty).Callback(() =>
            {
                restoreLatestValueForKey = true;
            });

            var redisMessageBus = new Mock<RedisMessageBus>(new DefaultDependencyResolver(), new RedisScaleoutConfiguration("connection", "Key"),
                redisConnection.Object, new TraceSource("RedisTest")) { CallBase = true };

            await redisMessageBus.Object.OnConnectionRestored();

            Assert.True(restoreLatestValueForKey, "RestoreLatestValueForKey not invoked");
        }

        [Fact]
        public void ConnectionFailedChangesStateToClosed()
        {
            var redisConnection = new Mock<FakeRedisConnection>() { CallBase = true };

            var redisMessageBus = new RedisMessageBus(new DefaultDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object, new TraceSource("RedisTest"));

            redisConnection.Raise(mock => mock.ConnectionFailed += (sender, ex) => { }, new Exception());

            Assert.Equal(0, redisMessageBus.ConnectionState);
        }

        [Fact]
        public void ConnectRetriesOnError()
        {
            int invokationCount = 0;
            var wh = new ManualResetEventSlim();
            var redisConnection = new Mock<FakeRedisConnection>() { CallBase = true };

            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>())).Returns<string>(connectionString =>
            {
                if (++invokationCount == 2)
                {
                    wh.Set();
                    return TaskAsyncHelper.Empty;
                }
                else
                {
                    return tcs.Task;
                }
            });

            var redisMessageBus = new RedisMessageBus(new DefaultDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
            redisConnection.Object, new TraceSource("RedisTest"));

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
        }
    }
}
