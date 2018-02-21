using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SymbolSource.Contract.Storage;
using Xunit;

namespace SymbolSource.Contract.Tests
{
    internal static class StorageItemExtensions
    {
        public static async Task PutText(this IStorageItem item, string text)
        {
            using (var stream = await item.Put())
            {
                Assert.NotNull(stream);

                using (var writer = new StreamWriter(stream))
                    await writer.WriteAsync(text);
            }
        }

        public static async Task<string> GetText(this IStorageItem item)
        {
            using (var stream = await item.Get())
            {
                if (stream == null)
                    return null;

                using (var reader = new StreamReader(stream))
                    return await reader.ReadToEndAsync();
            }
        }
    }

    public abstract class StorageServiceTests : IDisposable
    {
        protected abstract IStorageService Storage { get; }

        public virtual void Dispose()
        {
            DisposeAsync().Wait();
        }

        private async Task DisposeAsync()
        {
            var internals = new List<string>();

            foreach (var feedName in Storage.QueryFeeds())
            {
                var feed = Storage.GetFeed(feedName);

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var internalName in feed.QueryInternals())
                    internals.Add(string.Format("{0}/{1}", feedName, internalName));

                if (!await feed.Delete())
                    internals.Add(feedName);
            }

            if (internals.Count > 0)
            {
                Debugger.Break();
                throw new Exception("Internal inconsistencies:\n" + string.Join("\n", internals));
            }
        }

        private static string GetRandomUserName()
        {
            return Path.GetRandomFileName();
        }

        private static string GetRandomItemContent()
        {
            return Path.GetRandomFileName();
        }

