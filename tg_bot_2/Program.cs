using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Collections.Generic;
using static TelegramChatBot.Quiz;
using static System.Formats.Asn1.AsnWriter;
using File = System.IO.File;
using Newtonsoft.Json;

namespace TelegramChatBot
{

    // tg_bot
    class Program
    {
        private static Quiz quiz;
        private static TelegramBotClient botClient;
        private static Dictionary<long, QuestionState> States;
        private static Dictionary<long, int> UserScores;
        private static string StateFilename = "state.json";
        static void Main(string[] args)
        {
            quiz = new Quiz("data.txt");
            States = new Dictionary<long, QuestionState>();
            if (File.Exists(StateFilename))
            {
                var json = File.ReadAllText(StateFilename);
                States = JsonConvert.DeserializeObject<Dictionary<long, QuestionState>>(json);

            }
            UserScores = new Dictionary<long, int>();
            Console.WriteLine("Привет, Ванико!");
            var token = "6192598129:AAENGVhFkZQdvF_TWJ9p2OvUbhpx53CiaUI";
            botClient = new TelegramBotClient(token);
            botClient.OnMessage += BotClient_OnMessage;
            botClient.StartReceiving();
            Console.ReadLine();
            var stateJson = JsonConvert.SerializeObject(States);
            File.WriteAllText(StateFilename, stateJson);
        }

        private static void BotClient_OnMessage(object? sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var questionItem = quiz.NextQuestion();
            var chatID = e.Message.Chat.Id;
            if (!States.TryGetValue(chatID, out var state))
            {
                state = new QuestionState();
                States[chatID] = state;

            }

            if (state.CurrentItem == null)
            {
                state.CurrentItem = quiz.NextQuestion();
            }

            var question = state.CurrentItem;
            var tryAnswer = e.Message.Text.ToLower().Replace('ё', 'е');
            if (tryAnswer == question.Answer)
            {

                var fromId = e.Message.From.Id;
                if (UserScores.ContainsKey(fromId))
                {
                    UserScores[fromId]++;

                }
                else
                {
                    UserScores[fromId] = 1;
                }

                botClient.SendTextMessageAsync(chatID, $"Answer correct.\nУ вас {UserScores[fromId]} очков");
                NewRound(chatID);
            }
            else
            {
                state.Opened++;
                if (state.IsEnd)
                {
                    botClient.SendTextMessageAsync(chatID, $"Никто не угадала. Правильный ответ -> {question.Answer}");
                    NewRound(chatID);
                }
                botClient.SendTextMessageAsync(chatID, state.DisplayQuestion);


            }




        }

        public static void NewRound(long chatID)
        {
            if (!States.TryGetValue(chatID, out var state))
            {
                state = new QuestionState();
                States[chatID] = state;

            }



            state.CurrentItem = quiz.NextQuestion();
            state.Opened = 0;
            botClient.SendTextMessageAsync(chatID, state.DisplayQuestion);
        }


    }

    public class Quiz
    {
        public List<QuestionItem> Questions { get; set; }

        private Random random;
        private int count;
        public Quiz(string path = "data.txt")
        {
            var lines = System.IO.File.ReadAllLines(path);
            Questions = lines
                .Select(line => line.Split('|'))
                .Select(line => new QuestionItem()
                {
                    Question = line[0],
                    Answer = line[1]
                })
                .ToList();
            random = new Random();
            count = Questions.Count;
            var score = 0;
        }

        public QuestionItem NextQuestion()
        {
            var index = random.Next(count - 1);
            var question = Questions[index];
            return question;
        }



    }

    public class QuestionItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class QuestionState
    {
        public QuestionItem CurrentItem { get; set; }
        public int Opened { get; set; }
        public string AnswerHint => CurrentItem.Answer
                    .Substring(0, Opened)
                    .PadRight(CurrentItem.Answer.Length, '_');
        public string DisplayQuestion => $"{CurrentItem.Question}: {CurrentItem.Answer.Length} букв \n{AnswerHint}";
        public bool IsEnd => Opened == CurrentItem.Answer.Length;

    }



}

