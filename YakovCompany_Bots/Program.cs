using Newtonsoft.Json;
using System;
using System.Security.AccessControl;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YakovCompany_bot.Model;

namespace YakovCompany_bot
{
    internal class Worker
    {

        public const string TokenBot = "***";
        private static void Main(string[] args)
        {
                Bots();
            Console.ReadLine();
        }

        public static async void Bots()
        {
            Console.WriteLine("Запуск Телеграмм бота");
            string TELEGRAM_TOKEN = TokenBot;
            var botclient = new TelegramBotClient(TELEGRAM_TOKEN);
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };
            botclient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
                );
            var me = await botclient.GetMeAsync();
            await Task.Delay(int.MaxValue);
            cts.Cancel();
            long chatidpublic = 0;
            var waiter = new ManualResetEventSlim(false);
            waiter.Wait();
            Console.WriteLine("Stopping bot");
        }

        private static async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API ERROR:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
        }
        public List<Report> _reports = new List<Report>();
        async static Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            Chat chatid = new Chat();
            if (update.Message != null)
                chatid = update.Message.Chat;
            if (update.CallbackQuery != null)
                if (update.CallbackQuery.Message != null)
                    chatid = update.CallbackQuery.Message.Chat;
            var QuestionMenu = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да","Yes")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Нет","NO")
                }
                });
            var FirstMenu = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Я предприниматель / Руководитель бизнеса","Bisness_BT")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Я руководитель высшего звена","High_BT")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Я работаю в HR / Внутренних коммуникациях","HR_BT")
                }
                });
            if (update.Message != null && update.Message.Text != null)
            {
                try
                {
                    if (message.Text.ToLower().Contains("/admin") && !System.IO.File.Exists($"{Environment.CurrentDirectory}/Admin.json"))
                    {
                        var admin = new Appconfig.Admin();
                        admin.chatid = chatid.Id;
                        System.IO.File.WriteAllText($"{Environment.CurrentDirectory}/Admin.json", JsonConvert.SerializeObject(admin));
                        await client.SendTextMessageAsync(chatid, "Администратор добавлен");
                        return;
                    }

                    if (message.Text.ToLower() == "/start" && message.ReplyToMessage == null)
                    {
                        if (System.IO.File.Exists($"{Environment.CurrentDirectory}/Reports/{chatid.Id.ToString()}.json"))
                            System.IO.File.Delete($"{Environment.CurrentDirectory}/Reports/{chatid.Id.ToString()}.json");

                        CheckReportFile(chatid, "/start");
                        await client.SendTextMessageAsync(chatid, text: "Введите своё ФИО:", replyMarkup: new ForceReplyMarkup { Selective = true });
                        return;
                    }
                    else
                    {
                        var report = OpenReportfile(chatid.Id);
                        if (update.Message.ReplyToMessage != null || report.chatid != null)
                        {
                            if (report.Nickname == null || (update.Message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите своё ФИО:")))
                            {
                                CheckReportFile(chatid, "Введите своё ФИО:" + message.Text);
                                DropTwoMessage(update, client);
                                await client.SendTextMessageAsync(chatid, $"Выберите свою должность: ", replyMarkup: FirstMenu);
                                return;
                            }
                            if (report.NameOrg == null || (update.Message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введите названия компании:")))
                            {
                                report = CheckReportFile(chatid, "Введите названия компании: " + message.Text);
                                DropTwoMessage(update, client);
                                await client.SendTextMessageAsync(chatid, $"Вопрос:\n{ProcessAnswer(chatid.Id, client, report)}", replyMarkup: QuestionMenu);
                                return;
                            }
                            //if (report.questions == null||report.questions.Count<13 || (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text.Contains("Вопрос:")))
                            // {
                            //    report = CheckReportFile(chatid, "Вопрос: " + message.Text);
                            //    DropTwoMessage(update, client);
                            //    await client.SendTextMessageAsync(chatid, $"Вопрос:\n{ProcessAnswer(Convert.ToInt32(chatid.Id),client, report)}");
                            //    return;
                            //}
                        }


                    }
                }
                catch
                {

                }
            }


            if (update.CallbackQuery != null)
            {
                if (update.CallbackQuery.Data == "Bisness_BT")
                {
                    CheckReportFile(chatid, "должность :" + "Я предприниматель / Руководитель бизнеса");
                    DropOneMessage(update, client);
                    await client.SendTextMessageAsync(chatid, text: "Введите названия компании:", replyMarkup: new ForceReplyMarkup { Selective = true });
                    return;
                }
                if (update.CallbackQuery.Data == "High_BT")
                {
                    CheckReportFile(chatid, "должность :" + "Я руководитель высшего звена");
                    DropOneMessage(update, client);
                    await client.SendTextMessageAsync(chatid, text: "Введите названия компании:", replyMarkup: new ForceReplyMarkup { Selective = true });
                    return;
                }
                if (update.CallbackQuery.Data == "HR_BT")
                {
                    CheckReportFile(chatid, "должность :" + "Я работаю в HR / Внутренних коммуникациях");
                    DropOneMessage(update, client);
                    await client.SendTextMessageAsync(chatid, text: "Введите названия компании:", replyMarkup: new ForceReplyMarkup { Selective = true });
                    return;
                }
                if (update.CallbackQuery.Data == "Yes")
                {
                    var report = CheckReportFile(chatid, "Вопрос: " + "Да");
                    var message1 = ProcessAnswer(chatid.Id, client, report);
                    DropOneMessage(update, client);
                    if (message1 == "\U0001f7e1 Компания в целом готова к реализации изменений. Есть зоны для улучшения.\r\n\r\nПроведена хорошая работа к старту изменений. \r\n\r\n➡️ Чтобы изменения завершились успешно, и компания получила результат, обратите внимание на следующие шаги:\r\n\r\n1. Проверьте, все ли сотрудники точно понимают свою роль в изменениях\r\n2. Проверьте, насколько тщательно проработан план по управлению изменениями (предусмотрены ли коммуникации и обучение всех категорий сотрудников от руководителей до линейного персонала)\r\n3. В процессе изменений регулярно собирайте обратную связь с сотрудников  и проводите аудит плана по управлению изменениями\r\n4. На основании аудита и обратной связи инициируйте необходимые корректирующие мероприятия")
                    {
                        await client.SendTextMessageAsync(chatid, message1);
                        return;
                    }
                    await client.SendTextMessageAsync(chatid, $"Вопрос:\n{message1}", replyMarkup: QuestionMenu);
                }
                if (update.CallbackQuery.Data == "NO")
                {
                    var report = CheckReportFile(chatid, "Вопрос: " + "Нет");
                    var message1 = ProcessAnswer(chatid.Id, client, report);
                    if (message1 == "\U0001f7e1 Компания в целом готова к реализации изменений. Есть зоны для улучшения.\r\n\r\nПроведена хорошая работа к старту изменений. \r\n\r\n➡️ Чтобы изменения завершились успешно, и компания получила результат, обратите внимание на следующие шаги:\r\n\r\n1. Проверьте, все ли сотрудники точно понимают свою роль в изменениях\r\n2. Проверьте, насколько тщательно проработан план по управлению изменениями (предусмотрены ли коммуникации и обучение всех категорий сотрудников от руководителей до линейного персонала)\r\n3. В процессе изменений регулярно собирайте обратную связь с сотрудников  и проводите аудит плана по управлению изменениями\r\n4. На основании аудита и обратной связи инициируйте необходимые корректирующие мероприятия")
                    {
                        await client.SendTextMessageAsync(chatid, message1);
                        return;
                    }
                    DropOneMessage(update, client);
                    await client.SendTextMessageAsync(chatid, $"Вопрос:\n{message1}", replyMarkup: QuestionMenu);
                    return;
                }
            }

        }

        public static Report OpenReportfile(long chat)
        {
            var report = JsonConvert.DeserializeObject<Report>(System.IO.File.ReadAllText($"{Environment.CurrentDirectory}/Reports/{chat.ToString()}.json"));
            return report;
        }

        public static void SaveReportFileQuestions(long chat, Report report)
        {
            System.IO.File.WriteAllText($"{Environment.CurrentDirectory}/Reports/{chat.ToString()}.json", JsonConvert.SerializeObject(report));
        }

        public static string ProcessAnswer(long id, ITelegramBotClient client, Report report)
        {
            var questions = JsonConvert.DeserializeObject<List<Question>>(System.IO.File.ReadAllText($"{Environment.CurrentDirectory}/questions.json"));
            var currentquestions = new List<Question>();
            currentquestions = questions;
            Random random = new Random();
            if (report.questions.Count == questions.Count)
            {
                SendAdminReport(id, client, report);
                return "\U0001f7e1 Компания в целом готова к реализации изменений. Есть зоны для улучшения.\r\n\r\nПроведена хорошая работа к старту изменений. \r\n\r\n➡️ Чтобы изменения завершились успешно, и компания получила результат, обратите внимание на следующие шаги:\r\n\r\n1. Проверьте, все ли сотрудники точно понимают свою роль в изменениях\r\n2. Проверьте, насколько тщательно проработан план по управлению изменениями (предусмотрены ли коммуникации и обучение всех категорий сотрудников от руководителей до линейного персонала)\r\n3. В процессе изменений регулярно собирайте обратную связь с сотрудников  и проводите аудит плана по управлению изменениями\r\n4. На основании аудита и обратной связи инициируйте необходимые корректирующие мероприятия";
            }
            var question = currentquestions[random.Next(0, currentquestions.Count)];
            if (report.questions.Any(a => new QuestionComparer().Equals(a, question)))
            {
                // вопрос уже есть в отчете
                ProcessAnswer(id, client, report);
            }
            if (!report.questions.Contains(question, new QuestionComparer()))
            {
                report.questions.Add(question);
                SaveReportFileQuestions(id, report);
            }
            return question.Text;
        }

        public class QuestionComparer : IEqualityComparer<Question>
        {
            public bool Equals(Question x, Question y)
            {
                return x.Text == y.Text;
            }

            public int GetHashCode(Question obj)
            {
                return obj.Text.GetHashCode();
            }
        }

        public static async void SendAdminReport(long chat, ITelegramBotClient client, Report report)
        {
            string questions = null;
            foreach (var question in report.questions.OrderBy(a => a.id))
            {
                questions = questions + "\n" + question.id + ". " + question.Text + " : \n" + question.Answer;
            }
            var message = $"ФИО: {report.FIO};\r\nДолжность: {report.Nickname};\r\nНазвания организации: {report.NameOrg};\r\nОтветы: {questions}";
            try
            {
                await client.SendTextMessageAsync(JsonConvert.DeserializeObject<Report>(System.IO.File.ReadAllText($@"{Environment.CurrentDirectory}/Admin.json")).chatid, message);
            }
            catch
            {

            }
        }

        public static Report CheckReportFile(Chat chat, string content)
        {

            if (!System.IO.File.Exists($"{Environment.CurrentDirectory}/Reports/{chat.Id.ToString()}.json") || content == "/start")
            {
                CreateFolderReports(chat);
                Report report = new Report();
                report.chatid = chat.Id;
                System.IO.File.WriteAllText($"{Environment.CurrentDirectory}/Reports/{chat.Id.ToString()}.json", JsonConvert.SerializeObject(report));
            }
            else
            {
                var CurrentData = new Report();
                CurrentData = JsonConvert.DeserializeObject<Report>(System.IO.File.ReadAllText($@"{Environment.CurrentDirectory}/Reports/{chat.Id.ToString()}.json"));
                if (content.Contains("Вопрос"))
                {
                    Question question = new Question();
                    question.Answer = content.Split(":")[1];
                    CurrentData.questions.Last().Answer = question.Answer;
                }
                if (content.Contains("ФИО"))
                {
                    CurrentData.FIO = content.Split(":")[1];
                }
                if (content.Contains("компании"))
                {
                    CurrentData.NameOrg = content.Split(":")[1];
                }
                if (content.Contains("должность"))
                {
                    CurrentData.Nickname = content.Split(":")[1];
                }
                SaveReportFileQuestions(chat.Id, CurrentData);
                return CurrentData;
            }
            return new Report();
        }

        public static async void CreateFolderReports(Chat chatId)
        {
            if (!Directory.Exists($"{Environment.CurrentDirectory}/Reports"))
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}/Reports");
            }
        }


        public static async void DropOneMessage(Update update, ITelegramBotClient client)
        {
            try
            {
                var message = update.CallbackQuery.Message;
                var chatid = update.CallbackQuery.Message.Chat;
                var messageId = message.MessageId;
                await client.DeleteMessageAsync(chatid, messageId);
            }
            catch
            {

            }
        }


        public static async void DropTwoMessage(Update update, ITelegramBotClient client)
        {
            try
            {
                var message = update.Message;
                var chatid = update.Message.Chat;
                var messageId = message.MessageId;
                var messageId1 = message.MessageId - 1;
                await client.DeleteMessageAsync(chatid, messageId1);
                await client.DeleteMessageAsync(chatid, messageId);
            }
            catch
            {

            }
        }
    }
}
