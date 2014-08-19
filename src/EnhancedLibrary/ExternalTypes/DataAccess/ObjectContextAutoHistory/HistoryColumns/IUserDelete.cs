﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedLibrary.ExternalTypes.DataAccess.ObjectContextAutoHistory.HistoryColumns
{
    public interface IUserDelete<T> where T : struct
    {
        T? UserDel { get; set; }
        DateTime? DataDel { get; set; }
    }
}