// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// Koromo Copy Backend Server Source Code

using koromo_copy_backend.Crypto;
using koromo_copy_backend.Log;
using koromo_copy_backend.Setting;
using koromo_copy_backend.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace koromo_copy_backend.Server
{      
    public class WebSocketServer
    {
        private int count = 0;

        public async void Start(string listenerPrefix)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                Logs.Instance.PushError(e.ToString());
                Logs.Instance.PushError($"Koromo Copy Server is already running or {listenerPrefix} is in use.");

                // Cannot continue!
                Environment.Exit(0);
            }
            Logs.Instance.Push("Start websocket server - " + listenerPrefix + "\r\n\tWaiting for koromo-copy-frontend-client-ui connected...");

            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    try
                    {
                        listenerContext.Response.StatusCode = 400;
                        var res = Encoding.UTF8.GetBytes("Koromo Copy WebSocket Server<br>400 Bad Request");
                        listenerContext.Response.ContentType = "text/html";
                        listenerContext.Response.OutputStream.Write(res, 0, res.Length);
                        listenerContext.Response.Close();
                        Logs.Instance.PushError("Sorry, http web server is currently not support!");
                    }
                    catch { }
                }
            }
        }

        Dictionary<string, WebSocket> wsd = new Dictionary<string, WebSocket>();

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocketContext webSocketContext = null;
            var sign = Hash.GetHashSHA1(Crypto.Random.RandomString(12)).ToLower();
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref count);
                var header = webSocketContext.Headers;
                Logs.Instance.Push($"New client is detected ({count}) requested from: {webSocketContext.RequestUri}\r\n\tAssgined Id: {sign}");
            }
            catch (Exception e)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Logs.Instance.PushError($"Websocket attach error! " + e.Message);
                return;
            }

            if (count >= 2)
            {
                Logs.Instance.PushWarning("Running more than one client can cause unexpected errors!");
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            wsd.Add(sign, webSocket);

            try
            {
                byte[] receiveBuffer = new byte[65535];

                while (webSocket.State == WebSocketState.Open)
                {
                    var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Logs.Instance.Push($"Close WebSockets: {sign}");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        var msg = Encoding.UTF8.GetString(receiveBuffer).Trim('\0');
                        Logs.Instance.Push($"Message received from: {sign}\r\n\tRaw-Text: " + msg);

                        await process_msg(webSocket, sign, msg, receiveResult.EndOfMessage);
                    }
                    else
                    {
                        //await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Binary, receiveResult.EndOfMessage, CancellationToken.None);
                    }

                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                }
            }
            catch (Exception e)
            {
                Logs.Instance.PushError($"WebSocket error target: {sign}\r\n\t" + e.ToString());
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
                Interlocked.Decrement(ref count);
            }
        }

        private async Task send(WebSocket ws, string msg, bool endofmsg)
        {
            var mm = Encoding.UTF8.GetBytes(msg);
            await ws.SendAsync(new ArraySegment<byte>(mm, 0, mm.Length), WebSocketMessageType.Text, endofmsg, CancellationToken.None);
        }

        public async Task SendMessage(string sign, string msg)
        {
            await send(wsd[sign], msg, true);
        }

        public async Task SendMessage(string sign, Protocol data)
        {
            await send(wsd[sign], JsonConvert.SerializeObject(data, Formatting.None), true);
        }

        private async Task process_msg(WebSocket ws, string sign, string msg, bool endofmsg)
        {
            if (msg == "hello")
            {
                // ping-pong test
                byte[] receiveBuffer = new byte[1024];
                var ping = Encoding.UTF8.GetBytes("ping");
                Logs.Instance.Push($"Ping-pong test starts - {sign}");
                long total_ticks = 0;
                for (int i = 0; i < 1000; i++)
                {
                    var starts = DateTime.Now;
                    await ws.SendAsync(new ArraySegment<byte>(ping, 0, ping.Length), WebSocketMessageType.Text, endofmsg, CancellationToken.None);
                    await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    var ends = DateTime.Now;

                    var pong = Encoding.UTF8.GetString(receiveBuffer).Trim('\0');
                    if (pong != "pong")
                    {
                        Logs.Instance.PushError($"Ping-pong test error - {sign} {pong} [{i+1}/50]");
                        Logs.Instance.Push($"Close WebSockets: {sign}");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        return;
                    }
                    total_ticks += (ends - starts).Ticks;
                }
                Logs.Instance.Push($"Ping-pong test ends - {sign}\r\n\tTotal: {total_ticks.ToString("#,#")} ticks, Avg: {((double)total_ticks / 1000).ToString("#,#.#")} ticks, Ping: {((double)(total_ticks / 1000) / (TimeSpan.TicksPerMillisecond / 1000)).ToString("#,#.#")} ms");
            }
            else
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<Protocol>(msg);
                }
                catch (Exception e)
                {
                    Logs.Instance.PushError($"Message processing error from: {sign}\r\n{e}");
                }
            }
        }
    }

    public class Protocol
    {
        public long Index;

        // Is this message client side request?
        // or server side event?
        public bool IsRequest;
        public string Type;
        public object Data;
    }

    public class Server : ILazy<Server>
    {
        public void Start()
        {
            var ws = new WebSocketServer();
            ws.Start(Settings.Instance.Model.ConnectionAddress.Replace("ws", "http"));
        }
    }
}
