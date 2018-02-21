using System;

namespace SymbolSource.Contract
{
    public static class HelperExtensions
    {
        public static string ToInternetBase64(this byte[] data)
        {
            //var builder = new StringBuilder();

            //foreach (var b in data)
            //    builder.Append(b.ToString("x2"));

            //return builder.ToString();

            var base64 = Convert.ToBase64String(data);
            base64 = base64.Replace('+', '-');
            base64 = base64.Replace('/', '_');
            return base64;
        }

        public static string ToEncodedSeconds(this DateTime datetime)
        {
            return new string(DecimalToBase(26 + 10, (int)(datetime.Subtract(new DateTime(2015, 1, 1))).TotalSeconds));           
        }

        private static char ConvertDigit(int digit)
        {
            if (digit < 10)
                return (char) ('0' + digit);
            digit -= 10;
            if (digit < 26)
                return (char) ('A' + digit);
            digit -= 26;
            return (char)('a' + digit);
        }

        private static char[] DecimalToBase(int baseval, int value)
        {
            var result = new []
            {
                ConvertDigit(0),
                ConvertDigit(0), 
                ConvertDigit(0), 
                ConvertDigit(0),
                ConvertDigit(0), 
                ConvertDigit(0)
            };

            int position = 0;

            while (value > 0 && position < result.Length)
            {
                result[position] = ConvertDigit(value % baseval);
                value = value / baseval;
                position++;
            }

            Array.Reverse(result);

            return result;
        }
    }
}
