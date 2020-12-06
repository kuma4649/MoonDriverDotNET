using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class work
    {
        public const int EXIT_SUCCESS = 0;
        public const int EXIT_FAILURE = -1;

        public const int VersionNo = 27;
        public const int MML_MAX = 128;
        public const int MML_MAX_NAME = 512;

        public string[] mml_names = new string[MML_MAX];
        public string[] mml_short_names = new string[MML_MAX];
        public int debug_flag = 0;
        
        public string ef_name = "effect.h";
        public string inc_name = "define.inc";
        public string in_name;
        public string out_name;
        public string mdr_name;

        public int warning_flag = 1;
        public int include_flag = 0;
        public int mml_num = 0;

        //version.cから移動
        public int message_flag = version.LANGUAGE;         // 表示メッセージの出力設定( 0:Jp 1:En )

        public string srcBuf { get; internal set; }
        public MmlDatum2[] destBuf { get; internal set; }
    }
}
