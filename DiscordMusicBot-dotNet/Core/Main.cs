﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot_dotNet.Commands;
using DiscordMusicBot_dotNet.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordMusicBot_dotNet.Core {
    class Main {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private TokenManager _token;
        private AudioService _audio;

        public async Task MainAsync() {
            var config = new DiscordSocketConfig {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            _services = ConfigureServices();
            _client = new DiscordSocketClient(config);
            _client.Log += Log;
            _client.Ready += ReadyAsync;
            _services.GetRequiredService<CommandService>().Log += Log;
            //_commands = new CommandService();
            //_client.MessageReceived += CommandRecieved;
            _client.SlashCommandExecuted += SlashCommandManager.SlashCommandHandler;
            _token = new TokenManager();
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.LoginAsync(TokenType.Bot, _token.DiscordToken);
            await _client.StartAsync();
            await _client.SetGameAsync(null);
            await Task.Delay(-1);
        }

        private async Task CommandRecieved(SocketMessage messageParam) {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            var argPos = 0;
            var context = new SocketCommandContext(_client, message);

            if (message.HasCharPrefix('*', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess) {
                    await context.Channel.SendMessageAsync("コマンドが見つかりませんでした。*helpでコマンド一覧を表示することができます。");
                }
            }
        }

        private async Task ReadyAsync() {
            _audio = new AudioService(_client);
            await SlashCommandManager.RegisterSlashCommandAsync(_client, _audio);
            Console.WriteLine($"{_client.CurrentUser} is connected!");
        }

        private ServiceProvider ConfigureServices() {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<AudioService>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage message) {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}
