using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLite.CodeFirst
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CaseSensitiveAttribute : Attribute
    {
        public bool IsCaseSensitive { get; }
        public CaseSensitiveAttribute(bool CaseSensitive = true) => IsCaseSensitive = CaseSensitive;
    }

}
