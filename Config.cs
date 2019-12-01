using System;
using System.IO;
using Newtonsoft.Json;

using guid = System.UInt64;

namespace Valkyrja.monitoring
{
	public class Config
	{
		public const string Filename = "config.json";
		public const guid Rhea = 89805412676681728;
		public const string RheaName = "Rhea#1234";
		public const int MessageCharacterLimit = 2000;

		public string BotToken = "";
		public string Prefix = "!";
		public bool UseApi = false;
		//public float TargetFps = 0.03f;
		//public string Host = "127.0.0.1";
		//public string Port = "3306";
		//public string Username = "db_user";
		//public string Password = "db_password";
		//public string Database = "db_botwinder";
		//public guid PrintShardsOnGuildId;
		public guid[] AdminIDs = { Rhea, 89777099576979456 };
		public string[] Commands = { "ping", "help" };
		public string MaintenanceMessage = "Valkyrja is down for maintenance that may take a while - to receive updates please see the `#news` channel in **Valhalla**.";
		public string DownMessage = "Valkyrja is currently down for really quick maintenance!";

		private Config(){}
		public static Config Load()
		{
			string path = Filename;

			if( !File.Exists(path) )
			{
				string json = JsonConvert.SerializeObject(new Config(), Formatting.Indented);
				File.WriteAllText(path, json);
				Console.WriteLine("Default config created.");
				Environment.Exit(0);
			}

			Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
			return config;
		}

		public void Save()
		{
			string path = Path.Combine(Filename);
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		/*public string GetDbConnectionString()
		{
			return $"server={this.Host};userid={this.Username};pwd={this.Password};port={this.Port};database={this.Database};sslmode=none;";
		}*/
	}
}
