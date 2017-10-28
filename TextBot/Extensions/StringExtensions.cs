using System;
using System.Collections.Generic;
using System.Text;

namespace TextBot.Extensions
{
    public static class StringExtensions
    {
        public static string[] Split(this string input, StringSplitOptions options, params char[] seperator)
        {
            return input.Split(seperator, options);
        }
    }
}
