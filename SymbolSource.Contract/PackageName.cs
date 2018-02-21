using System;

namespace SymbolSource.Contract
{
    public class PackageName
    {
        public PackageName(string id, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException("id");

            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException("version");

            Id = id;
            Version = version;
        }

        public string Id { get; private set; }
        public string Version { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Id, Version);
        }

        protected bool Equals(PackageName other)
        {
            return string.Equals(Id, other.Id)
                && string.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            
            if (ReferenceEquals(this, obj)) 
                return true;
            
            if (obj.GetType() != GetType())
                return false;

            return Equals((PackageName)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ Version.GetHashCode();
            }
        }
    }
}