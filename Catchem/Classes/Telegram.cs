using System;
using System.Collections.Generic;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;

namespace Catchem.Classes
{
    public class Telegram
    {
        public static bool StopTelegram;
        public static TelegramBot TelegramBot;
        public Queue<Update> TelegramMessages;
        public bool FirstMessageUpdate = true;

        public async void Start(string accessToken)
        {

            if (string.IsNullOrEmpty(accessToken))
            {
                TelegramLog("Error: Please enter Telegram HTTP API token.");
                return;
            }

            TelegramBot = new TelegramBot(accessToken);
            var me = await TelegramBot.MakeRequestAsync(new GetMe());
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
            StopTelegram = Equals(true);
        }


        public async Task UpdateMessages()
        {
            long offset = 0;
            while (!StopTelegram)
            {
                var updates = TelegramBot.MakeRequestAsync(new GetUpdates() { Offset = offset }).Result;
                if (updates != null)
                {
                    if (FirstMessageUpdate)
                    {
                        if (updates.Count() > 1)
                        {
                            if (updates[(updates.Count() - 1)].Message == null) continue;
                            TelegramMessages.Enqueue(updates[updates.Count() - 1]);
                        }
                        FirstMessageUpdate = false;
                    }
                    else if (FirstMessageUpdate == false)
                    {
                        foreach (var update in updates)
                        {
                            offset = update.UpdateId + 1;
                            if (update.Message == null)
                            {
                                continue;
                            }
                            TelegramMessages.Enqueue(update);
                        }
                    }
                    await UseMessage();
                }
            }
        }

        public async Task UseMessage()
        {
            while (TelegramMessages.Count > 0)
            {
                var update = TelegramMessages.Dequeue();
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
                    await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, helpMsg));
                    continue;
                }
                switch (messageReceived.ToLower())
                {
                    case "listbots":
                        var botNumber = 0;
                        var botsString = "Current Bots Avaliable: \n";
                        foreach (var bot in MainWindow.BotsCollection)
                        {
                            botNumber++;
                            string status = "STOPPED";
                            if (bot.Started) status = "RUNNING";
                            botsString += $"{botNumber}) {bot.ProfileName} [{status}] \n";
                        }
                        if (botNumber == 0)
                            await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "There are no bots created"));
                        if (botNumber > 0)
                            await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, botsString));
                        continue;
                    case "start all":
                        foreach (var bot in MainWindow.BotsCollection)
                        {
                            if (bot.Started == false)
                            {
                                bot.Start();
                            }
                        }
                        await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped all running Bots"));
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
                            await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "No such bot Exists"));
                            continue;
                        }
                        selectedBot--;
                        if (MainWindow.BotsCollection.ElementAt(selectedBot).Started == false)
                            MainWindow.BotsCollection.ElementAt(selectedBot).Start();
                        await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Started Bot: " + MainWindow.BotsCollection.ElementAt(selectedBot).ProfileName));
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
                    await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped all running Bots"));
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
                            await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "No such Bot Exists"));
                            continue;
                        }
                        selectedBot--;
                        if (MainWindow.BotsCollection.ElementAt(selectedBot).Started)
                            MainWindow.BotsCollection.ElementAt(selectedBot).Stop();
                        await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Stopped Bot: " + MainWindow.BotsCollection.ElementAt(selectedBot).ProfileName));
                    }
                    continue;
                }
                if (messageReceived.Length >= 50)
                {
                    await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Unknown Command"));
                }
                else
                {
                    await TelegramBot.MakeRequestAsync(new SendMessage(update.Message.Chat.Id, "Unknown Command: \n" + messageReceived));
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