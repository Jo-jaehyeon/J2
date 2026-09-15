using System.Collections;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace J2.Networking
{
    public class Connector : IDisposable
    {

        private readonly object	_lock = new object ();

        private readonly HashSet<Socket>	_connecting = new HashSet<Socket>();

        private bool			_disposed;
        public event Action<SocketError> ConnectFailed;
        // Original Client API: endpoint + session factory.

        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                var args = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = endPoint,
                    UserToken = Tuple.Create(socket, sessionFactory)
                };

                args.Completed += OnConnectCompleted;

                lock (_lock)
                {
                    if (_disposed)
                    {
                        args.Dispose();
                        socket.Close();

                        throw new ObjectDisposedException(nameof(Connector));
                    }

                    _connecting.Add(socket);
                }

                try
                {
                    if (!socket.ConnectAsync(args))
                    {
                        OnConnectCompleted(null, args);
                    }
                }
                catch
                {
                    lock (_lock)
                    {
                        _connecting.Remove(socket);
                    }

                    socket.Close();
                    args.Dispose();

                    throw;
                }
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            var state = (Tuple<Socket, Func<Session>>)args.UserToken;

            var socket = state.Item1;

            try
            {
                lock (_lock)
                {
                    _connecting.Remove(socket);

                    if (_disposed)
                    {
                        socket.Close();

                        return;
                    }

                    if (args.SocketError == SocketError.Success)
                    {
                        // Session.Start invokes OnConnected before registering receives.
                        state.Item2().Start(socket);

                        return;
                    }
                }

                socket.Close();
                ConnectFailed?.Invoke(args.SocketError);
            }
            catch (Exception error)
            {
                socket.Close();
                Console.WriteLine(error);
            }
            finally
            {
                args.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;

                foreach (var socket in _connecting)
                {
                    socket.Close();
                }

                _connecting.Clear();
            }
        }
    }
}
