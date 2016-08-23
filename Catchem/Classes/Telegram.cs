using System;
using System.Collections.Generic;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Markup;
using Catchem.Pages;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;

namespace Catchem.Classes
{
    public class Telegram
    {
        public static bool _stopTelegram = false;
        public static TelegramBot _telegram;
        public Queue<Update> _telegramMessages;
        public bool _firstMessageUpdate = true;

        public async void Start(string accessToken)
        {
            
           if (string.IsNullOrEmpty(accessToken))
            {
                TelegramLog("Error: Please enter Telegram HTTP API token.");
                return;
            }
            
            _telegram = new TelegramBot(accessToken);
            var me = await _telegram.MakeRequestAsync(new GetMe());
            if (me == null)
            {
                TelegramLog("Error: Please enter Telegram HTTP API token.");
                //Log to console  [08:56:32](TLGRM-ERR)Failed to start Telegram. Please check API Token.
                //Stops Telegram Bot From Being Used
                return;
            }
            TelegramLog("Telegram Started Successfuly with account: @" + me.Username);
           await UpdateMessages();
        }

        public void Stop()
        {
            _stopTelegram = Equals(true);
        }


        public async Task UpdateMessages()
        {
            long offset = 0;
            while (!_stopTelegram)
            {
                var updates = _telegram.MakeRequestAsync(new GetUpdates() {Offset = offset}).Result;
                if (updates != null)
                {
                    if (_firstMessageUpdate)
                    {
                        if (updates.Count() > 1)
                        {
                            if (updates[(updates.Count() - 1)].Message == null) continue;
                            _telegramMessages.Enqueue(updates[updates.Count() - 1]);
                        }
                        _firstMessageUpdate = false;
                    }
                    else if (_firstMessageUpdate == false)
                    {
                        foreach (var update in updates)
                        {
                            offset = update.UpdateId + 1;
                            if (update.Message == null)
                            {
                                continue;
                            }
                            _telegramMessages.Enqueue(update);
                        }
                    }
                    await UseMessage();
                }
            }
        }

        public async Task UseMessage()
        {
            while (_telegramMessages.Count > 0)
            {
                var update = _telegramMessages.Dequeue();
                var messageReceived = update.Message.Text;
                string[] splitMessage = messageReceived.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                TelegramLog("Recived Message From " + update.Message.From.Username);
                if (string.IsNullOrEmpty(messageReceived)) continue;
                if (messageReceived == "help")
                {
                    string helpMsg = "The following commands are avaliable: \n" +
                                     "- listbots \n" +
                                     "- start [bot Number / all] \n" +
                                     "- stop [bot Number / all]";
                    await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, helpMsg));
                    continue;
                }
                if (messageReceived.ToLower() == "listbots")
                {
                    int _botNumber = 0;
                    string botsString = "Current Bots Avaliable: \n";
                    foreach (var bot in MainWindow.BotsCollection)
                    {
                        _botNumber++;
                        string status = "STOPPED";
                        if (bot.Started) status = "RUNNING";
                        botsString += $"{_botNumber}) {bot.ProfileName} [{status}] \n";
                    }
                    if (_botNumber == 0)
                        await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "There are no bots created"));
                    if (_botNumber > 0)
                        await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, botsString));
                    continue;
                }
                if (messageReceived.ToLower() == "start all")
                {
                    foreach (var bot in MainWindow.BotsCollection)
                    {
                        if (bot.Started == false)
                        {
                            bot.Start();
                        }
                    }
                    await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped all running Bots"));
                    continue;
                }
                if (splitMessage[0].ToLower() == "start")
                {
                    if (splitMessage.Length != 2) continue;
                    if (string.IsNullOrEmpty(splitMessage[1]) || string.IsNullOrEmpty(splitMessage[2])) continue;
                    int selectedBot;
                    if (int.TryParse(splitMessage[1], out selectedBot))
                    {
                        int botAmount = MainWindow.BotsCollection.Count;
                        if (selectedBot > botAmount || selectedBot <= 0)
                        {
                           await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "No such bot Exists"));
                            continue;
                        }
                        selectedBot --;
                        if (MainWindow.BotsCollection.ElementAt(selectedBot).Started == false)
                            MainWindow.BotsCollection.ElementAt(selectedBot).Start();
                        await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Started Bot: " + MainWindow.BotsCollection.ElementAt(selectedBot).ProfileName));
                    }
                    continue;
                }

                if (messageReceived.ToLower() == "stop all")
                {
                    foreach (var bot in MainWindow.BotsCollection)
                    {
                        if (bot.Started)
                        {
                            bot.Stop();
                        }
                    }
                    await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped all running Bots"));
                    continue;
                }

                if (splitMessage[0].ToLower() == "stop")
                {
                    if (splitMessage.Length != 2) continue;
                    if (string.IsNullOrEmpty(splitMessage[1]) || string.IsNullOrEmpty(splitMessage[2])) continue;
                    int selectedBot;
                    if (int.TryParse(splitMessage[1], out selectedBot))
                    {
                        int botAmount = MainWindow.BotsCollection.Count();
                        if (selectedBot > botAmount || selectedBot <= 0)
                        {
                            await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "No such Bot Exists"));
                            continue;
                        }
                        selectedBot --;
                        if (MainWindow.BotsCollection.ElementAt(selectedBot).Started)
                            MainWindow.BotsCollection.ElementAt(selectedBot).Stop();
                        await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped Bot: " + MainWindow.BotsCollection.ElementAt(selectedBot).ProfileName));
                    }
                    continue;
                }
                if (messageReceived.Length >= 50)
                {
                   await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Unknown Command"));
                }
                else
                {
                    await _telegram.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Unknown Command: \n" + messageReceived));
                }
                await Task.Delay(50);
            }  
        }

        public static void TelegramLog(string logMessage)
        {
            foreach (var bot in MainWindow.BotsCollection)
            {
                bot.Session.EventDispatcher.Send(new TelegramMessageEvent
                {
                    Message = logMessage
                });
            }
        }

        
    }
}
