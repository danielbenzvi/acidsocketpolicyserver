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
    class Program
    {
        static void Main(string[] args)
        {
            Console.Out.WriteLine("[" + DateTime.Now.ToString() + "] - Adobe socket policy server - starting on port 843...");
            Statistics.Start();

           retry:

            try
            {
                Logic logic = new Logic(843);

                logic.Start();

                while (logic.IsListening)
                {
                    Thread.Sleep(1000);
                    Console.Out.WriteLine("[Connected clients: " + Statistics.ConnectedClients + ", Requests: " + Statistics.RequestsPerSecond + "/sec, Uptime: " + Statistics.Uptime.ToString() + "]");
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Failed to listen: " + ex.Message + " retrying in 5 seconds...");
                Thread.Sleep(5000);

                goto retry;
            }
        }
    }
}
