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
using System.Threading;

namespace AcidSocketPolicyServer
{
    class Logic
    {
        class CleanupEntry
        {
            public Client Client;
            public DateTime TimeOfInsertion;
        }

        Listener _listener;
        Dictionary<Guid, CleanupEntry> _cleanupPool;
        
        public Logic(int port)
        {
            //<cross-domain-policy><allow-access-from domain="*" to-ports="*" /></cross-domain-policy>
            _listener = new Listener(port, 1000);
            _listener.OnConnect += new Listener.OnConnectDelegate(_listener_OnConnect);
            _cleanupPool = new Dictionary<Guid, CleanupEntry>();            


        }

        public bool IsListening
        {
            get
            {
                return _listener.IsListening;
            }
        }

        public void Start()
        {
            _listener.Start();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessCleanupPool), null);
        }

        void ProcessCleanupPool(object obj)
        {
            while (_listener.IsListening)
            {
                Thread.Sleep(10000);

                CleanupEntry[] theList = null;

                lock (_cleanupPool)
                {
                    theList = _cleanupPool.Values.ToArray<CleanupEntry>();
                }

                // 15 seconds timeout
                foreach (CleanupEntry entry in theList)
                {
                    if ((DateTime.Now - entry.TimeOfInsertion).TotalSeconds > 25)
                    {
                        lock (_cleanupPool)
                        {
                            if (_cleanupPool.ContainsKey(entry.Client.UniqueId))
                                _cleanupPool.Remove(entry.Client.UniqueId);
                        }

                        Console.Out.Write("|");
                        Console.Out.Flush();

                        try
                        {
                            entry.Client.Stop();
                        }
                        catch (Exception) { }
                    }
                }
            }
        }
        void _listener_OnConnect(Client client)
        {
            client.OnPolicyRequest += new Client.OnPolicyRequestDelegate(client_OnPolicyRequest);
            client.OnDisconnect += new Client.OnDisconnectDelegate(client_OnDisconnect);
            client.Start();
            Statistics.AddConnectedClient();

            lock (_cleanupPool)
            {
                _cleanupPool.Add(client.UniqueId, new CleanupEntry() { Client = client, TimeOfInsertion = DateTime.Now });
            }
        }

        void client_OnDisconnect(Client client)
        {
            Statistics.RemoveConnectedClient();

            lock (_cleanupPool)
            {
                lock (_cleanupPool)
                {
                    if (_cleanupPool.ContainsKey(client.UniqueId))
                        _cleanupPool.Remove(client.UniqueId);
                }                
            }
        }

        void client_OnPolicyRequest(Client client)
        {
            Statistics.CountRequest();

            byte[] buf = Encoding.ASCII.GetBytes("<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"*\" /></cross-domain-policy>\0");

            try
            {
                client.Send(buf, 0, buf.Length);
            }
            catch (Exception)
            {                
                try
                {
                    client.Stop();
                }
                catch (Exception) { }
            }
        }
    }
}
