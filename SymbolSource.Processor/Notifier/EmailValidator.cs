using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SymbolSource.Processor.Notifier
{
    public static class EmailValidator
    {
        public static bool IsValidEmailAddress(this string email)
        {

            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None,
                    TimeSpan.FromMilliseconds(200));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email, @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(280));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            domainName = idn.GetAscii(domainName);
            
            return match.Groups[1].Value + domainName;
        }

    }
}
