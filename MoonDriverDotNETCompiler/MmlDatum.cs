using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class MmlDatum2 : MmlDatum
    {
        public string code;

        public MmlDatum2() : base()
        {
            code = "";
        }

        public MmlDatum2(string code, int dat) : base(dat)
        {
            this.code = code;
        }

        public MmlDatum2(string code, enmMMLType type, List<object> args, LinePos linePos, int dat) : base(type, args, linePos, dat)
        {
            this.code = code;
        }

        public MmlDatum2(string code, int dat, enmMMLType type, LinePos linePos, params object[] args) : base(dat, type, linePos, args)
        {
            this.code = code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="args">
        ///  0:none
        /// -1:data
        /// -2:label
        /// -3:ref label
        /// -4:macro
        /// -5:define
        /// -6:db ref define
        /// -7:ref macro
        /// </param>
        public MmlDatum2(string code, params object[] args) : base(-1, enmMMLType.unknown, null, args)
        {
            this.code = code;
        }

        public override string ToString()
        {
            string c = string.IsNullOrEmpty(code) ? "" : code;
            string d = "";
            while (c.Length>0 && c[c.Length - 1] == '\n')
            {
                c = c.Substring(0, c.Length - 1);
                d += "\n";
            }
            return string.Format("{0} : {1}{2}", c, base.ToString(), d);
        }
    }
}