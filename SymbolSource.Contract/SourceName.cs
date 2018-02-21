namespace SymbolSource.Contract
{
    public class SourceName
    {
        public SourceName(string fileName, string hash)
        {
            FileName = fileName;
            Hash = hash;
        }

        public string FileName { get; set; }
        public string Hash { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", FileName, Hash);
        }
    }
}