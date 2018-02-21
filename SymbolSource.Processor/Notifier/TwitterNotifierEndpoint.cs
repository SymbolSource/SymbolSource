using System.Threading.Tasks;
using SymbolSource.Contract;
using TweetSharp;

namespace SymbolSource.Processor.Notifier
{
    public class TwitterNotifierEndpoint : INotifierEndpoint
    {
        private readonly TwitterService twitter;

        public TwitterNotifierEndpoint()
        {
            twitter = new TwitterService(
                "zOXRJFIWrqeGeZGBHp9TgqZM1", 
                "WyGgX82kkA7xyJrzi8Fz3tdpRPYNxm7wP5zTSIvnxlVaqjthRO");

            twitter.AuthenticateWith(
                "3416201481-EKzqhhF330g1N15NVbOSFV9i4SLPVjX6IE32RPo",
                "WvSFv0pgF3S9u1nwQUJSvCBZkxN1xiMyLCxvatnhHDpo8");
        }

        private async Task Send(UserInfo userInfo, string format, params object[] args)
        {
            if (userInfo.UserHandle == null || !userInfo.UserHandle.StartsWith("@"))
                return;

            await twitter.SendTweetAsync(new SendTweetOptions
            {
                Status = userInfo.UserHandle + " " + string.Format(format, args)
            });
        }

        public async Task Starting(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "thanks for submitting {0} - I just started processing", packageName);
        }

        public async Task Damaged(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "I can't open {0}, if that's the right name of the package - @SymbolSource please help!", packageName);
        }

        public async Task Indexed(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "good news - I just finished processing {0}", packageName);
        }

        public async Task Deleted(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "good news - I just finished deleting {0}", packageName);
        }

        public async Task PartiallyIndexed(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "something went wrong while I was indexing {0} - @SymbolSource please help!", packageName);
        }

        public async Task PartiallyDeleted(UserInfo userInfo, PackageName packageName)
        {
            await Send(userInfo, "something went wrong while I was deleting {0} - @SymbolSource please help!", packageName);
        }
    }
}