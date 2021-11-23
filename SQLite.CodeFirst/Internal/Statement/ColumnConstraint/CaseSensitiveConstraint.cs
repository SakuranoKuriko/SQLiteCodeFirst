using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLite.CodeFirst.Statement.ColumnConstraint
{
    internal class CaseSensitiveConstraint : IColumnConstraint
    {
        public bool IsCaseSensitive { get; set; }

        public string CreateStatement()
        {
            return IsCaseSensitive ? string.Empty : "COLLATE NOCASE";
        }
    }
}
