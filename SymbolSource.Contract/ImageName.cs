namespace SymbolSource.Contract
{
    public class ImageName
    {
        public ImageName(string name, string hash)
        {
            Name = name;
            Hash = hash;
        }

        public string Name { get; private set; }
        public string Hash { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Hash);
        }
    }
}