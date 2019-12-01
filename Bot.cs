using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Discord;
using Discord.WebSocket;

using guid = System.UInt64;

namespace Valkyrja.monitoring
{
	public class SigrunClient
	{
		internal readonly DiscordSocketClient Client = new DiscordSocketClient();
		private  readonly Config Config = Config.Load();
		private  readonly Regex RegexCommandParams = new Regex("\"[^\"]+\"|\\S+", RegexOptions.Compiled);
		private readonly HttpClient HttpClient = new HttpClient();
		//private CancellationTokenSource MainUpdateCancel;
		//private Task MainUpdateTask;

		private bool Maintenance = false;
		private Dictionary<guid, string> Prefixes = new Dictionary<guid,String>();

		public SigrunClient()
		{
			this.Client.MessageReceived += ClientOnMessageReceived;
			this.Client.MessageUpdated += ClientOnMessageUpdated;
			this.Client.Disconnected += ClientDisconnected;
			this.Client.GuildAvailable += OnGuildAvailable;
		}

		public async Task Connect()
		{
			await this.Client.LoginAsync(TokenType.Bot, this.Config.BotToken).ConfigureAwait(false);
			await this.Client.StartAsync().ConfigureAwait(false);

			this.HttpClient.DefaultRequestHeaders.Accept.Clear();
			this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
			this.HttpClient.DefaultRequestHeaders.Add("User-Agent", "Valkyrja Monitoring");

			/*if( this.MainUpdateTask == null )
			{
				this.MainUpdateCancel = new CancellationTokenSource();
				this.MainUpdateTask = Task.Factory.StartNew(MainUpdate, this.MainUpdateCancel.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}*/
		}

//Update
		/*private async Task MainUpdate()
		{
			while( !this.MainUpdateCancel.IsCancellationRequested )
			{
				DateTime frameTime = DateTime.UtcNow;

				if( this.Client.ConnectionState != ConnectionState.Connected ||
				    this.Client.LoginState != LoginState.LoggedIn )
				{
					await Task.Delay(10000);
					continue;
				}

				//dostuff

				TimeSpan deltaTime = DateTime.UtcNow - frameTime;
				await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, (TimeSpan.FromSeconds(1f / this.Config.TargetFps) - deltaTime).TotalMilliseconds)));
			}
		}*/

		private void GetCommandAndParams(string message, out string command, out string trimmedMessage, out string[] parameters)
		{
			string input = message.Substring(this.Config.Prefix.Length);
			trimmedMessage = "";
			parameters = null;

			MatchCollection regexMatches = this.RegexCommandParams.Matches(input);
			if( regexMatches.Count == 0 )
			{
				command = input.Trim();
				return;
			}

			command = regexMatches[0].Value;

			if( regexMatches.Count > 1 )
			{
				trimmedMessage = input.Substring(regexMatches[1].Index).Trim('\"', ' ', '\n');
				Match[] matches = new Match[regexMatches.Count];
				regexMatches.CopyTo(matches, 0);
				parameters = matches.Skip(1).Select(p => p.Value).ToArray();
				for(int i = 0; i < parameters.Length; i++)
					parameters[i] = parameters[i].Trim('"');
			}
		}

		private Task ClientDisconnected(Exception exception)
		{
			Console.WriteLine($"Discord Client died:\n{  exception.Message}\nShutting down.");
			Environment.Exit(0); //HACK - The library often reconnects in really shitty way and no longer works
			return Task.CompletedTask;
		}

		private async Task ClientOnMessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage socketMessage, ISocketMessageChannel arg3)
		{
			await ClientOnMessageReceived(socketMessage);
		}

		private async Task ClientOnMessageReceived(SocketMessage socketMessage)
		{
			try
			{
				await HandleCommands(socketMessage);
			}
			catch( Exception e )
			{
				LogException(e, socketMessage.Author.Username + socketMessage.Author.Discriminator + ": " + socketMessage.Content);
			}
		}

		private async Task OnGuildAvailable(SocketGuild guild)
		{
			try
			{
				if( this.Config.UseApi && !this.Prefixes.ContainsKey(guild.Id) )
				{

					string prefix = await this.HttpClient.GetStringAsync($"https://valkyrja.app/api/prefixes/{guild.Id}");
					this.Prefixes.Add(guild.Id, prefix);
				}
			}
			catch(Exception exception)
			{
				LogException(exception, "--OnGuildAvailable: " + guild.Id);
			}
		}

		private async Task HandleCommands(SocketMessage socketMessage)
		{
			if( !(socketMessage.Channel is SocketGuildChannel channel && this.Prefixes.ContainsKey(channel.Guild.Id) && socketMessage.Content.StartsWith(this.Prefixes[channel.Guild.Id])) )
				return;

			string commandString = "", trimmedMessage = "";
			string[] parameters;
			GetCommandAndParams(socketMessage.Content, out commandString, out trimmedMessage, out parameters);
			string response = "";
			commandString = commandString.ToLower();

			try
			{
				switch( commandString )
				{
					case "maintenance":
						if( this.Config.AdminIDs.Contains(socketMessage.Author.Id) )
						{
							this.Maintenance = true;
							response = "State: `Down for Maintenance`";
						}
						break;
					case "endmaintenance":
						if( this.Config.AdminIDs.Contains(socketMessage.Author.Id) )
						{
							this.Maintenance = false;
							response = "State: `Online`";
						}
						break;
					default:
						if( this.Maintenance )
						{
							if( Config.Commands.Contains(commandString) )
								response = this.Config.MaintenanceMessage;
						}
						else
						{
							bool alive = false;
							try
							{
								alive = !this.Config.UseApi || (bool.TryParse(await this.HttpClient.GetStringAsync("https://valkyrja.app/api/status"), out alive) && alive);
							}
							catch( HttpRequestException e )
							{
								LogException(e, channel.Guild.Id.ToString());
							}

							if( !alive)
								response = this.Config.DownMessage;
						}
						break;
				}
			}
			catch( Exception e )
			{
				response = "Command haz spit out an error: \n  " + e.Message;
				LogException(e, socketMessage.Author.Username + socketMessage.Author.Discriminator + ": " + socketMessage.Content);
			}

			if( !string.IsNullOrWhiteSpace(response) )
				await socketMessage.Channel.SendMessageAsync(response);
		}

		public void LogException(Exception e, string data = "")
		{
			Console.WriteLine("Exception: " + e.Message);

			if( string.IsNullOrWhiteSpace(data) )
				Console.WriteLine("Data: " + data);

			Console.WriteLine("Stack: " + e.StackTrace);
			Console.WriteLine(".......exception: " + e.Message);
		}
	}
}
