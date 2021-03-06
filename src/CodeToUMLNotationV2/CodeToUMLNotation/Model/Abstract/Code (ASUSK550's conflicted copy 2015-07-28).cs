﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeToUMLNotation.Model.Abstract
{
    public abstract class Code
    {
        public Visibility Visibility { get; set; }
        public string Name { get; private set; }
        public bool Static { get; private set; }
        public bool Virtual { get; private set; }
        public bool Abstract { get; private set; }


        public Code(bool @static, Visibility visibility, bool @virtual, string name, bool @abstract)
        {
            if (Static && (Abstract || Virtual))
                throw new InvalidOperationException("cannot be abstract while static");

            Static = @static;
            Visibility = visibility;
            Virtual = @virtual;
            Abstract = @abstract;

            ParameterValidator.ThrowIfArgumentNullOrEmpty(name, "name");
            Name = name;
        }


        protected string AppendSuffix(string str)
        {
            // optional meassing static, readonly, virtual
            if (@Static)
                str += " [STATIC]";

            if (Abstract)
                str += " ** abstract **";

            if (Virtual)
                str += " « virtual »";

            return str;
        }
    }
}
