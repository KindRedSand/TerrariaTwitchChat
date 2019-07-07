using System;

namespace TwitchChat.Razorwing.Framework.Configuration
{
    public class BindableBool : Bindable<bool>
    {
        public BindableBool(bool value = false)
            : base(value)
        {
        }

        public static implicit operator bool(BindableBool value) => value != null && value.Value;

        public override string ToString() => Value.ToString();

        public override void Parse(object s)
        {
            string str = s as string;
            if (str == null)
                throw new InvalidCastException($@"Input type {s.GetType()} could not be cast to a string for parsing");

            Value = str == @"1" || str.Equals(@"true", StringComparison.OrdinalIgnoreCase);
        }

        public void Toggle() => Value = !Value;
    }
}
