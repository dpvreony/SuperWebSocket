﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;
using SuperWebSocket;
using SuperWebSocket.SubProtocol;
using WebSocket4Net;

namespace SuperWebSocketTest
{
    [TestFixture]
    public class WebSocketClientTestMonoHybi00 : WebSocketClientTestMono
    {
        public WebSocketClientTestMonoHybi00()
            : base(WebSocketVersion.DraftHybi00)
        {
            
        }
    }
    
    [TestFixture]
    public class WebSocketClientTestMonoHybi10 : WebSocketClientTestMono
    {
        public WebSocketClientTestMonoHybi10()
            : base(WebSocketVersion.DraftHybi10)
        {
            
        }
    }
    
    public abstract class WebSocketClientTestMono
    {
        protected WebSocketServer m_WebSocketServer;
        private AutoResetEvent m_MessageReceiveEvent = new AutoResetEvent(false);
        private AutoResetEvent m_OpenedEvent = new AutoResetEvent(false);
        private AutoResetEvent m_CloseEvent = new AutoResetEvent(false);
        private string m_CurrentMessage = string.Empty;
        
        private WebSocketVersion m_Version = WebSocketVersion.DraftHybi00;
        
        protected WebSocketClientTestMono(WebSocketVersion version)
        {
            m_Version = version;
        }
        
        [TestFixtureSetUp]
        public virtual void Setup()
        {
            LogUtil.Setup(new ConsoleLogger());

            m_WebSocketServer = new WebSocketServer(new BasicSubProtocol("Basic", new List<Assembly> { this.GetType().Assembly }));
            m_WebSocketServer.Setup(new RootConfig(), new ServerConfig
            {
                Port = 2012,
                Ip = "Any",
                MaxConnectionNumber = 100,
                Mode = SocketMode.Async,
                Name = "SuperWebSocket Server"
            }, SocketServerFactory.Instance);
        }

        [SetUp]
        public void StartServer()
        {
            m_WebSocketServer.Start();
        }

        [TearDown]
        public void StopServer()
        {
            m_WebSocketServer.Stop();
        }

        [Test]
        public void ConnectionTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("ws://127.0.0.1:{0}/websocket", m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(1000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }

        void webSocketClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            m_CurrentMessage = e.Message;
            m_MessageReceiveEvent.Set();
        }

        void webSocketClient_Closed(object sender, EventArgs e)
        {
            m_CloseEvent.Set();
        }

        void webSocketClient_Opened(object sender, EventArgs e)
        {
            m_OpenedEvent.Set();
        }

        [Test, Repeat(10)]
        public void SendMessageTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("ws://127.0.0.1:{0}/websocket", m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(1000))
                Assert.Fail("Failed to Opened session ontime");

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                sb.Append(Guid.NewGuid().ToString());
            }

            string messageSource = sb.ToString();

            Random rd = new Random();

            for (int i = 0; i < 100; i++)
            {
                int startPos = rd.Next(0, messageSource.Length - 2);
                int endPos = rd.Next(startPos + 1, messageSource.Length - 1);

                string message = messageSource.Substring(startPos, endPos - startPos);
    
                Console.WriteLine("Client:" + message);
                webSocketClient.Send("ECHO " + message);                

                if (!m_MessageReceiveEvent.WaitOne(1000))
                    Assert.Fail("Cannot get response in time!");

                Assert.AreEqual(message, m_CurrentMessage);
            }

            webSocketClient.Close();

            if (!m_CloseEvent.WaitOne(1000))
                Assert.Fail("Failed to close session ontime");
        }


        [Test, Repeat(10)]
        public void CloseWebSocketTest()
        {
            WebSocket webSocketClient = new WebSocket(string.Format("ws://127.0.0.1:{0}/websocket", m_WebSocketServer.Config.Port), "basic", m_Version);
            webSocketClient.Opened += new EventHandler(webSocketClient_Opened);
            webSocketClient.Closed += new EventHandler(webSocketClient_Closed);
            webSocketClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.Open();

            if (!m_OpenedEvent.WaitOne(1000))
                Assert.Fail("Failed to Opened session ontime");

            Assert.AreEqual(WebSocketState.Open, webSocketClient.State);

            webSocketClient.Send("QUIT");

            if (!m_CloseEvent.WaitOne())
                Assert.Fail("Failed to close session ontime");

            Assert.AreEqual(WebSocketState.Closed, webSocketClient.State);
        }
    }
}