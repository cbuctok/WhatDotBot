namespace WhatDotBot.Models
{
    using System;
    using System.Collections.Generic;
    using Telegram.Bot.Types;

    public enum Subscription
    {
        All,
        None
    }

    public class Subscriber : IEquatable<Subscriber>
    {
        public Subscriber()
        {
        }

        public long ChatId { get; set; }
        public string Name { get; set; }
        public Subscription Sub { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Subscriber);
        }

        public bool Equals(Subscriber other)
        {
            return other != null
                && ChatId == other.ChatId
                && Name == other.Name
                && Sub == other.Sub;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ChatId, Name, Sub);
        }

        public static bool operator ==(Subscriber left, Subscriber right)
        {
            return EqualityComparer<Subscriber>.Default.Equals(left, right);
        }

        public static bool operator !=(Subscriber left, Subscriber right)
        {
            return !(left == right);
        }
    }
}