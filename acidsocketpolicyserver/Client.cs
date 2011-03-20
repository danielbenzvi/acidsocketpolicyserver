/*
    Lightweight Adobe flash socket policy provider
    Copyright (C) 2011 Daniel Ben-Zvi [daniel.benzvi@gmail.com]

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace AcidSocketPolicyServer
{  
    public class Client
    {              
        Socket _socket;
        IPAddress _ip;

        Guid _guid;

        bool _isSendOperationRunning;
        bool _hasDispatchedDisconnect;

        public delegate void OnDisconnectDelegate(Client client);
        public delegate void OnPolicyRequestDelegate(Client client);

        public event OnDisconnectDelegate OnDisconnect;
        public event OnPolicyRequestDelegate OnPolicyRequest;

        List<ArraySegment<byte>> _outgoingBuffers;

        //public void delegate OnDisconnectDelegate(StompServerClient client);

        public Client(Socket socket)
        {                   
            _socket = socket;
            _ip = ((IPEndPoint)socket.RemoteEndPoint).Address;
            _guid = Guid.NewGuid();
            _outgoingBuffers = new List<ArraySegment<byte>>();            
        }

        public Guid UniqueId
        {
            get
            {
                return _guid;
            }
        }

        public override string ToString()
        {
            return "(Client [" + RemoteEndpoint.ToString() + "])";
        }

        public IPAddress RemoteEndpoint
        {
            get
            {
                return _ip;
            }
        }

        public void Start()
        {
            BeginReceive();
        }
        
        public void Stop()
        {
            try
            {
                _socket.Close();
            }
            catch (Exception) { }
        }


        void Flush()
        {
            
            if (!_isSendOperationRunning)
            {
                
                lock (_outgoingBuffers)
                {
                    if (_outgoingBuffers.Count > 0)
                    {
                        
                        try
                        {
                            /* Mono has a little problem with sending a list of ArraySegments. 
                             * This is a little workaround -danielb */

                            if (AppCompatibility.IsMono)
                            {
                                _socket.BeginSend(_outgoingBuffers[0].Array, _outgoingBuffers[0].Offset, _outgoingBuffers[0].Count, SocketFlags.None, new AsyncCallback(OnClientSend), _socket);
                                _outgoingBuffers.RemoveAt(0);
                            }
                            else
                            {
                                _socket.BeginSend(_outgoingBuffers, SocketFlags.None, new AsyncCallback(OnClientSend), _socket);
                                _outgoingBuffers = new List<ArraySegment<byte>>();
                            }                        
                        }
                        catch (Exception)
                        {
                            OnInternalDisconnect();
                        }
                    }
                }
            }                            
        }

        public void Send(byte[] buffer, int startIndex, int length)
        {            
            lock (_outgoingBuffers)
            {
                _outgoingBuffers.Add(new ArraySegment<byte>(buffer, startIndex, length));                
                Flush();
            }
        }

        private void OnClientSend(IAsyncResult ar)
        {
            try
            {                
                int sent = _socket.EndSend(ar);                

                if (sent == 0)
                {
                    OnInternalDisconnect();
                }
                else
                {
                    // yofi
                }

                lock (_outgoingBuffers)
                {
                    _isSendOperationRunning = false;
                    Flush();
                }
            }
            catch (Exception)
            {
                OnInternalDisconnect();
            }
        }

        private void OnClientReceive(IAsyncResult ar)
        {            
            try
            {
                int readBytes = _socket.EndReceive(ar);

                if (readBytes > 0)
                {
                    if (OnPolicyRequest != null)
                        OnPolicyRequest(this);

                    BeginReceive();
                }
                else
                {
                    throw new Exception("Connection closed.");
                }
            }
            catch (Exception)
            {
                OnInternalDisconnect();              
            }
        }


        private void BeginReceive()
        {
            try
            {
                byte[] buf = new byte[2];

                _socket.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(OnClientReceive), null);
            }
            catch (Exception)
            {
                OnInternalDisconnect();
            }
        }

        void OnInternalDisconnect()
        {
            try
            {
                _socket.Close();
            }
            catch (Exception) { }

            if (!_hasDispatchedDisconnect)
            {
                _hasDispatchedDisconnect = true;
                if (OnDisconnect != null)
                    OnDisconnect(this);
            }

        }

    }
}
