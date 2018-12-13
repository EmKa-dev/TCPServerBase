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


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        public void ShouldRejectConnectionOverAllowedThresholdTheory(int maxallowed)
        {
            //Arrange

            SocketError expected = SocketError.ConnectionRefused;

            //Create ClientSockets
            List<Socket> sockets = new List<Socket>();

            for (int i = 0; i < maxallowed; i++)
            {
                sockets.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            }

            Socket failSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            //Create server object
            TCPServer_Sync server = new TCPServer_Sync(new EmptyLogger(), 8989, new Dictionary<int, IMessageManager>(), maxallowed);


            //Act

            //Start server
            Task.Run(() => server.Start());

            //Fill available server slots
            foreach (var client in sockets)
            {
                try
                {
                    //Throw in a wait because the sockets need time to make the connections
                    client.Connect(new IPEndPoint(IPAddress.Loopback, 8989));
                    Thread.Sleep(50);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            //Connect with the socket we expect to fail

            // 0 = SocketError.Sucess
            SocketError actual = 0;

            try
            {
                failSocket.Connect(IPAddress.Loopback, 8989);
            }
            catch (SocketException e)
            {
                actual = e.SocketErrorCode;
            }



            //Assert

            Assert.Equal(expected, actual);

            //Notes:
            //So it appears that the actual exception type thrown when the connection is refused is a 
            //''System.Net.Internals.SocketExceptionFactory + ExtendedSocketException''
            //which, in a try-catch, somehow translates to a ordinary SocketException,
            //But in ''Assert.Throws<SocketException>..'', it does not.
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ShouldThrowArgumentExceptionIfAllowedIsZeroOrLess(int maxallowed)
        {
            //Arrange

            //Act

            //Assert
            Assert.Throws<ArgumentException>(() => new TCPServer_Sync(new EmptyLogger(), 9494, new Dictionary<int, IMessageManager>(), maxallowed));
        }


        #region Tests to be developed

        //[Fact]
        //public void ShouldRejectExtremeAmountOfSuccessiveConnectionAttempts()
        //{
        //    //Server which handles extreme amounts of connections
        //    var server = new TCPServer_Sync(new EmptyLogger(), 9898, new Dictionary<int, IMessageManager>(), int.MaxValue);


        //    SocketError actual = 0;

        //    for (int i = 0; i < 80; i++)
        //    {
        //        try
        //        {
        //            new Socket(SocketType.Stream, ProtocolType.Tcp).Connect(IPAddress.Loopback, 9898);

        //        }
        //        catch (SocketException e)
        //        {
        //            actual = e.SocketErrorCode;
        //            continue;
        //            //throw;
        //        }
        //    }

        //    SocketError expected = SocketError.ConnectionRefused;

        //    Assert.Equal(expected, actual);


        //}

        #endregion



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