        private static async Task TestItemRoundtrip(IStorageItem item)
        {
            var expectedContent = GetRandomItemContent();
            Assert.False(await item.Exists());
            Assert.Null(await item.Get());
            Assert.False(await item.Delete());
            await item.PutText(expectedContent);

            var actualContent = await item.GetText();
            Assert.Equal(expectedContent, actualContent);

            Assert.True(await item.Exists());
            Assert.True(await item.Delete());
            Assert.False(await item.Exists());
            Assert.Null(await item.Get());
            Assert.False(await item.Delete());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestPackage(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var feed = Storage.GetFeed(feedName);

            var package = feed.GetPackage("userName", PackageState.New, packageName);
            await TestItemRoundtrip(package);
            Assert.Empty(await feed.QueryPackages("userName", PackageState.New));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestPackageWithoutUserName(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var feed = Storage.GetFeed(feedName);

            var package = feed.GetPackage("userName", PackageState.New, packageName);
            var packageWithoutUserName = feed.GetPackage(null, PackageState.New, packageName);

            await package.PutText("packageContent");
            Assert.Equal(new[] { packageName }, await feed.QueryPackages(PackageState.New));

            Assert.True(await packageWithoutUserName.Delete());
            Assert.Empty(await feed.QueryPackages(PackageState.New));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSymbol(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var symbolName = new SymbolName("imageName", "imageHash");
            var feed = Storage.GetFeed(feedName);

            var symbol = feed.GetSymbol(packageName, symbolName);
            await TestItemRoundtrip(symbol);
        }

        private static async Task TestPackageRelatedItemRoundtrip(
            PackageName packageName,
            IPackageRelatedStorageItem item,
            IPackageRelatedStorageItem itemWithoutPackageName,
            string itemContent)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await itemWithoutPackageName.PutText(itemContent));

            Assert.Empty(await item.PackageNames.List());
            Assert.Empty(await itemWithoutPackageName.PackageNames.List());

            await item.PutText(itemContent);
            Assert.Equal(new[] { packageName }, await item.PackageNames.List());
            Assert.Equal(new[] { packageName }, await itemWithoutPackageName.PackageNames.List());

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await itemWithoutPackageName.Delete());

            Assert.True(await item.Delete());

            Assert.Empty(await item.PackageNames.List());
            Assert.Empty(await itemWithoutPackageName.PackageNames.List());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSymbolWithoutPackageName(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var symbolName = new SymbolName("imageName", "imageHash");
            var feed = Storage.GetFeed(feedName);

            var symbol = feed.GetSymbol(packageName, symbolName);
            var symbolWithoutPackageName = feed.GetSymbol(null, symbolName);

            await TestPackageRelatedItemRoundtrip(packageName, symbol, symbolWithoutPackageName, "symbolContent");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSource(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var sourceName = new SourceName("fileName", "hash");
            var feed = Storage.GetFeed(feedName);

            var source = feed.GetSource(packageName, sourceName);
            await TestItemRoundtrip(source);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSymbolDeduplication(string feedName)
        {
            var symbolName = new SymbolName("imageName", "symbolHash");
            var feed = Storage.GetFeed(feedName);
            await TestDeduplication(packageName => feed.GetSymbol(packageName, symbolName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSourceWithoutPackageName(string feedName)
        {
            var packageName = new PackageName("packageId", "1.0");
            var sourceName = new SourceName("imageName", "imageHash");
            var feed = Storage.GetFeed(feedName);

            var source = feed.GetSource(packageName, sourceName);
            var sourceWithoutPackageName = feed.GetSource(null, sourceName);

            await TestPackageRelatedItemRoundtrip(packageName, source, sourceWithoutPackageName, "sourceContent");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestSourceDeduplication(string feedName)
        {
            var sourceName = new SourceName("fileName", "hash");
            var feed = Storage.GetFeed(feedName);
            await TestDeduplication(packageName => feed.GetSource(packageName, sourceName));
        }

        private static async Task TestDeduplication(Func<PackageName, IPackageRelatedStorageItem> getItem)
        {
            var packageName1 = new PackageName("packageId1", "1.0");
            var packageName2 = new PackageName("packageId2", "2.0");

            var item1 = getItem(packageName1);
            var item2 = getItem(packageName2);

            var sourceContent1 = GetRandomItemContent();
            await item1.PutText(sourceContent1);
            Assert.Equal(sourceContent1, await item1.GetText());
            Assert.Equal(new[] { packageName1 }, await item1.PackageNames.List());

            var sourceContent2 = GetRandomItemContent();
            await item2.PutText(sourceContent2);
            Assert.Equal(sourceContent2, await item1.GetText());
            Assert.Equal(sourceContent2, await item2.GetText());
            Assert.Equal(new[] { packageName1, packageName2 }, await item1.PackageNames.List());
            Assert.Equal(new[] { packageName1, packageName2 }, await item2.PackageNames.List());

            await item1.Delete();
            Assert.Equal(sourceContent2, await item1.GetText());
            Assert.Equal(sourceContent2, await item2.GetText());
            Assert.Equal(new[] { packageName2 }, await item1.PackageNames.List());
            Assert.Equal(new[] { packageName2 }, await item2.PackageNames.List());

            await item2.Delete();
            Assert.Null(await item2.GetText());
            Assert.Empty(await item2.PackageNames.List());
        }

        private static async Task TestMoveOrCopyPackage(
            Func<string, IPackageStorageItem> getSource,
            Func<IPackageStorageItem, Task<IPackageStorageItem>> moveOrCopy)
        {
            var sourcePackage = getSource("userName");
            Assert.Null(await moveOrCopy(sourcePackage));

            var expectedContent = GetRandomItemContent();
            await sourcePackage.PutText(expectedContent);

            var destinationPackage = await moveOrCopy(sourcePackage);

            var actualContent = await destinationPackage.GetText();
            Assert.Equal(expectedContent, actualContent);
        }

        [InlineData("move between states in default feed", null,
            PackageState.Indexing, "packageId", "1.0",
            PackageState.Succeded, "packageId", "1.0")]

        [InlineData("move between states in named feed", "feed-name",
            PackageState.Indexing, "packageId", "1.0",
            PackageState.Succeded, "packageId", "1.0")]

        [InlineData("change name in default feed", null,
            PackageState.New, "unknown", "1.0-unknown",
            PackageState.New, "packageId", "2.0")]

        [InlineData("move between states and change name in default feed", null,
            PackageState.New, "unknown", "1.0-unknown",
            PackageState.IndexingQueued, "packageId", "2.0")]

        [Theory]
        public async void TestMovePackage(
            string testCaption, string feedName,
            PackageState sourceState, string sourceId, string sourceVersion,
            PackageState destinationState, string destinationId, string destinationVersion)
        {
            var sourceName = new PackageName(sourceId, sourceVersion);
            var destinationName = new PackageName(destinationId, destinationVersion);
            var feed = Storage.GetFeed(feedName);

            await TestMoveOrCopyPackage(
                userName => feed.GetPackage(userName, sourceState, sourceName),
                sourcePackage => sourcePackage.Move(destinationState, destinationName));

            if (sourceState != destinationState)
                Assert.Empty(await feed.QueryPackages(sourceState));

            Assert.Equal(new[] { destinationName }, await feed.QueryPackages(destinationState));

            Assert.False(await feed.GetPackage(null, sourceState, sourceName).Delete());
            Assert.True(await feed.GetPackage(null, destinationState, destinationName).Delete());
        }

        [InlineData("copy between states in default feed", null,
            PackageState.Indexing, "packageId", "1.0",
            PackageState.Succeded, "packageId", "1.0")]

        [InlineData("copy between states in named feed", "feed-name",
            PackageState.Indexing, "packageId", "1.0",
            PackageState.Succeded, "packageId", "1.0")]

        [InlineData("copy to a different name in default feed", null,
            PackageState.New, "unknown", "1.0-unknown",
            PackageState.New, "packageId", "2.0")]

        [InlineData("copy between states with a different name in default feed", null,
            PackageState.New, "unknown", "1.0-unknown",
            PackageState.IndexingQueued, "packageId", "2.0")]

        [Theory]
        public async void TestCopyPackage(
            string testCaption, string feedName,
            PackageState sourceState, string sourceId, string sourceVersion,
            PackageState destinationState, string destinationId, string destinationVersion)
        {
            var sourceName = new PackageName(sourceId, sourceVersion);
            var destinationName = new PackageName(destinationId, destinationVersion);
            var feed = Storage.GetFeed(feedName);

            await TestMoveOrCopyPackage(
                userName => feed.GetPackage(userName, sourceState, sourceName),
                sourcePackage => sourcePackage.Copy(destinationState, destinationName));

            if (sourceState != destinationState)
            {
                Assert.Equal(new[] { sourceName }, await feed.QueryPackages(sourceState));
                Assert.Equal(new[] { destinationName }, await feed.QueryPackages(destinationState));
            }
            else
            {
                Assert.Equal(new[] { destinationName, sourceName }, await feed.QueryPackages(sourceState));
            }

            Assert.True(await feed.GetPackage(null, sourceState, sourceName).Delete());
            Assert.True(await feed.GetPackage(null, destinationState, destinationName).Delete());
        }

        [Theory]
        [InlineData(null, PackageState.New)]
        [InlineData("feed-name", PackageState.New)]
        public async void TestOverwriteUserName(string feedName, PackageState packageState)
        {
            var packageName = new PackageName("packageId", "1.0");
            var feed = Storage.GetFeed(feedName);

            var userName1 = GetRandomUserName();
            var packageContent1 = GetRandomItemContent();

            var userName2 = GetRandomUserName();
            var packageContent2 = GetRandomItemContent();

            var package1 = feed.GetPackage(userName1, packageState, packageName);
            await package1.PutText(packageContent1);

            Assert.Equal(new[] { packageName }, await feed.QueryPackages(packageState));
            Assert.Equal(new[] { packageName }, await feed.QueryPackages(userName1, packageState));
            Assert.Empty(await feed.QueryPackages(userName2, packageState));

            var package2 = feed.GetPackage(userName2, packageState, packageName);
            await package2.PutText(packageContent2);

            Assert.Equal(new[] { packageName }, await feed.QueryPackages(packageState));
            Assert.Empty(await feed.QueryPackages(userName1, packageState));
            Assert.Equal(new[] { packageName }, await feed.QueryPackages(userName2, packageState));

            Assert.False(await package1.Exists());

            Assert.Equal(packageContent2, await package2.GetText());
            Assert.True(await package2.Delete());

            Assert.Empty(await feed.QueryPackages(packageState));
            Assert.Empty(await feed.QueryPackages(userName1, packageState));
            Assert.Empty(await feed.QueryPackages(userName2, packageState));
        }

        private async Task TestOverwriteUserNameOnMoveOrCopy(
            string feedName,
            Func<IPackageStorageItem, PackageState, PackageName, Task<IPackageStorageItem>> moveOrCopy)
        {
            var packageName = new PackageName("packageId", "1.0");
            var feed = Storage.GetFeed(feedName);

            var userName1 = GetRandomUserName();
            var userName2 = GetRandomUserName();

            var package1 = feed.GetPackage(userName1, PackageState.New, packageName);
            await package1.PutText("packageContent");

            var package2 = feed.GetPackage(userName2, PackageState.New, packageName);
            Assert.Null(await moveOrCopy(package2, PackageState.Indexing, packageName));
            Assert.False(await package2.Exists());
            Assert.True(await package1.Exists());
            Assert.True(await package1.Delete());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestOverwriteUserNameOnMove(string feedName)
        {
            await TestOverwriteUserNameOnMoveOrCopy(feedName, (p, s, n) => p.Move(s, n));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("feed-name")]
        public async void TestOverwriteUserNameOnCopy(string feedName)
        {
            await TestOverwriteUserNameOnMoveOrCopy(feedName, (p, s, n) => p.Copy(s, n));
        }
    }
}
