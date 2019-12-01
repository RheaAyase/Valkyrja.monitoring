using System;
using System.Threading.Tasks;
using Discord;

namespace Valkyrja.monitoring
{
    public class Program
    {
	    public static SigrunClient Sigrun = null;

        public static void Main(string[] args)
        {
	        Connect();

			while( true )
			{
				Task.Delay(300000).Wait();
				if( Sigrun.Client.ConnectionState == ConnectionState.Disconnected )
					Connect();
			}
		}

	    public static void Connect()
	    {
		    Console.WriteLine("Valkyrja.Monitoring: Connecting...");

		    if( Sigrun != null )
			    Sigrun.Client.Dispose();
		    Sigrun = new SigrunClient();
		    Sigrun.Connect().Wait();

		    Console.WriteLine("Valkyrja.Monitoring: Connected.");
	    }
    }
}
