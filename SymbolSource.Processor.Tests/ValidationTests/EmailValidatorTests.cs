using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SymbolSource.Processor.Notifier;
using Xunit;

namespace SymbolSource.Processor.Tests.ValidationTests
{
    public class EmailValidatorTests
    {
        [Fact]
        public void TestCorrectAddresses()
        {
            string email1 = "tomasz.kujawa@gmail.com";
            string email2 = "tomasz.kujawa+t@gmail.com";
            string email3 = "tomasz.kujawa2@gmail.com";
            string email4 = "tomasz_kujawa@gmail.com";
            string email5 = "tomaszKujawa@gmail.com";
            var result1 = EmailValidator.IsValidEmailAddress(email1);
            var result2 = EmailValidator.IsValidEmailAddress(email2);
            var result3 = EmailValidator.IsValidEmailAddress(email3);
            var result4 = EmailValidator.IsValidEmailAddress(email4);
            var result5 = EmailValidator.IsValidEmailAddress(email5);
            
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.True(result4);
            Assert.True(result5);
        }

        [Fact]
        public void TestEmptyLogin()
        {
            string email = "@gmail.com";
            var result = EmailValidator.IsValidEmailAddress(email);

            Assert.False(result);
        }

        [Fact]
        public void TestEmptyDomain()
        {
            string email = "tomasz.kujawa";
            var result = EmailValidator.IsValidEmailAddress(email);

            Assert.False(result);
        }

        [Fact]
        public void TestWrongDomain()
        {
            string email = "tomasz.kujawa@gmail";
            var result = EmailValidator.IsValidEmailAddress(email);

            Assert.False(result);
        }

        [Fact]
        public void TestDoubleAt()
        {
            string email = "tomasz@kujawa@gmail.com";
            var result = EmailValidator.IsValidEmailAddress(email);

            Assert.False(result);
        }
    }
}
