using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TcpServerBaseLibrary.Interfaces;

namespace TcpServerBaseLibrary.ServerObjects_Sync
{
    public class TCPServer_Sync : ITCPServer
    {
        private readonly int _MaxConnectionsAllowed;

        private readonly int _Listeningport;

        private TcpListener tcplistener;

        private TCPServerState _serverState = TCPServerState.Listening;

        private List<IWorkingTCPConnection> TCPConnections = new List<IWorkingTCPConnection>();

        //Dependency objects
        private ILogger _Logger;

        private readonly Dictionary<int, IMessageManager> _Parsers;


        #region Constructors

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="port"></param>
        /// <param name="parsers">Message managers specified for each message type</param>
        /// <param name="allowed">Max connections allow, default is 3</param>
        public TCPServer_Sync(ILogger logger, int port, Dictionary<int, IMessageManager> parsers, int allowed = 3)
        {

            if (allowed < 1)
            {
                throw new ArgumentException("Max allowed connections cannot be 0", nameof(allowed));

                //_serverState = TCPServerState.ConnectionThresholdReached;
            }

            _Logger = logger;
            _Listeningport = port;
            _Parsers = parsers;
            _MaxConnectionsAllowed = allowed;


        }



        #endregion


        /// <summary>
        /// Starts the server to process incoming connection requests and incoming data, which is processed using the provided parsers
        /// This method enters a (infinite) loop
        /// </summary>
        public void Start()
        {

            _Logger.Info("Server started!");

            
            //TODO: Make mthod for seraching for IP
            tcplistener = new TcpListener(IPAddress.Any, _Listeningport);

            //Starts the listener
            this.tcplistener.Start();


            //Main loop to keep the server going
            while (true)
            {

                if (_serverState != TCPServerState.ConnectionThresholdReached)
                {

                    if (tcplistener.Pending())
                    {

                        var newconnection = this.tcplistener.AcceptTcpClient();

                        SetupNewConnection(newconnection);
                    }
                }



                //Check for closed down connections before using it
                if (TCPConnections.Any(x => x.IsDisposed == true))
                {
                    //Remove any closed connections from TCPConnections              
                    RemoveClosedConnections(TCPConnections.Where(x => x.IsDisposed == true).ToList());
                }
                

                //Loops through available connections and let them run their methods according to their state
                foreach (var connection in TCPConnections)
                {
                    connection.ExecuteState(100);
                }
            }
        }

        public void Stop()
        {
            //TODO Add a mechanism to stop the server (Cancellationstoken), or IsDisposed
        
        }

        private void SetupNewConnection(TcpClient newconnection)
        {

            WorkingTCPConnection_Sync newconn = new WorkingTCPConnection_Sync(newconnection.Client, _Logger);

            _Logger.Info($"Connection made with {newconn.WorkSocket.RemoteEndPoint.ToString()}");

            TCPConnections.Add(newconn);

            //_Logger.LogMessage($"Connection count : {TCPConnections.Count}");

            //Register eventhandlers
            newconn.CompleteDataReceived += OnCompleteDataReceived;


            if (TCPConnections.Count >= _MaxConnectionsAllowed)
            {
                _serverState = TCPServerState.ConnectionThresholdReached;

                _Logger.Debug("Connection threshold reached");
                _Logger.Debug("Stopping listener");

                tcplistener.Stop();
                return;
            }
        }


        private void RemoveClosedConnections(List<IWorkingTCPConnection> list)
        {

            foreach (var item in list)
            {

                TCPConnections.Remove(item);
                _Logger.Debug("Connection removed from list");
            }


            if (TCPConnections.Count < _MaxConnectionsAllowed)
            {
                //Reset server state to default

                if (_serverState != TCPServerState.Listening)
                {

                    _serverState = TCPServerState.Listening;

                    _Logger.Debug("Starting listener again");
                    tcplistener.Start();
                }
            }
        }

        private void OnCompleteDataReceived(MessageObject obj)
        {
            _Parsers[obj.MessageHeader.MessageTypeIdentifier].HandleMessage(obj);
        }
    }
}
