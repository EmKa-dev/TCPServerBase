using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using TcpServerBaseLibrary;
using TcpServerBaseLibrary.Interfaces;
using System.Net.Sockets;
using TcpServerBaseLibrary.ServerObjects_Sync;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace ServerBaseLibraryXUnitTest
{
    public class TcpServerTest
    {

        [Fact]
        public void ShouldConnectToServer_Test()
        {
            //Arrange

            //Create ClientSocket
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Create server object
            TCPServer_Sync server = new TCPServer_Sync(new EmptyLogger(), 8585, new Dictionary<int, IMessageManager>());


            //Start server
            Task.Run(() => server.Start());

            //Act

            //Connect to server
            client.Connect(new IPEndPoint(IPAddress.Loopback, 8585));

            //Assert

            Assert.True(client.Connected);
        }

        [Fact]
        public void ShouldHandleSentString_Test()
        {

            //Arrange

            //Create message handlers
            var stringhandler = new TestStringHandler();

            var handlers = new Dictionary<int, IMessageManager>
            {
                { 0, stringhandler }
            };

            //Create ClientSocket
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            //Create server object (Pass in handlers)
            TCPServer_Sync server = new TCPServer_Sync(new EmptyLogger(), 8181, handlers);



            //Start server
            Task.Run (() => server.Start());


            //Act

            //Create testdata
            string teststring = "TestString";
            byte[] testmsgdata = Encoding.ASCII.GetBytes(teststring);
            var header = new ApplicationProtocolHeader(testmsgdata.Length, 0);

            //Connect to server
            client.Connect(new IPEndPoint(IPAddress.Loopback, 8181));


            //Send messageheader
            client.Send(header.WrapHeaderData());

            byte[] headerbuffer = new byte[8];

            //Receive ACK
            client.Receive(headerbuffer);
            if (header.Equals(new ApplicationProtocolHeader(headerbuffer)))
            {
                //Send actual message
                client.Send(testmsgdata);
            }
            else
            {
                return;
            }


            //Get handled message
            while (String.IsNullOrEmpty(stringhandler.HandledString))
            {
                //Wait for string to get handled
            }

            //Assert



            string expected = teststring;
            string actual = stringhandler.HandledString;

            Assert.Equal(expected, actual);

        }

        [Fact]
        public void ShouldRejectConnectionOverAllowedThreshold()
        {
            //Arrange

            //Create ClientSocket
            List<Socket> sockets = new List<Socket>
            {
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            };


            //Create server object
            TCPServer_Sync server = new TCPServer_Sync(new EmptyLogger(), 8585, new Dictionary<int, IMessageManager>());


            //Act

            //Start server
            Task.Run(() => server.Start());

            //Connect to server
            foreach (var client in sockets)
            {
                try
                {
                    //Throw in a wait because the sockets need time to make the connections
                    Thread.Sleep(100);
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 8585));
                }
                catch (Exception)
                {
                    continue;
                    //throw;
                }
            }



            //Assert
            bool poll = sockets[3].Poll(300000, SelectMode.SelectWrite);

            Assert.False(poll);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void ShouldRejectConnectionOverAllowedThresholdTheory(int maxallowed)
        {
            //Arrange

            //Create ClientSockets
            List<Socket> sockets = new List<Socket>();

            for (int i = 0; i < maxallowed+1; i++)
            {
                sockets.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }


            //Create server object
            TCPServer_Sync server = new TCPServer_Sync(new EmptyLogger(), 8989, new Dictionary<int, IMessageManager>(), maxallowed);


            //Act

            //Start server
            Task.Run(() => server.Start());

            //Connect to server
            foreach (var client in sockets)
            {
                try
                {
                    //Throw in a wait because the sockets need time to make the connections
                    Thread.Sleep(100);
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 8989));
                }
                catch (Exception)
                {
                    continue;
                    //throw;
                }
            }



            //Assert
            bool actual = sockets[sockets.Count-1].Poll(300000, SelectMode.SelectWrite);

            Assert.False(actual);
        }

        #region Setup methods



        #endregion


        #region Dummy objects


        public class TestStringHandler : IMessageManager
        {
            public string HandledString;

            public void HandleMessage(MessageObject msgobj)
            {
                HandledString = Encoding.ASCII.GetString(msgobj.CompleteData);
            }
        }

        public class EmptyLogger : ILogger
        {
            public void Debug(string message) { }

            public void Error(string message) { }

            public void Info(string message) { }
        }

        #endregion
    }
}
