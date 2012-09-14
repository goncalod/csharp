﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnhancedLibrary.Types.ObjectContextAutoHistory.HistoryColumns
{
    public interface IUserUpdateNotNullable<T> where T : struct
    {
        T UserAlt { get; set; }
        DateTime DataAlt { get; set; }
    }
}
