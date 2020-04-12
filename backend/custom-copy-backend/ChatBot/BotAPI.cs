// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using custom_copy_backend.DataBase;
using custom_copy_backend.Extractor;
using custom_copy_backend.Setting;
using custom_copy_backend.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace custom_copy_backend.ChatBot
{
    public class BotAPI
    {
        public static Task ProcessMessage<T>(T bot, BotUserIdentifier user, string msg) where T : BotModel
        {
            return Task.Run(async () =>
            {
                try
                {
                    BotManager.Instance.Messages.Add(new ChatMessage { ChatBotName = typeof(T).Name, Identification = user.ToString(), RawMessage = msg });

                    var command = msg.Split(' ')[0];
                    var filter = new HashSet<string>();

                    // IsSenario = 0/1
                    // Senario = <string>
                    var option = new Dictionary<string, string>();

                    Log.Logs.Instance.Push("[Bot] Received Message - " + msg + "\r\n" + Log.Logs.SerializeObject(user));

                    UserDBModel userdb = null;
                    var query_user = BotManager.Instance.Users.Where(x => x.ChatBotId == user.ToString()).ToList();
                    if (query_user != null && query_user.Count > 0)
                    {
                        userdb = query_user[0];
                    }

                    switch (command.ToLower())
                    {
                        default:
                            await bot.SendMessage(user, $"'{command}'은/는 적절한 명령어가 아닙니다!\r\n자세한 정보는 '/help' 명령어를 통해 확인해주세요!");
                            break;
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        Log.Logs.Instance.PushError("[Bot Error] " + e.Message + "\r\n" + e.StackTrace);
                        await bot.SendMessage(user, "서버오류가 발생했습니다! 관리자에게 문의해주세요!");
                    }
                    catch (Exception e2)
                    {
                        Log.Logs.Instance.PushError("[Bot IError] " + e2.Message + "\r\n" + e2.StackTrace);
                    }
                }

            });
        }
    }
}
