using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TcpServerBaseLibrary.Interfaces;

namespace TcpServerBaseLibrary.ServerObjects_Sync
{
    public class TCPServer_Sync : ITCPServer
    {
        private int _MaxConnectionsAllowed;

        private readonly int _Listeningport;

        private TcpListener tcplistener;

        private TCPServerState _serverState = TCPServerState.Default;

        private List<IWorkingTCPConnection> TCPConnections = new List<IWorkingTCPConnection>();
        private List<IWorkingTCPConnection> ClosedConnections = new List<IWorkingTCPConnection>();


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

                if (ClosedConnections.Count > 0)
                {
                    //Remove any closed connections from TCPConnections
                    RemoveClosedConnections();
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
            newconn.ConnectionClosedEvent += this.OnConnectionClosed;
            newconn.CompleteDataReceived += OnCompleteDataReceived;

            if (TCPConnections.Count >= _MaxConnectionsAllowed)
            {
                _serverState = TCPServerState.ConnectionThresholdReached;

                _Logger.Debug("Connection threshold reached");

                return;
            }
        }


        private void RemoveClosedConnections()
        {
            foreach (var item in ClosedConnections)
            {
                TCPConnections.Remove(item);
            }

            if (TCPConnections.Count < _MaxConnectionsAllowed)
            {
                //Reset server state to default
                _serverState = TCPServerState.Default;
            }
        }

        private void OnCompleteDataReceived(MessageObject obj)
        {
            _Parsers[obj.MessageHeader.MessageTypeIdentifier].HandleMessage(obj);
        }

        private void OnConnectionClosed(IWorkingTCPConnection connection)
        {
            if (TCPConnections.Contains(connection))
            {
                connection.ConnectionClosedEvent -= this.OnConnectionClosed;

                ClosedConnections.Add(connection);
            }
        }
    }
}
