using System.Collections.Generic;
using System.IO;

namespace SymbolSource.Processor.Legacy.Projects
{
    public interface  IPackageFile
    {
        IEnumerable<IPackageEntry> Entries { get; }
    }

    public interface IPackageEntry
    {
        string FullPath { get; }
        Stream GetStream();
    }

    public interface IAddInfoBuilder
    {
        IEnumerable<IBinaryInfo> Build(IPackageFile directoryInfo);
        IEnumerable<IBinaryInfo> Build(IPackageFile directoryInfo, IEnumerable<IPackageEntry> includeFiles);
    }

    public interface IBinaryInfo
    {
        string Name { get; }
        string Type { get; }

        string Hash { get; }
        string SymbolHash { get; }

        IPackageEntry File { get; }
        IDocumentationInfo DocumentationInfo { get; }
        ISymbolInfo SymbolInfo { get; }
    }

    public interface IDocumentationInfo
    {
        string Type { get; }
        IPackageEntry File { get; }
    }

    public interface ISymbolInfo
    {
        string Type { get; }
        string Hash { get; }

        IPackageEntry File { get; }
        IEnumerable<ISourceInfo> SourceInfos { get; }
    }

    public interface ISourceInfo
    {

        string OriginalPath { get; }
        string Hash { get; }

        IPackageEntry File { get; }
    }

    public interface IAddInfoVisitor
    {
        IEnumerable<IBinaryInfo> Visit(IEnumerable<IBinaryInfo> binaryInfos);
        IBinaryInfo Visit(IBinaryInfo binaryInfo);
        IDocumentationInfo Visit(IDocumentationInfo documentInfo);
        ISymbolInfo Visit(ISymbolInfo symbolInfo);
        IEnumerable<ISourceInfo> Visit(IEnumerable<ISourceInfo> sourceInfos);
        ISourceInfo Visit(ISourceInfo sourceInfo);
    }
}
