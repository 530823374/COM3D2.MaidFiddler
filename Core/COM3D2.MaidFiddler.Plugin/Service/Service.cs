﻿using System;
using System.Net;
using COM3D2.MaidFiddler.Core.Hooks;
using COM3D2.MaidFiddler.Core.Utils;
using UnityEngine;

namespace COM3D2.MaidFiddler.Core.Service
{
    public partial class Service : IDisposable
    {
        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
        private readonly MonoBehaviour parent;

        public Service(MonoBehaviour parent)
        {
            this.parent = parent;
            Debugger.WriteLine(LogLevel.Info, "Created a service provider!");

            InitPlayerStatus();
            InitMaidMgr();
            InitMaidStatus();
            InitGameMain();
            InitEventEmitter();

            MiscHooks.DummySkillTreeCreationStart += (s, args) => EmitEvents = false;
            MiscHooks.DummySkillTreeCreationEnd += (s, args) => EmitEvents = true;
        }

        public int GameVersion => (int) typeof(Misc).GetField(nameof(Misc.GAME_VERSION)).GetValue(null);

        public string Version => typeof(Service).Assembly.GetName().Version.ToString();

        public void Dispose()
        {
            eventServer?.Dispose();
        }
    }
}