using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    class version
    {
        public const string patchstr = "patches FDS enable patch([OK]), 4-46, 4-356, 5-17, 5-95, 5-313, 5-658";
        public const string hogereleasestr = "ppmck release 9 by h7";
        public const string moon_verstr = "Extended for MoonDriver ver 0.4";

#if ENGLISH
        public const int LANGUAGE =	1;			// 0だとデフォルトで日本語 1だと英語
#else
        public const int LANGUAGE = 0;
#endif

    }
}
