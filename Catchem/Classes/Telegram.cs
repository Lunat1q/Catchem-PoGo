using System;
using System.Collections.Generic;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catchem.Events;
using PoGo.PokeMobBot.Logic.Event;
using TelegramMessageEvent = PoGo.PokeMobBot.Logic.Event.TelegramMessageEvent;

namespace Catchem.Classes
{
    public class Telegram
    {
        public static bool StopTelegram;
        public static TelegramBot TelegramBot;
        public Queue<Update> TelegramMessages;
        public bool FirstMessageUpdate = true;
        public IEventDispatcher EventDispatcher;
        private bool _started;

        public Telegram()
        {
            TelegramMessages = new Queue<Update>();
            EventDispatcher = new EventDispatcher();
            StopTelegram = true;
        }

        /// <summary>
        /// Start Tlgrm bot
        /// </summary>
        /// <param name="accessToken">API KEY</param>
        public async void Start(string accessToken)
        {
            if (_started) return;
            StopTelegram = false;
            if (string.IsNullOrEmpty(accessToken))
            {
                EventDispatcher.Send(new TelegramMessageEvent
                {
                    Message = "Error: Please enter Telegram HTTP API token."
                });
                return;
            }

            TelegramBot = new TelegramBot(accessToken) {WebProxy = WebRequest.DefaultWebProxy};
            TelegramBot.WebProxy.Credentials = CredentialCache.DefaultCredentials;
            try
            {
                var me = await TelegramBot.MakeRequestAsync(new GetMe());
                if (me == null)
                {
                    EventDispatcher.Send(new TelegramMessageEvent
                    {
                        Message = "Error: Please enter Telegram HTTP API token."
                    });
                    //Log to console  [08:56:32](TLGRM-ERR)Failed to start Telegram. Please check API Token.
                    //Stops Telegram Bot From Being Used
                    return;
                }
                EventDispatcher.Send(new TelegramMessageEvent
                {
                    Message = "Telegram Started Successfuly with account: @" + me.Username
                });
                UpdateMessagesWorker();
                ReadMessagesWorker();
                _started = true;
            }
            catch (Exception)
            {
                EventDispatcher.Send(new TelegramMessageEvent
                {
                    Message = "Error during request"
                });
            }
        }

        public void Stop()
        {
            StopTelegram = true;
            EventDispatcher.Send(new TelegramMessageEvent
            {
                Message = "Telegram bot has been stopped!"
            });
        }


        public async void UpdateMessagesWorker()
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
                }
                await Task.Delay(1000);
            }
            _started = false;
        }

        public async void SendToTelegram(string message, long chatId)
        {
            await TelegramBot.MakeRequestAsync(new SendMessage(chatId, message));
        }

        public async void ReadMessagesWorker()
        {
            while (!StopTelegram)
            {
                if (TelegramMessages != null && TelegramMessages.Count > 0)
                {
                    var update = TelegramMessages.Dequeue();
                    if (update == null) continue;
                    var messageReceived = update.Message.Text;
                    EventDispatcher.Send(new TelegramMessageEvent
                    {
                        Message = "Recived Message From " + update.Message.From.Username
                    });

                    if (string.IsNullOrEmpty(messageReceived)) continue;

                    var messageFractions = messageReceived.ToLower().Split(' ');
                    if (messageFractions.Length < 1) return;

                    EventDispatcher.Send(new TelegramCommandEvent()
                    {
                        Command = messageFractions[0],
                        Args = messageFractions.Where((x, i) => i > 0).ToArray(),
                        ChatId = update.Message.Chat.Id
                    });
                }
                await Task.Delay(50);
            }
        }
    }
}