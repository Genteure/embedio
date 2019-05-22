﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Tests;
using EmbedIO.Tests.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Internal
{
    internal sealed class HttpContextImpl : IHttpContextImpl
    {
        private readonly HttpListenerContext _context;

        private readonly Stack<Action<IHttpContext>> _closeCallbacks;

        private bool _closed;

        public HttpContextImpl(HttpListenerContext context)
        {
            _context = context;

            Id = _context.Request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            LocalEndPoint = _context.Request.LocalEndPoint;
            RemoteEndPoint = _context.Request.RemoteEndPoint;
            Request = new HttpRequest(_context);
            User = _context.User;
            Response = new HttpResponse(_context);
        }

        public HttpContextImpl(TestHttpRequest request)
        {
            _context = null;

            Id = request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            LocalEndPoint = request.LocalEndPoint;
            RemoteEndPoint = request.RemoteEndPoint;
            Request = request;
            User = null;
            Response = new TestHttpResponse();
        }

        public string Id { get; }

        public IPEndPoint LocalEndPoint { get; }

        public IPEndPoint RemoteEndPoint { get; }

        public IHttpRequest Request { get; }

        public IHttpResponse Response { get; }

        public IPrincipal User { get; }

        public ISessionProxy Session { get; set; }

        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        public void OnClose(Action<IHttpContext> callback)
        {
            if (_closed)
                throw new InvalidOperationException("HTTP context has already been closed.");

            _closeCallbacks.Push(Validate.NotNull(nameof(callback), callback));
        }

        public async Task<IWebSocketContext> AcceptWebSocketAsync(
            IEnumerable<string> requestedProtocols,
            string acceptedProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            CancellationToken ct)
        {
            if (_context == null)
                throw new NotImplementedException("This HTTP context does not support the WebSocket protocol.");

            var context = await _context.AcceptWebSocketAsync(
                acceptedProtocol,
                receiveBufferSize,
                keepAliveInterval)
                .ConfigureAwait(false);
            return new WebSocketContext(this, context.SecWebSocketVersion, requestedProtocols, acceptedProtocol, new WebSocket(context.WebSocket), ct);
        }

        public void Close()
        {
            _closed = true;

            // Always close the response stream no matter what.
            Response.Close();

            foreach (var callback in _closeCallbacks)
            {
                try
                {
                    callback(this);
                }
                catch (Exception e)
                {
                    e.Log($"HTTP context", $"[Id] Exception thrown by a HTTP context close callback.");
                }
            }
        }
    }
}