﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using COM3D2.MaidFiddler.Core.Rpc.Util;
using COM3D2.MaidFiddler.Core.Utils;

namespace COM3D2.MaidFiddler.Core.Rpc
{
    public class PipedEventServer : IDisposable
    {
        private BinaryReader br;

        private readonly BinaryWriter bw;
        private readonly Thread waitForConnectionThread;
        private int currentCache;
        private readonly List<Dictionary<string, object>>[] eventCaches;
        private ulong id;
        private readonly NamedPipeServerStream pipeStream;
        private bool waiterRunning;
        private readonly AutoResetEvent waitForConnectionEvent;

        private uint waitThreadId;

        public PipedEventServer(string name)
        {
            pipeStream = new NamedPipeServerStream(name, PipeDirection.InOut);
            bw = new BinaryWriter(pipeStream);
            br = new BinaryReader(pipeStream);
            eventCaches = new List<Dictionary<string, object>>[2];
            eventCaches[0] = new List<Dictionary<string, object>>();
            eventCaches[1] = new List<Dictionary<string, object>>();

            waitForConnectionEvent = new AutoResetEvent(false);
            waitForConnectionThread = new Thread(RunWaitForConnection);
            waiterRunning = true;
            waitForConnectionThread.Start();

            IsConnected = false;
            WaitForConnection();
        }

        public bool IsConnected { get; private set; }

        public void Dispose()
        {
            try
            {
                if (IsConnected)
                {
                    pipeStream.Disconnect();
                    IsConnected = false;
                }

                Debugger.WriteLine(LogLevel.Info, "Closing Event Emitter...");
                pipeStream.Close();
                Debugger.WriteLine(LogLevel.Info, "Closed Event Emitter!");

                if (waitThreadId != 0)
                {
                    waiterRunning = false;
                    ThreadHelpers.CancelSynchronousIo(waitThreadId);
                    waitForConnectionThread.Join();
                    waitThreadId = 0;
                }
            }
            catch (Exception) { }
        }

        public event EventHandler ConnectionLost;

        public void WaitForConnection()
        {
            if (IsConnected)
                return;
            waitForConnectionEvent.Set();
        }

        public void AddEvent(string name, Dictionary<string, object> args)
        {
            eventCaches[currentCache].Add(new Dictionary<string, object> {["event_name"] = name, ["args"] = args});
        }

        public void Disconnect()
        {
            pipeStream.Disconnect();
            IsConnected = false;
        }

        public void EmitEvents()
        {
            if (!IsConnected || eventCaches[currentCache].Count == 0)
                return;

            Debugger.WriteLine(LogLevel.Info, "Emitting events!");
            int cur = currentCache;
            currentCache = 1 - currentCache;

            var msg = new Message {ID = id++, Data = new Call {Method = "emit", Args = new List<object> {eventCaches[cur].ToArray()}}};

            var data = SerializerUtils.Serialize(msg);

            try
            {
                bw.Write((uint) data.Length);
                bw.Write(data);
                bw.Flush();
            }
            catch (EndOfStreamException e)
            {
                Debugger.WriteLine(LogLevel.Info, "EventEmitter: Connection closed on event emmitter!");
                pipeStream.Disconnect();
                IsConnected = false;
                ConnectionLost?.Invoke(null, EventArgs.Empty);
            }

            eventCaches[cur].Clear();
        }

        private void RunWaitForConnection()
        {
            waitThreadId = ThreadHelpers.GetCurrentThreadId();
            Debugger.WriteLine(LogLevel.Info, $"Wait Thread ID: {waitThreadId}");

            while (waiterRunning)
            {
                waitForConnectionEvent.WaitOne();
                try
                {
                    pipeStream.WaitForConnection();
                    IsConnected = true;
                }
                catch (Exception)
                {
                    Debugger.WriteLine(LogLevel.Info, "EventServer: Waiting aborted! Closing...");
                    waiterRunning = false;
                    IsConnected = false;
                    return;
                }
            }
        }
    }
}