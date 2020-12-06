using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Common
{
    public class GD3
    {
        public string TrackName = "";
        public string TrackNameJ = "";
        public string GameName = "";
        public string GameNameJ = "";
        public string SystemName = "";
        public string SystemNameJ = "";
        public string Composer = "";
        public string ComposerJ = "";
        public string Converted = "";
        public string Notes = "";
        public string VGMBy = "";
        public string Version = "";
        public string UsedChips = "";

        public List<Tuple<int, int, string>> Lyrics = null;
    }
}
