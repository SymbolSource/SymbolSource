namespace SymbolSource.Contract
{
    public enum PackageState
    {
        None,
        New,
        Original,
        IndexingQueued,
        Indexing,
        Succeded,
        DeletingQueued,
        Deleting,
        Deleted,
        Partial,
        DamagedNew,
        DamagedIndexing,
        DamagedDeleting,
    }
}