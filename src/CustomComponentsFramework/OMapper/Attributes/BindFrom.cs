﻿using System;

namespace OMapper.Attributes
{
    public sealed class BindFrom : Attribute
    {
        internal String OverridedReadColumn;

        public BindFrom(String sqlColumnResult)
        {
            OverridedReadColumn = sqlColumnResult;
        }
    }
}