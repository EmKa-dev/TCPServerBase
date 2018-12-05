using System;
using System.Net.Sockets;

namespace TcpServerBaseLibrary.ServerObjects_Sync
{
    internal class WorkingTCPConnection_Sync : IWorkingTCPConnection
    {
        private byte[] _ReceiveBuffer;
        private byte[] _ProtocolPrefixBuffer = new byte[8];
        private int _BytesRead;

        //Dependency objects
        private ILogger _Logger;

        private TCPConnectionState _ConnectionState = TCPConnectionState.ReceivingHeader;

        public Socket WorkSocket { get; set; }

        public WorkingTCPConnection_Sync(Socket client, ILogger logger)
        {
            WorkSocket = client;
            _Logger = logger;
        }

        /// <summary>
        /// Execute functions according to the internal state
        /// </summary>
        /// <param name="microseconds"> Time in microseconds allowed to wait for operation </param>
        public void ExecuteState(int microseconds)
        {

            switch (_ConnectionState)
            {
                case TCPConnectionState.ReceivingHeader:

                    //Try catch here is because the previous MessageHandler might have closed the socket already

                    try
                    {
                        if (WorkSocket.Poll(microseconds, SelectMode.SelectRead))
                        {
                            ReceiveHeaderData();
                        }
                    }
                    catch (Exception)
                    {
                        CloseConnectionGracefully();
                        //throw;
                    }

                    break;

                case TCPConnectionState.ReceivingMessageData:


                    try
                    {
                        if (WorkSocket.Poll(microseconds, SelectMode.SelectRead))
                        {
                            ListenForMessage();
                        }
                    }
                    catch (Exception)
                    {
                        CloseConnectionGracefully();
                        //throw;
                    }

                    break;
                default:
                    break;
            }
        }

        private void ReceiveHeaderData()
        {

            try
            {

                // End the data receiving that the socket has done and get
                // the number of bytes read.
                int received = this.WorkSocket.Receive(_ProtocolPrefixBuffer, _BytesRead, _ProtocolPrefixBuffer.Length - _BytesRead, SocketFlags.None);

                _Logger.Debug($"Read {received} bytes header data");

                //Append to field to keep track of bytes received between read attempts
                _BytesRead += received;

                // If no bytes were received, the connection is closed
                if (received <= 0)
                {
                    CloseConnectionGracefully();

                    return;
                }

                if (_BytesRead == 8)
                {

                    //Sends acknowledgment to client that we have read the header and is ready to receive the message
                    SendAcknowledgment(new ApplicationProtocolHeader(_ProtocolPrefixBuffer));

                    _ConnectionState = TCPConnectionState.ReceivingMessageData;

                    return;

                }
                else if (_BytesRead < 8)
                {
                    //We havn't gotten the whole header, wait for more data to come in
                    _Logger.Debug("Waiting for rest of header data");
                }
            }
            catch (Exception e)
            {

                _Logger.Debug("Error receiving header data");
                _Logger.Error(e.Message);

                CloseConnectionGracefully();

            }
        }


        private void ListenForMessage()
        {
            ApplicationProtocolHeader header = new ApplicationProtocolHeader(_ProtocolPrefixBuffer);

            //Reset bytes read, as we will now start to receive the rest of the data
            _BytesRead = 0;

            //Set buffer to appropriate size
            PrepareBuffer(header.Lenght);

            _Logger.Debug("Starts reading message data");

            try
            {
                while (this._BytesRead < _ReceiveBuffer.Length)
                {
                    _BytesRead += this.WorkSocket.Receive(this._ReceiveBuffer, _BytesRead, _ReceiveBuffer.Length - _BytesRead, SocketFlags.None);
                }

                MessageObject dataobj = new MessageObject(this.WorkSocket, header, _ReceiveBuffer);

                //Pass dataobject to whoever listens for it
                NotifyCompleteDataReceived(dataobj);

                PrepareToReadNextHeader();

                return;
            }
            catch (Exception e)
            {
                _Logger.Debug("Error receiving message data");
                _Logger.Error(e.Message);

                CloseConnectionGracefully();
            }
        }

        #region Helper methods

        private void PrepareToReadNextHeader()
        {
            //Reset bytes read as we prepare to read the next header
            this._BytesRead = 0;
            _ProtocolPrefixBuffer = new byte[8];

            _ConnectionState = TCPConnectionState.ReceivingHeader;
        }

        private void SendAcknowledgment(ApplicationProtocolHeader head)
        {
            _Logger.Debug("Sending header acknowledgment");

            try
            {
                WorkSocket.Send(head.WrapHeaderData());
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void PrepareBuffer(int size)
        {
            this._ReceiveBuffer = new byte[size];
        }

        private void CloseConnectionGracefully()
        {
            _Logger.Info($"Connection with : {WorkSocket.RemoteEndPoint.ToString()} closing gracefully");

            this.WorkSocket.Shutdown(SocketShutdown.Both);
            this.WorkSocket.Close();

            this.NotifyConnectionClosed();
        }

        #endregion



        public event Action<MessageObject> CompleteDataReceived;
        public event Action<IWorkingTCPConnection> ConnectionClosedEvent;


        private void NotifyCompleteDataReceived(MessageObject Data)
        {
            _Logger.Debug("Complete message received");

            this.CompleteDataReceived?.Invoke(Data);

        }

        private void NotifyConnectionClosed()
        {
            _Logger.Info("Connection closed");

            ConnectionClosedEvent?.Invoke(this);
        }
    }
}
