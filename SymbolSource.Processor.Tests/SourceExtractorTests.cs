using System;
using System.Collections.Generic;
using System.IO;
using SymbolSource.Processor.Legacy;
using Xunit;

namespace SymbolSource.Processor.Tests
{
    public abstract class SourceExtractorTests
    {
        protected abstract ISourceExtractor CreateSourceExtractor();

        private Stream GetStream(string name)
        {
            var type = typeof(SourceExtractorTests);
            var fullName = string.Format("{0}.{1}.{2}.pdb", type.Namespace, type.Name, name);
            return type.Assembly.GetManifestResourceStream(fullName);
        }

        protected void Test(string name, int expectedLength, params string[] expectedList)
        {
            using (var stream = GetStream(name))
            {
                var extractor = CreateSourceExtractor();
                IList<string> result;

                if(extractor is IDisposable)
                    using ((IDisposable) extractor)
                        result = extractor.ReadSources(stream);
                else
                    result = extractor.ReadSources(stream);

                Assert.NotNull(result);
                Assert.Equal(expectedLength, result.Count);
                if (expectedList != null)
                {
                    for(int i = 0; i < expectedList.Length; i++)
                        Assert.Equal(expectedList[i], result[i]);
                }
            }
        }

        [Fact]
        public void NETBinaryCSharp()
        {
            Test("NETBinaryCSharp", 1, @"s:\Demos\DemoLibrary\Common\SimpleCalculator.cs");
        }

        [Fact]
        public void CplusPlus1()
        {
            Test("CplusPlus1", 551, null);
        }
    }

    public class ManagedSourceExtractorTests : SourceExtractorTests
    {
        protected override ISourceExtractor CreateSourceExtractor()
        {
            return new ManagedSourceExtractor();
        }
    }

    public class SrcToolSourceExtractorTests : SourceExtractorTests
    {
        protected override ISourceExtractor CreateSourceExtractor()
        {
            return new SrcToolSourceExtractor();
        }
    }
}
