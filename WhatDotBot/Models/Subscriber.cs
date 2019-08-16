using Telegram.Bot.Types;

namespace WhatDotBot.Models
{
    public enum Subscription
    {
        All,
        None
    }

    public class Subscriber
    {
        public Subscriber()
        {
        }

        public Subscriber(Message message)
        {
            ChatId = message.Chat.Id;
            Name = $"{message.Chat.FirstName} {message.Chat.LastName}";
            Sub = Subscription.All;
        }

        public long ChatId { get; set; }
        public string Name { get; set; }
        public Subscription Sub { get; set; }
    }
}