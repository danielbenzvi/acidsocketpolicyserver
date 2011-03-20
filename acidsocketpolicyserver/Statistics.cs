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
    public static class Statistics
    {        
        static int[] _requestSamples = new int[10];

        static int _currentRequests = 0;

        static int _connectedClients = 0;
        static DateTime _started = DateTime.Now;

        static bool _running;

        private static void StatisticsThread(object obj)
        {
            while (_running)
            {
                Thread.Sleep(950);

                int currentIncoming = Interlocked.Exchange(ref _currentRequests, 0);

                Interlocked.Exchange(ref _requestSamples[DateTime.Now.Second % _requestSamples.Length], _currentRequests);                
            }

            _running = false;
        }

        public static void Stop()
        {
            _running = false;
        }

        public static void Start()
        {
            if (!_running)
            {
                _running = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(StatisticsThread), null);
            }
        }

        public static void CountRequest()
        {
            Interlocked.Increment(ref _requestSamples[DateTime.Now.Second % _requestSamples.Length]);
        }



        public static double RequestsPerSecond
        {
            get
            {
                double val = 0;

                for (int i = 0; i < _requestSamples.Length; i++)
                    val += (double)_requestSamples[i];

                val /= (double)_requestSamples.Length;

                return val;
            }
        }

        public static void AddConnectedClient()
        {
            Interlocked.Increment(ref _connectedClients);
        }

        public static int ConnectedClients
        {
            get
            {
                return _connectedClients;
            }
        }

        public static void RemoveConnectedClient()
        {
            Interlocked.Decrement(ref _connectedClients);
        }
    
        public static TimeSpan Uptime
        {
            get
            {
                return DateTime.Now - _started;
            }
        }


    }
}
