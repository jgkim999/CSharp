using Client;
using Client.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using WpfClient.Models;

namespace WpfClient.Net
{
    public class NetworkStatistics : INetworkStatistics
    {
        private readonly Channel<NetworkStat> _channel = Channel.CreateUnbounded<NetworkStat>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true,
            });
        private readonly Task _consumer;
        private CancellationTokenSource _cancelTokenSource;
        private ReaderWriterLock _rwl = new ReaderWriterLock();

        private readonly ObjectPool<NetworkStat> _pool = new ObjectPool<NetworkStat>(() => new NetworkStat());

        private List<NetworkStat> _data = new();

        public NetworkStatistics()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _consumer = Consume(_cancelTokenSource.Token);
        }

        public void AddReceive(long size)
        {
            DateTime now = DateTime.UtcNow;
            DateTimeOffset unixTime = new DateTimeOffset(now);
            long unixTimeMinutes = unixTime.ToUnixTimeSeconds() / 60;

            NetworkStat row = _pool.Get();
            row.At = now;
            row.UnixMinutes = unixTimeMinutes;
            row.Received = size;
            row.Sent = 0;
            _channel.Writer.WriteAsync(row);
        }

        public void AddSent(long size)
        {
            DateTime now = DateTime.UtcNow;
            DateTimeOffset unixTime = new DateTimeOffset(now);
            long unixTimeMinutes = unixTime.ToUnixTimeSeconds() / 60;

            NetworkStat row = _pool.Get();
            row.At = now;
            row.UnixMinutes = unixTimeMinutes;
            row.Received = 0;
            row.Sent = size;
            _channel.Writer.WriteAsync(row);
        }

        private async Task Consume(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var state in _channel.Reader.ReadAllAsync(cancellationToken))
                {
                    _rwl.AcquireWriterLock(1000);
                    try
                    {
                        var index = _data.FindIndex(e => e.UnixMinutes == state.UnixMinutes);
                        if (index == -1)
                        {
                            // insert
                            _data.Add(state);
                        }
                        else
                        {
                            _data[index].Sent += state.Sent;
                            _data[index].Received += state.Received;
                            _pool.Return(state);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        _rwl.ReleaseWriterLock();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
        }

        public IEnumerable<NetworkStat> Stat(int minutes)
        {
            DateTime end = DateTime.UtcNow;
            DateTimeOffset unixTime = new DateTimeOffset(end);
            long unixTimeMinutes = unixTime.ToUnixTimeSeconds() / 60;

            DateTime start = unixTime.AddMinutes(-minutes).DateTime;

            _rwl.AcquireReaderLock(1000);
            try
            {
                var rows = from n in _data
                        where start < n.At && n.At <= end
                        select n;
                return rows;
            }
            catch (Exception)
            {
                return Enumerable.Empty<NetworkStat>();
            }
            finally
            {
                _rwl.ReleaseReaderLock();
            }
        }

        public NetworkStat StatSum(int minutes)
        {
            DateTime end = DateTime.UtcNow;
            DateTimeOffset unixTime = new DateTimeOffset(end);
            long unixTimeMinutes = unixTime.ToUnixTimeSeconds() / 60;

            DateTime start = unixTime.AddMinutes(-minutes).DateTime;

            var row = new NetworkStat
            {
                At = end,
                UnixMinutes = unixTimeMinutes
            };

            _rwl.AcquireReaderLock(1000);
            try
            {
                var rows = from n in _data
                           where start < n.At && n.At <= end
                           select n;
                foreach (var item in rows)
                {
                    row.Received += item.Received;
                    row.Sent += item.Sent;
                }                
                return row;
            }
            catch (Exception)
            {
                return row;
            }
            finally
            {
                _rwl.ReleaseReaderLock();
            }
        }
    }
}
