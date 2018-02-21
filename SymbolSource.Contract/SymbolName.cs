namespace SymbolSource.Contract
{
    public class SymbolName
    {
        public SymbolName(string imageName, string symbolHash)
        {
            ImageName = imageName;
            SymbolHash = symbolHash;
        }

        public string ImageName { get; private set; }
        public string SymbolHash { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", ImageName, SymbolHash);
        }
    }
}