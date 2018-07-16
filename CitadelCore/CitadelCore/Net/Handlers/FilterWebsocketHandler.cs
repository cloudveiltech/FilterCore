/*
* Copyright © 2017 Jesse Nicholson and CloudVeil Technology, Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Net.WebSockets.Managed;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using CitadelCore.Net.Proxy;
using CitadelCore.Logging;
using System.Text;
using System.Threading;
using CitadelCore.Net.Http;
using CitadelCore.Extensions;
using CitadelCore.Diagnostics;
using Microsoft.AspNetCore.WebSockets.Protocol;

namespace CitadelCore.Net.Handlers
{

    /// <summary>
    /// The FilterWebsocketHandler handler is designed to proxy Websocket requests and responses,
    /// while providing an opportunity for users to inspect and optionally filter and modifiy
    /// requests and responses at different stages of the transaction.
    /// </summary>
    internal class FilterWebsocketHandler : AbstractFilterResponseHandler
    {
        public FilterWebsocketHandler(MessageBeginCallback messageBeginCallback, MessageEndCallback messageEndCallback) : base(messageBeginCallback, messageEndCallback)
        {
            Console.WriteLine("New FilterWebsocketHandler");
        }

        public override async Task Handle(HttpContext context)
        {
            DiagnosticsWebSession diagSession = new DiagnosticsWebSession();

            try
            {
                // First we need the URL for this connection, since it's been requested to be upgraded to
                // a websocket.
                var fullUrl = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(context.Request);

                // Need to replate the scheme with appropriate websocket scheme.
                if (fullUrl.StartsWith("http://"))
                {
                    fullUrl = "ws://" + fullUrl.Substring(7);
                }
                else if (fullUrl.StartsWith("https://"))
                {
                    fullUrl = "wss://" + fullUrl.Substring(8);
                }

                diagSession.ClientRequestUri = fullUrl;

                // Next we need to try and parse the URL as a URI, because the websocket client requires
                // this for connecting upstream.
                Uri wsUri = null;

                if (!Uri.TryCreate(fullUrl, UriKind.RelativeOrAbsolute, out wsUri))
                {
                    LoggerProxy.Default.Error("Failed to parse websocket URI.");
                    return;
                }

                string subProtocol = context.Request.Headers[Constants.Headers.SecWebSocketProtocol];
                var wsServer = new ClientWebSocket();

                if(subProtocol != null && subProtocol.Length > 0)
                {
                    wsServer.Options.AddSubProtocol(subProtocol);
                }

                wsServer.Options.Cookies = new System.Net.CookieContainer();

                foreach (var cookie in context.Request.Cookies)
                {
                    try
                    {
                        wsServer.Options.Cookies.Add(new Uri(fullUrl, UriKind.Absolute), new System.Net.Cookie(cookie.Key, System.Net.WebUtility.UrlEncode(cookie.Value)));
                    }
                    catch (Exception e)
                    {
                        LoggerProxy.Default.Error("Error while attempting to add websocket cookie.");
                        LoggerProxy.Default.Error(e);
                    }
                }

                if (context.Connection.ClientCertificate != null)
                {
                    wsServer.Options.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509CertificateCollection(new[] { context.Connection.ClientCertificate.ToV2Certificate() });
                }


                LoggerProxy.Default.Info(string.Format("Connecting websocket to {0}", wsUri.AbsoluteUri));

                if(Collector.IsDiagnosticsEnabled)
                {
                    var diagHeaderBuilder = new StringBuilder();
                    foreach(var hdr in context.Request.Headers)
                    {
                        diagHeaderBuilder.AppendFormat($"{hdr.Key}: {hdr.Value.ToString()}\r\n");
                    }

                    diagSession.ClientRequestHeaders = diagHeaderBuilder.ToString();
                }

                var reqHeaderBuilder = new StringBuilder();
                foreach (var hdr in context.Request.Headers)
                {
                    if (!ForbiddenHttpHeaders.IsForbidden(hdr.Key))
                    {
                        reqHeaderBuilder.AppendFormat("{0}: {1}\r\n", hdr.Key, hdr.Value.ToString());

                        try
                        {
                            if (!ForbiddenWsHeaders.IsForbidden(hdr.Key))
                            {
                                wsServer.Options.SetRequestHeader(hdr.Key, hdr.Value.ToString());
                                Console.WriteLine("Set Header: {0} ::: {1}", hdr.Key, hdr.Value.ToString());
                            }
                        }
                        catch (Exception hdrException)
                        {
                            Console.WriteLine("Failed Header: {0} ::: {1}", hdr.Key, hdr.Value.ToString());
                            LoggerProxy.Default.Error(hdrException);
                        }
                    }
                }

                reqHeaderBuilder.Append("\r\n");

                diagSession.ServerRequestHeaders = reqHeaderBuilder.ToString();

                string serverSubProtocol = null;

                await wsServer.ConnectAsync(wsUri, context.RequestAborted);
                if (wsServer.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    serverSubProtocol = wsServer.SubProtocol;
                }
                else
                {

                }

                // Create, via acceptor, the client websocket. This is the local machine's websocket.
                var wsClient = await context.WebSockets.AcceptWebSocketAsync(serverSubProtocol);
                
                /*
                TODO - Much of this is presently lost to us because the socket
                we get from AcceptWebSocketAsync is a mostly internal implementation
                that is NOT a ClientWebSocket.

                Ideally we would xfer all such properties from the client to our proxy
                client socket.

                wsServer.Options.ClientCertificates = wsClient.Options.ClientCertificates;
                wsServer.Options.Cookies = wsClient.Options.Cookies;
                wsServer.Options.Credentials = wsClient.Options.Credentials;
                wsServer.Options.KeepAliveInterval = wsClient.Options.KeepAliveInterval;
                wsServer.Options.Proxy = wsClient.Options.Proxy;
                wsServer.Options.UseDefaultCredentials = wsClient.Options.UseDefaultCredentials;
                */

                LoggerProxy.Default.Info(string.Format("Connecting websocket to {0}", wsUri.AbsoluteUri));

                ProxyNextAction nxtAction = ProxyNextAction.AllowAndIgnoreContentAndResponse;
                string customResponseContentType = string.Empty;
                byte[] customResponse = null;
                m_msgBeginCb?.Invoke(wsUri, reqHeaderBuilder.ToString(), null, context.Request.IsHttps ? MessageType.SecureWebSocket : MessageType.WebSocket, MessageDirection.Request, out nxtAction, out customResponseContentType, out customResponse);

                switch(nxtAction)
                {
                    case ProxyNextAction.DropConnection:
                        {
                            if(customResponse != null)
                            {

                            }

                            await wsClient.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return;
                        }
                }

                // Spawn an async task that will poll the remote server for data in a loop, and then
                // write any data it gets to the client websocket.
                var serverTask = Task.Run(async () =>
                {
                    var serverBuffer = new byte[1024 * 4];
                    var serverStatus = await wsServer.ReceiveAsync(new ArraySegment<byte>(serverBuffer), context.RequestAborted);

                    while(!serverStatus.CloseStatus.HasValue && !wsClient.CloseStatus.HasValue && !context.RequestAborted.IsCancellationRequested)
                    {
                        await wsClient.SendAsync(new ArraySegment<byte>(serverBuffer, 0, serverStatus.Count), serverStatus.MessageType, serverStatus.EndOfMessage, context.RequestAborted);

                        serverStatus = await wsServer.ReceiveAsync(new ArraySegment<byte>(serverBuffer), context.RequestAborted);
                    }

                    await wsServer.CloseAsync(serverStatus.CloseStatus.Value, serverStatus.CloseStatusDescription, context.RequestAborted);
                });

                // Spawn an async task that will poll the local client websocket, in a loop, and then
                // write any data it gets to the remote server websocket.
                var clientTask = Task.Run(async () =>
                {
                    var clientBuffer = new byte[1024 * 4];
                    var clientResult = await wsClient.ReceiveAsync(new ArraySegment<byte>(clientBuffer), context.RequestAborted);

                    while(!clientResult.CloseStatus.HasValue && !wsServer.CloseStatus.HasValue && !context.RequestAborted.IsCancellationRequested)
                    {
                        await wsServer.SendAsync(new ArraySegment<byte>(clientBuffer, 0, clientResult.Count), clientResult.MessageType, clientResult.EndOfMessage, context.RequestAborted);

                        clientResult = await wsClient.ReceiveAsync(new ArraySegment<byte>(clientBuffer), context.RequestAborted);
                    }

                    await wsClient.CloseAsync(clientResult.CloseStatus.Value, clientResult.CloseStatusDescription, context.RequestAborted);
                });

                // Above, we have created a bridge between the local and remote websocket. Wait for both
                // associated tasks to complete.
                await Task.WhenAll(serverTask, clientTask);
            }
            catch(Exception wshe)
            {
                LoggerProxy.Default.Error(wshe);
            }
            finally
            {
                Collector.ReportSession(diagSession);
            }
        }
    }
}