using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SymbolSource.Contract.Processor;

namespace SymbolSource.Contract
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string UserHandle { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(UserHandle))
                return string.Format("{0} ({1})", UserName, UserHandle);

            return UserName;
        }
    }

    public class PackageMessage
    {
        public UserInfo UserInfo { get; set; }
        public string FeedName { get; set; }
        public PackageState PackageState { get; set; }
        public PackageName PackageName { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2} as {3}", FeedName, PackageState, PackageName, UserInfo);
        }
    }
}
