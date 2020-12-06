using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class datamake
    {
        private Compiler compiler = null;
        private work wk = null;
        private strings str = new strings();

        public datamake(Compiler compiler, work wk)
        {
            this.compiler = compiler;
            this.wk = wk;

            for (int i = 0; i < tone_tbl.Length; i++) tone_tbl[i] = new int[1024];
            for (int i = 0; i < envelope_tbl.Length; i++) envelope_tbl[i] = new int[1024];
            for (int i = 0; i < pitch_env_tbl.Length; i++) pitch_env_tbl[i] = new int[1024];
            for (int i = 0; i < arpeggio_tbl.Length; i++) arpeggio_tbl[i] = new int[1024];

            for (int i = 0; i < tonetbl_tbl.Length; i++) tonetbl_tbl[i] = new int[1024 + 2];
            for (int i = 0; i < opl3op_tbl.Length; i++) opl3op_tbl[i] = new int[1024 + 2];

            track_count = new LEN[work.MML_MAX][][];
            for (int i = 0; i < work.MML_MAX; i++)
            {
                track_count[i] = new LEN[_TRACK_MAX][];
                for (int j = 0; j < _TRACK_MAX; j++)
                {
                    track_count[i][j] = new LEN[2];
                    for (int k = 0; k < 2; k++)
                    {
                        track_count[i][j][k] = new LEN();
                    }
                }
            }

        }

        private int mml_idx = 0;
        private string songlabel;//[64];


        //extern int getFileSize(char* ptr);
        //extern int Asc2Int(char* ptr, int* cnt);
        //extern void strupper(char* ptr);
        //extern char* readTextFile(char* filename);
        //extern FILE* openDmc(char* name);
        //extern char* skipSpaceOld(char* ptr);
        //extern char* skipSpace(char* ptr);

        //extern char* skipQuote(char* ptr);
        //extern char* skipComment(char* ptr);
        //extern int isComment(char* ptr);

        //void putBankOrigin(FILE* fp, int bank);
        //int checkBankRange(int bank);
        //int double2int(double d);

        //#define arraysizeof(x) ( sizeof(x) / sizeof(x[0]) )


        private int error_flag;                 // エラーが発生していると0以外に
        private int octave;                     // 変換中のオクターブ
        private double length;                      // 変換中の音長
        private int octave_flag = 0;            // オクターブスイッチ ("<" ">" の処理)
        private int gate_denom = 8;             //qコマンドの分母
        private int pitch_correction = 0;       //拡張音源のディチューン、ピッチエンベロープ、LFOの方向修正

        private int loop_flag;                  // チャンネルループがあると0以外に
        private int putAsm_pos;                 //

        private string mml_file_name;                //現在のmmlファイル名(アセンブラ出力時に使用)
        private int mml_line_pos;               //
        private int mml_trk;                    //

        private int nest;                       // リピートのネスト数
        private LEN[][][] track_count;//[MML_MAX][_TRACK_MAX] [2];			// 音長トータル保管場所(音長/フレーム/ループ音長/ループフレーム)
        private int volume_flag;                // 音量の状態
        private double tbase = 0.625;               // 変換中の[frame/count]割合

        private int transpose;                  // 現在のトランスポーズ値

        //for MoonDriver
        private const ulong ALLTRACK = 0xffff_ffff;

        private const int BOPL4FLAG = 0x01;
        private const int BOPL3FLAG = 0x02;

        private const int OPL4_MAX = 24;
        private const int OPL3_MAX = 18;

        private int BOPL3TRACK() { return BTRACK(24); }

        private const int MAX_VOLUME = 127;

        private const int HUSIC_EXT = 1;


        public enum enmEFTBL
        {
            END = 0xffff,
            LOOP = 0xfffe
        };

        public enum enmMCK
        {
            MCK_REPEAT_END = 0xa0,
            MCK_REPEAT_ESC = 0xa1,

            MDR_OPBASE = 0xe6,
            MDR_LDOP2 = 0xe5,
            MDR_TVP = 0xe4,
            MDR_DRUM = 0xf2,
            MDR_FBS = 0xe3,
            MDR_OPMODE = 0xeb,

            MCK_SLAR = 0xe9,

            MDR_REVERB = 0xea,
            MDR_DAMP = 0xeb,

            MDR_JUMP = 0xe2,
            MDR_DRUM_BIT = 0xe1,
            MDR_DRUM_NOTE = 0xe0,

            MCK_GOTO = 0xee,
            MCK_SET_SHIFT_AMOUNT = 0xef,
            MCK_SET_FDS_HWENV = 0xf0,
            MCK_SET_SUN5B_NOISE_FREQ = 0xf1,
            MCK_SET_SUN5B_HARD_SPEED = 0xf2,
            MCK_SET_FDS_HWEFFECT = 0xf3,
            MCK_WAIT = 0xf4,
            MCK_DATA_WRITE = 0xf5,
            MCK_DIRECT_FREQ = 0xf6,
            MCK_SET_NOTEENV = 0xf7,
            MCK_SET_PITCHENV = 0xf8,
            MCK_SET_HWSWEEP = 0xf9,
            MCK_SET_DETUNE = 0xfa,
            MCK_SET_LFO = 0xfb,
            MCK_REST = 0xfc,
            MCK_SET_VOL = 0xfd, MCK_SUN5B_HARD_ENV = 0xfd,
            MCK_SET_TONE = 0xfe,
            MCK_DATA_END = 0xff
        };


        private int sndgen_flag = BOPL4FLAG;    // 拡張音源フラグ
                                                // トラック許可フラグ
        private ulong track_allow_flag = ALLTRACK;
        //実際に使ったトラック
        private ulong actual_track_flag = 0;
        private int dpcm_track_num = 1;         // DPCMトラック
        private int fds_track_num = 0;          // FDSトラック
        private int vrc7_track_num = 0;         // VRC7トラック
        private int vrc6_track_num = 0;         // VRC6トラック
        private int n106_track_num = 0;         // 拡張音源(namco106)使用トラック数
        private int fme7_track_num = 0;         // FME7トラック
        private int mmc5_track_num = 0;         // MMC5トラック

        private int opl4_track_num = OPL4_MAX;
        private int opl3_track_num = 0;



        private int[] bank_sel = new int[_TRACK_MAX];   // 0 〜 127 = バンク切り替え , 0xFF = 変更無し
        private int allow_bankswitching = 1;
        private int dpcm_bankswitch = 0;
        private int auto_bankswitch = 0;
        private int curr_bank = 0x00;
        private int[] bank_usage = new int[128];        //bank_usage[0]は今のところ無意味
        private int bank_maximum = 0;       //8KB
        private int dpcm_extra_bank_num = 0;    //8KB

        private int[][] tone_tbl = new int[_TONE_MAX][];//[1024];	// Tone
        private int[][] envelope_tbl = new int[_ENVELOPE_MAX][];//[1024];	// Envelope
        private int[][] pitch_env_tbl = new int[_PITCH_ENV_MAX][];//[1024];	// Pitch Envelope
        private int[][] pitch_mod_tbl;//[_PITCH_MOD_MAX][   5];	// LFO
        private int[][] arpeggio_tbl = new int[_ARPEGGIO_MAX][];//[1024];	// Arpeggio
        private int[][] fm_tone_tbl;//[_FM_TONE_MAX][2+64];	// FM Tone
        private int[][] vrc7_tone_tbl;//[_VRC7_TONE_MAX][2+64];	// VRC7 Tone(配列数は使用関数の関係)
        private int[][] n106_tone_tbl;//[_N106_TONE_MAX][2+64];	// NAMCO106 Tone
        private int[][] hard_effect_tbl;//[_HARD_EFFECT_MAX][5];	// FDS Hardware Effect
        private int[][] effect_wave_tbl;//[_EFFECT_WAVE_MAX][33];	// Effect Wave (4088) Data

        private int[][] wtb_tone_tbl;//[_WTB_TONE_MAX][2+64];		// HuSIC WaveTable Tone

        private int[][] tonetbl_tbl = new int[_TONETBL_MAX][];//[1024+2];
        private int[][] opl3op_tbl = new int[_OPL3TBL_MAX][];//[1024+2];
        private int[] opl3op_flag = new int[_OPL3TBL_MAX]; // operator flag

        private DPCMTBL[] dpcm_tbl = new DPCMTBL[64];//[_DPCM_MAX];                // DPCM
        private DPCMTBL[] xpcm_tbl = new DPCMTBL[64];//[_DPCM_MAX];                // XPCM(for HuSIC)

        /* DPCM用バッファ */
        public class DPCMTBL
        {
            public int flag;                       // 音色使用/未使用フラグ
            public int index;                      // 実際にファイルに書き込まれるインデックス番号
                                                   // ココが-1以外の時はfilenameは無視されてindex番号のDPCMを使用する(ソート時)
            public string fname;
            public int freq;
            public int start_adr;
            public int size;
            public int delta_init;
            public int bank_ofs;					//16KB(0x4000)
        }

        private byte[] dpcm_data;   // DPCM展開データ
        private int dpcm_size = 0;
        private int dpcm_reststop = 0;

        // HuSIC
        private int panvol = 0;                 // 現在のパンボリューム
        private int xpcm_size = 0;

        private int use_jump = 0; // jumpを行う


        private string song_name = "Song Name";//[1024]
        private string composer = "Artist";//[1024]
        private string maker = "Maker";//[1024]

        private string programer_buf = "";//[1024]
        private string programer = null;

        // MoonDriver
        // PCMファイル
        private string pcm_name = "";//[1024]
        private int use_pcm = 0;

        private const string str_track = "ABCDEFGHIJKLMNOPQRSTUVWXabcdefghijklmnopqr";// _TRACK_STR;

        // エラー番号
        private enum enmErrNum
        {
            COMMAND_NOT_DEFINED = 0,
            DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0,
            DEFINITION_IS_WRONG,
            TONE_DEFINITION_IS_WRONG,
            ENVELOPE_DEFINITION_IS_WRONG,
            PITCH_ENVELOPE_DEFINITION_IS_WRONG,
            NOTE_ENVELOPE_DEFINITION_IS_WRONG,
            LFO_DEFINITION_IS_WRONG,
            DPCM_DEFINITION_IS_WRONG,
            DPCM_PARAMETER_IS_LACKING,
            FM_TONE_DEFINITION_IS_WRONG,
            ABNORMAL_PARAMETERS_OF_FM_TONE,
            N106_TONE_DEFINITION_IS_WRONG,
            ABNORMAL_PARAMETERS_OF_N106_TONE,
            ABNORMAL_VALUE_OF_REPEAT_COUNT,
            ABNORMAL_TONE_NUMBER,
            ABNORMAL_ENVELOPE_NUMBER,
            ABNORMAL_ENVELOPE_VALUE,
            ABNORMAL_PITCH_ENVELOPE_NUMBER,
            ABNORMAL_NOTE_ENVELOPE_NUMBER,
            ABNORMAL_LFO_NUMBER,
            ABNORMAL_PITCH_VALUE,
            ABNORMAL_VOLUME_VALUE,
            ABNORMAL_TEMPO_VALUE,
            ABNORMAL_QUANTIZE_VALUE,
            ABNORMAL_SHUFFLE_QUANTIZE_VALUE,
            ABNORMAL_SWEEP_VALUE,
            ABNORMAL_DETUNE_VALUE,
            ABNORMAL_SHIFT_AMOUNT,
            ABNORMAL_NOTE_AFTER_COMMAND,
            RELATIVE_VOLUME_WAS_USED_WITHOUT_SPECIFYING_VOLUME,
            VOLUME_RANGE_OVER_OF_RELATIVE_VOLUME,
            VOLUME_RANGE_UNDER_OF_RELATIVE_VOLUME,
            DATA_ENDED_BY_CONTINUATION_NOTE,
            DPCM_FILE_NOT_FOUND,
            DPCM_FILE_SIZE_OVER,
            DPCM_FILE_TOTAL_SIZE_OVER,
            INVALID_TRACK_HEADER,
            HARD_EFFECT_DEFINITION_IS_WRONG,
            EFFECT_WAVE_DEFINITION_IS_WRONG,
            ABNORMAL_HARD_EFFECT_NUMBER,
            ABNORMAL_TRANSPOSE_VALUE,
            TUPLET_BRACE_EMPTY,
            BANK_IDX_OUT_OF_RANGE,
            FRAME_LENGTH_LESSTHAN_0,
            ABNORMAL_NOTE_LENGTH_VALUE,
            PARAMETER_IS_LACKING,
            ABNORMAL_SELFDELAY_VALUE,
            CANT_USE_BANK_2_OR_3_WITH_DPCMBANKSWITCH,
            CANT_USE_SHIFT_AMOUNT_WITHOUT_PITCH_CORRECTION,
            UNUSE_COMMAND_IN_THIS_TRACK,

            /* for HuSIC */
            WTB_TONE_DEFINITION_IS_WRONG,
            ABNORMAL_PARAMETERS_OF_WTB_TONE,
            XPCM_DEFINITION_IS_WRONG,
            XPCM_PARAMETER_IS_LACKING,
            XPCM_FILE_NOT_FOUND,
            XPCM_FILE_SIZE_OVER,
            XPCM_FILE_TOTAL_SIZE_OVER,
            FMLFO_PARAM_IS_WRONG,

            TONETBL_DEFINITION_IS_WRONG,
            ABNORMAL_PARAMETERS_OF_TONETBL,

            ABNORMAL_PARAMETERS,

            /* MoonDriver */
            COMMAND_REDUNDANT,

        };

        // エラー文字列
        private string[] ErrorlMessage = new string[]{
    "指定のコマンドはありません",                            "Command not defined",
    "ループ深度が0以外でデータが終了しました",             "Data ended by loop depth except 0",
    "設定に誤りがあります",                                   "Definition is wrong",
    "PSG音色設定に誤りがあります",                          "PSG Tone definition is wrong",
    "エンベロープ設定に誤りがあります",                     "Envelope definition is wrong",
    "ピッチエンベロープ設定に誤りがあります",              "Pitch envelope definition is wrong",
    "ノートエンベロープ設定に誤りがあります",              "Note envelope definition is wrong",
    "LFO設定に誤りがあります",                                "LFO definition is wrong",
    "DPCM設定に誤りがあります",                               "DPCM definition is wrong",
    "DPCM設定のパラメータが足りません",                       "DPCM parameter is lacking",
    "FM音色設定に誤りがあります",                           "FM tone definition is wrong",
    "FM用音色のパラメータが異常です",                     "Abnormal parameters of FM tone",
    "namco106音色設定に誤りがあります",                     "namco106 tone definition is wrong",
    "namco106用音色のパラメータが異常です",                   "Abnormal parameters of namco106 tone",
    "繰り返し回数の値が異常です",                            "Abnormal value of repeat count",
    "音色番号が異常です",                                    "Abnormal tone number",
    "エンベロープ番号が異常です",                            "Abnormal envelope number",
    "エンベロープの値が異常です",                            "Abnormal envelope value",
    "ピッチエンベロープ番号の値が異常です",                   "Abnormal pitch envelope number",
    "ノートエンベロープ番号の値が異常です",                   "Abnormal note envelope number",
    "LFO番号の値が異常です",                             "Abnormal LFO number",
    "音程の値が異常です",                                    "Abnormal pitch value",
    "音量の値が異常です",                                    "Abnormal volume value",
    "テンポの値が異常です",                                   "Abnormal tempo value",
    "クォンタイズの値が異常です",                            "Abnormal quantize value",
    "シャッフルクォンタイズの値が異常です",                   "Abnormal shuffle quantize value",
    "スイープの値が異常です",                              "Abnormal sweep value",
    "ディチューンの値が異常です",                            "Abnormal detune value",
    "ピッチシフト量の値が異常です",                           "Abnormal pitch shift amount value",

    "コマンド後のノートが異常です",                           "Abnormal note after command",

    "音量が指定されていない状態で相対音量を使用しました",    "Relative volume was used without specifying volume",
    "相対音量(+)で音量の範囲を超えました",                  "Volume range over(+) of relative volume",
    "相対音量(-)で音量の範囲を超えました",                  "Volume range under(-) of relative volume",
    "連符処理の途中でデータが終了しました",                   "Data ended by Continuation note",
    "DPCMファイルがありません",                               "DPCM file not found",
    "DPCMデータのサイズが4081byteを超えました",               "DPCM file size over",
    "DPCMデータのサイズが規定のサイズを超えました",         "DPCM file total size over",
    "指定のトラックヘッダは無効です",                      "Invalid track header",
    "ハードウェアエフェクト設定に誤りがあります。",           "Hardware effect definition is wrong",
    "エフェクト波形設定に誤りがあります。",                   "Effect wavetable definition is wrong",
    "ハードウェアエフェクト番号の値が異常です。",            "Abnormal hardware effect number",
    "トランスポーズの値が異常です",                           "Abnormal transpose value",
    "連符の{}の中に音符がありません",                     "Tuplet {} empty",
    "バンクが範囲を超えました",                             "Bank index out of range",
    "音長が負の値です(unexpected error)",                   "Frame length is negative value (unexpected error)",
    "音長の値が異常です",                                    "Abnormal note length value",
    "設定のパラメータが足りません",                           "Parameter is lacking",
    "セルフディレイの値が異常です",                           "Abnormal self-delay value",
    "DPCMサイズが0x4000を超える場合はバンク2と3は使用できません",      "Cannot use bank 2 or 3 if DPCM size is greater than 0x4000",
    "#PITCH-CORRECTIONを指定しない限りピッチシフト量コマンドは使用できません",     "Cannot use SA<num> without #PITCH-CORRECTION",
    "このトラックでは使用できないコマンドです",             "Unuse command in this track",

    /* for HuSIC */
    "WaveTable音色設定に誤りがあります",                    "WaveTable tone definition is wrong",
    "WaveTable用音色のパラメータが異常です",              "Abnormal parameters of WaveTable tone",
    "XPCM設定に誤りがあります",                               "XPCM definition is wrong",
    "XPCM設定のパラメータが足りません",                       "XPCM parameter is lacking",
    "XPCMファイルがありません",                               "XPCM file not found",
    "XPCMデータのサイズが8192byteを超えました",               "XPCM file size over",
    "XPCMデータのサイズが規定のサイズを超えました",         "XPCM file total size over",
    "FMLFOパラメータに誤りがあります",                       "FMLFO parameter is wrong",

    "ToneTable設定に誤りがあります",                      "ToneTable definition is wrong",
    "ToneTableパラメータが異常です",                      "Abnormal parameters of ToneTable",

    "パラメータが異常です",                                   "Abnormal parameters",

    "コマンドが重複しています",                             "Command is redundant",
        };



        private enum enmSys
        {
            TOO_MANY_INCLUDE_FILES = 0,
            FRAME_LENGTH_IS_0,
            REPEAT2_FRAME_ERROR_OVER_3,
            IGNORE_PMCKC_BANK_CHANGE,
            THIS_NUMBER_IS_ALREADY_USED,
            DPCM_FILE_SIZE_ERROR,
        };

        private string[] WarningMessage = new string[] {
    "インクルードファイルの数が多すぎます",                   "Too many include files",
    "フレーム音長が0になりました。",                      "frame length is 0",
    "リピート2のフレーム誤差が3フレームを超えています。",   "Repeat2 frame error over 3 frames",
    "#BANK-CHANGE使用時は#SETBANK, NBは無視します",           "Ignoring #SETBANK and NB if #BANK-CHANGE used",
    "定義番号が重複しています",                             "This definition number is already used",
    "DPCMサイズ mod 16 が1ではありません",                 "DPCM size mod 16 is not 1",
        };


        /*--------------------------------------------------------------
        // トラックマスク関数
        ---------------------------------------------------------------*/

        private static int isAllTrack(int trk)
        {
            return 1;
        }

        /* テンポベース -> フレームベースへの変換パラメータ */
        private const double _BASE = 192.0;
        private const int _BASETEMPO = 75;

        public class LEN
        {
            public double cnt;
            public int frm;
        }

        public class GATE_Q
        {
            public int rate;
            public int adjust;
            // gate length = delta * rate/gate_denom + adjust
        }

        public class HEAD
        {
            public string str;
            public int status;

            public HEAD(string str, int status)
            {
                this.str = str;
                this.status = status;
            }
        }

        private const int _HEADER = 1;
        private const int _TITLE = 2;
        private const int _COMPOSER = 3;
        private const int _MAKER = 4;
        private const int _PROGRAMER = 5;
        private const int _OCTAVE_REV = 6;
        private const int _EX_DISKFM = 7;
        private const int _EX_NAMCO106 = 8;
        private const int _INCLUDE = 9;
        private const int _BANK_CHANGE = 10;
        private const int _EFFECT_INCLUDE = 11;
        private const int _SET_SBANK = 12;
        private const int _EX_VRC7 = 13;
        private const int _EX_VRC6 = 14;
        private const int _EX_FME7 = 15;
        private const int _EX_MMC5 = 16;
        private const int _NO_BANKSWITCH = 17;
        private const int _DPCM_RESTSTOP = 18;
        private const int _GATE_DENOM = 19;
        private const int _AUTO_BANKSWITCH = 20;
        private const int _PITCH_CORRECTION = 21;

        private const int _SET_EFFECT = 0x20;
        private const int _SET_TONE = 0x21;
        private const int _SET_ENVELOPE = 0x22;
        private const int _SET_PITCH_MOD = 0x23;
        private const int _SET_PITCH_ENV = 0x24;
        private const int _SET_ARPEGGIO = 0x25;
        private const int _SET_DPCM_DATA = 0x26;
        private const int _SET_FM_TONE = 0x27;
        private const int _SET_N106_TONE = 0x28;
        private const int _SET_VRC7_TONE = 0x29;
        private const int _SET_HARD_EFFECT = 0x2A;
        private const int _SET_EFFECT_WAVE = 0x2B;
        // MoonDriver
        private const int _SET_TONETBL = 0x2E;
        private const int _SET_FMOP = 0x2F;
        private const int _SET_FMOP_FOUR = 0x30;
        private const int _EX_OPL3 = 0x31;
        private const int _OPL4_NOUSE = 0x32;
        private const int _PCM_FILE = 0x33;

        private const int _TRACK = 0x40;
        private const uint _SAME_LINE = 0x8000_0000;

        private const int _TRACK_MAX = (24 + 18);
        //				 012345678901234567890123012345678901234567
        private const string _TRACK_STR = "ABCDEFGHIJKLMNOPQRSTUVWXabcdefghijklmnopqr";

        private int BTRACK(int a) { return a; }
        private int BNOISETRACK() { return BTRACK(3); }
        private int BDPCMTRACK() { return BTRACK(4); }
        private int BFMTRACK() { return BTRACK(5); }
        private int BVRC7TRACK() { return BTRACK(6); }
        private int BVRC6TRACK() { return BTRACK(12); }
        private int BVRC6SAWTRACK() { return BTRACK(14); }
        private int BN106TRACK() { return BTRACK(15); }
        private int BFME7TRACK() { return BTRACK(23); }
        private int BMMC5TRACK() { return BTRACK(26); }

        //private uint ALLTRACK = 0xffffffff;

        private const int _PITCH_MOD_MAX = 64;
        private const int _PITCH_ENV_MAX = 128;
        private const int _ENVELOPE_MAX = 128;
        private const int _TONE_MAX = 128;
        private const int _DPCM_MAX = 64;
        private const int _ARPEGGIO_MAX = 128;
        private const int _FM_TONE_MAX = 128;
        private const int _N106_TONE_MAX = 128;
        private const int _VRC7_TONE_MAX = 64;
        private const int _HARD_EFFECT_MAX = 16;
        private const int _EFFECT_WAVE_MAX = 8;

        // MoonSound
        private const int _TONETBL_MAX = 256;
        private const int _OPL3TBL_MAX = 256;

        /* コマンドステータス */
        private const int PARAM_MAX = 8;
        public class CMD
        {

            public string filename;
            public int line;
            public double cnt;    //トラック開始時点を0として、そこからの経過したカウント数
            public int frm;    //↑をフレーム単位にしたもの
            public double lcnt;   //トラックのループ点(Lコマンド)を0として、そこからの経過したカウント数(ただしLより前は0)
            public int lfrm;   //↑をフレーム単位にしたもの
            public int cmd;
            public double len;    //単位:count
            public int[] param = new int[PARAM_MAX];
        }

        private const long PARAM_OMITTED = 0x8000_0000;

        private const int _NOTE_C = 0;
        private const int _NOTE_D = 2;
        private const int _NOTE_E = 4;
        private const int _NOTE_F = 5;
        private const int _NOTE_G = 7;
        private const int _NOTE_A = 9;
        private const int _NOTE_B = 11;
        private const int MIN_NOTE = -3;
        private const int MAX_NOTE = 0x8f;

        private enum enmMML
        {
            _TEMPO = (MAX_NOTE + 1),
            _OCTAVE,
            _OCT_UP,
            _OCT_DW,
            _LENGTH,
            _ENVELOPE,
            _REL_ENV,
            _VOLUME,
            _VOL_PLUS, // 0x98
            _VOL_MINUS,
            _HARD_ENVELOPE,
            _TONE,
            _ORG_TONE,
            _REL_ORG_TONE,
            _SWEEP,
            _SLAR,
            _SONG_LOOP, // 0xa0
            _REPEAT_ST,
            _REPEAT_END,
            _REPEAT_ESC,
            _CONT_NOTE,
            _CONT_END,
            _QUONTIZE,
            _QUONTIZE2,

            _TIE, // 0xa8
            _DETUNE,
            _LFO_ON,
            _LFO_OFF,
            _EP_ON,
            _EP_OFF,
            _EN_ON,
            _EN_OFF,
            _NOTE, // 0xb0
            _KEY,
            _WAIT,
            _DATA_BREAK,
            _DATA_WRITE,
            _DATA_THRUE,

            _NEW_BANK,

            _REPEAT_ST2,
            _REPEAT_END2, // 0xb8
            _REPEAT_ESC2,

            _TEMPO2,
            _TRANSPOSE,
            _MH_ON,
            _MH_OFF,

            _SHUFFLE_QUONTIZE,
            _SHUFFLE_QUONTIZE_RESET,
            _SHUFFLE_QUONTIZE_OFF, // 0xc0

            /*_ARTICULATION_ADJUST,*/
            _KEY_OFF,
            _SELF_DELAY_OFF,
            _SELF_DELAY_ON,
            _SELF_DELAY_QUEUE_RESET,
            _XX_COMMAND,
            _VRC7_TONE,
            _SUN5B_HARD_SPEED,
            _SUN5B_HARD_ENV, // 0xc8
            _SUN5B_NOISE_FREQ,
            _SHIFT_AMOUNT,

            // HuSIC

            _NOISE_SW,
            _PAN,
            _L_PAN,
            _R_PAN,
            _C_PAN,
            _WAVE_CHG, // 0xd0
            _MODE_CHG,
            _FMLFO_SET,
            _FMLFO_OFF,
            _FMLFO_FRQ,

            // MoonDriver
            _REVERB_SET,
            _DAMP_SET,

            _SET_OPBASE,
            _LOAD_OP2, // 0xd8
            _SET_TVP,
            _DRUM_SW,
            _SET_FBS,
            _SET_OPM,

            _JUMP_FLAG,

            _DRUM_BIT,
            _DRUM_NOTE,

            _DATA_WRITE_OFS,


            _REST = 0xfc,
            _NOP = 0xfe,
            _TRACK_END = 0xff
        };

        private const int SELF_DELAY_MAX = 8;

        // ヘッダ
        private HEAD[] head = new HEAD[] {
    new HEAD( "#TITLE",             _TITLE      ),
    new HEAD( "#COMPOSER",          _COMPOSER  ),
    new HEAD( "#MAKER",             _MAKER     ),
    new HEAD( "#PROGRAMER",         _PROGRAMER ),
    new HEAD( "#OCTAVE-REV",        _OCTAVE_REV),
    new HEAD( "#GATE-DENOM",        _GATE_DENOM),
    new HEAD( "#INCLUDE",           _INCLUDE    ),
    //		{ "#EX-DISKFM",			_EX_DISKFM			},
    //		{ "#EX-NAMCO106",		_EX_NAMCO106		},
    //		{ "#EX-VRC7",		    _EX_VRC7		 },
    //		{ "#EX-VRC6",		    _EX_VRC6		 },
    //		{ "#EX-FME7",		    _EX_FME7		 },
    //		{ "#EX-MMC5",		    _EX_MMC5		 },
    new HEAD( "#NO-BANKSWITCH",     _NO_BANKSWITCH   ),
    new HEAD( "#AUTO-BANKSWITCH",   _AUTO_BANKSWITCH ),
    new HEAD( "#PITCH-CORRECTION",  _PITCH_CORRECTION ),
    new HEAD( "#BANK-CHANGE",       _BANK_CHANGE ),
    new HEAD( "#SETBANK",           _SET_SBANK  ),
    new HEAD( "#EFFECT-INCLUDE",    _EFFECT_INCLUDE ),
    new HEAD( "#DPCM-RESTSTOP",     _DPCM_RESTSTOP   ),
    // for HuSIC
    // { "@XPCM",				    _SET_XPCM_DATA	},
    // { "@WT",						_SET_WTB_TONE	 },
    // for MoonDriver
     new HEAD( "@TONE",             _SET_TONETBL    ),
     new HEAD( "@OPF",              _SET_FMOP_FOUR  ),
     new HEAD( "@OPL",              _SET_FMOP       ),
     new HEAD( "#EX-OPL3",          _EX_OPL3        ),
     new HEAD( "#OPL4-NOUSE",       _OPL4_NOUSE     ),
     new HEAD( "#PCMFILE",          _PCM_FILE       ),
     new HEAD( "@DPCM",             _SET_DPCM_DATA  ),
     new HEAD( "@MP",               _SET_PITCH_MOD  ),
     new HEAD( "@EN",               _SET_ARPEGGIO   ),
     new HEAD( "@EP",               _SET_PITCH_ENV  ),
     new HEAD( "@FM",               _SET_FM_TONE    ),
     new HEAD( "@MH",               _SET_HARD_EFFECT),
     new HEAD( "@MW",               _SET_EFFECT_WAVE),
     new HEAD( "@OP",               _SET_VRC7_TONE  ),
     new HEAD( "@N",                _SET_N106_TONE  ),
     new HEAD( "@V",                _SET_ENVELOPE   ),
     new HEAD( "@",                 _SET_TONE       ),
     new HEAD( "",                  -1              ),
        };

        public class MML
        {
            public string cmd;
            public int num;
            public Func<int, int> check;// (int trk);
                                        //		unsigned long		enable;
            public MML(string cmd, int num, Func<int, int> check)
            {
                this.cmd = cmd;
                this.num = num;
                this.check = check;
            }

        }

        // MMLコマンド
        private MML[] mml = new MML[]{
    new MML( "`", (int)enmMML._DRUM_BIT,       isAllTrack ),
    new MML( "c", _NOTE_C,         isAllTrack ),
    new MML( "d", _NOTE_D,         isAllTrack ),
    new MML( "e", _NOTE_E,         isAllTrack ),
    new MML( "f", _NOTE_F,         isAllTrack ),
    new MML( "g", _NOTE_G,         isAllTrack ),
    new MML( "a", _NOTE_A,         isAllTrack ),
    new MML( "b", _NOTE_B,         isAllTrack ),
    new MML( "@n",(int)enmMML. _KEY,           isAllTrack ),
    new MML( "n", (int)enmMML._NOTE,           isAllTrack ),
    new MML( "w", (int)enmMML._WAIT,           isAllTrack ),
    new MML( "@t",(int)enmMML. _TEMPO2,        isAllTrack ),
    new MML( "t", (int)enmMML._TEMPO,          isAllTrack ),
    new MML( "o", (int)enmMML._OCTAVE,         isAllTrack ),
    new MML( ">", (int)enmMML._OCT_UP,         isAllTrack ),
    new MML( "<", (int)enmMML._OCT_DW,         isAllTrack ),
    new MML( "l", (int)enmMML._LENGTH,         isAllTrack ),
    new MML( "v+",(int)enmMML. _VOL_PLUS,      isAllTrack ),
    new MML( "v-",(int)enmMML. _VOL_MINUS,     isAllTrack ),
    new MML( "v", (int)enmMML._VOLUME,         isAllTrack ),
    new MML( "NB",(int)enmMML. _NEW_BANK,      isAllTrack ),
    new MML( "EPOF",(int)enmMML. _EP_OFF,      isAllTrack ),
    new MML( "EP",  (int)enmMML._EP_ON,        isAllTrack ),
    new MML( "ENOF",(int)enmMML. _EN_OFF,      isAllTrack ),
    new MML( "EN",  (int)enmMML._EN_ON,        isAllTrack ),
    new MML( "MPOF",(int)enmMML. _LFO_OFF,     isAllTrack ),
    new MML( "MP",  (int)enmMML._LFO_ON,       isAllTrack ),

    //// for HuSIC
//		{ "FSOF", _FMLFO_OFF,	(HULFO_TRK) },
//		{ "FS",	 _FMLFO_SET,	(HULFO_TRK) },
//		{ "FF",	 _FMLFO_FRQ,	(HULFO_TRK) },
//		{ "N", _NOISE_SW,		(HUNOISE_TRK) },

    //// for MoonDriver

    new MML( "j",(int)enmMML. _JUMP_FLAG,      isAllTrack ),
    new MML( "VOP", (int)enmMML._REVERB_SET,   isAllTrack ),
    new MML( "RV", (int)enmMML._REVERB_SET,    isAllTrack ),
    new MML( "DA",(int)enmMML. _DAMP_SET,      isAllTrack ),

    new MML( "OPB", (int)enmMML._SET_OPBASE,   isAllTrack ),
    new MML( "WX" , (int)enmMML._LOAD_OP2,     isAllTrack ),
    new MML( "TVP", (int)enmMML._SET_TVP,      isAllTrack ),
    new MML( "DR" , (int)enmMML._DRUM_SW,      isAllTrack ),
    new MML( "DN" , (int)enmMML._DRUM_NOTE,    isAllTrack ),
    new MML( "FB" , (int)enmMML._SET_FBS,      isAllTrack ),
    new MML( "OPM", (int)enmMML._SET_OPM,     isAllTrack ),

    new MML( "PL",(int)enmMML._L_PAN,         isAllTrack ),
    new MML( "PR",(int)enmMML._R_PAN,         isAllTrack ),
    new MML( "PC",(int)enmMML._C_PAN,         isAllTrack ),
    new MML( "P", (int)enmMML._PAN,            isAllTrack ),
    new MML( "W", (int)enmMML._WAVE_CHG,       isAllTrack ),
    new MML( "M", (int)enmMML._MODE_CHG,       isAllTrack ),

    new MML( "SDQR", (int)enmMML._SELF_DELAY_QUEUE_RESET,  isAllTrack ),
    new MML( "SDOF", (int)enmMML._SELF_DELAY_OFF,  isAllTrack ),
    new MML( "SD", (int)enmMML._SELF_DELAY_ON,     isAllTrack ),

    new MML( "D", (int)enmMML._DETUNE,         isAllTrack ),
    new MML( "K", (int)enmMML._TRANSPOSE,      isAllTrack ),

    new MML( "@q",(int)enmMML. _QUONTIZE2,     isAllTrack ),
    new MML( "@vr",(int)enmMML. _REL_ENV,      isAllTrack ),
    new MML( "@v",(int)enmMML. _ENVELOPE,      isAllTrack ),

//		{ "@@r", _REL_ORG_TONE,		(TRACK(0)|TRACK(1)|FMTRACK|VRC7TRACK|VRC6PLSTRACK|N106TRACK|MMC5PLSTRACK) },
//		{ "@@", _ORG_TONE,		(TRACK(0)|TRACK(1)|FMTRACK|VRC7TRACK|VRC6PLSTRACK|N106TRACK|MMC5PLSTRACK) },

    new MML( "@", (int)enmMML._TONE,            isAllTrack ),
    new MML( "&", (int)enmMML._SLAR,            isAllTrack ),

    new MML( "yo",(int)enmMML. _DATA_WRITE_OFS, isAllTrack ),

    new MML( "y", (int)enmMML._DATA_WRITE,      isAllTrack ),
    new MML( "x", (int)enmMML._DATA_THRUE,      isAllTrack ),

    new MML( "|:",(int)enmMML. _REPEAT_ST2,     isAllTrack ),
    new MML( ":|",(int)enmMML. _REPEAT_END2,    isAllTrack ),
    new MML( "\\",(int)enmMML. _REPEAT_ESC2,    isAllTrack ),

    new MML( "k", (int)enmMML._KEY_OFF,         isAllTrack ),

    new MML( "L", (int)enmMML._SONG_LOOP,      isAllTrack ),
    new MML( "[", (int)enmMML._REPEAT_ST,      isAllTrack ),
    new MML( "]", (int)enmMML._REPEAT_END,     isAllTrack ),
    new MML( "|", (int)enmMML._REPEAT_ESC,     isAllTrack ),
    new MML( "{", (int)enmMML._CONT_NOTE,      isAllTrack ),
    new MML( "}", (int)enmMML._CONT_END,       isAllTrack ),
    new MML( "q", (int)enmMML._QUONTIZE,       isAllTrack ),
    new MML( "r", (int)enmMML._REST,           isAllTrack ),
    new MML( "^", (int)enmMML._TIE,            isAllTrack ),
    new MML( "!", (int)enmMML._DATA_BREAK,     isAllTrack ),
    new MML("", (int)enmMML._TRACK_END,     isAllTrack   ),
        };


        /*--------------------------------------------------------------
            エラー表示
         Input:

         Output:
            none
        --------------------------------------------------------------*/
        private void dispError(int no, string file, int line)
        {
            no = no * 2;
            if (wk.message_flag != 0)
            {
                no++;
            }
            if (!string.IsNullOrEmpty(file))
            {
                Console.Write("Error	: {0} {1:D6}: {2}\r\n", file, line, ErrorlMessage[no]);
            }
            else
            {
                Console.Write("Error	: {0}\n", ErrorlMessage[no]);
            }
            error_flag = 1;
        }



        /*--------------------------------------------------------------
            ワーニング表示
         Input:

         Output:
            none
        --------------------------------------------------------------*/
        private void dispWarning(int no, string file, int line)
        {
            if (wk.warning_flag != 0)
            {
                no = no * 2;
                if (wk.message_flag != 0)
                {
                    no++;
                }
                if (!string.IsNullOrEmpty(file))
                {
                    Console.Write("Warning: {0} {1:D6}: {2}\r\n", file, line, WarningMessage[no]);
                }
                else
                {
                    Console.Write("Warning: {0}\r\n", WarningMessage[no]);
                }
            }
        }



        /*--------------------------------------------------------------
            C言語タイプのリマークの削除
         Input:
            char	*ptr		:データ格納ポインタ
         Output:
            none
        --------------------------------------------------------------*/
        private void deleteCRemark(ref string buf)
        {
            StringBuilder sb = new StringBuilder(buf);
            int ptr = 0;

            int within_com = 0;
            while (sb.Length > ptr)
            {
                if (sb[ptr] == '/' && sb[ptr + 1] == '*')
                {
                    within_com = 1;
                    sb[ptr++] = ' ';
                    sb[ptr++] = ' ';
                    while (sb.Length > ptr)
                    {
                        if (sb[ptr] == '*' && sb[ptr + 1] == '/')
                        {
                            sb[ptr++] = ' ';
                            sb[ptr++] = ' ';
                            within_com = 0;
                            break;
                        }
                        if (sb[ptr] != '\n')
                        {
                            sb[ptr] = ' ';
                        }
                        ptr++;
                    }
                }
                else if (ptr + 1 < sb.Length && sb[ptr] == '\r' && sb[ptr + 1] == '\n')
                {
                    sb[ptr++] = ' ';
                }
                else
                {
                    ++ptr;
                }
            }
            if (within_com != 0)
            {
                Console.Write("Warning :");
                Console.Write(wk.message_flag != 0 ? "Reached EOF in comment" : "コメントが閉じられないままファイル終端に達しました");
                Console.Write("\r\n");
            }

            buf = sb.ToString();
        }


        /*----------------------------------------------------------*/
        /*	ファイル行数を求める										*/
        /* Input:														*/
        /*	char	*data		:データ格納ポインタ						*/
        /* Output:														*/
        /*	none														*/
        /*----------------------------------------------------------*/
        private int getLineCount(ref int ptr, string buf)
        {
            int line;

            line = 0;

            while (ptr < buf.Length)
            {
                if (buf[ptr] == '\n')
                {
                    line++;
                }
                ptr++;
            }
            if (buf[ptr - 1] != '\n')
            {
                line++;
            }
            return line;
        }


        /*--------------------------------------------------------------
        --------------------------------------------------------------*/
        private LINE[] readMmlFile(string fname, string fname_short)
        {
            LINE[] lbuf;
            int line_count;
            int i;
            string filestr;
            filestr = wk.srcBuf;// System.IO.File.ReadAllText(fname);

            if (string.IsNullOrEmpty(filestr))
            {
                error_flag = 1;
                return null;
            }

            int filestrPtr = 0;
            deleteCRemark(ref filestr);

            line_count = getLineCount(ref filestrPtr, filestr);
            lbuf = new LINE[(line_count + 1)];  /* ラインバッファを確保 */

            lbuf[0] = new LINE();
            lbuf[0].status = _HEADER;       /* LINEステータス[0]はmallocされた	*/
            lbuf[0].str = filestr;      /* ポインタとサイズが格納されている */
            lbuf[0].ostr = filestr;      /* ポインタとサイズが格納されている */
            lbuf[0].line = line_count;
            lbuf[0].filename = fname;
            lbuf[0].shortname = fname_short;

            filestrPtr = 0;

            for (i = 1; i <= line_count; i++)
            {
                lbuf[i] = new LINE();
                lbuf[i].filename = fname;
                lbuf[i].shortname = fname_short;
                lbuf[i].line = i;

                int nextPtr = filestr.IndexOf("\n", filestrPtr);
                lbuf[i].str = filestr.Substring(filestrPtr, nextPtr - filestrPtr);
                lbuf[i].ostr = lbuf[i].str;
                filestrPtr = nextPtr + 1;
            }

            return lbuf;
        }

        public class LINE
        {//typedef struct st_line
            public string filename;/* ファイル名							*/
            public string shortname;/* 短縮ファイル名					    */
            public int line;        /* 行番号								*/
            public int status;      /* 行ステータス(下記define参照)			*/
            public int param;       /* パラメータ(音色/トラック番号etc.)	*/
            public string str;      /* 行文字列								*/
            public LINE[] inc_ptr;    /* インクルードファイルのデータポインタ */

            public string ostr;     /* 元々の行文字列    					*/
        }



        /*--------------------------------------------------------------
            改行/EOFを0(NULL)にする(バッファを行単位で切り分け)
         Input:
            char	*ptr	:データ格納ポインタ
         Output:
            無し
        --------------------------------------------------------------*/
        private int changeNULL(int ptr, ref string buf)
        {
            StringBuilder sb = new StringBuilder(buf);

            while (ptr < sb.Length && sb[ptr] != '\n')
            {
                if (sb[ptr] == '\0') break;
                ptr++;
            }

            if (ptr < sb.Length)
            {
                sb[ptr] = '\0';
                ptr++;
            }

            buf = sb.ToString();
            return ptr;
        }


        /*---------------------------------------------------------
            @hoge123 = { ag ae aeag g} の処理

            @HOGE¥s*(¥d+)¥s*(=|)¥s*{.*?(}.*|)$
        -----------------------------------------------------------*/
        private int setEffectSub(LINE[] lptr, int line, ref int ptr_status_end_flag, int min, int max, int error_no)
        {
            int param, cnt = 0;
            string temp; int tempPtr = 0;
            temp = lptr[line].str;
            tempPtr = str.skipSpace(lptr[line].str, 0);
            param = str.Asc2Int(temp, tempPtr, ref cnt);

            if (cnt == 0)
                goto on_error;
            if (param < min || max <= param)
                goto on_error;

            lptr[line].param = param;
            tempPtr = str.skipSpace(temp, tempPtr + cnt);

            if (temp[tempPtr] == '=')
            {
                tempPtr++;
                tempPtr = str.skipSpace(temp, tempPtr);
            }

            if (temp[tempPtr] != '{')
                goto on_error;

            lptr[line].str = temp.Substring(tempPtr);
            ptr_status_end_flag = 1;


            while (tempPtr < temp.Length && temp[tempPtr] != '\0')
            {
                if (temp[tempPtr] == '}')
                {
                    ptr_status_end_flag = 0;
                }
                if (temp[tempPtr] == '\"')
                {
                    tempPtr = str.skipQuote(temp, tempPtr);
                }
                else
                if (str.isComment(temp, tempPtr))
                    tempPtr = str.skipComment(temp, tempPtr);
                else
                    tempPtr++;

            }

            return 1;
        on_error:
            lptr[line].status = 0;
            dispError(error_no, lptr[line].filename, line);
            return 0;
        }


        private int skipTrackHeader(string st, int ptr)
        {
            if (ptr >= st.Length || st[ptr] == '\0')
                return 0;

            while (ptr < st.Length && st[ptr] != 0 && str_track.IndexOf(st[ptr]) >= 0) ptr++;

            return str.skipSpace(st, ptr);
        }

        private int isTrackNum(string str, int ptr, int trk)
        {
            int temp;
            if (str[ptr] == 0)
                return 0;

            while ((temp = str_track.IndexOf(str[ptr])) >= 0)
            {
                if (temp == trk)
                    return 1;

                ptr++;
            }

            return 0;
        }


        /*--------------------------------------------------------------
            ヘッダーを求める
         Input:
            char	*ptr	:データ格納ポインタ
         Output:
            無し
        --------------------------------------------------------------*/
        private void getLineStatus(LINE[] lbuf, int inc_nest)
        {

            int line, i, param, cnt, track_flag, status_end_flag, bank, bank_ch;
            string temp, temp2;
            int tempPtr;
            int temp2Ptr;
            string ln;
            int ptr;


            int lptr = 0;
            status_end_flag = 0;

            for (line = 1; line <= lbuf[lptr].line; line++)
            {
                ln = lbuf[line].str;
                ptr = 0;

                ptr = str.skipSpace(ln, ptr);
                /* 前の行がエフェクト定義処理だった？ */
                if (((lbuf[lptr + line - 1].status & _SET_EFFECT) != 0) && (status_end_flag != 0))
                {
                    lbuf[lptr + line].status = (int)(lbuf[lptr + line - 1].status | _SAME_LINE);
                    lbuf[lptr + line].param = lbuf[lptr + line - 1].param;
                    lbuf[lptr + line].str = ln;
                    temp = ln;
                    tempPtr = ptr;
                    ptr = changeNULL(ptr, ref ln);
                    temp = ln;
                    status_end_flag = 1;

                    while (tempPtr < temp.Length && temp[tempPtr] != '\0')
                    {
                        if (temp[tempPtr] == '}')
                        {
                            status_end_flag = 0;
                        }
                        if (temp[tempPtr] == '\"')
                            tempPtr = str.skipQuote(temp, tempPtr);
                        else
                        if (str.isComment(temp, tempPtr))
                            tempPtr = str.skipComment(temp, tempPtr);
                        else
                            tempPtr++;
                    }

                    /* 行の先頭に何も無い時は無効な行とする */
                }
                else if (ptr == ln.Length || ln[ptr] == '\n' || ln[ptr] == '\0')
                {
                    lbuf[lptr + line].status = 0;
                    lbuf[lptr + line].str = ln;
                    ptr = changeNULL(ptr, ref ln);
                }
                else
                {
                    /* #/@付きヘッダーの時はヘッダーの文字列を大文字にする */
                    if (ln[ptr] == '#' || ln[ptr] == '@')
                    {
                        StringBuilder sb = new StringBuilder(ln);
                        i = 1;
                        while ((sb[ptr + i] != ' ') && (sb[ptr + i] != '\t') && (sb[ptr + i] != '\n'))
                        {
                            sb[ptr + i] = sb[ptr + i].ToString().ToUpper()[0];
                            i++;
                        }
                        /* ヘッダーも字列をテーブル文字列と比較 */
                        for (i = 0; head[i].status != -1; i++)
                        {
                            if (sb.ToString().Substring(ptr).IndexOf(head[i].str) >= 0)
                            {
                                break;
                            }
                        }
                        lbuf[lptr + line].status = head[i].status;
                        lbuf[lptr + line].str = lbuf[lptr + line].str.Substring(str.skipSpaceOld(ln, ptr + head[i].str.Length));   /* ヘッダー＋空白を飛ばしたところを先頭に */
                    }
                    else if (str_track.IndexOf(ln[ptr]) >= 0)
                    {
                        track_flag = 0;
                        temp = ln;
                        tempPtr = ptr;
                        while (tempPtr < ln.Length && temp[tempPtr] != ' ' && temp[tempPtr] != '\t')
                        {
                            temp2Ptr = str_track.IndexOf(temp[tempPtr]);
                            if (temp2Ptr < 0)
                            {
                                dispError((int)enmErrNum.INVALID_TRACK_HEADER, lbuf[lptr + line].filename, line);
                            }
                            else
                            {
                                track_flag = 1;
                            }
                            tempPtr++;
                        }

                        if (track_flag != 0)
                        {
                            lbuf[lptr + line].status = _TRACK;
                            lbuf[lptr + line].param = track_flag;
                            lbuf[lptr + line].str = ln.Substring(ptr);
                        }
                        else
                        {
                            lbuf[lptr + line].status = 0;
                            lbuf[lptr + line].param = 0;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //    lptr[line].status = -1;
                        //    lptr[line].str = skipSpace(ptr);
                    }

                    ptr = changeNULL(ptr, ref ln);

                    switch (lbuf[lptr + line].status)
                    {
                        //    /* Includeコマンドの処理 */
                        //    case _INCLUDE:
                        //        if (inc_nest > 16)
                        //        {               /* ネストは16段まで(再帰で呼ばれると終了しないので) */
                        //            dispWarning(TOO_MANY_INCLUDE_FILES, lptr[line].filename, line);
                        //            lptr[line].status = 0;
                        //        }
                        //        else
                        //        {
                        //            LINE* ltemp;
                        //            temp = skipSpaceOld(lptr[line].str); /* /をとばさないようにしてみる */
                        //            ltemp = readMmlFile(temp, temp);
                        //            if (ltemp != NULL)
                        //            {
                        //                lptr[line].inc_ptr = ltemp;
                        //                ++inc_nest;
                        //                getLineStatus(lptr[line].inc_ptr, inc_nest);
                        //                --inc_nest;
                        //            }
                        //            else
                        //            {
                        //                lptr[line].status = 0;                  /* ファイル読み込み失敗に付きエラー */
                        //                error_flag = 1;
                        //            }
                        //        }
                        //        break;
                        //    /* LFOコマンド */
                        //    case _SET_PITCH_MOD:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _PITCH_MOD_MAX, LFO_DEFINITION_IS_WRONG);
                        //        break;
                        /* ピッチエンベロープコマンド */
                        case _SET_PITCH_ENV:
                            setEffectSub(lbuf, line, ref status_end_flag, 0, _PITCH_ENV_MAX, (int)enmErrNum.PITCH_ENVELOPE_DEFINITION_IS_WRONG);
                            break;
                        //    /* 音量エンベロープコマンド */
                        //    case _SET_ENVELOPE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _ENVELOPE_MAX, ENVELOPE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* 自作音色 */
                        //    case _SET_TONE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _TONE_MAX, TONE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* アルペジオ */
                        //    case _SET_ARPEGGIO:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _ARPEGGIO_MAX, NOTE_ENVELOPE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* DPCM登録コマンド */
                        //    case _SET_DPCM_DATA:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _DPCM_MAX, DPCM_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* VRC7 Tone */
                        //    case _SET_VRC7_TONE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _VRC7_TONE_MAX, FM_TONE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* FM音色 */
                        //    case _SET_FM_TONE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _DPCM_MAX, FM_TONE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* HuSIC XPCM */
                        //    case _SET_XPCM_DATA:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _FM_TONE_MAX, XPCM_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* HuSIC WTB */
                        //    case _SET_WTB_TONE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _WTB_TONE_MAX, WTB_TONE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* MoonDriver */
                        //    // 波形音源音色
                        //    case _SET_TONETBL:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _TONETBL_MAX, TONETBL_DEFINITION_IS_WRONG);
                        //        break;
                        //    // FM音源音色
                        case _SET_FMOP:
                        case _SET_FMOP_FOUR:
                            setEffectSub(lbuf, line, ref status_end_flag, 0, _OPL3TBL_MAX, (int)enmErrNum.FM_TONE_DEFINITION_IS_WRONG);
                            break;
                        /* MoonDriver OPL3 FM */
                        case _EX_OPL3:
                            sndgen_flag |= BOPL3FLAG;
                            opl3_track_num = (OPL3_MAX);
                            break;
                        /* MoonDriver OPL4 no use*/
                        case _OPL4_NOUSE:
                            sndgen_flag &= (~BOPL4FLAG);
                            opl4_track_num = 0;
                            break;


                        //    /* namco106音源音色 */
                        //    case _SET_N106_TONE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _N106_TONE_MAX, N106_TONE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* ハードウェアエフェクト */
                        //    case _SET_HARD_EFFECT:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _HARD_EFFECT_MAX, HARD_EFFECT_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* エフェクト波形 */
                        //    case _SET_EFFECT_WAVE:
                        //        setEffectSub(lptr, line, &status_end_flag, 0, _EFFECT_WAVE_MAX, EFFECT_WAVE_DEFINITION_IS_WRONG);
                        //        break;
                        //    /* DISKSYSTEM FM音源使用フラグ */
                        //    case _EX_DISKFM:
                        //        sndgen_flag |= BDISKFM;
                        //        track_allow_flag |= FMTRACK;
                        //        fds_track_num = 1;
                        //        break;
                        //    /* VRC7 FM音源使用フラグ */
                        //    case _EX_VRC7:
                        //        sndgen_flag |= BVRC7;
                        //        track_allow_flag |= VRC7TRACK;
                        //        vrc7_track_num = 6;
                        //        break;
                        //    /* VRC6 音源使用フラグ */
                        //    case _EX_VRC6:
                        //        sndgen_flag |= BVRC6;
                        //        track_allow_flag |= VRC6TRACK;
                        //        vrc6_track_num = 3;
                        //        break;
                        //    /* FME7 音源使用フラグ */
                        //    case _EX_FME7:
                        //        sndgen_flag |= BFME7;
                        //        track_allow_flag |= FME7TRACK;
                        //        fme7_track_num = 3;
                        //        break;
                        //    /* MMC5 音源使用フラグ */
                        //    case _EX_MMC5:
                        //        sndgen_flag |= BMMC5;
                        //        track_allow_flag |= MMC5TRACK;
                        //        mmc5_track_num = 2;
                        //        break;
                        //    /* namco106 拡張音源使用フラグ */
                        //    case _EX_NAMCO106:
                        //        temp = skipSpace(lptr[line].str);
                        //        param = Asc2Int(temp, &cnt);
                        //        if (cnt != 0 && (0 <= param && param <= 8))
                        //        {
                        //            if (param == 0)
                        //            {
                        //                param = 1;
                        //            }
                        //            lptr[line].param = param;
                        //            n106_track_num = param;
                        //            sndgen_flag |= BNAMCO106;
                        //            for (i = 0; i < param; i++)
                        //            {
                        //                track_allow_flag |= TRACK(BN106TRACK + i);
                        //            }
                        //        }
                        //        else
                        //        {
                        //            dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //            lptr[line].status = 0;
                        //        }
                        //        break;
                        //    /* DPCM sound stops on 'r' command */
                        //    case _DPCM_RESTSTOP:
                        //        dpcm_reststop = 1;
                        //        break;
                        //    /* NSF mapper の bankswitching 禁止 */
                        //    case _NO_BANKSWITCH:
                        //        allow_bankswitching = 0;
                        //        break;
                        //    /* 自動バンク切り替え */
                        //    case _AUTO_BANKSWITCH:
                        //        temp = skipSpace(lptr[line].str);
                        //        param = Asc2Int(temp, &cnt);
                        //        if (cnt != 0 && (0 <= param && param <= 8192))
                        //        {
                        //            // 最初の一回しか有効にしない
                        //            if (!auto_bankswitch)
                        //            {
                        //                bank_usage[0] = 8192 - param;
                        //            }
                        //            auto_bankswitch = 1;
                        //        }
                        //        else
                        //        {
                        //            dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //        }
                        //        break;
                        //    /* バンク切り替え埋め込み(暫定処理の互換措置) */
                        //    case _BANK_CHANGE:
                        //        /*
                        //            #BANK-CHANGE <num0>,<num1>
                        //            上記バンク切り替えの拡張書式です。<num0>はバンク番号で0〜2の値が
                        //            入ります。<num1>はトラック番号で1〜14の数値が入り、1がAトラックに
                        //            対応しており以下2=B、3=C、…P=7となっています。
                        //            ちなみに以下は同じことをしています。
                        //            #BANK-CHANGE	n
                        //            #BANK-CHANGE	0,n

                        //            #BANK-CHANGEで同じバンクにトラックを持っていった場合、
                        //            最後に指定したものだけが有効。という仕様はあまり理解されていなかった。
                        //            ppmckでは全て有効とするため、その点は非互換。

                        //            mckc用の古いMMLをコンパイルするためには
                        //            最後のもの以外消す。

                        //        */
                        //        /*
                        //            数字とトラックの対応は非互換。

                        //            mckc
                        //            A B C D E | F | P Q R	S	T	U	V	W
                        //            1 2 3 4 5 | 6 | 7 8 9 10 11 12 13 14
                        //            pmckc
                        //            A B C D E | F | G H I	J	K	L |	P	Q	R	S	T	U	V	W
                        //            1 2 3 4 5 | 6 | 7 8 9 10 11 12 | 13 14 15 16 17 18 19 20
                        //            ppmckc
                        //            A B C D E | F | G H I	J	K	L |	M	N	O |	P	Q	R	S	T	U	V	W |	X	Y	Z |	a	b
                        //            1 2 3 4 5 | 6 | 7 8 9 10 11 12 | 13 14 15 | 16 17 18 19 20 21 22 23 | 24 25 26 | 27 28

                        //            mckc用の古いMMLをコンパイルするためには
                        //            P以降は 手動で 9 を足せばOK。(自動にはしないほうがよいでしょう)
                        //            てかこんな表を見なきゃいけないことが間違って(ry
                        //        */
                        //        temp = skipSpace(lptr[line].str);
                        //        param = Asc2Int(temp, &cnt);
                        //        if (cnt != 0)
                        //        {
                        //            temp += cnt;
                        //            temp = skipSpace(temp);
                        //            if (*temp == ',')
                        //            {
                        //                /* 拡張書式 */
                        //                temp++;
                        //                if ((0 <= param) && (param <= 2))
                        //                {
                        //                    bank = param; /* 0,1,2が1,2,3に対応 */
                        //                    //printf( "bank: %d\n", bank );
                        //                    temp = skipSpace(temp);
                        //                    param = Asc2Int(temp, &cnt); /* 1,2,3 がABCに対応 だから 0,1,2に対応 */
                        //                    if (cnt != 0 && (1 <= param && param <= _TRACK_MAX))
                        //                    {
                        //                        //bank_change[bank] = param-1;
                        //                        bank_sel[param - 1] = bank + 1;
                        //                    }
                        //                    else
                        //                    {
                        //                        dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                        lptr[line].status = 0;
                        //                        //bank_change[bank] = 0xff;
                        //                    }
                        //                }
                        //                else
                        //                {
                        //                    dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                    lptr[line].status = 0;
                        //                }
                        //            }
                        //            else
                        //            {
                        //                /* 非拡張書式 bank 1に入れる */
                        //                if (cnt != 0 && (1 <= param && param <= _TRACK_MAX))
                        //                {
                        //                    //bank_change[0] = param-1;
                        //                    bank_sel[param - 1] = 1;
                        //                }
                        //                else
                        //                {
                        //                    dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                    lptr[line].status = 0;
                        //                    //bank_change[0] = 0xff;
                        //                }
                        //            }
                        //        }
                        //        else
                        //        {
                        //            dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //            lptr[line].status = 0;
                        //        }
                        //        break;
                        //    /* バンク切り替え */
                        //    case _SET_SBANK:
                        //        temp = skipSpace(lptr[line].str);

                        //        if ((temp2 = strchr(str_track, *temp)) != NULL)
                        //        {
                        //            /* ABC..によるトラック指定 */
                        //            param = (int)((temp2 - str_track) + 1);
                        //            temp++;
                        //        }
                        //        else
                        //        {
                        //            /* 数字によるトラック指定 */
                        //            param = Asc2Int(temp, &cnt);
                        //            if (cnt == 0)
                        //            {
                        //                dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                lptr[line].status = 0;
                        //                break;
                        //            }
                        //            else
                        //            {
                        //                temp += cnt;
                        //            }
                        //        }

                        //        temp = skipSpace(temp);
                        //        if (*temp == ',')
                        //        {       /* バンク拡張 */
                        //            temp++;
                        //            if ((1 <= param) && (param <= _TRACK_MAX))
                        //            {
                        //                bank_ch = param;
                        //                // printf( "bank: %d\n", bank );
                        //                temp = skipSpace(temp);
                        //                param = Asc2Int(temp, &cnt);
                        //                if (cnt != 0)
                        //                {
                        //                    if (checkBankRange(param) == 0)
                        //                    {
                        //                        dispError(BANK_IDX_OUT_OF_RANGE, lptr[line].filename, line);
                        //                        break;
                        //                    }
                        //                    bank_sel[bank_ch - 1] = param;
                        //                }
                        //                else
                        //                {
                        //                    dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                    lptr[line].status = 0;
                        //                }
                        //            }
                        //            else
                        //            {
                        //                dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //                lptr[line].status = 0;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //            lptr[line].status = 0;
                        //        }
                        //        break;

                        //    /*	*/
                        //    case _EFFECT_INCLUDE:
                        //        include_flag = 1;
                        //        break;
                        /* タイトル */
                        case _TITLE:
                            //tempPtr = str.skipSpaceOld(lbuf[lptr + line].str, 0);
                            song_name = lbuf[lptr + line].str.TrimStart();
                            if (song_name.Length > 0 && song_name[song_name.Length - 1] == ' ')
                                song_name = song_name.Substring(0, song_name.Length - 1);
                            song_name = song_name.Substring(0, Math.Min(song_name.Length, 1023));
                            break;
                        /* 作曲者 */
                        case _COMPOSER:
                            //temp = skipSpaceOld(lptr[line].str);
                            composer = lbuf[lptr + line].str.TrimStart();
                            if (composer.Length > 0 && composer[composer.Length - 1] == ' ')
                                composer = composer.Substring(0, composer.Length - 1);
                            composer = composer.Substring(0, Math.Min(composer.Length, 1023));
                            break;
                        /* メーカー */
                        case _MAKER:
                            //temp = skipSpaceOld(lptr[line].str);
                            maker = lbuf[lptr + line].str.TrimStart();
                            if (maker.Length > 0 && maker[maker.Length - 1] == ' ')
                                maker = maker.Substring(0, maker.Length - 1);
                            maker = maker.Substring(0, Math.Min(maker.Length, 1023));
                            break;
                        /* 打ち込み者 */
                        case _PROGRAMER:
                            //temp = skipSpaceOld(lptr[line].str);
                            programer_buf = lbuf[lptr + line].str.TrimStart();
                            if (programer_buf.Length > 0 && programer_buf[programer_buf.Length - 1] == ' ')
                                programer_buf = programer_buf.Substring(0, programer_buf.Length - 1);
                            programer_buf = programer_buf.Substring(0, Math.Min(programer_buf.Length, 1023));
                            programer = programer_buf;
                            break;
                        //    /* PCMファイル */
                        //    case _PCM_FILE:
                        //        temp = skipSpaceOld(lptr[line].str);
                        //        strncpy(pcm_name, temp, 1023);
                        //        use_pcm = 1;
                        //        break;

                        //    /* オクターブ記号の反転 */
                        //    case _OCTAVE_REV:
                        //        temp = skipSpace(lptr[line].str);
                        //        param = Asc2Int(temp, &cnt);
                        //        if (cnt != 0)
                        //        {
                        //            if (param == 0)
                        //            {
                        //                octave_flag = 0;
                        //            }
                        //            else
                        //            {
                        //                octave_flag = 1;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            octave_flag = 1;
                        //        }
                        //        break;
                        //    /* qコマンド分母変更 */
                        //    case _GATE_DENOM:
                        //        temp = skipSpace(lptr[line].str);
                        //        param = Asc2Int(temp, &cnt);
                        //        if (cnt != 0 && param > 0)
                        //        {
                        //            gate_denom = param;
                        //        }
                        //        else
                        //        {
                        //            dispError(DEFINITION_IS_WRONG, lptr[line].filename, line);
                        //            lptr[line].status = 0;
                        //        }
                        //        break;
                        //    /*ディチューン、ピッチエンベロープ、LFOの方向修正 */
                        //    case _PITCH_CORRECTION:
                        //        pitch_correction = 1;
                        //        break;
                        /* ヘッダ無し */
                        case -1:
                            if ((lbuf[lptr + line - 1].status & _SET_EFFECT) != 0)
                            {
                                lbuf[lptr + line].status = (int)(lbuf[lptr + line - 1].status | _SAME_LINE);
                                lbuf[lptr + line].str = ln;// ptr;
                            }
                            else
                            {
                                /* エラーチェック */
                                dispError((int)enmErrNum.COMMAND_NOT_DEFINED, lbuf[lptr + line].filename, line);
                                lbuf[lptr + line].status = 0;
                                lbuf[lptr + line].str = ln;// ptr;
                            }
                            break;
                        case _TRACK:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }



        /*--------------------------------------------------------------
            音色の取得
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void getTone(LINE[] lptr)
        {
            int line, i, no, end_flag, offset, num, cnt;
            string ptrs; int ptr;

            cnt = 0;

            for (line = 1; line < lptr.Length; line++)// lptr[line].line; line++)
            {
                /* 音色定義だけど_SAME_LINEの時はエラー */
                if (lptr[line].status == ((int)_SET_TONE | unchecked((int)_SAME_LINE)))
                {
                    dispError((int)enmErrNum.TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
                    continue;
                }

                /* インクルードファイル処理 */
                if (lptr[line].status == _INCLUDE)
                {
                    getTone(lptr[line].inc_ptr);
                    continue;
                }

                /* 音色データ発見？ */
                if (lptr[line].status != _SET_TONE)
                    continue;

                no = lptr[line].param;              /* 音色番号取得 */
                ptrs = lptr[line].str;
                ptr = 0;
                ptr++;                              /* '{'の分を飛ばす */
                if (tone_tbl[no][0] != 0)
                {
                    dispWarning((int)enmSys.THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
                }
                tone_tbl[no][0] = 0;
                offset = 0;
                i = 1;
                end_flag = 0;

                while (end_flag == 0)
                {
                    ptr = str.skipSpace(ptrs, ptr);
                    switch (ptrs[ptr])
                    {
                        case '}':
                            if (tone_tbl[no][0] >= 1)
                            {
                                tone_tbl[no][i] = (int)enmEFTBL.END;
                                tone_tbl[no][0]++;
                            }
                            else
                            {
                                dispError((int)enmErrNum.PARAMETER_IS_LACKING, lptr[line].filename, line);
                                tone_tbl[no][0] = 0;
                            }
                            end_flag = 1;
                            line += offset;
                            break;
                        case '|':
                            tone_tbl[no][i] = (int)enmEFTBL.LOOP;
                            tone_tbl[no][0]++;
                            i++;
                            ptr++;
                            break;
                        case '\0':
                            offset++;
                            if (line + offset <= lptr[line].line)
                            {
                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
                                {
                                    ptrs = lptr[line + offset].str;
                                    ptr = 0;
                                }
                            }
                            else
                            {
                                dispError((int)enmErrNum.TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
                                tone_tbl[no][0] = 0;
                                end_flag = 1;
                            }
                            break;
                        default:
                            num = str.Asc2Int(ptrs, ptr, ref cnt);
                            //vrc6用に制限を外す(内蔵矩形波、MMC5は3まで)
                            //if( cnt != 0 && (0 <= num && num <= 3) ) {
                            if (cnt != 0 && (0 <= num && num <= 7))
                            {
                                tone_tbl[no][i] = num;
                                tone_tbl[no][0]++;
                                ptr += cnt;
                                i++;
                            }
                            else
                            {
                                dispError((int)enmErrNum.TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
                                tone_tbl[no][0] = 0;
                                end_flag = 1;
                            }
                            break;
                    }
                    ptr = str.skipSpace(ptrs, ptr);
                    if (ptrs[ptr] == ',')
                    {
                        ptr++;
                    }
                }
            }
        }



        //    /*--------------------------------------------------------------
        //        エンベロープの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getEnvelope(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* エンベロープデータ発見？ */
        //            if (lptr[line].status == _SET_ENVELOPE)
        //            {
        //                no = lptr[line].param;              /* エンベロープ番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (envelope_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                envelope_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (envelope_tbl[no][0] >= 1)
        //                            {
        //                                envelope_tbl[no][i] = EFTBL_END;
        //                                envelope_tbl[no][0]++;
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                envelope_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '|':
        //                            envelope_tbl[no][i] = EFTBL_LOOP;
        //                            envelope_tbl[no][0]++;
        //                            i++;
        //                            ptr++;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                envelope_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0 && (0 <= num && num <= 127))
        //                            {
        //                                envelope_tbl[no][i] = num;
        //                                envelope_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                            }
        //                            else
        //                            {
        //                                dispError(ENVELOPE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                envelope_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* エンベロープ定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_ENVELOPE | _SAME_LINE))
        //            {
        //                dispError(ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getEnvelope(lptr[line].inc_ptr);
        //            }
        //        }
        //    }

        /*--------------------------------------------------------------
            ピッチエンベロープの取得
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void getPitchEnv(LINE[] lptr)
        {
            int line, i, no, end_flag, offset, num, cnt;
            string buf; int ptr;

            cnt = 0;

            for (line = 1; line < lptr.Length; line++)
            {
                /* ピッチエンベロープ定義だけど_SAME_LINEの時はエラー */
                if (lptr[line].status == (_SET_PITCH_ENV | unchecked((int)_SAME_LINE)))
                {
                    dispError((int)enmErrNum.PITCH_ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
                    continue;
                }

                /* インクルードファイル処理 */
                if (lptr[line].status == _INCLUDE)
                {
                    getPitchEnv(lptr[line].inc_ptr);
                    continue;
                }

                /* ピッチエンベロープデータ発見？ */
                if (lptr[line].status != _SET_PITCH_ENV)
                    continue;

                no = lptr[line].param;              /* ピッチエンベロープ番号取得 */
                buf = lptr[line].str;
                ptr = 0;
                ptr++;                              /* '{'の分を飛ばす */
                if (pitch_env_tbl[no][0] != 0)
                {
                    dispWarning((int)enmSys.THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
                }
                pitch_env_tbl[no][0] = 0;
                offset = 0;
                i = 1;
                end_flag = 0;
                while (end_flag == 0)
                {
                    ptr = str.skipSpace(buf, ptr);
                    char c = (ptr < buf.Length) ? buf[ptr] : '\0';

                    switch (c)
                    {
                        case '}':
                            if (pitch_env_tbl[no][0] >= 1)
                            {
                                pitch_env_tbl[no][i] = (int)enmEFTBL.END;
                                pitch_env_tbl[no][0]++;
                            }
                            else
                            {
                                dispError((int)enmErrNum.PARAMETER_IS_LACKING, lptr[line].filename, line);
                                pitch_env_tbl[no][0] = 0;
                            }
                            end_flag = 1;
                            line += offset;
                            break;
                        case '|':
                            pitch_env_tbl[no][i] = (int)enmEFTBL.LOOP;
                            pitch_env_tbl[no][0]++;
                            i++;
                            ptr++;
                            break;
                        case '\0':
                            offset++;
                            if (line + offset < lptr.Length)
                            {
                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
                                {
                                    buf = lptr[line + offset].str;
                                    ptr = 0;
                                }
                            }
                            else
                            {
                                dispError((int)enmErrNum.PITCH_ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
                                pitch_env_tbl[no][0] = 0;
                                end_flag = 1;
                            }
                            break;
                        default:
                            num = str.Asc2Int(buf, ptr, ref cnt);

                            // ピッチ方向修正
                            if (pitch_correction != 0)
                                num = 0 - num;

                            if (cnt != 0 && (-127 <= num && num <= 126))
                            {
                                pitch_env_tbl[no][i] = num;
                                pitch_env_tbl[no][0]++;
                                ptr += cnt;
                                i++;
                            }
                            else
                            {
                                dispError((int)enmErrNum.PITCH_ENVELOPE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
                                pitch_env_tbl[no][0] = 0;
                                end_flag = 1;
                            }
                            break;
                    }

                    ptr = str.skipSpace(buf, ptr);
                    c = (ptr < buf.Length) ? buf[ptr] : '\0';

                    if (c == ',')
                    {
                        ptr++;
                    }
                }

            }
        }

        //    /*--------------------------------------------------------------
        //        ピッチモジュレーションの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getPitchMod(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_PITCH_MOD)
        //            {
        //                no = lptr[line].param;              /* LFO番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (pitch_mod_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                pitch_mod_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (pitch_mod_tbl[no][0] >= 3)
        //                            {
        //                                //OK.
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                pitch_mod_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(LFO_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                pitch_mod_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0)
        //                            {
        //                                switch (i)
        //                                {
        //                                    case 1:
        //                                    case 2:
        //                                    case 3:
        //                                        if (0 <= num && num <= 255)
        //                                        {
        //                                            pitch_mod_tbl[no][i] = num;
        //                                            pitch_mod_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            pitch_mod_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    case 4:
        //                                        if (0 <= num && num <= 255)
        //                                        {
        //                                            pitch_mod_tbl[no][i] = num;
        //                                            pitch_mod_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            pitch_mod_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    default:
        //                                        dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        pitch_mod_tbl[no][0] = 0;
        //                                        end_flag = 1;
        //                                        break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                pitch_mod_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_PITCH_MOD | _SAME_LINE))
        //            {
        //                dispError(LFO_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getPitchMod(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        ノートエンベロープの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getArpeggio(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* アルペジオデータ発見？ */
        //            if (lptr[line].status == _SET_ARPEGGIO)
        //            {
        //                no = lptr[line].param;              /* エンベロープ番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (arpeggio_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                arpeggio_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (arpeggio_tbl[no][0] >= 1)
        //                            {
        //                                arpeggio_tbl[no][i] = EFTBL_END;
        //                                arpeggio_tbl[no][0]++;
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                arpeggio_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '|':
        //                            arpeggio_tbl[no][i] = EFTBL_LOOP;
        //                            arpeggio_tbl[no][0]++;
        //                            i++;
        //                            ptr++;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(NOTE_ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                arpeggio_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0)
        //                            {
        //                                if (num >= 0)
        //                                {
        //                                    arpeggio_tbl[no][i] = num;
        //                                }
        //                                else
        //                                {
        //                                    arpeggio_tbl[no][i] = (-num) | 0x80;
        //                                }
        //                                arpeggio_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                            }
        //                            else
        //                            {
        //                                dispError(NOTE_ENVELOPE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                arpeggio_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* アルペジオ定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_ARPEGGIO | _SAME_LINE))
        //            {
        //                dispError(NOTE_ENVELOPE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getArpeggio(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        DPCMの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getDPCM(LINE* lptr)
        //    {
        //        int line, i, no, offset, end_flag, num, cnt;
        //        char* ptr;
        //        FILE* fp;
        //        DPCMTBL* tbl;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            // DPCMデータ発見？
        //            if (lptr[line].status == _SET_DPCM_DATA)
        //            {
        //                no = lptr[line].param;              // DPCM番号取得
        //                ptr = lptr[line].str;
        //                ptr++;                              // '{'の分を飛ばす
        //                tbl = &dpcm_tbl[no];
        //                if (tbl->flag != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                tbl->flag = 1;                      // フラグを使用中に
        //                tbl->index = -1;
        //                tbl->fname = NULL;
        //                tbl->freq = 0;
        //                tbl->size = 0;
        //                tbl->delta_init = 0;
        //                offset = 0;
        //                i = 0;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        // データ終了
        //                        case '}':
        //                            switch (i)
        //                            {
        //                                case 0:
        //                                case 1:
        //                                    dispError(DPCM_PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                    tbl->flag = 0;
        //                                    break;
        //                                default:
        //                                    line += offset;
        //                                    break;
        //                            }
        //                            end_flag = 1;
        //                            break;
        //                        // 改行
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(DPCM_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                tbl->flag = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            switch (i)
        //                            {
        //                                // ファイル名を登録
        //                                case 0:
        //                                    // ファイル名は"..."で囲まれている？
        //                                    if (*ptr == '\"')
        //                                    {
        //                                        ptr++;
        //                                        //ptr = skipSpace( ptr );
        //                                        //"file.dmc"はOK. " file.dmc"はNG.
        //                                        tbl->fname = ptr;
        //                                        while (*ptr != '\"' && *ptr != '\0')
        //                                        {
        //                                            ptr++;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        tbl->fname = ptr;
        //                                        //空白があるところまではファイル名
        //                                        // '/'';'はとばさない
        //                                        while (*ptr != ' ' && *ptr != '\t' && *ptr != '\0')
        //                                        {
        //                                            ptr++;
        //                                        }
        //                                    }
        //                                    *ptr = '\0';
        //                                    ptr++;
        //                                    // ファイル存在チェック/サイズチェック
        //                                    if ((fp = openDmc(tbl->fname)) == NULL)
        //                                    {
        //                                        dispError(DPCM_FILE_NOT_FOUND, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    else
        //                                    {
        //                                        fseek(fp, 0, SEEK_END);
        //                                        tbl->size = (int)ftell(fp);
        //                                        fseek(fp, 0, SEEK_SET);
        //                                        fclose(fp);
        //                                    }
        //                                    i++;
        //                                    break;
        //                                // 再生周波数を登録
        //                                case 1:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && (0 <= num && num <= 15))
        //                                    {
        //                                        tbl->freq = num;
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(DPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                // 再生サイズを登録
        //                                case 2:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && num == 0)
        //                                    {
        //                                        //値が0のときは省略と同じ
        //                                        ptr += cnt;
        //                                        i++;
        //                                        break;
        //                                    }
        //                                    if (cnt != 0 && (0 < num && num < 16384))
        //                                    {
        //                                        tbl->size = num;
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(DPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                // デルタカウンタ($4011)初期値を登録
        //                                case 3:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && ((0 <= num && num <= 0x7f) || num == 0xff))
        //                                    {
        //                                        tbl->delta_init = num;
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(DPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                // ループ情報($4010のbit7,6)を登録
        //                                case 4:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && (0 <= num && num <= 2))
        //                                    {
        //                                        tbl->freq |= (num << 6);
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(DPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                default:
        //                                    dispError(DPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                    tbl->flag = 0;
        //                                    end_flag = 1;
        //                                    break;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                if (tbl->size > (0xff) * 16 + 1)
        //                {
        //                    dispError(DPCM_FILE_SIZE_OVER, lptr[line + offset].filename, line);
        //                    tbl->flag = 0;
        //                }
        //                else if ((tbl->size % 16) != 1)
        //                {
        //                    dispWarning(DPCM_FILE_SIZE_ERROR, lptr[line + offset].filename, line);
        //                }
        //                // DPCM定義だけど_SAME_LINEの時はエラー
        //            }
        //            else if (lptr[line].status == (_SET_DPCM_DATA | _SAME_LINE))
        //            {
        //                dispError(DPCM_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                // インクルードファイル処理
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getDPCM(lptr[line].inc_ptr);
        //            }
        //        }
        //    }

        //    /*--------------------------------------------------------------
        //        XPCMの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getXPCM(LINE* lptr)
        //    {
        //        int line, i, no, offset, end_flag, num, cnt;
        //        char* ptr;
        //        FILE* fp;
        //        DPCMTBL* tbl;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            // DPCMデータ発見？
        //            if (lptr[line].status == _SET_XPCM_DATA)
        //            {
        //                no = lptr[line].param;              // DPCM番号取得
        //                ptr = lptr[line].str;
        //                ptr++;                              // '{'の分を飛ばす
        //                tbl = &xpcm_tbl[no];
        //                if (tbl->flag != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                tbl->flag = 1;                      // フラグを使用中に
        //                tbl->index = -1;
        //                tbl->fname = NULL;
        //                tbl->freq = 0;
        //                tbl->size = 0;
        //                tbl->delta_init = 0;
        //                offset = 0;
        //                i = 0;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        // データ終了
        //                        case '}':
        //                            switch (i)
        //                            {
        //                                case 0:
        //                                    dispError(XPCM_PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                    tbl->flag = 0;
        //                                    break;
        //                                default:
        //                                    line += offset;
        //                                    break;
        //                            }
        //                            end_flag = 1;
        //                            break;
        //                        // 改行
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(XPCM_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                tbl->flag = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            switch (i)
        //                            {
        //                                // ファイル名を登録
        //                                case 0:
        //                                    // ファイル名は"..."で囲まれている？
        //                                    if (*ptr == '\"')
        //                                    {
        //                                        ptr++;
        //                                        //ptr = skipSpace( ptr );
        //                                        //"file.dmc"はOK. " file.dmc"はNG.
        //                                        tbl->fname = ptr;
        //                                        while (*ptr != '\"' && *ptr != '\0')
        //                                        {
        //                                            ptr++;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        tbl->fname = ptr;
        //                                        //空白があるところまではファイル名
        //                                        // '/'';'はとばさない
        //                                        while (*ptr != ' ' && *ptr != '\t' && *ptr != '\0')
        //                                        {
        //                                            ptr++;
        //                                        }
        //                                    }
        //                                    *ptr = '\0';
        //                                    ptr++;
        //                                    // ファイル存在チェック/サイズチェック
        //                                    if ((fp = openDmc(tbl->fname)) == NULL)
        //                                    {
        //                                        dispError(XPCM_FILE_NOT_FOUND, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    else
        //                                    {
        //                                        fseek(fp, 0, SEEK_END);
        //                                        tbl->size = (int)ftell(fp);
        //                                        fseek(fp, 0, SEEK_SET);
        //                                        fclose(fp);
        //                                    }
        //                                    i++;
        //                                    break;
        //                                // 再生周波数を登録
        //                                case 1:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && (0 <= num && num <= 15))
        //                                    {
        //                                        tbl->freq = num;
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(XPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                // 再生サイズを登録
        //                                case 2:
        //                                    num = Asc2Int(ptr, &cnt);
        //                                    if (cnt != 0 && num == 0)
        //                                    {
        //                                        //値が0のときは省略と同じ
        //                                        ptr += cnt;
        //                                        i++;
        //                                        break;
        //                                    }
        //                                    if (cnt != 0 && (0 < num && num < 16384))
        //                                    {
        //                                        tbl->size = num;
        //                                    }
        //                                    else
        //                                    {
        //                                        dispError(XPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        tbl->flag = 0;
        //                                        end_flag = 1;
        //                                    }
        //                                    ptr += cnt;
        //                                    i++;
        //                                    break;
        //                                default:
        //                                    dispError(XPCM_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                    tbl->flag = 0;
        //                                    end_flag = 1;
        //                                    break;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                if (tbl->size > 0xffff)
        //                {
        //                    dispError(XPCM_FILE_SIZE_OVER, lptr[line + offset].filename, line);
        //                    tbl->flag = 0;
        //                }
        //                // DPCM定義だけど_SAME_LINEの時はエラー
        //            }
        //            else if (lptr[line].status == (_SET_DPCM_DATA | _SAME_LINE))
        //            {
        //                dispError(XPCM_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                // インクルードファイル処理
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getXPCM(lptr[line].inc_ptr);
        //            }
        //        }
        //    }




        //    /*--------------------------------------------------------------
        //        FDS FM音色の取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getFMTone(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_FM_TONE)
        //            {
        //                no = lptr[line].param;              /* 音色番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (fm_tone_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                fm_tone_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (fm_tone_tbl[no][0] == 64)
        //                            {
        //                                //OK.
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                fm_tone_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                fm_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0 && (0 <= num && num <= 0x3f))
        //                            {
        //                                fm_tone_tbl[no][i] = num;
        //                                fm_tone_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                                if (i > 65)
        //                                {
        //                                    dispError(ABNORMAL_PARAMETERS_OF_FM_TONE, lptr[line + offset].filename, line + offset);
        //                                    fm_tone_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                fm_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_FM_TONE | _SAME_LINE))
        //            {
        //                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getFMTone(lptr[line].inc_ptr);
        //            }
        //        }
        //    }

        //    /*--------------------------------------------------------------
        //        WaveTable音色の取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getWTBTone(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_WTB_TONE)
        //            {
        //                no = lptr[line].param;              /* 音色番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                wtb_tone_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(WTB_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                wtb_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0 && (0 <= num && num <= 0x1f))
        //                            {
        //                                wtb_tone_tbl[no][i] = num;
        //                                wtb_tone_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                                if (i > 33)
        //                                {
        //                                    dispError(ABNORMAL_PARAMETERS_OF_WTB_TONE, lptr[line + offset].filename, line + offset);
        //                                    wtb_tone_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(WTB_TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                wtb_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                if (i != 33)
        //                {
        //                    if (!error_flag)
        //                    {
        //                        dispError(ABNORMAL_PARAMETERS_OF_WTB_TONE, lptr[line].filename, line);
        //                        wtb_tone_tbl[no][0] = 0;
        //                    }
        //                }


        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_WTB_TONE | _SAME_LINE))
        //            {
        //                dispError(WTB_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getWTBTone(lptr[line].inc_ptr);
        //            }
        //        }
        //    }

        //    /*--------------------------------------------------------------
        //        ToneTableの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getToneTable(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_TONETBL)
        //            {
        //                no = lptr[line].param;              /* 音色番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                tonetbl_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(TONETBL_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                tonetbl_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0)
        //                            {
        //                                tonetbl_tbl[no][i] = num;
        //                                tonetbl_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                                if (i > 1024 + 1)
        //                                {
        //                                    dispError(ABNORMAL_PARAMETERS_OF_TONETBL, lptr[line + offset].filename, line + offset);
        //                                    tonetbl_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(TONETBL_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                tonetbl_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                if ((i % 9) != 1)
        //                {
        //                    if (!error_flag)
        //                    {
        //                        dispError(ABNORMAL_PARAMETERS_OF_TONETBL, lptr[line].filename, line);
        //                        tonetbl_tbl[no][0] = 0;
        //                    }
        //                }


        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_TONETBL | _SAME_LINE))
        //            {
        //                dispError(TONETBL_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getToneTable(lptr[line].inc_ptr);
        //            }
        //        }
        //    }

        /*--------------------------------------------------------------
            OPL3OPの取得
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        void getOPL3tbl(LINE[] lptr)
        {
            int line, i, no, end_flag, offset, num, cnt;
            string buf; int ptr;

            cnt = 0;

            int op_flag = 0;

            for (line = 1; line < lptr.Length; line++)
            {
                /* 音色定義だけど_SAME_LINEの時はエラー */
                if (lptr[line].status == (_SET_FMOP | unchecked((int)_SAME_LINE)) ||
                                   lptr[line].status == (_SET_FMOP_FOUR | unchecked((int)_SAME_LINE)))
                {
                    dispError((int)enmErrNum.FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
                    continue;
                }

                /* インクルードファイル処理 */
                if (lptr[line].status == _INCLUDE)
                {
                    getOPL3tbl(lptr[line].inc_ptr);
                    continue;
                }

                /* 音色データ発見？ */
                if (lptr[line].status != _SET_FMOP &&
                         lptr[line].status != _SET_FMOP_FOUR)
                    continue;

                // 4opモード
                if (lptr[line].status == _SET_FMOP_FOUR)
                    op_flag = 1;
                else
                    op_flag = 0;

                no = lptr[line].param;      /* 音色番号取得 */
                buf = lptr[line].str;
                ptr = 0;
                ptr++;              /* '{'の分を飛ばす */

                opl3op_flag[no] = op_flag; // op_flag
                opl3op_tbl[no][0] = 0;
                offset = 0;
                i = 1;
                end_flag = 0;
                while (end_flag == 0)
                {
                    ptr = str.skipSpace(buf, ptr);
                    char c = (ptr < buf.Length) ? buf[ptr] : '\0';

                    switch (c)
                    {
                        case '}':
                            end_flag = 1;
                            line += offset;
                            break;
                        case '\0':
                            offset++;
                            if (line + offset < lptr.Length)
                            {
                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
                                {
                                    buf = lptr[line + offset].str;
                                    ptr = 0;
                                }
                            }
                            else
                            {
                                dispError((int)enmErrNum.FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
                                opl3op_tbl[no][0] = 0;
                                line += offset;
                                end_flag = 1;
                            }
                            break;
                        default:
                            num = str.Asc2Int(buf, ptr, ref cnt);
                            if (cnt != 0)
                            {
                                opl3op_tbl[no][i] = num;
                                opl3op_tbl[no][0]++;
                                ptr += cnt;
                                i++;
                                if (i > 1024 + 1)
                                {
                                    dispError((int)enmErrNum.ABNORMAL_PARAMETERS_OF_FM_TONE, lptr[line + offset].filename, line + offset);
                                    opl3op_tbl[no][0] = 0;
                                    line += offset;
                                    end_flag = 1;
                                }
                            }
                            else
                            {
                                dispError((int)enmErrNum.FM_TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
                                opl3op_tbl[no][0] = 0;
                                line += offset;
                                end_flag = 1;
                            }
                            break;
                    }

                    ptr = str.skipSpace(buf, ptr);
                    c = (ptr < buf.Length) ? buf[ptr] : '\0';

                    if (c == ',')
                    {
                        ptr++;
                    }
                }

                if ((i % 12) != 6)
                {
                    if (error_flag == 0)
                    {
                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS_OF_FM_TONE, lptr[line].filename, line);
                        opl3op_tbl[no][0] = 0;
                    }
                }

            }
        }



        //    /*--------------------------------------------------------------
        //        VRC7音色の取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getVRC7Tone(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_VRC7_TONE)
        //            {
        //                no = lptr[line].param;              /* 音色番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (vrc7_tone_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                vrc7_tone_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (vrc7_tone_tbl[no][0] == 8)
        //                            {
        //                                //OK.
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                vrc7_tone_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                vrc7_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0 && (0 <= num && num <= 0xff))
        //                            {
        //                                vrc7_tone_tbl[no][i] = num;
        //                                vrc7_tone_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                                if (i > 9)
        //                                {
        //                                    dispError(ABNORMAL_PARAMETERS_OF_FM_TONE, lptr[line + offset].filename, line + offset);
        //                                    vrc7_tone_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                vrc7_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                if (i != 9)
        //                {
        //                    if (!error_flag)
        //                    {
        //                        dispError(ABNORMAL_PARAMETERS_OF_FM_TONE, lptr[line].filename, line);
        //                        vrc7_tone_tbl[no][0] = 0;
        //                    }
        //                }


        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_VRC7_TONE | _SAME_LINE))
        //            {
        //                dispError(FM_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getVRC7Tone(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        namco106音色の取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getN106Tone(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;
        //        //		 16 14 12 10	8	6	4	2
        //        int n106_tone_max[] = { 4, 4, 5, 6, 8, 10, 16, 32 };
        //        int n106_tone_num;

        //        cnt = 0;
        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_N106_TONE)
        //            {
        //                no = lptr[line].param;              /* 音色番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (n106_tone_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                n106_tone_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            //要素の数はwhileを抜けた後でチェック
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(N106_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                n106_tone_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (i == 1)
        //                            {                       // 登録バッファ(0〜5)
        //                                if (cnt != 0 && (0 <= num && num <= 32))
        //                                {
        //                                    n106_tone_tbl[no][1] = num;
        //                                    n106_tone_tbl[no][0]++;
        //                                    ptr += cnt;
        //                                    i++;
        //                                }
        //                                else
        //                                {
        //                                    dispError(N106_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                    n106_tone_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (cnt != 0 && (0 <= num && num <= 15))
        //                                {
        //                                    n106_tone_tbl[no][i] = num;
        //                                    n106_tone_tbl[no][0]++;
        //                                    ptr += cnt;
        //                                    i++;
        //                                    if (i > 2 + 32)
        //                                    {
        //                                        dispError(ABNORMAL_PARAMETERS_OF_N106_TONE, lptr[line + offset].filename, line + offset);
        //                                        n106_tone_tbl[no][0] = 0;
        //                                        line += offset;
        //                                        end_flag = 1;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    dispError(N106_TONE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                    n106_tone_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                switch (n106_tone_tbl[no][0])
        //                {
        //                    case 16 * 2 + 1: n106_tone_num = 0; break;
        //                    case 14 * 2 + 1: n106_tone_num = 1; break;
        //                    case 12 * 2 + 1: n106_tone_num = 2; break;
        //                    case 10 * 2 + 1: n106_tone_num = 3; break;
        //                    case 8 * 2 + 1: n106_tone_num = 4; break;
        //                    case 6 * 2 + 1: n106_tone_num = 5; break;
        //                    case 4 * 2 + 1: n106_tone_num = 6; break;
        //                    case 2 * 2 + 1: n106_tone_num = 7; break;
        //                    default: n106_tone_num = -1; break;
        //                }
        //                if (n106_tone_num == -1)
        //                {
        //                    dispError(ABNORMAL_PARAMETERS_OF_N106_TONE, lptr[line].filename, line);
        //                    n106_tone_tbl[no][0] = 0;
        //                }
        //                if (n106_tone_tbl[no][1] >= n106_tone_max[n106_tone_num])
        //                {
        //                    dispError(N106_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                    n106_tone_tbl[no][0] = 0;
        //                }
        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_N106_TONE | _SAME_LINE))
        //            {
        //                dispError(N106_TONE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getN106Tone(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        ハードウェアエフェクトの取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getHardEffect(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_HARD_EFFECT)
        //            {
        //                no = lptr[line].param;              /* エフェクト番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (hard_effect_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                hard_effect_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (hard_effect_tbl[no][0] == 4)
        //                            {
        //                                //OK.
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                hard_effect_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(LFO_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                                hard_effect_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0)
        //                            {
        //                                switch (i)
        //                                {
        //                                    case 1:
        //                                        if (0 <= num && num <= 255)
        //                                        {
        //                                            hard_effect_tbl[no][i] = num;
        //                                            hard_effect_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            hard_effect_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    case 2:
        //                                        if (0 <= num && num <= 4095)
        //                                        {
        //                                            hard_effect_tbl[no][i] = num;
        //                                            hard_effect_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            hard_effect_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    case 3:
        //                                        if (0 <= num && num <= 255)
        //                                        {
        //                                            hard_effect_tbl[no][i] = num;
        //                                            hard_effect_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            hard_effect_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    case 4:
        //                                        if (0 <= num && num <= 7)
        //                                        {
        //                                            hard_effect_tbl[no][i] = num;
        //                                            hard_effect_tbl[no][0]++;
        //                                            ptr += cnt;
        //                                            i++;
        //                                        }
        //                                        else
        //                                        {
        //                                            dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                            hard_effect_tbl[no][0] = 0;
        //                                            end_flag = 1;
        //                                        }
        //                                        break;
        //                                    default:
        //                                        dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                        hard_effect_tbl[no][0] = 0;
        //                                        end_flag = 1;
        //                                        break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(LFO_DEFINITION_IS_WRONG, lptr[line + offset].filename, line);
        //                                hard_effect_tbl[no][0] = 0;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_HARD_EFFECT | _SAME_LINE))
        //            {
        //                dispError(LFO_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getHardEffect(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        エフェクト波形の取得
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void getEffectWave(LINE* lptr)
        //    {
        //        int line, i, no, end_flag, offset, num, cnt;
        //        char* ptr;

        //        cnt = 0;

        //        for (line = 1; line <= lptr->line; line++)
        //        {
        //            /* 音色データ発見？ */
        //            if (lptr[line].status == _SET_EFFECT_WAVE)
        //            {
        //                no = lptr[line].param;              /* 波形番号取得 */
        //                ptr = lptr[line].str;
        //                ptr++;                              /* '{'の分を飛ばす */
        //                if (effect_wave_tbl[no][0] != 0)
        //                {
        //                    dispWarning(THIS_NUMBER_IS_ALREADY_USED, lptr[line].filename, line);
        //                }
        //                effect_wave_tbl[no][0] = 0;
        //                offset = 0;
        //                i = 1;
        //                end_flag = 0;
        //                while (end_flag == 0)
        //                {
        //                    ptr = skipSpace(ptr);
        //                    switch (*ptr)
        //                    {
        //                        case '}':
        //                            if (effect_wave_tbl[no][0] == 32)
        //                            {
        //                                //OK.
        //                            }
        //                            else
        //                            {
        //                                dispError(PARAMETER_IS_LACKING, lptr[line].filename, line);
        //                                effect_wave_tbl[no][0] = 0;
        //                            }
        //                            end_flag = 1;
        //                            line += offset;
        //                            break;
        //                        case '\0':
        //                            offset++;
        //                            if (line + offset <= lptr->line)
        //                            {
        //                                if ((lptr[line + offset].status & _SAME_LINE) == _SAME_LINE)
        //                                {
        //                                    ptr = lptr[line + offset].str;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(EFFECT_WAVE_DEFINITION_IS_WRONG, lptr[line].filename, line + offset);
        //                                effect_wave_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                        default:
        //                            num = Asc2Int(ptr, &cnt);
        //                            if (cnt != 0 && (0 <= num && num <= 7))
        //                            {
        //                                effect_wave_tbl[no][i] = num;
        //                                effect_wave_tbl[no][0]++;
        //                                ptr += cnt;
        //                                i++;
        //                                if (i > 33)
        //                                {
        //                                    dispError(EFFECT_WAVE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                    effect_wave_tbl[no][0] = 0;
        //                                    line += offset;
        //                                    end_flag = 1;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                dispError(EFFECT_WAVE_DEFINITION_IS_WRONG, lptr[line + offset].filename, line + offset);
        //                                effect_wave_tbl[no][0] = 0;
        //                                line += offset;
        //                                end_flag = 1;
        //                            }
        //                            break;
        //                    }
        //                    ptr = skipSpace(ptr);
        //                    if (*ptr == ',')
        //                    {
        //                        ptr++;
        //                    }
        //                }
        //                /* 音色定義だけど_SAME_LINEの時はエラー */
        //            }
        //            else if (lptr[line].status == (_SET_EFFECT_WAVE | _SAME_LINE))
        //            {
        //                dispError(EFFECT_WAVE_DEFINITION_IS_WRONG, lptr[line].filename, line);
        //                /* インクルードファイル処理 */
        //            }
        //            else if (lptr[line].status == _INCLUDE)
        //            {
        //                getEffectWave(lptr[line].inc_ptr);
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        DPCMデータのダブりを削除
        //     Input:
        //        無し
        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void sortDPCM(DPCMTBL dpcm_tbl[_DPCM_MAX] )
        //    {
        //        int i, j;

        //        for (i = 0; i < _DPCM_MAX; i++)
        //        {
        //            if (dpcm_tbl[i].flag == 0 || dpcm_tbl[i].index != -1) continue;
        //            for (j = 0; j < _DPCM_MAX; j++)
        //            {
        //                if (i == j) continue;
        //                if (dpcm_tbl[j].flag == 0) continue;
        //                // ファイル名が同じ？
        //                if (strcmp(dpcm_tbl[i].fname, dpcm_tbl[j].fname) == 0
        //                 && dpcm_tbl[i].size >= dpcm_tbl[j].size)
        //                {
        //                    dpcm_tbl[j].index = i;
        //                }
        //            }
        //        }
        //    }



        //    /*--------------------------------------------------------------
        //        DPCMサイズ修正(16byteバウンダリに修正)
        //     Input:

        //     Output:
        //         int: DPCMサイズ
        //    --------------------------------------------------------------*/
        //    int checkDPCMSize(DPCMTBL dpcm_tbl[_DPCM_MAX] )
        //    {
        //        int i;
        //        int adr = 0;
        //        int size = 0;
        //        int bank = 0; //0x4000ごとに増加
        //        for (i = 0; i < _DPCM_MAX; i++)
        //        {
        //            if (dpcm_tbl[i].flag != 0)
        //            {
        //                /*
        //                     $4013 * 16 + 1 = size
        //                     $4013 = (size - 1) / 16

        //                     newsize % 16 == 1が成立するように調整
        //                     size%16	(size-1)%16	diff(floor)	diff(ceil)
        //                     1		0		0		0
        //                     2		1		-1		+15
        //                     3		2		-2		+14
        //                     4		3		-3		+13
        //                     15		14		-14		+2
        //                     0		15		-15		+1
        //                */
        //                //printf("%s size $%x\n", dpcm_tbl[i].fname, dpcm_tbl[i].size);
        //                if ((dpcm_tbl[i].size % 16) != 1)
        //                {
        //                    int diff;
        //                    diff = (16 - ((dpcm_tbl[i].size - 1) % 16)) % 16; //ceil
        //                                                                      //diff =		- ((dpcm_tbl[i].size - 1) % 16); //floor
        //                    dpcm_tbl[i].size += diff;
        //                }
        //                //printf("%s fixed size $%x\n", dpcm_tbl[i].fname, dpcm_tbl[i].size);
        //                // スタートアドレスを設定
        //                if (dpcm_tbl[i].index == -1)
        //                {
        //                    if (((adr % 0x4000 + dpcm_tbl[i].size) > 0x4000) || (adr % 0x4000 == 0 && adr != 0))
        //                    {
        //                        /* 16KB境界をまたがる場合・または前回のアドレス切り上げで新しい16KB領域に乗った場合 */
        //                        adr += (0x4000 - (adr % 0x4000)) % 0x4000;
        //                        bank++;
        //                        dpcm_bankswitch = 1;
        //                    }
        //                    //printf("%s bank %d a %x s %x\n", dpcm_tbl[i].fname, bank, adr, size);

        //                    dpcm_tbl[i].start_adr = adr;
        //                    dpcm_tbl[i].bank_ofs = bank;
        //                    adr += dpcm_tbl[i].size;
        //                    size = adr;
        //                    // adr % 64 == 0が成立するように切り上げ
        //                    adr += (64 - (adr % 64)) % 64;
        //                }
        //            }
        //        }
        //        return size;
        //    }

        //    /*--------------------------------------------------------------
        //        DPCMサイズ修正(16byteバウンダリに修正)
        //     Input:

        //     Output:
        //         int: DPCMサイズ
        //    --------------------------------------------------------------*/
        //    int checkXPCMSize(DPCMTBL xpcm_tbl[_DPCM_MAX] )
        //    {
        //        int i;
        //        int size = 0;

        //        for (i = 0; i < _DPCM_MAX; i++)
        //        {
        //            if (xpcm_tbl[i].flag != 0)
        //                size += xpcm_tbl[i].size;
        //        }
        //        return size;
        //    }





        //    /*--------------------------------------------------------------
        //        DPCMデータ読み込み
        //     Input:

        //     Output:
        //    --------------------------------------------------------------*/
        //    void readDPCM(DPCMTBL dpcm_tbl[_DPCM_MAX] )
        //    {
        //        int i;
        //        FILE* fp;

        //        for (i = 0; i < dpcm_size; i++)
        //        {
        //            dpcm_data[i] = 0xaa;
        //        }

        //        for (i = 0; i < _DPCM_MAX; i++)
        //        {
        //            if (dpcm_tbl[i].flag != 0 && dpcm_tbl[i].index == -1)
        //            {
        //                fp = openDmc(dpcm_tbl[i].fname);
        //                if (fp == NULL)
        //                {
        //                    //				disperror( DPCM_FILE_NOT_FOUND, 0 );
        //                }
        //                else
        //                {
        //                    fread(&dpcm_data[dpcm_tbl[i].start_adr], 1, dpcm_tbl[i].size, fp);
        //                    fclose(fp);
        //                }
        //            }
        //        }
        //#if 0
        //    for( i = 0; i < _DPCM_TOTAL_SIZE; i++ ) {
        //        if( (i&0x0f) != 0x0f ) {
        //            printf( "%02x,", dpcm_data[i] );
        //        } else {
        //            printf( "%02x\n", dpcm_data[i] );
        //        }
        //    }
        //#endif
        //    }



        /*--------------------------------------------------------------
            音色/エンベロープのループチェック
         Input:

         Output:
            int	: 一番大きい音色番号
        --------------------------------------------------------------*/
        private int checkLoop(int[][] ptr, int max) //[128][1024]
        {
            int i, j, lp_flag, ret;

            ret = 0;

            for (i = 0; i < max; i++)
            {
                if (ptr[i][0] != 0)
                {
                    lp_flag = 0;
                    for (j = 1; j <= ptr[i][0]; j++)
                    {
                        if (ptr[i][j] == (int)enmEFTBL.LOOP) lp_flag = 1;
                    }
                    if (lp_flag == 0)
                    {
                        j = ptr[i][0];
                        ptr[i][j + 1] = ptr[i][j];
                        ptr[i][j] = ptr[i][j - 1];
                        ptr[i][j - 1] = (int)enmEFTBL.LOOP;
                        ptr[i][0]++;
                    }
                    ret = i + 1;
                }
            }
            return ret;
        }



        //    /*--------------------------------------------------------------
        //        音色の使用個数を返す
        //     Input:

        //     Output:
        //        int	: 一番大きい音色番号
        //    --------------------------------------------------------------*/
        //    int getMaxTone(int ptr[128][66], int max)
        //    {
        //        int i, ret;

        //        ret = 0;

        //        for (i = 0; i < max; i++)
        //        {
        //            if (ptr[i][0] != 0)
        //            {
        //                ret = i + 1;
        //            }
        //        }
        //        return ret;
        //    }

        /*--------------------------------------------------------------
            ToneTableの使用個数を返す
         Input:

         Output:
            int	: 一番大きい音色番号
        --------------------------------------------------------------*/
        private int getMaxToneTable(int[][] ptr, int max)//[_TONETBL_MAX][1024+2]
        {
            int i, ret;
            ret = 0;
            for (i = 0; i < max; i++)
            {
                if (ptr[i][0] != 0)
                {
                    ret = i + 1;
                }
            }
            return ret;
        }

        /*--------------------------------------------------------------
            Opl3tblの使用個数を返す
         Input:

         Output:
            int	: 一番大きい音色番号
        --------------------------------------------------------------*/
        private int getMaxOpl3tbl(int[][] ptr, int max)//[_OPL3TBL_MAX][1024+2]
        {
            int i, ret;
            ret = 0;
            for (i = 0; i < max; i++)
            {
                if (ptr[i][0] != 0)
                {
                    ret = i + 1;
                }
            }
            return ret;
        }



        //    /*--------------------------------------------------------------
        //        LFOの使用個数を返す
        //     Input:

        //     Output:
        //        int	: 一番大きいLFO番号
        //    --------------------------------------------------------------*/
        //    int getMaxLFO(int ptr[_PITCH_MOD_MAX][5], int max)
        //    {
        //        int i, ret;

        //        ret = 0;
        //        for (i = 0; i < max; i++)
        //        {
        //            if (ptr[i][0] != 0)
        //            {
        //                ret = i + 1;
        //            }
        //        }
        //        return ret;
        //    }



        //    /*--------------------------------------------------------------
        //        DPCMの使用個数を返す
        //     Input:

        //     Output:
        //        int	: 一番大きい音色番号
        //    --------------------------------------------------------------*/
        //    int getMaxDPCM(DPCMTBL dpcm_tbl[_DPCM_MAX] )
        //    {
        //        int i, ret = 0;

        //        for (i = 0; i < _DPCM_MAX; i++)
        //        {
        //            if (dpcm_tbl[i].flag != 0)
        //            {
        //                ret = i + 1;
        //            }
        //        }
        //        return ret;
        //    }



        //    /*--------------------------------------------------------------
        //        ハードウェアエフェクトの使用個数を返す
        //     Input:

        //     Output:
        //        int	: 一番大きい音色番号
        //    --------------------------------------------------------------*/
        //    int getMaxHardEffect(int ptr[_HARD_EFFECT_MAX][5], int max)
        //    {
        //        int i, ret;

        //        ret = 0;

        //        for (i = 0; i < max; i++)
        //        {
        //            if (ptr[i][0] != 0)
        //            {
        //                ret = i + 1;
        //            }
        //        }
        //        return ret;
        //    }



        //    /*--------------------------------------------------------------
        //        エフェクト波形の使用個数を返す
        //     Input:

        //     Output:
        //        int	: 一番大きい音色番号
        //    --------------------------------------------------------------*/
        //    int getMaxEffectWave(int ptr[_EFFECT_WAVE_MAX][33], int max)
        //    {
        //        int i, ret;

        //        ret = 0;

        //        for (i = 0; i < max; i++)
        //        {
        //            if (ptr[i][0] != 0)
        //            {
        //                ret = i + 1;
        //            }
        //        }
        //        return ret;
        //    }



        /*--------------------------------------------------------------
            音色/エンベロープの書き込み
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void writeTone(List<MmlDatum2> fp, int[][] tbl, string str, int max)//[128][1024]
        {
            int i, j, x;
            string t;

            t = string.Format("{0}_table:", str);
            fp.Add(new MmlDatum2(t + "\n", -2, t));

            if (max != 0)
            {
                for (i = 0; i < max; i++)
                {
                    if (tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:D03}", str, i);
                        fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                    }
                    else
                    {
                        fp.Add(new MmlDatum2("\tdw\t0\n", -1, 0, -1, 0));
                    }
                }
            }

            t = string.Format("{0}_lp_table:", str);
            fp.Add(new MmlDatum2(t + "\n", -2, t));

            if (max != 0)
            {
                for (i = 0; i < max; i++)
                {
                    if (tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_lp_{1:d03}", str, i);
                        fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                    }
                    else
                    {
                        fp.Add(new MmlDatum2("\tdw\t0\n", -1, 0, -1, 0));
                    }
                }

                for (i = 0; i < max; i++)
                {
                    if (tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:D03}:", str, i);
                        fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                        x = 0;
                        for (j = 1; j <= tbl[i][0]; j++)
                        {
                            if (tbl[i][j] == (int)enmEFTBL.LOOP)
                            {
                                if (x != 0) fp.Add(new MmlDatum2("\n", 0));
                                t = string.Format("{0}_lp_{1:d03}:", str, i);
                                fp.Add(new MmlDatum2(string.Format("{0}\n", t), -2, t));
                                x = 0;
                            }
                            else if (x == 0)
                            {
                                fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}", tbl[i][j] & 0xff), -1, tbl[i][j] & 0xff));
                                x++;
                            }
                            else if (x == 7)
                            {
                                fp.Add(new MmlDatum2(string.Format(",${0:x02}\n", tbl[i][j] & 0xff), -1, tbl[i][j] & 0xff));
                                x = 0;
                            }
                            else
                            {
                                fp.Add(new MmlDatum2(string.Format(",${0:x02}", tbl[i][j] & 0xff), -1, tbl[i][j] & 0xff));
                                x++;
                            }
                        }
                    }
                }
            }

            fp.Add(new MmlDatum2("\n\n", 0));
        }



        //    /*--------------------------------------------------------------
        //        FM音色の書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeToneFM(FILE* fp, int tbl[_FM_TONE_MAX][66], char* str, int max)
        //    {
        //        int i, j, x;

        //        fprintf(fp, "%s_data_table:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\tdw\t%s_%03d\n", str, i);
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdw\t0\n");
        //                }
        //            }

        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\n%s_%03d:\n", str, i);
        //                    x = 0;
        //                    for (j = 1; j <= tbl[i][0]; j++)
        //                    {               // tbl[i][0] = データー量(byte)
        //                        if (x == 0)
        //                        {
        //                            fprintf(fp, "\tdb\t$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                        else if (x == 7)
        //                        {
        //                            fprintf(fp, ",$%02x\n", tbl[i][j] & 0xff);
        //                            x = 0;
        //                        }
        //                        else
        //                        {
        //                            fprintf(fp, ",$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    /*--------------------------------------------------------------
        //        WTB音色の書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeToneWTB(FILE* fp, int tbl[_WTB_TONE_MAX][66], char* str, int max)
        //    {
        //        int i, j, x;

        //        fprintf(fp, "%s_data_table:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\tdw\t%s_%03d\n", str, i);
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdw\t0\n");
        //                }
        //            }

        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\n%s_%03d:\n", str, i);
        //                    x = 0;
        //                    for (j = 1; j <= tbl[i][0]; j++)
        //                    {               // tbl[i][0] = データー量(byte)
        //                        if (x == 0)
        //                        {
        //                            fprintf(fp, "\tdb\t$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                        else if (x == 7)
        //                        {
        //                            fprintf(fp, ",$%02x\n", tbl[i][j] & 0xff);
        //                            x = 0;
        //                        }
        //                        else
        //                        {
        //                            fprintf(fp, ",$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        fprintf(fp, "\n\n");
        //    }

        /*--------------------------------------------------------------
             ToneTableの書き込み
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void writeToneTable(List<MmlDatum2> fp, int[][] tbl, string str, int max)//[_TONETBL_MAX][1024+2]
        {
            int i, j, k, x;
            string t;
            byte b, b2;
            ushort s;

            t = string.Format("{0}_data_table:", str);
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -2, t));
            if (max != 0)
            {
                for (i = 0; i < max; i++)
                {
                    if (tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:d03}", str, i);
                        fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                    }
                    else
                    {
                        fp.Add(new MmlDatum2("\tdw\t0\n", -1, 0, -1, 0));
                    }
                }

                for (i = 0; i < max; i++)
                {
                    if (tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:d03}:", str, i);
                        fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                        x = 0;
                        for (j = 0, k = 1; j < tbl[i][0] / 9; k += 9, j++)
                        {
                            b = (byte)(tbl[i][k] & 0xff);
                            b2= (byte)(tbl[i][k + 1] & 0xff);
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02},${1:x02}\n", b, b2), -1, b, b2));
                            s = (ushort)(tbl[i][k + 2] & 0xffff);
                            fp.Add(new MmlDatum2(string.Format("\tdw\t${0:x04}\n", s), -1, (byte)s, (byte)(s >> 8)));
                            s = (ushort)(tbl[i][k + 3] & 0xffff);
                            fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", s), -1, (byte)s, (byte)(s >> 8)));
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02},${1:x02},${2:x02},${3:x02},${4:x02}\n"
                                , tbl[i][k + 4] & 0xff
                                , tbl[i][k + 5] & 0xff
                                , tbl[i][k + 6] & 0xff
                                , tbl[i][k + 7] & 0xff
                                , tbl[i][k + 8] & 0xff
                                )
                                , -1
                                , tbl[i][k + 4] & 0xff
                                , tbl[i][k + 5] & 0xff
                                , tbl[i][k + 6] & 0xff
                                , tbl[i][k + 7] & 0xff
                                , tbl[i][k + 8] & 0xff
                                ));

                        }
                    }
                }
            }

            fp.Add(new MmlDatum2("\n\n", 0));
        }

        /*--------------------------------------------------------------
             Opl3tblの書き込み
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void writeOPL3tbl(List<MmlDatum2> fp, string str, int max)
        {
            int i, j, k, x;
            string t;
            byte b;

            t = string.Format("{0}_data_table:", str);
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -2, t));
            if (max != 0)
            {
                for (i = 0; i < max; i++)
                {
                    if (opl3op_tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:d03}", str, i);
                        fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                    }
                    else
                    {
                        fp.Add(new MmlDatum2("\tdw\t0\n", -1, 0, -1, 0));
                    }
                }

                for (i = 0; i < max; i++)
                {
                    if (opl3op_tbl[i][0] != 0)
                    {
                        t = string.Format("{0}_{1:d03}:", str, i);
                        fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                        x = 0;

                        int cnt_val = opl3op_tbl[i][2] & 0x03;

                        if (opl3op_flag[i] != 0)
                        {
                            // 4OPモード
                            int[] opf_table = new int[] { 0, 2, 1, 3 };
                            cnt_val = opf_table[cnt_val];
                        }

                        // Reg.$C0
                        b = (byte)(((opl3op_tbl[i][1] & 0x07) << 1) | ((cnt_val & 0x01)));
                        fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                        // Reg.$C0 + 3
                        b = (byte)((cnt_val & 0x02) >> 1);
                        fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                        // Reg.$BD
                        fp.Add(new MmlDatum2("\tdb\t$00\n\n", -1, (byte)0));

                        for (j = 0, k = 6; j < (opl3op_tbl[i][0] - 5) / 12; k += 12, j++)
                        {
                            // Reg.$20
                            b = (byte)(((opl3op_tbl[i][k] & 0x1) << 7) |
                                ((opl3op_tbl[i][k + 1] & 0x1) << 6) |
                                ((opl3op_tbl[i][k + 2] & 0x1) << 5) |
                                ((opl3op_tbl[i][k + 3] & 0x1) << 4) |
                                (opl3op_tbl[i][k + 4] & 0xf));
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                            // Reg.$40
                            b = (byte)(((opl3op_tbl[i][k + 5] & 0x3) << 6) |
                                ((opl3op_tbl[i][k + 6] & 0x3f)));
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                            // Reg.$60
                            b = (byte)(((opl3op_tbl[i][k + 7] & 0x0f) << 4) |
                                ((opl3op_tbl[i][k + 8] & 0x0f)));
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                            // Reg.$80
                            b = (byte)(((opl3op_tbl[i][k + 9] & 0x0f) << 4) |
                                ((opl3op_tbl[i][k + 10] & 0x0f)));
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n", b), -1, b));

                            // Reg.$E0
                            b = (byte)(opl3op_tbl[i][k + 11] & 0x07);
                            fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}\n\n", b), -1, b));

                        }
                    }
                }
            }

            fp.Add(new MmlDatum2("\n\n", 0));
        }



        //    /*--------------------------------------------------------------
        //        VRC7音色の書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeToneVRC7(FILE* fp, int tbl[_VRC7_TONE_MAX][66], char* str, int max)
        //    {
        //        int i, j, x;

        //        fprintf(fp, "%s_data_table:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\tdw\t%s_%03d\n", str, i);
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdw\t0\n");
        //                }
        //            }

        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\n%s_%03d:\n", str, i);
        //                    x = 0;
        //                    for (j = 1; j <= tbl[i][0]; j++)
        //                    {               // tbl[i][0] = データー量(byte)
        //                        if (x == 0)
        //                        {
        //                            fprintf(fp, "\tdb\t$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                        else if (x == 7)
        //                        {
        //                            fprintf(fp, ",$%02x\n", tbl[i][j] & 0xff);
        //                            x = 0;
        //                        }
        //                        else
        //                        {
        //                            fprintf(fp, ",$%02x", tbl[i][j] & 0xff);
        //                            x++;
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        fprintf(fp, "\n\n");
        //    }




        //    /*--------------------------------------------------------------
        //        ハードウェアエフェクトの書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeHardEffect(FILE* fp, int tbl[_HARD_EFFECT_MAX][5], char* str, int max)
        //    {
        //        int i;

        //        fprintf(fp, "%s_effect_select:\n", str);
        //        for (i = 0; i < max; i++)
        //        {
        //            fprintf(fp, "\tdb\t$%02x,$84,$%02x,$85,$00,$87,$80,$88\n",
        //                tbl[i][1], (tbl[i][3] | 0x80));
        //            fprintf(fp, "\tdb\t$%02x,$86,$%02x,$87,$%02x,$ff,$00,$00\n",
        //                tbl[i][4], (tbl[i][2] & 0x00FF), ((tbl[i][2] & 0x0F00) >> 8));
        //        }
        //    }

        //    /*--------------------------------------------------------------
        //        エフェクト波形の書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeEffectWave(FILE* fp, int tbl[_EFFECT_WAVE_MAX][33], char* str, int max)
        //    {
        //        int i, j, x;

        //        fprintf(fp, "%s_4088_data:\n", str);
        //        for (i = 0; i < max; i++)
        //        {
        //            if (tbl[i][0] != 0)
        //            {
        //                x = 0;
        //                for (j = 1; j <= tbl[i][0]; j++)
        //                {               // tbl[i][0] = データー量(byte)
        //                    if (x == 0)
        //                    {
        //                        fprintf(fp, "\tdb\t$%02x", tbl[i][j] & 0xff);
        //                        x++;
        //                    }
        //                    else if (x == 7)
        //                    {
        //                        fprintf(fp, ",$%02x\n", tbl[i][j] & 0xff);
        //                        x = 0;
        //                    }
        //                    else
        //                    {
        //                        fprintf(fp, ",$%02x", tbl[i][j] & 0xff);
        //                        x++;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                /* ダミーデータを出力 */
        //                for (j = 0; j < 4; j++)
        //                {
        //                    fprintf(fp, "\tdb\t$00,$00,$00,$00,$00,$00,$00,$00\n");
        //                }
        //            }
        //        }

        //        fprintf(fp, "\n\n");
        //    }



        //    /*--------------------------------------------------------------
        //        N106音色の書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeToneN106(FILE* fp, int tbl[_N106_TONE_MAX][2+64], char* str, int max)
        //    {
        //        int i, j, x;

        //        // 使用チャンネル書き込み
        //        fprintf(fp, "%s_channel:\n", str);
        //        fprintf(fp, "\tdb\t%d\n", n106_track_num);
        //        // パラメータ書き込み
        //        fprintf(fp, "%s_wave_init:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                switch (tbl[i][0])
        //                {
        //                    case 2 * 2 + 1: j = 7; x = tbl[i][1] * 2 * 2; break;
        //                    case 4 * 2 + 1: j = 6; x = tbl[i][1] * 4 * 2; break;
        //                    case 6 * 2 + 1: j = 5; x = tbl[i][1] * 6 * 2; break;
        //                    case 8 * 2 + 1: j = 4; x = tbl[i][1] * 8 * 2; break;
        //                    case 10 * 2 + 1: j = 3; x = tbl[i][1] * 10 * 2; break;
        //                    case 12 * 2 + 1: j = 2; x = tbl[i][1] * 12 * 2; break;
        //                    case 14 * 2 + 1: j = 1; x = tbl[i][1] * 14 * 2; break;
        //                    case 16 * 2 + 1: j = 0; x = tbl[i][1] * 16 * 2; break;
        //                    default: j = 0; x = 0; break;
        //                }
        //                fprintf(fp, "\tdb\t$%02x,$%02x\n", j, x);
        //            }
        //        }
        //        // パラメータ書き込み
        //        fprintf(fp, "%s_wave_table:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "\tdw\t%s_wave_%03d\n", str, i);
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdw\t0\n");
        //                }
        //            }
        //        }
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (tbl[i][0] != 0)
        //                {
        //                    fprintf(fp, "%s_wave_%03d:\n", str, i);
        //                    fprintf(fp, "\tdb\t");
        //                    for (j = 0; j < tbl[i][0] / 2 - 1; j++)
        //                    {
        //                        fprintf(fp, "$%02x,", (tbl[i][2 + (j * 2) + 1] << 4) + tbl[i][2 + (j * 2) + 0]);
        //                    }
        //                    fprintf(fp, "$%02x\n", (tbl[i][2 + (j * 2) + 1] << 4) + tbl[i][2 + (j * 2) + 0]);
        //                }
        //            }
        //        }
        //        fprintf(fp, "\n\n");
        //    }



        //    /*--------------------------------------------------------------
        //        DPCMテーブルの書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeDPCM(FILE* fp, DPCMTBL dpcm_tbl[_DPCM_MAX], char* str, int max)
        //    {
        //        int i;
        //        int freq, adr, size, delta_init;
        //        char* fname;

        //        fprintf(fp, "%s:\n", str);
        //        for (i = 0; i < max; i++)
        //        {
        //            if (dpcm_tbl[i].flag != 0)
        //            {
        //                /*
        //                     $4013 * 16 + 1 = size
        //                     $4013 = (size - 1) / 16
        //                     $4012 * 64 = adr
        //                     adr = $4012 / 64
        //                */
        //                freq = dpcm_tbl[i].freq;
        //                size = (dpcm_tbl[i].size - 1) / 16;
        //                delta_init = dpcm_tbl[i].delta_init;
        //                if (dpcm_tbl[i].index == -1)
        //                {
        //                    adr = dpcm_tbl[i].start_adr / 64;
        //                    fname = dpcm_tbl[i].fname;
        //                }
        //                else
        //                {
        //                    adr = dpcm_tbl[dpcm_tbl[i].index].start_adr / 64;
        //                    fname = dpcm_tbl[dpcm_tbl[i].index].fname;
        //                }
        //                fprintf(fp, "\tdb\t$%02x,$%02x,$%02x,$%02x\t;%s\n", freq, delta_init, adr % 0x100, size, fname);
        //            }
        //            else
        //            {
        //                fprintf(fp, "\tdb\t$00,$00,$00,$00\t;unused\n");
        //            }
        //        }

        //        if (dpcm_bankswitch)
        //        {
        //            fprintf(fp, "%s_bank:\n", str);
        //            for (i = 0; i < max; i++)
        //            {
        //                int bank_ofs = 0;
        //                if (dpcm_tbl[i].flag != 0)
        //                {
        //                    if (dpcm_tbl[i].index == -1)
        //                    {
        //                        bank_ofs = dpcm_tbl[i].bank_ofs;
        //                        fname = dpcm_tbl[i].fname;
        //                    }
        //                    else
        //                    {
        //                        bank_ofs = dpcm_tbl[dpcm_tbl[i].index].bank_ofs;
        //                        fname = dpcm_tbl[dpcm_tbl[i].index].fname;
        //                    }
        //                    if (bank_ofs == 0)
        //                    {
        //                        fprintf(fp, "\tdb\t2*2\t;%s\n", fname);
        //                    }
        //                    else
        //                    {
        //                        bank_ofs -= 1;
        //                        fprintf(fp, "\tdb\t(DPCM_EXTRA_BANK_START + %d*2)*2\t;%s\n", bank_ofs, fname);
        //                    }
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdb\t0\t;unused\n");
        //                }
        //            }
        //        }

        //        fprintf(fp, "\n");
        //    }


        //    void writeXPCM(FILE* fp, DPCMTBL xpcm_tbl[_DPCM_MAX], char* str, int max)
        //    {
        //        int i;
        //        int freq, adr, size;
        //        //	int		cur_bank=0;

        //        fprintf(fp, "%s:\n", str);
        //        if (max != 0)
        //        {
        //            for (i = 0; i < max; i++)
        //            {
        //                if (xpcm_tbl[i].flag != 0)
        //                {
        //                    freq = xpcm_tbl[i].freq;
        //                    size = xpcm_tbl[i].size;
        //                    if (xpcm_tbl[i].index == -1)
        //                    {
        //                        fprintf(fp, "\tdw\t_xpcm%03d,$%04x\n", i, size);
        //                        fprintf(fp, "\tdb\tbank(_xpcm%03d),$00,$00,$00\n", i);
        //                    }
        //                    else
        //                    {
        //                        fprintf(fp, "\tdw\t_xpcm%03d,$%04x\n", xpcm_tbl[i].index, size);
        //                        fprintf(fp, "\tdb\tbank(_xpcm%03d),$00,$00,$00\n", xpcm_tbl[i].index);
        //                    }

        //                    //				fprintf( fp, "\tdb\t$%02x,$%02x,$%02x,$%02x\n", freq, 0, adr, size );
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\tdw\t$0000,$0000\n\tdb\t$00,$00,$00,$00\n");
        //                    //				fprintf( fp, "\tdb\t$00,$00,$00,$00\n", freq, 0, adr, size );
        //                }
        //            }

        //            if (xpcm_size != 0)
        //            {
        //                fprintf(fp, "\n\t.bank\tDATA_BANK+%1d\n", curr_bank);
        //                fprintf(fp, "\t.org\t$%04x\n\n", 0x6000);
        //                adr = 0;
        //                for (i = 0; i < max; i++)
        //                {
        //                    if (xpcm_tbl[i].flag != 0 && xpcm_tbl[i].index == -1)
        //                    {
        //                        if (adr + xpcm_tbl[i].size > 0x1FFF)
        //                        {
        //                            curr_bank++;
        //                            fprintf(fp, "\t.bank\tDATA_BANK+%1d\n", curr_bank);
        //                            fprintf(fp, "\t.org\t$%04x\n\n", 0x6000);
        //                            adr = 0;
        //                        }
        //                        fprintf(fp, "_xpcm%03d:\n", i);
        //                        fprintf(fp, "\t.incbin \"%s\"\n", xpcm_tbl[i].fname);
        //                        adr += xpcm_tbl[i].size;
        //                    }
        //                }
        //                fprintf(fp, "\n\t.bank\tCONST_BANK\n");
        //                curr_bank++;
        //            }
        //        }
        //        fprintf(fp, "\n\n");
        //    }



        //    /*--------------------------------------------------------------

        //    --------------------------------------------------------------*/
        //    static void writeDPCMSampleSub(FILE* fp)
        //    {

        //        fprintf(fp, "\t.org\t$FFFA\n");
        //        fprintf(fp, "\t.dw\tDMC_NMI\n");
        //        fprintf(fp, "\t.dw\tDMC_RESET\n");
        //        fprintf(fp, "\t.dw\tDMC_IRQ\n");
        //    }

        //    /*--------------------------------------------------------------
        //        DPCMデータの書き込み
        //     Input:

        //     Output:
        //        無し
        //    --------------------------------------------------------------*/
        //    void writeDPCMSample(FILE* fp)
        //    {
        //        int i;
        //        int nes_bank = 1; //8KB
        //        int bank_ofs = 0; //16KB

        //        fprintf(fp, "; begin DPCM samples\n");
        //        for (i = 0; i < dpcm_size; i++)
        //        {
        //            if (i % 0x2000 == 0)
        //            {
        //                nes_bank++;
        //                if (nes_bank == 4)
        //                {
        //                    nes_bank = 2;
        //                    bank_ofs++;
        //                }
        //                if (bank_ofs == 0)
        //                {
        //                    fprintf(fp, "\t.bank\t%1d\n", nes_bank);
        //                    putBankOrigin(fp, nes_bank);
        //                }
        //                else
        //                {
        //                    fprintf(fp, "\t.bank\tDPCM_EXTRA_BANK_START + %d*2 + %d - 2\n", bank_ofs - 1, nes_bank);
        //                    dpcm_extra_bank_num++;
        //                    fprintf(fp, "\t.org\t$%04x\n", 0x8000 + 0x2000 * nes_bank);
        //                }
        //            }
        //            if ((i & 0x0f) == 0x00)
        //            {
        //                fprintf(fp, "\tdb\t$%02x", dpcm_data[i]);
        //            }
        //            else if ((i & 0x0f) != 0x0f)
        //            {
        //                fprintf(fp, ",$%02x", dpcm_data[i]);
        //            }
        //            else
        //            {
        //                fprintf(fp, ",$%02x\n", dpcm_data[i]);
        //            }
        //            if (bank_ofs == 0)
        //            {
        //                bank_usage[nes_bank]++;
        //            }
        //        }
        //        fprintf(fp, "\n");
        //        fprintf(fp, "; end DPCM samples\n\n");

        //        if (dpcm_extra_bank_num)
        //        {
        //            int x;
        //            fprintf(fp, "; begin DPCM vectors\n");
        //            fprintf(fp, "\t.bank\t3\n");
        //            writeDPCMSampleSub(fp);
        //            for (x = 2; x <= dpcm_extra_bank_num; x += 2)
        //            {
        //                fprintf(fp, "\t.bank\tDPCM_EXTRA_BANK_START + %d\n", x - 1);
        //                writeDPCMSampleSub(fp);
        //            }
        //            fprintf(fp, "; end DPCM vectors\n");
        //        }
        //        fprintf(fp, "\n");
        //    }



        /*--------------------------------------------------------------
            タイトル/作曲者/メーカー/打ち込み者をコメントとして書き込み
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void writeSongInfo(List<MmlDatum2> fp)
        {
            fp.Add(new MmlDatum2(string.Format("; Title: {0}\n", song_name), -1));
            fp.Add(new MmlDatum2(string.Format("; Composer: {0}\n", composer), -1));
            fp.Add(new MmlDatum2(string.Format("; Maker: {0}\n", maker), -1));

            if (programer != null)
            {
                fp.Add(new MmlDatum2(string.Format("; Programer: {0}\n", programer), -1));
            }
            fp.Add(new MmlDatum2("\n", -1));
        }



        /*--------------------------------------------------------------

         Input: 文字列データをdbとしてmaxバイト出力(終端以降は0で埋める)

         Output:

        --------------------------------------------------------------*/
        private void printStrDb(List<MmlDatum2> fp, string str, int max)
        {
            Common.myEncoding enc = new Common.myEncoding();
            byte[] ary = enc.GetSjisArrayFromString(str);

            string des = "";
            List<int> lstInt = new List<int>();
            List<int[]> aryInt = new List<int[]>();

            for (int i = 0; i < max - 1; i++)
            {
                if (i < ary.Length)
                {
                    des += string.Format("${0:x02}", ary[i]);
                    lstInt.Add(-1);
                    lstInt.Add(ary[i]);
                }
                else
                {
                    des += "$00";
                    lstInt.Add(-1);
                    lstInt.Add(0);
                }

                if ((i + 1) % 8 == 0)
                {
                    des += "@@";
                    aryInt.Add(lstInt.ToArray());
                    lstInt.Clear();
                }
                else des += ", ";
            }

            des += "$00";
            lstInt.Add(-1);
            lstInt.Add(0);
            aryInt.Add(lstInt.ToArray());

            string[] sDes = des.Split(new string[] { "@@" }, StringSplitOptions.None);

            for (int i = 0; i < sDes.Length; i++)
            {

                fp.Add(new MmlDatum2(string.Format("\tdb\t{0}\n", sDes[i]), aryInt[i]));
            }
        }



        /*--------------------------------------------------------------
            タイトル/作曲者/メーカーをmacroとして書き込み
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void writeSongInfoMacro(List<MmlDatum2> fp)
        {
            string t = "TITLE\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            printStrDb(fp, song_name, 32);
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            t = "COMPOSER\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            printStrDb(fp, composer, 32);
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            t = "MAKER\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            printStrDb(fp, maker, 32);
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));

            // テキスト
            t = "TITLE_TEXT\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            fp.Add(new MmlDatum2(string.Format("\tdb\t\"{0}\",$00\n", song_name), -1, string.Format("\"{0}\",$00", song_name)));
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));

            t = "COMPOSER_TEXT\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            fp.Add(new MmlDatum2(string.Format("\tdb\t\"{0}\",$00\n", composer), -1, string.Format("\"{0}\",$00", composer)));
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));

            t = "MAKER_TEXT\t.macro";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            fp.Add(new MmlDatum2(string.Format("\tdb\t\"{0}\",$00\n", maker), -1, string.Format("\"{0}\",$00", maker)));
            t = "\t.endm";
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));

            if (use_pcm != 0)
            {
                t = "PCMFILE\t.macro";
                fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
                fp.Add(new MmlDatum2(string.Format("\tdb\t\"{0}\",$00\n", pcm_name), -1, string.Format("\"{0}\",$00", pcm_name)));
                t = "\t.endm";
                fp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
            }

        }





        /*--------------------------------------------------------------
            パラメータがn個のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBuf(int n, CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            int cnt = 0, i;
            int[] param = new int[PARAM_MAX];

            for (i = 0; i < PARAM_MAX; i++)
            {
                param[i] = 0;
            }

            if (n != 0)
            {
                for (i = 0; i < n; i++)
                {
                    cnt = 0;
                    param[i] = str.Asc2Int(buf, ptr, ref cnt);
                    if (cnt == 0)
                    {       /* パラメータが無い場合はエラーの出る数値に書き換える */
                        param[i] = unchecked((int)PARAM_OMITTED);
                    }
                    ptr += cnt;

                    if (i < n - 1) // nが2個以上のときは","の処理が入る
                    {

                        ptr = str.skipSpace(buf, ptr);
                        if (ptr < buf.Length && buf[ptr] == ',')
                        {
                            ptr++;
                            ptr = str.skipSpace(buf, ptr);
                        }
                        else //	","の区切りがない場合、パラメータは省略されている
                        {
                            for (i++; i < n; i++) // 現在の次のパラメータから省略
                                param[i] = unchecked((int)PARAM_OMITTED);
                        }
                    }
                }
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = com_no;
                cmd[cmdPtr].len = 0;
                if (n != 0)
                {
                    for (i = 0; i < n; i++)
                    {
                        cmd[cmdPtr].param[i] = param[i];
                    }
                }
            }

            return ptr;
        }



        /*--------------------------------------------------------------
            音長パラメータの取得
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int getLengthSub(string buf, int ptr, ref double len, double def)
        {
            int cnt = 0;
            double temp;

            /* フレーム指定 */
            if (buf[ptr] == '#')
            {
                ptr++;
                len = str.Asc2Int(buf, ptr, ref cnt);
                if (cnt != 0)
                {
                    ptr += cnt;
                    len = len / tbase;
                }
                else
                {
                    len = -1;
                }
                /* カウント指定 */
            }
            else if (buf[ptr] == '%')
            {
                ptr++;
                len = str.Asc2Int(buf, ptr, ref cnt);
                if (cnt != 0)
                {
                    ptr += cnt;
                }
                else
                {
                    len = -1;
                }
                /* 音楽的音長指定 */
            }
            else
            {
                len = str.Asc2Int(buf, ptr, ref cnt);
                if (cnt != 0)
                {
                    ptr += cnt;
                    if (len > 0)
                        len = _BASE / (len);
                }
                else
                {
                    /* パラメータが無い場合はエラーの出る数値に書き換える */
                    len = def;
                }
                /* エラー/lコマンドの時は処理させない */
                if (len != -1)
                {
                    /* 符点の処理(複数可能に) */
                    temp = len;
                    while (buf[ptr] == '.')
                    {
                        temp /= 2;
                        len += temp;
                        ptr++;
                    }
                }
            }
            return ptr;
        }


        /*--------------------------------------------------------------
            音長取得
         Output:
            *len:
        --------------------------------------------------------------*/
        private int getLength(string buf, int ptr, ref double len, double def)
        {
            ptr = getLengthSub(buf, ptr, ref len, def);
            /* 音長減算(一回だけ可能) */
            if (buf[ptr] == '-' || buf[ptr] == '~')
            {
                double len_adjust = 0;
                ptr++;
                ptr = getLengthSub(buf, ptr, ref len_adjust, def);
                if (len - len_adjust > 0)
                {
                    len = len - len_adjust;
                }
                else
                {
                    //dispError();呼び出し元でエラー捕捉
                    len = len - len_adjust;
                }
            }
            return ptr;
        }



        /*--------------------------------------------------------------
            パラメータが1個(音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufL(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            cmd[cmdPtr].cnt = 0;
            cmd[cmdPtr].line = line;
            cmd[cmdPtr].cmd = com_no;

            ptr = getLength(buf, ptr, ref cmd[cmdPtr].len, -1);
            if (cmd[cmdPtr].len > 0)
            {
                if (enable != 0)
                {
                    length = cmd[cmdPtr].len;
                }
            }
            else
            {
                dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
            }

            return ptr;
        }



        /*--------------------------------------------------------------
            パラメータが1個(音階/音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufN(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            int oct_ofs, note;
            double len = 0;

            com_no += transpose;

            /* c+-++-++--とかも出来るように対策(普通しないけど) */
            while (true)
            {
                if (buf[ptr] == '+')
                {
                    com_no++;
                    ptr++;
                }
                else if (buf[ptr] == '-')
                {
                    com_no--;
                    ptr++;
                }
                else
                {
                    break;
                }
            }
            /* オクターブをまたぐ時の補正処理 */
            oct_ofs = 0;
            while (com_no < _NOTE_C)
            {
                com_no += 12;
                oct_ofs--;
            }
            while (com_no > _NOTE_B)
            {
                com_no -= 12;
                oct_ofs++;
            }

            note = ((octave + oct_ofs) << 4) + com_no;
            /* 音階の範囲チェック */
            if (note < 0)
            {
                switch (note)
                {
                    case -5: note = 15; break;
                    case -6: note = 14; break;
                    case -7: note = 13; break;
                    default: note = 0; break;
                }
            }
            else if (note > MAX_NOTE)
            {
                note = MAX_NOTE;
            }

            ptr = getLength(buf, ptr, ref len, length);
            if (len <= 0)
            {
                dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                len = 0.0;
            }
            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = note;
                cmd[cmdPtr].len = len;
            }

            return ptr;
        }

        // ドラムフラグコマンドの処理
        private int setCommandBufD(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            double len = 0;
            int bit = 0x00;

            int loop_end = 0;

            while (loop_end == 0)
            {
                switch (buf[ptr])
                {
                    case 'H':
                        bit |= (1 << 0);
                        break;
                    case 'C':
                        bit |= (1 << 1);
                        break;
                    case 'M':
                        bit |= (1 << 2);
                        break;
                    case 'S':
                        bit |= (1 << 3);
                        break;
                    case 'B':
                        bit |= (1 << 4);
                        break;
                    default:
                        loop_end = 1;
                        break;
                }
                if (loop_end == 0)
                    ptr++;
            }

            // デフォルトの音長は0
            ptr = getLength(buf, ptr, ref len, 0);
            if (len < 0)
            {
                dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                len = 0.0;
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = com_no;
                cmd[cmdPtr].len = len;
                cmd[cmdPtr].param[0] = bit;
            }

            return ptr;
        }



        /*--------------------------------------------------------------
            パラメータが1個(音階(直接指定)/音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufN0(CMD[] cmd, int cmdPtr, string buf, int ptr, int line, int enable)
        {
            int cnt, note;
            double len = 0;

            cnt = 0;
            note = str.Asc2Int(buf, ptr, ref cnt);
            if (cnt == 0)
            {
                dispError((int)enmErrNum.ABNORMAL_PITCH_VALUE, cmd[cmdPtr].filename, line);
                return ptr + 1;
            }
            ptr += cnt;

            // 音階の範囲チェック
            if (note < 0)
            {
                note = 0;
            }
            else if (note > MAX_NOTE)
            {
                note = MAX_NOTE;
            }

            ptr = str.skipSpace(buf, ptr);               // 余分なスペースをスキップ
                                                         // ","があるときは音長が存在する
            if (buf[ptr] == ',')
            {
                ptr++;
                ptr = str.skipSpace(buf, ptr);           // 余分なスペースをスキップ

                ptr = getLength(buf, ptr, ref len, length);
                if (len <= 0)
                {
                    dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                    len = 0.0;
                }
                // ","がないときはデフォルトの音長を使用する
            }
            else
            {
                len = length;
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = note;
                cmd[cmdPtr].len = len;
            }

            return ptr;
        }



        /*--------------------------------------------------------------
            パラメータが1個(周波数(直接指定)/音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufN1(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            int cnt, freq;
            double len = 0;

            cnt = 0;
            freq = str.Asc2Int(buf, ptr, ref cnt);
            // 文字数チェック
            if (cnt == 0)
            {
                dispError((int)enmErrNum.ABNORMAL_PITCH_VALUE, cmd[cmdPtr].filename, line);
                return ptr + 1;
            }
            ptr += cnt;
            // パラメータ範囲チェック
            if (0x0008 <= freq || freq >= 0x07f2)
            {
                dispError((int)enmErrNum.ABNORMAL_PITCH_VALUE, cmd[cmdPtr].filename, line);
                return ptr + 1;
            }
            // "," があるときは音長取得
            ptr = str.skipSpace(buf, ptr);
            if (buf[ptr] == ',')
            {
                ptr++;
                ptr = str.skipSpace(buf, ptr);
                ptr = getLength(buf, ptr, ref len, length);
                if (len <= 0)
                {
                    dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                    len = 0.0;
                }
                // "," がないときはデフォルト音長に
            }
            else
            {
                len = length;
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = com_no;
                cmd[cmdPtr].len = len;
                cmd[cmdPtr].param[0] = freq;
            }

            return ptr;
        }



        /*--------------------------------------------------------------
            パラメータが1個(休符/音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufR(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            double len = 0;

            ptr = getLength(buf, ptr, ref len, length);
            if (len <= 0)
            {
                dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                len = 0.0;
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = com_no;
                cmd[cmdPtr].len = len;
            }

            return ptr;
        }


        /*--------------------------------------------------------------
            パラメータが1個(キーオフ/音長)のコマンドの処理
         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int setCommandBufK(CMD[] cmd, int cmdPtr, int com_no, string buf, int ptr, int line, int enable)
        {
            double len = 0;

            ptr = getLength(buf, ptr, ref len, length);
            if (len < 0)
            { /* 音長0あり */
                dispError((int)enmErrNum.ABNORMAL_NOTE_LENGTH_VALUE, cmd[cmdPtr].filename, line);
                len = 0.0;
            }

            if (enable != 0)
            {
                cmd[cmdPtr].cnt = 0;
                cmd[cmdPtr].line = line;
                cmd[cmdPtr].cmd = com_no;
                cmd[cmdPtr].len = len;
            }

            return ptr;
        }



        /*--------------------------------------------------------------

         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private CMD[] analyzeData(int trk, CMD[] cmd, ref int cmdPtr, LINE[] lptr)
        {
            int i, line, com, cnt;
            string buf; int ptr;


            cnt = 0;

            transpose = 0;

            for (line = 1; line < lptr.Length; line++)
            {
                if ((lptr[line].status == _TRACK) && (isTrackNum(lptr[line].str, 0, trk) != 0))
                {
                    ptr = skipTrackHeader(lptr[line].str, 0);
                    buf = lptr[line].str;//.Substring(ptr);

                    while (ptr < buf.Length && buf[ptr] != '\0')
                    {
                        ptr = str.skipSpace(buf, ptr);           // 余分なスペースをスキップ
                        if (ptr == buf.Length || buf[ptr] == '\0') break;        // このラインは終わり？
                                                                                 // コマンドを検索する
                        for (i = 0; mml[i].num != (int)enmMML._TRACK_END; i++)
                        {
                            int n = mml[i].cmd.Length;
                            n = Math.Min(n, buf.Length - ptr);
                            if (mml[i].cmd == buf.Substring(ptr, n)) break;
                        }

                        ptr += mml[i].cmd.Length;      // コマンドの文字数だけ文字をスキップ
                        cmd[cmdPtr].filename = lptr[line].shortname;   // エラー出力時のファイル名取得
                        switch (mml[i].num)
                        {
                            /* オクターブ */
                            case (int)enmMML._OCTAVE:
                                com = str.Asc2Int(buf, ptr, ref cnt);
                                if (cnt != 0)
                                {
                                    // コマンドは有効の時は処理を登録
                                    if ((mml[i].check(trk)) != 0)
                                    {
                                        if (trk == BTRACK(0) || trk == BTRACK(1) || trk == BTRACK(2))
                                        {
                                            octave = com;
                                        }
                                        else
                                        {
                                            octave = com;
                                        }
                                    }
                                    ptr += cnt;
                                }
                                break;
                            /* オクターブアップ */
                            case (int)enmMML._OCT_UP:
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (octave_flag == 0) { octave++; } else { octave--; }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* オクターブダウン */
                            case (int)enmMML._OCT_DW:
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (octave_flag == 0) { octave--; } else { octave++; }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* 音長設定 */
                            case (int)enmMML._LENGTH:
                                ptr = setCommandBufL(cmd, cmdPtr, (int)enmMML._LENGTH, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* 音符(nコマンド) */
                            case (int)enmMML._NOTE:
                                ptr = setCommandBufN0(cmd, cmdPtr, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* 音符(@nコマンド) */
                            case (int)enmMML._KEY:
                                ptr = setCommandBufN1(cmd, cmdPtr, (int)enmMML._KEY, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* 音符 */
                            case _NOTE_C:
                            case _NOTE_D:
                            case _NOTE_E:
                            case _NOTE_F:
                            case _NOTE_G:
                            case _NOTE_A:
                            case _NOTE_B:
                                ptr = setCommandBufN(cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* ドラムビット */
                            /* ドラムノート */
                            case (int)enmMML._DRUM_BIT:
                            case (int)enmMML._DRUM_NOTE:
                                ptr = setCommandBufD(cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            /* 休符/連符 */
                            case (int)enmMML._REST:
                            case (int)enmMML._CONT_END:
                            case (int)enmMML._TIE:
                            case (int)enmMML._WAIT:
                                ptr = setCommandBufR(cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* キーオフ */
                            case (int)enmMML._KEY_OFF:
                                ptr = setCommandBufK(cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /* コマンドパラメータが0個の物 */
                            case (int)enmMML._SLAR:         /* スラー */
                            case (int)enmMML._SONG_LOOP:            /* 曲ループ */
                            case (int)enmMML._REPEAT_ST:        /* リピート(現状では展開する) */
                            case (int)enmMML._REPEAT_ESC:       /* リピート途中抜け */
                            case (int)enmMML._CONT_NOTE:        /* 連符開始 */
                            case (int)enmMML._LFO_OFF:
                            case (int)enmMML._EP_OFF:
                            case (int)enmMML._EN_OFF:
                            case (int)enmMML._MH_OFF:
                            case (int)enmMML._REPEAT_ST2:       /* リピート2 */
                            case (int)enmMML._REPEAT_ESC2:  /* リピート途中抜け2 */
                            //					case _SHUFFLE_QUONTIZE_RESET:
                            //					case _SHUFFLE_QUONTIZE_OFF:
                            case (int)enmMML._SELF_DELAY_OFF:
                            case (int)enmMML._SELF_DELAY_QUEUE_RESET:
                                setCommandBuf(0, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._JUMP_FLAG:
                                use_jump = 1;
                                setCommandBuf(0, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;


                            /* コマンドパラメータが1個の物 */
                            case (int)enmMML._TEMPO:            /* テンポ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] <= 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TEMPO_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = (int)enmMML._NOP;
                                    }
                                    else
                                    {
                                        tbase = (double)_BASETEMPO / (double)cmd[cmdPtr].param[0];
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._TONE:         /* 音色切り替え */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    //vrc6用に制限を外す(内蔵矩形波、MMC5は@3まで)
                                    //if( cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 3 ) {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 127)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._REL_ORG_TONE:     /* リリース音色 */
                            case (int)enmMML._ORG_TONE:     /* 音色切り替え */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((mml[i].num == (int)enmMML._REL_ORG_TONE) && (cmd[cmdPtr].param[0] == 255))
                                    {
                                        //ok
                                    }
                                    else if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 127)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._ENVELOPE:     /* エンベロープ指定 */
                                cmd[cmdPtr].filename = lptr[line].filename;
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] == 255)
                                    {
                                        volume_flag = 0x0000;
                                    }
                                    else if (0 <= cmd[cmdPtr].param[0] && cmd[cmdPtr].param[0] <= 127)
                                    {
                                        volume_flag = 0x8000;
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_ENVELOPE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._REL_ENV:      /* リリースエンベロープ指定 */
                                cmd[cmdPtr].filename = lptr[line].filename;
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] == 255)
                                    {
                                        volume_flag = 0x0000;
                                    }
                                    else if (0 <= cmd[cmdPtr].param[0] && cmd[cmdPtr].param[0] <= 127)
                                    {
                                        volume_flag = 0x8000;
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_ENVELOPE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._VOL_PLUS:     /* 音量指定 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] == PARAM_OMITTED)
                                    {
                                        cmd[cmdPtr].param[0] = 1;
                                    }
                                    if ((0 <= volume_flag && volume_flag <= MAX_VOLUME))
                                    {
                                        cmd[cmdPtr].cmd = (int)enmMML._VOLUME;
                                        cmd[cmdPtr].param[0] = volume_flag + cmd[cmdPtr].param[0];
                                        if (((cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > MAX_VOLUME)))
                                        {
                                            dispError((int)enmErrNum.VOLUME_RANGE_OVER_OF_RELATIVE_VOLUME, lptr[line].filename, line);
                                            cmd[cmdPtr].cmd = 0;
                                            cmd[cmdPtr].line = 0;
                                        }
                                        else
                                        {
                                            volume_flag = cmd[cmdPtr].param[0];
                                        }
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.RELATIVE_VOLUME_WAS_USED_WITHOUT_SPECIFYING_VOLUME, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._VOL_MINUS:        /* 音量指定 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] == PARAM_OMITTED)
                                    {
                                        cmd[cmdPtr].param[0] = 1;
                                    }
                                    if ((0 <= volume_flag && volume_flag <= MAX_VOLUME))
                                    {
                                        cmd[cmdPtr].cmd = (int)enmMML._VOLUME;
                                        cmd[cmdPtr].param[0] = volume_flag - cmd[cmdPtr].param[0];
                                        if (cmd[cmdPtr].param[0] < 0)
                                        {
                                            dispError((int)enmErrNum.VOLUME_RANGE_UNDER_OF_RELATIVE_VOLUME, lptr[line].filename, line);
                                            cmd[cmdPtr].cmd = 0;
                                            cmd[cmdPtr].line = 0;
                                        }
                                        else
                                        {
                                            volume_flag = cmd[cmdPtr].param[0];
                                        }
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.RELATIVE_VOLUME_WAS_USED_WITHOUT_SPECIFYING_VOLUME, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._VOLUME:           /* 音量指定 HuSIC */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (((cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > MAX_VOLUME)))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_VOLUME_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                    else
                                    {
                                        volume_flag = cmd[cmdPtr].param[0];
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._HARD_ENVELOPE:
                                ptr = setCommandBuf(2, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 1)
                                     && (cmd[cmdPtr].param[1] < 0 || cmd[cmdPtr].param[1] > 63))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_ENVELOPE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                    else
                                    {
                                        volume_flag = 0x8000;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._QUONTIZE:     /* クオンタイズ(length*n/gate_denom) */
                                ptr = setCommandBuf(2, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[1] == unchecked((int)PARAM_OMITTED))
                                    {
                                        cmd[cmdPtr].param[1] = 0;
                                    }
                                    if (cmd[cmdPtr].param[0] < 0
                                             || cmd[cmdPtr].param[0] > gate_denom
                                             || (cmd[cmdPtr].param[0] == 0 && cmd[cmdPtr].param[1] <= 0)
                                             || (cmd[cmdPtr].param[0] == gate_denom && cmd[cmdPtr].param[1] > 0))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_QUANTIZE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._QUONTIZE2:        /* クオンタイズ(length-n) */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
#if false
                            case _SHUFFLE_QUONTIZE:	/* シャッフルクオンタイズ設定 */
                            ptr = setCommandBuf( 3, cmd, mml[i].num, ptr, line, mml[i].check(trk) );
                            if( (mml[i].check(trk)) != 0 ) {
                                if (	 cmd[cmdPtr].param[0] <= 0
                                         ||cmd[cmdPtr].param[1] <= 0
                                         ||cmd[cmdPtr].param[2] <= 0
                                         ||cmd[cmdPtr].param[0] == PARAM_OMITTED
                                         ||cmd[cmdPtr].param[1] == PARAM_OMITTED
                                         ||cmd[cmdPtr].param[2] == PARAM_OMITTED	) {
                                    dispError( ABNORMAL_SHUFFLE_QUANTIZE_VALUE,	lptr[line].filename, line );
                                    cmd[cmdPtr].cmd = _NOP;
                                    cmd[cmdPtr].line = 0;
                                }
                            } else {
                                dispError( UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line );
                            }
                            break;
#endif
                            case (int)enmMML._LFO_ON:           /* ソフトＬＦＯ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 63))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_LFO_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            /* HuSIC */
                            case (int)enmMML._FMLFO_SET:            /* LFO Trig Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 255)
                                    {
                                        dispError((int)enmErrNum.FMLFO_PARAM_IS_WRONG, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._FMLFO_FRQ:            /* LFO Freq Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 255)
                                    {
                                        dispError((int)enmErrNum.FMLFO_PARAM_IS_WRONG, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._NOISE_SW:         /* Noise Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 1)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._MODE_CHG:         /* Mode Change Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._WAVE_CHG:         /* Wave Change Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._PAN:          /* PAN Command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 255)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_VOLUME_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._L_PAN:            /* Left PAN Command */
                            case (int)enmMML._R_PAN:            /* Right PAN Command */
                            case (int)enmMML._C_PAN:            /* Center PAN Command */

                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 15)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_VOLUME_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            /***************************************/

                            case (int)enmMML._REVERB_SET:           /* Reverb command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._DAMP_SET:         /* Damp command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._SET_OPBASE:           /* opbase command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._LOAD_OP2:         /* Load op2 command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._SET_TVP:          /* TVP command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._DRUM_SW:          /* drum command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SET_FBS:          /* FBS command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SET_OPM:          /* opmode command */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PARAMETERS, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispWarning((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;



                            /***************************************/

                            case (int)enmMML._EP_ON:            /* ピッチエンベロープ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 127))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PITCH_ENVELOPE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._EN_ON:            /* ノートエンベロープ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 127))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_NOTE_ENVELOPE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._MH_ON:            /* ハードウェアエフェクト */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 15))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_HARD_EFFECT_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._DETUNE:           /* ディチューン */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    // ピッチ方向の修正
                                    if (cmd[cmdPtr].param[0] != 255 && pitch_correction != 0)
                                        cmd[cmdPtr].param[0] = 0 - cmd[cmdPtr].param[0];

                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < -127 || cmd[cmdPtr].param[0] > 126))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_DETUNE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._TRANSPOSE:            /* トランスポーズ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < -127 || cmd[cmdPtr].param[0] > 126))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TRANSPOSE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                    transpose = cmd[cmdPtr].param[0];
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._REPEAT_END:       /* リピート終了 */
                            case (int)enmMML._REPEAT_END2:  /* リピート終了 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 2)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_VALUE_OF_REPEAT_COUNT, lptr[line].filename, line);
                                        cmd[cmdPtr].param[0] = 2;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._VRC7_TONE:            /* VRC7ユーザー音色切り替え */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 63)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TONE_NUMBER, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SUN5B_HARD_SPEED:     /* PSGハードウェアエンベロープ速度 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 65535)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_ENVELOPE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SUN5B_HARD_ENV:       /* PSGハードウェアエンベロープ選択 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 15)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_ENVELOPE_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                    else
                                    {
                                        volume_flag = 0x8000;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SUN5B_NOISE_FREQ: /* PSGノイズ周波数 */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 31)
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_PITCH_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                    else
                                    {
                                        volume_flag = 0x8000;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._TEMPO2:           /* フレーム基準テンポ */
                                ptr = setCommandBuf(2, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] <= 0) || (cmd[cmdPtr].param[1] <= 0))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_TEMPO_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = (int)enmMML._NOP;
                                    }
                                    else
                                    {
                                        tbase = (double)cmd[cmdPtr].param[0] * (double)cmd[cmdPtr].param[1] / _BASE;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._SWEEP:            /* スウィープ */
                                ptr = setCommandBuf(2, cmd, cmdPtr, (int)enmMML._SWEEP, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 15)
                                     || (cmd[cmdPtr].param[1] < 0 || cmd[cmdPtr].param[1] > 15))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_SWEEP_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._DATA_WRITE:       /* データ(レジスタ)書き込み */
                                ptr = setCommandBuf(2, cmd, cmdPtr, (int)enmMML._DATA_WRITE, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._DATA_WRITE_OFS:       /* オフセット付きレジスタ書き込み */
                                ptr = setCommandBuf(2, cmd, cmdPtr, (int)enmMML._DATA_WRITE_OFS, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._DATA_THRUE:       /* データ直接書き込み */
                                ptr = setCommandBuf(2, cmd, cmdPtr, (int)enmMML._DATA_THRUE, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
#if false
                            case _XX_COMMAND:		/* デバッグ用 */
                            ptr = setCommandBuf( 2, cmd, _XX_COMMAND, ptr, line, mml[i].check(trk) );
                            if( (mml[i].check(trk)) == 0 ) {
                                dispError( UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line );
                            }
                            break;
#endif
                            case (int)enmMML._SELF_DELAY_ON:        /* セルフディレイ */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) != 0)
                                {
                                    if ((cmd[cmdPtr].param[0] != 255)
                                     && (cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > SELF_DELAY_MAX))
                                    {
                                        dispError((int)enmErrNum.ABNORMAL_SELFDELAY_VALUE, lptr[line].filename, line);
                                        cmd[cmdPtr].cmd = 0;
                                        cmd[cmdPtr].line = 0;
                                    }
                                }
                                else
                                {
                                    cmd[cmdPtr].cmd = (int)enmMML._NOP;
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;
                            case (int)enmMML._DATA_BREAK:       /* データ変換中止 */
                                setCommandBuf(0, cmd, cmdPtr, (int)enmMML._TRACK_END, buf, ptr, line, mml[i].check(trk));
                                if ((mml[i].check(trk)) == 0)
                                {
                                    dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                }
                                break;

                            case (int)enmMML._NEW_BANK:
                                // 無視する場合でもptrは読み進める
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if (auto_bankswitch == 0)
                                {
                                    if ((mml[i].check(trk)) != 0)
                                    {
                                        if (cmd[cmdPtr].param[0] == PARAM_OMITTED)
                                        {
                                            /* そういう場合があります */
                                        }
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                    }
                                }
                                else
                                {
                                    cmd[cmdPtr].cmd = (int)enmMML._NOP;
                                }
                                break;

                            case (int)enmMML._SHIFT_AMOUNT:         /* ピッチシフト量 (0〜8) */
                                ptr = setCommandBuf(1, cmd, cmdPtr, mml[i].num, buf, ptr, line, mml[i].check(trk));
                                if (pitch_correction != 0)
                                {
                                    if ((mml[i].check(trk)) != 0)
                                    {
                                        if ((cmd[cmdPtr].param[0] < 0 || cmd[cmdPtr].param[0] > 8))
                                        {
                                            dispError((int)enmErrNum.ABNORMAL_SHIFT_AMOUNT, lptr[line].filename, line);
                                            cmd[cmdPtr].cmd = 0;
                                            cmd[cmdPtr].line = 0;
                                        }
                                    }
                                    else
                                    {
                                        dispError((int)enmErrNum.UNUSE_COMMAND_IN_THIS_TRACK, lptr[line].filename, line);
                                    }
                                }
                                else
                                {
                                    dispError((int)enmErrNum.CANT_USE_SHIFT_AMOUNT_WITHOUT_PITCH_CORRECTION, lptr[line].filename, line);
                                }
                                break;

                            default:                /* その他(エラー) */
                                dispError((int)enmErrNum.COMMAND_NOT_DEFINED, lptr[line].filename, line);
                                ptr++;
                                break;
                        }
                        if (cmd[cmdPtr].line != 0)
                        {
                            cmdPtr++;
                        }
                    }
                }
                else if (lptr[line].status == _INCLUDE)
                {
                    cmd = analyzeData(trk, cmd, ref cmdPtr, lptr[line].inc_ptr);
                }
            }
            return cmd;
        }




#if false
        public class SHFL_Q {
            public int flag;
            public double diff;
            public double _base; //シャッフルさせるN分音符のカウント長
        }


        /*--------------------------------------------------------------
            シャッフルクオンタイズ
        --------------------------------------------------------------*/
        private void shuffleQuontizeSub(CMD[] ptr, int ptrPtr, SHFL_Q shf, double count)
        {
            if (shf.flag != 0)
            {
                double noteoff_time;
                if (double2int(count / shf._base) % 2 == 1)
                {
                    //ノートオンの時刻が裏拍
                    ptr[ptrPtr].len = ptr[ptrPtr].len - shf.diff;
                }
                noteoff_time = count + ptr[ptrPtr].len;
                if (double2int(noteoff_time / shf._base) % 2 == 1)
                {
                    //ノートオフの時刻が裏拍
                    ptr[ptrPtr].len = ptr[ptrPtr].len + shf.diff;
                }
            }
        }





        private void shuffleQuontize(CMD[] ptr, int ptrPtr)
        {
            double count = 0.0; //音長の累積。すなわちイベント発生時刻(カウント単位)
            SHFL_Q shuffle = new SHFL_Q() { flag = 0, diff = 0.0, _base = 192.0 };
            while (true)
            {
                if (ptr[ptrPtr].cmd == (int)enmMML._SHUFFLE_QUONTIZE)
                {
                    shuffle.flag = 1;
                    shuffle._base = _BASE / ptr[ptrPtr].param[0];
                    Log.WriteLine(LogLevel.INFO, string.Format("shfl {0}\n", shuffle._base));
                    shuffle.diff = shuffle._base * 2 * ptr[ptrPtr].param[1] / (ptr[ptrPtr].param[2] + ptr[ptrPtr].param[1]) - shuffle._base;
                    /*
                    たとえば16分音符を2:1にわけるなら
                    shuffle.base = 192/16 = 12; つまりl16=l%12
                    shuffle.diff = 24 * 2/3 - 12
                                             = 16 - 12 = 4
                    というわけで8分音符(%24)を%12+4と%12-4、すなわち%16と%8にわける
                    */
                    ptr[ptrPtr].cmd = (int)enmMML._NOP;
                    ptrPtr++;
                }
                else if (ptr[ptrPtr].cmd == (int)enmMML._SHUFFLE_QUONTIZE_RESET)
                {
                    count = 0.0;
                    ptr[ptrPtr].cmd = (int)enmMML._NOP;
                    ptrPtr++;
                }
                else if (ptr[ptrPtr].cmd == (int)enmMML._SHUFFLE_QUONTIZE_OFF)
                {
                    shuffle.flag = 0;
                    ptr[ptrPtr].cmd = (int)enmMML._NOP;
                    ptrPtr++;
                }
                else if (ptr[ptrPtr].cmd == (int)enmMML._CONT_NOTE)
                {
                    //連符の中身には関与しないが、連符をカタマリとして捉える
                    while (true)
                    {
                        if (ptr[ptrPtr].cmd == (int)enmMML._TRACK_END)
                        {
                            //連符途中で終了
                            //ここではエラーを出さない
                            return;
                        }
                        else if (ptr[ptrPtr].cmd == (int)enmMML._CONT_END)
                        {
                            //このコマンドが持っている音長に対してクオンタイズ処理
                            shuffleQuontizeSub(ptr, ptrPtr, shuffle, count);
                            count += ptr[ptrPtr].len;
                            ptrPtr++;
                            break;
                        }
                        else if (
                          ptr[ptrPtr].cmd <= MAX_NOTE ||
                          ptr[ptrPtr].cmd == (int)enmMML._DRUM_BIT ||
                                              ptr[ptrPtr].cmd == (int)enmMML._DRUM_NOTE ||
                          ptr[ptrPtr].cmd == (int)enmMML._REST ||
                          ptr[ptrPtr].cmd == (int)enmMML._KEY ||
                          ptr[ptrPtr].cmd == (int)enmMML._NOTE ||
                          ptr[ptrPtr].cmd == (int)enmMML._WAIT ||
                          ptr[ptrPtr].cmd == (int)enmMML._TIE ||
                          temp->cmd == (int)enmMML._KEY_OFF)
                        {
                            //中身はスルー
                            ptrPtr++;
                        }
                        else
                        {
                            ptrPtr++;
                        }
                    }
                }
                else if (
                  ptr[ptrPtr].cmd <= MAX_NOTE ||
                  ptr[ptrPtr].cmd == (int)enmMML._DRUM_BIT ||
                  ptr[ptrPtr].cmd == (int)enmMML._DRUM_NOTE ||
                  ptr[ptrPtr].cmd == (int)enmMML._REST ||
                  ptr[ptrPtr].cmd == (int)enmMML._KEY ||
                  ptr[ptrPtr].cmd == (int)enmMML._NOTE ||
                  ptr[ptrPtr].cmd == (int)enmMML._WAIT ||
                  ptr[ptrPtr].cmd == (int)enmMML._TIE ||
                  temp->cmd == (int)enmMML._KEY_OFF)
                {
                    shuffleQuontizeSub(ptr, ptrPtr, shuffle, count);
                    count += ptr[ptrPtr].len;
                    ptrPtr++;
                }
                else if (ptr[ptrPtr].cmd == (int)enmMML._TRACK_END)
                {
                    break;
                }
                else
                {
                    //他のはスルー
                    ptrPtr++;
                }
            }
        }
#endif



        /*--------------------------------------------------------------
            ループ/連符の展開
         Input:
            *ptr
         Output:
            **cmd
        --------------------------------------------------------------*/
        private int translateData(CMD[] cmd, ref int cmdPtr, CMD[] ptr, int ptrPtr)
        {
            CMD[] top, end, temp;
            int topPtr, endPtr, tempPtr;
            int cnt, i, loop;
            double len, gate;

            loop = 0;
            gate = 0;
            top = ptr;
            topPtr = ptrPtr;
            end = null;
            endPtr = -1;

            while (true)
            {
                switch (ptr[ptrPtr].cmd)
                {
                    case (int)enmMML._REPEAT_ST:
                        ptrPtr++;
                        nest++;
                        ptrPtr = translateData(cmd, ref cmdPtr, ptr, ptrPtr);
                        if (ptrPtr == -1)
                        {
                            /* [が閉じられていない */
                            return -1;
                        }
                        nest--;
                        break;
                    case (int)enmMML._REPEAT_END:
                        if (nest <= 0)
                        {
                            dispError((int)enmErrNum.DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0, ptr[ptrPtr].filename, ptr[ptrPtr].line);
                            ptr[ptrPtr].cmd = (int)enmMML._NOP;
                            ptrPtr++;
                            break;
                        }
                        if (loop == 0)
                        {
                            loop = ptr[ptrPtr].param[0];
                            end = ptr;
                            endPtr = ptrPtr + 1;
                        }
                        if (loop == 1)
                        {
                            return endPtr;
                        }
                        ptr = top;
                        ptrPtr = topPtr;
                        loop--;
                        break;
                    case (int)enmMML._REPEAT_ESC:
                        if (nest <= 0)
                        {
                            dispError((int)enmErrNum.DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0, ptr[ptrPtr].filename, ptr[ptrPtr].line);
                            ptr[ptrPtr].cmd = (int)enmMML._NOP;
                            ptrPtr++;
                            break;
                        }
                        if (loop == 1)
                        {
                            if (endPtr != -1)
                            {
                                return endPtr;
                            }
                        }
                        ptrPtr++;
                        break;
                    case (int)enmMML._CONT_NOTE:
                        ptrPtr++;
                        temp = ptr;
                        tempPtr = ptrPtr;
                        /* {} の中に[cdefgab]|n|@n|r|wが何個あるか? */
                        cnt = 0;
                        len = 0;
                        while (true)
                        {
                            if (temp[tempPtr].cmd == (int)enmMML._TRACK_END)
                            {
                                dispError((int)enmErrNum.DATA_ENDED_BY_CONTINUATION_NOTE, wk.mml_names[mml_idx], ptr[(ptrPtr - 1)].line);
                                setCommandBuf(0, cmd, cmdPtr, (int)enmMML._TRACK_END, null, 0, ptr[ptrPtr].line, 1);
                                break;
                            }
                            else if (temp[tempPtr].cmd == (int)enmMML._CONT_END)
                            {
                                if (cnt == 0)
                                {
                                    dispError((int)enmErrNum.TUPLET_BRACE_EMPTY, wk.mml_names[mml_idx], ptr[(ptrPtr - 1)].line);
                                    len = 0;
                                }
                                else
                                {
                                    /* {}の中身は全部この長さになる */
                                    len = temp[tempPtr].len / (double)cnt;
                                }
                                break;
                            }
                            else if (
                          temp[tempPtr].cmd <= MAX_NOTE ||
                          temp[tempPtr].cmd == (int)enmMML._DRUM_BIT ||
                                          temp[tempPtr].cmd == (int)enmMML._DRUM_NOTE ||
                          temp[tempPtr].cmd == (int)enmMML._REST ||
                          temp[tempPtr].cmd == (int)enmMML._KEY ||
                          temp[tempPtr].cmd == (int)enmMML._NOTE ||
                          temp[tempPtr].cmd == (int)enmMML._WAIT ||
                          temp[tempPtr].cmd == (int)enmMML._KEY_OFF)
                            {
                                cnt++;
                            }
                            tempPtr++;
                        }
                        if (temp[tempPtr].cmd != (int)enmMML._TRACK_END)
                        {
                            while (ptr[ptrPtr].cmd != (int)enmMML._TRACK_END)
                            {
                                if (ptr[ptrPtr].cmd == (int)enmMML._CONT_END)
                                {
                                    ptrPtr++;
                                    break;
                                }
                                else if (
                              ptr[ptrPtr].cmd <= MAX_NOTE ||
                              ptr[ptrPtr].cmd == (int)enmMML._DRUM_BIT ||
                                                  ptr[ptrPtr].cmd == (int)enmMML._DRUM_NOTE ||
                              ptr[ptrPtr].cmd == (int)enmMML._REST ||
                              ptr[ptrPtr].cmd == (int)enmMML._KEY ||
                              ptr[ptrPtr].cmd == (int)enmMML._NOTE ||
                              ptr[ptrPtr].cmd == (int)enmMML._WAIT ||
                              temp[tempPtr].cmd == (int)enmMML._KEY_OFF)
                                {
                                    gate += len;
                                    if (cmd[cmdPtr] == null) cmd[cmdPtr] = new CMD();
                                    cmd[cmdPtr].filename = ptr[ptrPtr].filename;
                                    cmd[cmdPtr].cnt = ptr[ptrPtr].cnt;
                                    cmd[cmdPtr].frm = ptr[ptrPtr].frm;
                                    cmd[cmdPtr].line = ptr[ptrPtr].line;
                                    cmd[cmdPtr].cmd = ptr[ptrPtr].cmd;
                                    cmd[cmdPtr].len = len;
                                    for (i = 0; i < 8; i++)
                                    {
                                        cmd[cmdPtr].param[i] = ptr[ptrPtr].param[i];
                                    }
                                    gate -= cmd[cmdPtr].len;
                                }
                                else if (ptr[ptrPtr].cmd == (int)enmMML._TIE)
                                {
                                    /* 連符中のタイは削除 */
                                    cmd[cmdPtr].filename = ptr[ptrPtr].filename;
                                    cmd[cmdPtr].cnt = 0;
                                    cmd[cmdPtr].frm = 0;
                                    cmd[cmdPtr].line = ptr[ptrPtr].line;
                                    cmd[cmdPtr].cmd = (int)enmMML._NOP;
                                    cmd[cmdPtr].len = 0;
                                }
                                else
                                {
                                    cmd[cmdPtr].filename = ptr[ptrPtr].filename;
                                    cmd[cmdPtr].cnt = ptr[ptrPtr].cnt;
                                    cmd[cmdPtr].frm = ptr[ptrPtr].frm;
                                    cmd[cmdPtr].line = ptr[ptrPtr].line;
                                    cmd[cmdPtr].cmd = ptr[ptrPtr].cmd;
                                    cmd[cmdPtr].len = ptr[ptrPtr].len;
                                    for (i = 0; i < 8; i++)
                                    {
                                        cmd[cmdPtr].param[i] = ptr[ptrPtr].param[i];
                                    }
                                }
                                cmdPtr++;
                                ptrPtr++;
                            }

                        }
                        break;
                    case (int)enmMML._TRACK_END:
                        if (cmd[cmdPtr] == null) cmd[cmdPtr] = new CMD();
                        cmd[cmdPtr].filename = ptr[ptrPtr].filename;
                        cmd[cmdPtr].cnt = ptr[ptrPtr].cnt;
                        cmd[cmdPtr].frm = ptr[ptrPtr].frm;
                        cmd[cmdPtr].line = ptr[ptrPtr].line;
                        cmd[cmdPtr].cmd = ptr[ptrPtr].cmd;
                        cmd[cmdPtr].len = ptr[ptrPtr].len;
                        for (i = 0; i < 8; i++)
                        {
                            cmd[cmdPtr].param[i] = ptr[ptrPtr].param[i];
                        }
                        cmdPtr++;
                        ptrPtr++;
                        if (nest != 0)
                        {
                            dispError((int)enmErrNum.DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0, wk.mml_names[mml_idx], ptr[(ptrPtr - 1)].line);
                        }
                        return -1;
                    default:
                        if (cmd[cmdPtr] == null) cmd[cmdPtr] = new CMD();
                        cmd[cmdPtr].filename = ptr[ptrPtr].filename;
                        cmd[cmdPtr].cnt = ptr[ptrPtr].cnt;
                        cmd[cmdPtr].frm = ptr[ptrPtr].frm;
                        cmd[cmdPtr].line = ptr[ptrPtr].line;
                        cmd[cmdPtr].cmd = ptr[ptrPtr].cmd;
                        cmd[cmdPtr].len = ptr[ptrPtr].len;
                        for (i = 0; i < 8; i++)
                        {
                            cmd[cmdPtr].param[i] = ptr[ptrPtr].param[i];
                        }
                        cmdPtr++;
                        ptrPtr++;
                        break;
                }
            }
        }


        /*--------------------------------------------------------------

         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void putAsm(List<MmlDatum2> fp, int data)
        {
            string fn = "";
            int ln = 0;
            string t;
            int b;

            if (putAsm_pos == 0)
            {
                fn = mml_file_name;
                ln = mml_line_pos;
                b = data & 0xff;
                fp.Add(new MmlDatum2(string.Format("\tdb\t${0:x02}", b), -1, b));
            }
            else
            {
                b = data & 0xff;
                fp.Add(new MmlDatum2(string.Format(",${0:x02}", b), -1, b));
            }

            if (putAsm_pos == 7)
            {
                fp.Add(new MmlDatum2(string.Format("\t;Trk {0}; {1}: {2}", str_track[mml_trk], fn, ln), 0));
                fp.Add(new MmlDatum2("\n", 0));
            }
            if (++putAsm_pos > 7)
            {
                putAsm_pos = 0;
            }
            bank_usage[curr_bank]++;
        }

        private void putAsmFlash(List<MmlDatum2> fp)
        {
            if (putAsm_pos > 0)
            {
                fp.Add(new MmlDatum2(string.Format("\t;Trk {0}; {1}: {2}", str_track[mml_trk], mml_file_name, mml_line_pos), 0));
                fp.Add(new MmlDatum2("\n", 0));
                putAsm_pos = 0;
            }
        }


        /*--------------------------------------------------------------

        --------------------------------------------------------------*/
        private int[] bank_org_written_flag = new int[128];// = { 1 };
        private void putBankOrigin(List<MmlDatum2> fp, int bank)
        {
            int org;
            if (bank > 127)
            {
                //assert(0);
                return;
            }
            if (bank_org_written_flag[bank] == 0)
            {
                switch (bank)
                {
                    case 0:
                        org = 0x8000;
                        //assert(0);
                        break;
                    case 1:
                        org = 0xa000;
                        break;
                    case 2:
                        org = 0xc000;
                        break;
                    case 3:
                        org = 0xe000;
                        break;
                    default:
                        org = 0xa000;
                        break;
                }
                string t = string.Format(".org\t${0:x04}", org);
                fp.Add(new MmlDatum2(string.Format("\t{0}\n", t), 4, t));
                bank_org_written_flag[bank] = 1;
                if (bank > bank_maximum)
                {
                    bank_maximum = bank;
                }
            }

        }

        /*--------------------------------------------------------------
            !=0: OK,	==0: out of range
        --------------------------------------------------------------*/
        private int checkBankRange(int bank)
        {
            if (allow_bankswitching != 0)
            {
                if (bank < 0 || bank > 127)
                {
                    return 0;
                }
            }
            else
            {
                if (bank < 0 || bank > 3)
                {
                    return 0;
                }
            }
            return 1;
        }



        /*--------------------------------------------------------------

         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private int double2int(double d)
        {
            return (int)(d + 0.5);
        }


        /*******************************************************
         *
         *↓発音					 ↓キーオフ
         *		_									_
         *	 | ＼							 | ＼ 次の音(とかイベント)
         *	|		＼_________	 |		＼_________
         * |								＼|								＼
         * |									|									＼
         * <------------------> delta_time 発音から次のイベントまで
         * <--------------->		gate_time	発音からキーオフまで
         *									<-> left_time	キーオフから次のイベントまでの残り時間
         *
         *******************************************************/

        /*--------------------------------------------------------------
            スラー・タイを考慮したデルタタイムを得る
         Input:
            CMD *cmd; デルタタイムを読み始めるコマンドの位置
            int allow_slur = 1; スラー許可(音符の場合)
                                         = 0; スラーなし(休符とか)
         Output:
            int *delta; デルタタイム
         Return:
            CMD *cmd; この関数内でcmdを読み進めたので、新しいcmd位置を返す
        --------------------------------------------------------------*/
        private int getDeltaTime(CMD[] cmd, int cmdPtr, ref int delta, int allow_slur)
        {
            delta = 0;
            while (true)
            {
                if (loop_flag == 0)
                {
                    delta += (cmd[(cmdPtr + 1)].frm - cmd[cmdPtr].frm);
                }
                else
                {
                    delta += (cmd[(cmdPtr + 1)].lfrm - cmd[cmdPtr].lfrm);
                }
                cmdPtr++;
                /* if( cmd->cmd == _SLAR && allow_slur) {
                    cmd++;
                } else */
                if (cmd[cmdPtr].cmd != (int)enmMML._TIE)
                {
                    break;
                }
            }
            return cmdPtr;
        }

        /*--------------------------------------------------------------
            qと音長からゲートタイム計算
         Input:

         Output:

         Return:
            int gate;
        --------------------------------------------------------------*/
        private int calcGateTime(int delta_time, ref GATE_Q gate_q)
        {
            int gate;
            gate = (delta_time * gate_q.rate) / gate_denom + gate_q.adjust;
            if (gate > delta_time)
            {
                gate = delta_time;
            }
            else if (gate < 0)
            {
                gate = 0;
            }
            if (delta_time != 0 && gate <= 0)
            {
                gate = 1;
            }
            return gate;
        }

        /*--------------------------------------------------------------
            音長のあるコマンドの、音長部分の出力(256フレーム以上のときの処理)
         Input:
            int wait_com_no; 256フレーム以上のときに繋ぐコマンド(wかr)
            int len; フレーム音長
         Output:

        --------------------------------------------------------------*/
        private void putLengthAndWait(List<MmlDatum2> fp, int wait_com_no, int len, ref CMD cmd)
        {
            int len_nokori = len; /* 出力すべき残り音長(フレーム数) */

            if (len == 0)
            {
                dispWarning((int)enmSys.FRAME_LENGTH_IS_0, cmd.filename, cmd.line);
                return;
            }
            else if (len < 0)
            {
                dispError((int)enmErrNum.FRAME_LENGTH_LESSTHAN_0, cmd.filename, cmd.line);
                return;
            }

            if (len_nokori > 0xff)
            {
                putAsm(fp, 0xff);
                len_nokori -= 0xff;
            }
            else
            {
                putAsm(fp, len_nokori);
                len_nokori = 0;
            }
            while (len_nokori != 0)
            { /* 出力すべき残りのフレーム数が0になるまでリピート */
                if (len_nokori > 0xff)
                {
                    /* 残り256フレーム以上のとき */
                    putAsm(fp, wait_com_no);
                    putAsm(fp, 0xff); /* 255フレーム出力 */
                    len_nokori -= 0xff;
                }
                else
                {
                    /* 255フレーム以下のとき */
                    putAsm(fp, wait_com_no);
                    putAsm(fp, len_nokori); /* 残り全部出力 */
                    len_nokori = 0;
                }
            }
        }


        public class PLAYSTATE
        {
            public GATE_Q gate_q;
            public int env;            // 現在の通常の(キーオンのときの)エンベロープ番号or音量
            public int rel_env;        // 現在のリリースエンベロープ番号(-1:未使用)
            public int last_written_env;   // 最後に書き込んだエンベロープ番号or音量
            public int tone;           //
            public int rel_tone;       //
            public int last_written_tone;  //
            public int key_pressed;        // キーオンオフの状態
            public int[] last_note = new int[SELF_DELAY_MAX + 1];      // 最後に書いたノート(@nは無視で)
            public int[] last_note_keep = new int[SELF_DELAY_MAX + 1]; // ¥コマンド使用時のlast_note状態
            public int self_delay;     // いくつ前のノートを使用するか？（負ならセルフディレイしない）
        }

        private void defaultPlayState(PLAYSTATE[] ps, int psPtr)
        {
            int i;
            if (ps[psPtr].gate_q == null) ps[psPtr].gate_q = new GATE_Q();
            ps[psPtr].gate_q.rate = gate_denom;
            ps[psPtr].gate_q.adjust = 0;
            ps[psPtr].env = -1;
            ps[psPtr].rel_env = -1;
            ps[psPtr].last_written_env = -1;
            ps[psPtr].tone = -1;
            ps[psPtr].rel_tone = -1;
            ps[psPtr].last_written_tone = -1;
            ps[psPtr].key_pressed = 0;
            for (i = 0; i < ps[psPtr].last_note.Length; i++)
            {
                ps[psPtr].last_note[i] = -1;
                ps[psPtr].last_note_keep[i] = -1;
            }
            ps[psPtr].self_delay = -1;
        }

        /*--------------------------------------------------------------
            リリースエンベロープ＆音色出力、残り時間をrかwで埋める
         Input:
            *cmd putLengthWaitにエラー表示させるためだけに存在する
        --------------------------------------------------------------*/
        private void putReleaseEffect(List<MmlDatum2> fp, int left_time, ref CMD cmd, ref PLAYSTATE ps)
        {
            int note = (int)enmMCK.MCK_REST;		//デフォルトは残り時間は休符でつなぐ

            //二重キーオフチェック
            if (ps.key_pressed == 0)
            {
                putAsm(fp, note);
                putLengthAndWait(fp, (int)enmMCK.MCK_WAIT, left_time, ref cmd);
                return;
            }

            if ((ps.rel_env != -1)		// リリースエンベロープ動作中
             && (ps.last_written_env != ps.rel_env))
            {	// 現在のエンべロープと変換中のエンベロープが違う
                putAsm(fp, (int)enmMCK.MCK_SET_VOL);    // リリースエンベロープ出力
                putAsm(fp, ps.rel_env);
                ps.last_written_env = ps.rel_env;
                note = (int)enmMCK.MCK_WAIT;		//残り時間はウェイト
            }
            if ((ps.rel_tone != -1)        // リリース音色動作中
             && (ps.last_written_tone != ps.rel_tone))
            {   // 現在のエンべロープと変換中の音色が違う
                putAsm(fp, (int)enmMCK.MCK_SET_TONE);   // リリース音色出力
                putAsm(fp, ps.rel_tone);
                ps.last_written_tone = ps.rel_tone;
                note = (int)enmMCK.MCK_WAIT;        //残り時間はウェイト
            }
            if (note == (int)enmMCK.MCK_WAIT && ps.self_delay >= 0 && ps.last_note[ps.self_delay] >= 0)
            {
                /* セルフディレイ */
                note = ps.last_note[ps.self_delay];
            }
            if (left_time != 0)
            {
                putAsm(fp, note);
                putLengthAndWait(fp, note, left_time, ref cmd);
            }
        }



        private void doNewBank(List<MmlDatum2> fp, int trk, ref CMD cmd)
        {
            int banktemp = curr_bank;
            if (cmd.param[0] == unchecked((int)PARAM_OMITTED))
            {
                /* デフォルト */
                banktemp++;
            }
            else
            {
                banktemp = cmd.param[0];
            }

            if (checkBankRange(banktemp) == 0)
            {
                dispError((int)enmErrNum.BANK_IDX_OUT_OF_RANGE, cmd.filename, cmd.line);
                return;
            }
            if ((banktemp == 2 || banktemp == 3) && dpcm_bankswitch != 0)
            {
                dispError((int)enmErrNum.CANT_USE_BANK_2_OR_3_WITH_DPCMBANKSWITCH, cmd.filename, cmd.line);
                return;
            }
            putAsm(fp, (int)enmMCK.MCK_GOTO);
            string t = string.Format("bank({0}_{1:d02}_bnk{2:d03})", songlabel, trk, banktemp);
            fp.Add(new MmlDatum2(string.Format("\n\tdb\t{0}\n", t), -3, t));
            bank_usage[curr_bank]++;
            t = string.Format("{0}_{1:d02}_bnk{2:d03}", songlabel, trk, banktemp);
            fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
            bank_usage[curr_bank] += 2;

            t = string.Format(".bank\tDATA_BANK+{0}", banktemp);
            fp.Add(new MmlDatum2(string.Format("\n\t{0}\n", t), -4, ".bank", "DATA_BANK", "+", banktemp));
            if ((banktemp & 1) != 0)
            {
                t = ".org\t$A000";
                fp.Add(new MmlDatum2(string.Format("\n\t{0}\n", t), -4, t));
            }
            else
            {
                t = ".org\t$8000";
                fp.Add(new MmlDatum2(string.Format("\n\t{0}\n", t), -4, t));
            }

            //	fprintf( fp,"\n\t.bank\t%d\n",banktemp);
            curr_bank = banktemp;
            //	putBankOrigin(fp, curr_bank);
            t = string.Format("{0}_{1:d02}_bnk{2:d03}:", songlabel, trk, curr_bank);
            fp.Add(new MmlDatum2(string.Format("{0}\n", t), -2, t));
            putAsm_pos = 0; // 出力位置クリア
            return;
        }


        private int isCmdNotOutput(CMD[] cmd, int cmdPtr)
        {
            switch (cmd[cmdPtr].cmd)
            {
                case (int)enmMML._NOP:
                case (int)enmMML._TEMPO:
                case (int)enmMML._TEMPO2:
                case (int)enmMML._OCTAVE:
                case (int)enmMML._OCT_UP:
                case (int)enmMML._OCT_DW:
                case (int)enmMML._LENGTH:
                case (int)enmMML._TRANSPOSE:
                    return 1;
            }
            return 0;
        }

        private int isNextSlar(CMD[] cmd, int cmdPtr)
        {
            while (cmd[cmdPtr].cmd != (int)enmMML._TRACK_END
            && isCmdNotOutput(cmd, cmdPtr) != 0) cmdPtr++;

            if (cmd[cmdPtr].cmd == (int)enmMML._SLAR)
                return 1;

            return 0;
        }


        // 音色設定時にVOPを設定する
        private void putVOPData(List<MmlDatum2> fp, int trk, int param)
        {
            int opl3_head = opl4_track_num;

            // OPL3のトラックではない
            if (trk < opl3_head && trk >= opl3_head + OPL3_MAX)
                return;

            // 3番目の数値をVOPとして利用する
            int vop = opl3op_tbl[param][3];

            // VOPが0であれば設定しない
            if (vop == 0)
                return;

            putAsm(fp, (int)enmMCK.MDR_REVERB);
            putAsm(fp, vop & 0xff);
        }


        /*--------------------------------------------------------------

         Input:

         Output:
            無し
        --------------------------------------------------------------*/
        private void developeData(List<MmlDatum2> fp, int trk, CMD[] cmdtop, LINE[] lptr)
        {
            tbase = 0.625;
            length = 48;
            volume_flag = -1;

            //初期化
            {
                /* テンポラリワークを作成 */
                CMD[] cmd = cmdtop;
                int cmdPtr = 0;
                CMD[] temp = new CMD[32 * 1024];// malloc(sizeof(CMD) * 32 * 1024);
                int tempPtr = 0;
                CMD[] tempback = temp;
                int tempbackPtr = 0;

                int i, j;
                for (i = 0; i < 32 * 1024; i++)
                {
                    temp[tempPtr] = new CMD();
                    temp[tempPtr].cmd = 0;
                    temp[tempPtr].cnt = 0;
                    temp[tempPtr].frm = 0;
                    temp[tempPtr].line = 0;
                    for (j = 0; j < 8; j++)
                    {
                        temp[tempPtr].param[0] = 0;
                    }
                    tempPtr++;
                }

                tempPtr = tempbackPtr;
                /* チャンネルデータの頭からコマンドを解析、バッファにためる */
                temp = analyzeData(trk, temp, ref tempPtr, lptr);
                setCommandBuf(0, temp, tempPtr, (int)enmMML._TRACK_END, null, 0, 0, 1);
                tempPtr = tempbackPtr;
                //shuffleQuontize(temp, tempPtr);
                nest = 0;
                translateData(cmd, ref cmdPtr, temp, tempPtr);
                cmd = cmdtop;
                tempback = null;
            }

            tbase = 0.625;

            //MML 解析
            {
                CMD[] cmd = cmdtop;
                int cmdPtr = 0;
                double count, lcount, count_t;
                int frame, lframe, frame_p, frame_d;
                double tbase_p;

                /* カウントからフレームに変換 */
                /* なるべくキリのいい時点を起点にする */
                loop_flag = 0;

                count = 0; //トラック開始時点からの経過カウント数
                frame = 0; //トラック開始時点からの経過フレーム数
                lcount = 0; //ループ開始時点からの経過カウント数
                lframe = 0; //ループ開始時点からの経過フレーム数
                /*
                    カウントはテンポ関係なく加算していく
                    フレームは
                                A t120 l4 c	d	 e	 f	t240	 g	 a	 b	 c	 !
                    count:					0 48	96 144	192	192 240 288 336 384
                    frame:					0 30	60	90	120	120 135 150 165 180
                    tbase:			0.625					 0.3125
                    count_t:				0 48	96 144	192	384 432 480 528 576
                                B t240 l4 cc dd ee ff					g	 a	 b	 c	 !
                */
                count_t = 0; //最初から今まで現在のテンポだったと仮定した時、現在の状態と同じ時間を経過させるためのカウント数
                do
                {
                    cmd[cmdPtr].cnt = count;
                    cmd[cmdPtr].frm = frame;
                    cmd[cmdPtr].lcnt = lcount;
                    cmd[cmdPtr].lfrm = lframe;

                    //		printf("%s:%d:%4x %f %d %f\n", cmd->filename, cmd->line, cmd->cmd, cmd->cnt, cmd->frm, cmd->len);

                    if (cmd[cmdPtr].cmd == (int)enmMML._REPEAT_ST2)
                    {
                        double rcount = 0;
                        double rcount_esc = 0;      // ¥の手前まで
                        double rcount_t = 0;
                        double rcount_esc_t = 0;
                        int rframe = 0;
                        int rframe_esc = 0;
                        int rframe_err;
                        int repeat_esc_flag = 0;
                        CMD[] repeat_esc2_cmd_ptr = null;
                        int repeat_esc2_cmd_ptrPtr = -1;

                        cmdPtr++;
                        while (true)
                        {
                            cmd[cmdPtr].cnt = count;
                            cmd[cmdPtr].frm = frame;
                            cmd[cmdPtr].lcnt = lcount;
                            cmd[cmdPtr].lfrm = lframe;
                            if (cmd[cmdPtr].cmd == (int)enmMML._REPEAT_END2)
                            {
                                count_t += rcount_t * (cmd[cmdPtr].param[0] - 2) + rcount_esc_t;
                                count += rcount * (cmd[cmdPtr].param[0] - 2) + rcount_esc;
                                frame += rframe * (cmd[cmdPtr].param[0] - 2) + rframe_esc;
                                if (loop_flag != 0)
                                {
                                    lcount += rcount * (cmd[cmdPtr].param[0] - 2) + rcount_esc;
                                    lframe += rframe * (cmd[cmdPtr].param[0] - 2) + rframe_esc;
                                }
                                /* フレーム補正 */
                                rframe_err = double2int(count_t * tbase) - frame;
                                //printf( "frame-error: %d frame\n", rframe_err );
                                if (rframe_err > 0)
                                {
                                    //printf( "frame-correct: %d frame\n", rframe_err );
                                    if (rframe_err >= 3)
                                    {
                                        dispWarning((int)enmSys.REPEAT2_FRAME_ERROR_OVER_3, cmd[cmdPtr].filename, cmd[cmdPtr].line);
                                    }
                                    /* 2004.09.02 やっぱりやめる
                                    cmd->param[1] = rframe_err;
                                    frame += rframe_err;
                                    if( loop_flag != 0 ) {
                                        lframe += rframe_err;
                                    }
                                    */
                                }
                                else
                                {
                                    cmd[cmdPtr].param[1] = 0;
                                }
                                if (repeat_esc_flag != 0)
                                {
                                    // 繰り返し回数を対応する¥¥コマンドにも
                                    repeat_esc2_cmd_ptr[repeat_esc2_cmd_ptrPtr].param[0] = cmd[cmdPtr].param[0];
                                }
                                break;

                            }
                            else if (cmd[cmdPtr].cmd == (int)enmMML._REPEAT_ESC2)
                            {
                                repeat_esc_flag = 1;
                                repeat_esc2_cmd_ptr = cmd;
                                repeat_esc2_cmd_ptrPtr = cmdPtr;
                            }
                            else if (
                                cmd[cmdPtr].cmd <= MAX_NOTE ||
                                cmd[cmdPtr].cmd == (int)enmMML._REST ||
                                cmd[cmdPtr].cmd == (int)enmMML._DRUM_BIT ||
                                cmd[cmdPtr].cmd == (int)enmMML._DRUM_NOTE ||
                                cmd[cmdPtr].cmd == (int)enmMML._TIE ||
                                cmd[cmdPtr].cmd == (int)enmMML._KEY ||
                                cmd[cmdPtr].cmd == (int)enmMML._NOTE ||
                                cmd[cmdPtr].cmd == (int)enmMML._WAIT ||
                                cmd[cmdPtr].cmd == (int)enmMML._KEY_OFF)
                            {
                                count_t += cmd[cmdPtr].len;
                                rcount_t += cmd[cmdPtr].len;
                                frame_p = rframe;
                                rframe = double2int(rcount_t * tbase);
                                frame_d = rframe - frame_p;
                                count += cmd[cmdPtr].len;
                                frame += frame_d;
                                /* 対ループずれ対策 */
                                if (loop_flag != 0)
                                {
                                    lcount += cmd[cmdPtr].len;
                                    lframe += frame_d;
                                }
                                rcount += cmd[cmdPtr].len;
                                if (repeat_esc_flag == 0)
                                {
                                    rcount_esc_t += cmd[cmdPtr].len;
                                    rcount_esc += cmd[cmdPtr].len;
                                    rframe_esc += frame_d;
                                }
                            }
                            else if (cmd[cmdPtr].cmd == (int)enmMML._TEMPO)
                            {
                                tbase_p = tbase;
                                tbase = (double)_BASETEMPO / (double)cmd[cmdPtr].param[0];
                                count_t = count_t * tbase / tbase_p;
                                rcount_t = rcount_t * tbase / tbase_p;
                                rcount_esc_t = rcount_esc_t * tbase / tbase_p;
                            }
                            else if (cmd[cmdPtr].cmd == (int)enmMML._TEMPO2)
                            {
                                tbase_p = tbase;
                                tbase = (double)cmd[cmdPtr].param[0] * (double)cmd[cmdPtr].param[1] / _BASE;
                                count_t = count_t * tbase / tbase_p;
                                rcount_t = rcount_t * tbase / tbase_p;
                                rcount_esc_t = rcount_esc_t * tbase / tbase_p;
                            }
                            else if (cmd[cmdPtr].cmd == (int)enmMML._SONG_LOOP)
                            {
                                loop_flag = 1;
                            }
                            cmdPtr++;
                        }
                    }
                    else if (
                        cmd[cmdPtr].cmd <= MAX_NOTE ||
                        cmd[cmdPtr].cmd == (int)enmMML._DRUM_BIT ||
                        cmd[cmdPtr].cmd == (int)enmMML._DRUM_NOTE ||
                        cmd[cmdPtr].cmd == (int)enmMML._REST ||
                        cmd[cmdPtr].cmd == (int)enmMML._TIE ||
                        cmd[cmdPtr].cmd == (int)enmMML._KEY ||
                        cmd[cmdPtr].cmd == (int)enmMML._NOTE ||
                        cmd[cmdPtr].cmd == (int)enmMML._WAIT ||
                        cmd[cmdPtr].cmd == (int)enmMML._KEY_OFF)
                    {
                        count_t += cmd[cmdPtr].len;
                        frame_p = frame;
                        frame = double2int(count_t * tbase);
                        frame_d = frame - frame_p;
                        count += cmd[cmdPtr].len;
                        /* 対ループずれ対策 */
                        if (loop_flag != 0)
                        {
                            lcount += cmd[cmdPtr].len;
                            lframe += frame_d;
                        }
                    }
                    else if (cmd[cmdPtr].cmd == (int)enmMML._TEMPO)
                    {
                        tbase_p = tbase;
                        tbase = (double)_BASETEMPO / (double)cmd[cmdPtr].param[0];
                        count_t = count_t * tbase_p / tbase;
                    }
                    else if (cmd[cmdPtr].cmd == (int)enmMML._TEMPO2)
                    {
                        tbase_p = tbase;
                        tbase = (double)cmd[cmdPtr].param[0] * (double)cmd[cmdPtr].param[1] / _BASE;
                        count_t = count_t * tbase_p / tbase;
                    }
                    else if (cmd[cmdPtr].cmd == (int)enmMML._SONG_LOOP)
                    {
                        loop_flag = 1;
                    }
                } while (cmd[cmdPtr++].cmd != (int)enmMML._TRACK_END);
            }

            //展開
            {
                CMD[] cmd = cmdtop;
                int cmdPtr = 0;
                PLAYSTATE ps = new PLAYSTATE();
                int repeat_depth = 0;
                int repeat_index = 0;
                int repeat_esc_flag = 0;
                int i;
                string loop_point_label;//=new char[256];

                int drum_note_flag = 0;
                int drum_note_count = 0;

                defaultPlayState(new PLAYSTATE[] { ps }, 0);

                cmd = cmdtop;
                putAsm_pos = 0;
                loop_flag = 0;

                loop_point_label = string.Format("{0}_{1:d02}_lp", songlabel, trk);

                mml_trk = trk;
                string t = string.Format("{0}_{1:d02}:", songlabel, trk);
                fp.Add(new MmlDatum2(string.Format("\n{0}\t;Trk {1}\n", t, str_track[trk]), -2, t));

                mml_file_name = cmd[cmdPtr].filename;
                mml_line_pos = cmd[cmdPtr].line;

                // ジャンプする
                if (use_jump != 0)
                {
                    fp.Add(new MmlDatum2("\n;jump\n", 0));

                    putAsm(fp, (int)enmMCK.MDR_JUMP);
                    putAsm(fp, 0x01);
                    use_jump = 0;
                }
                //#if !HUSIC_EXT
                //                // 三角波/ノイズトラック対策
                //                if ((trk == BTRACK(2)) || (trk == BTRACK(3)))
                //                {
                //                    putAsm(fp, (int)enmMCK.MCK_SET_TONE);
                //                    putAsm(fp, 0x8f);
                //                }
                //#endif


                do
                {
                    int cmdtempPtr = cmdPtr; //各switch内でcmdポインタが進む可能性があるので一旦保存
                    mml_file_name = cmd[cmdPtr].filename;
                    mml_line_pos = cmd[cmdPtr].line;

                    // 自動バンク切り替え
                    if (auto_bankswitch != 0)
                    {
                        int bank_limit = 8192 - 20; // 適当に余裕を持たせる
                        if (bank_usage[curr_bank] > bank_limit)
                        {
                            CMD nbcmd = new CMD();
                            nbcmd.param[0] = curr_bank;
                            while (bank_usage[nbcmd.param[0]] > bank_limit)
                            {
                                nbcmd.param[0]++;
                            }
                            nbcmd.filename = cmd[cmdPtr].filename;
                            nbcmd.line = cmd[cmdPtr].line;
                            doNewBank(fp, trk, ref nbcmd);
                        }
                    }

                    switch (cmd[cmdtempPtr].cmd)
                    {
                        case (int)enmMML._NOP:
                        case (int)enmMML._TEMPO:
                        case (int)enmMML._TEMPO2:
                        case (int)enmMML._OCTAVE:
                        case (int)enmMML._OCT_UP:
                        case (int)enmMML._OCT_DW:
                        case (int)enmMML._LENGTH:
                        case (int)enmMML._TRANSPOSE:
                            cmdPtr++;
                            break;
                        case (int)enmMML._SLAR:
                            putAsm(fp, (int)enmMCK.MCK_SLAR);
                            cmdPtr++;
                            break;
                        case (int)enmMML._ENVELOPE:
                            putAsm(fp, (int)enmMCK.MCK_SET_VOL);
                            ps.env = cmd[cmdPtr].param[0] & 0x7f;
                            ps.last_written_env = ps.env;
                            putAsm(fp, ps.env);
                            ps.last_written_env = ps.env;
                            cmdPtr++;
                            break;
                        case (int)enmMML._REL_ENV:
                            if (cmd[cmdPtr].param[0] == 255)
                            {
                                ps.rel_env = -1;
                            }
                            else
                            {
                                ps.rel_env = cmd[cmdPtr].param[0] & 0x7f;
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._VOLUME:
                            putAsm(fp, (int)enmMCK.MCK_SET_VOL);
                            ps.env = (cmd[cmdPtr].param[0] & 0x7f) | 0x80;
                            putAsm(fp, ps.env);
                            ps.last_written_env = ps.env;
                            cmdPtr++;
                            break;
                        case (int)enmMML._HARD_ENVELOPE:
                            putAsm(fp, (int)enmMCK.MCK_SET_FDS_HWENV);
                            ps.env = ((cmd[cmdPtr].param[0] & 1) << 6) | (cmd[cmdPtr].param[1] & 0x3f);
                            putAsm(fp, (ps.env & 0xff));
                            ps.last_written_env = ps.env;
                            cmdPtr++;
                            break;
                        case (int)enmMML._TONE:
                            ps.tone = cmd[cmdPtr].param[0] | 0x80;
                            putAsm(fp, (int)enmMCK.MCK_SET_TONE);
                            putAsm(fp, ps.tone);
                            ps.last_written_tone = ps.tone;
                            cmdPtr++;
                            break;
                        case (int)enmMML._ORG_TONE:
                            ps.tone = cmd[cmdPtr].param[0] & 0x7f;
                            putAsm(fp, (int)enmMCK.MCK_SET_TONE);
                            putAsm(fp, ps.tone);
                            ps.last_written_tone = ps.tone;
                            cmdPtr++;
                            break;
                        case (int)enmMML._REL_ORG_TONE:
                            if (cmd[cmdPtr].param[0] == 255)
                            {
                                ps.rel_tone = -1;
                            }
                            else
                            {
                                ps.rel_tone = cmd[cmdPtr].param[0] & 0x7f;
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._SONG_LOOP:
                            //loop_count.cnt = cmd->cnt; //LEN
                            //loop_count.frm = cmd->frm;
                            t = string.Format("{0}:", loop_point_label);
                            fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                            loop_flag = 1;
                            putAsm_pos = 0;
                            cmdPtr++;
                            break;
                        case (int)enmMML._QUONTIZE:
                            ps.gate_q.rate = cmd[cmdPtr].param[0];
                            ps.gate_q.adjust = cmd[cmdPtr].param[1];
                            cmdPtr++;
                            break;
                        case (int)enmMML._QUONTIZE2:
                            ps.gate_q.rate = gate_denom;
                            ps.gate_q.adjust = -cmd[cmdPtr].param[0];
                            cmdPtr++;
                            break;
                        case (int)enmMML._DRUM_NOTE:
                            {
                                if (drum_note_flag != 0)
                                {
                                    dispError((int)enmErrNum.COMMAND_REDUNDANT, cmd[cmdtempPtr].filename, cmd[cmdtempPtr].line);
                                    cmdPtr++;
                                    break;

                                }
                                drum_note_flag = 1;
                                drum_note_count = 0;
                                putAsm(fp, (int)enmMCK.MDR_DRUM_NOTE);
                                putAsm(fp, cmd[cmdPtr].param[0] & 0x1f);
                                cmdPtr++;
                            }
                            break;
                        case (int)enmMML._DRUM_BIT:
                            {
                                if (cmd[cmdPtr].len == 0)
                                {
                                    putAsm(fp, (int)enmMCK.MDR_DRUM_BIT);
                                    putAsm(fp, cmd[cmdPtr].param[0] & 0x1f);
                                    cmdPtr++;
                                }
                                else
                                {
                                    int param = cmd[cmdPtr].param[0];
                                    int delta_time = 0;
                                    cmdPtr = getDeltaTime(cmd, cmdPtr, ref delta_time, 0);
                                    if (delta_time == 0)
                                    {
                                        dispWarning((int)enmSys.FRAME_LENGTH_IS_0, cmd[cmdtempPtr].filename, cmd[cmdtempPtr].line);
                                        break;
                                    }
                                    putAsm(fp, (int)enmMCK.MDR_DRUM_BIT);
                                    putAsm(fp, (param & 0x1f) | 0x80);
                                    putLengthAndWait(fp, (int)enmMCK.MCK_WAIT, delta_time, ref cmd[cmdtempPtr]);
                                }
                            }
                            break;

                        case (int)enmMML._REST:
                            {
                                int delta_time = 0;
                                cmdPtr = getDeltaTime(cmd, cmdPtr, ref delta_time, 0);
                                if (delta_time == 0)
                                {
                                    dispWarning((int)enmSys.FRAME_LENGTH_IS_0, cmd[cmdtempPtr].filename, cmd[cmdtempPtr].line);
                                    break;
                                }
                                putAsm(fp, (int)enmMCK.MCK_REST);
                                putLengthAndWait(fp, (int)enmMCK.MCK_REST, delta_time, ref cmd[cmdtempPtr]);
                                ps.key_pressed = 0;
                            }
                            break;
                        case (int)enmMML._WAIT:
                            {
                                int delta_time = 0;
                                cmdPtr = getDeltaTime(cmd, cmdPtr, ref delta_time, 0);
                                if (delta_time == 0)
                                {
                                    dispWarning((int)enmSys.FRAME_LENGTH_IS_0, cmd[cmdtempPtr].filename, cmd[cmdtempPtr].line);
                                    break;
                                }
                                putAsm(fp, (int)enmMCK.MCK_WAIT);
                                putLengthAndWait(fp, (int)enmMCK.MCK_WAIT, delta_time, ref cmd[cmdtempPtr]);
                            }
                            break;
                        case (int)enmMML._KEY_OFF: /* 長さつきキーオフ */
                            {
                                int delta_time = 0;
                                cmdPtr = getDeltaTime(cmd, cmdPtr, ref delta_time, 0);
                                if (delta_time == 0)
                                {
                                    /* 音長0を許す */
                                }
                                putReleaseEffect(fp, delta_time, ref cmd[cmdtempPtr], ref ps);
                                ps.key_pressed = 0;
                            }
                            break;
                        case (int)enmMML._LFO_ON:
                            putAsm(fp, (int)enmMCK.MCK_SET_LFO);
                            if ((cmd[cmdPtr].param[0] & 0xff) == 0xff)
                            {
                                putAsm(fp, 0xff);
                            }
                            else
                            {
                                putAsm(fp, cmd[cmdPtr].param[0] & 0x7f);
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._LFO_OFF:
                            putAsm(fp, (int)enmMCK.MCK_SET_LFO);
                            putAsm(fp, 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._DETUNE:
                            putAsm(fp, (int)enmMCK.MCK_SET_DETUNE);
                            if (cmd[cmdPtr].param[0] >= 0)
                            {
                                putAsm(fp, (cmd[cmdPtr].param[0] & 0x7f) | 0x80);
                            }
                            else
                            {
                                putAsm(fp, (-cmd[cmdPtr].param[0]) & 0x7f);
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._SWEEP:
                            putAsm(fp, (int)enmMCK.MCK_SET_HWSWEEP);
                            putAsm(fp, ((cmd[cmdPtr].param[0] & 0xf) << 4) + (cmd[cmdPtr].param[1] & 0xf));
                            cmdPtr++;
                            break;
                        case (int)enmMML._EP_ON:
                            putAsm(fp, (int)enmMCK.MCK_SET_PITCHENV);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._EP_OFF:
                            putAsm(fp, (int)enmMCK.MCK_SET_PITCHENV);
                            putAsm(fp, 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._EN_ON:
                            putAsm(fp, (int)enmMCK.MCK_SET_NOTEENV);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._EN_OFF:
                            putAsm(fp, (int)enmMCK.MCK_SET_NOTEENV);
                            putAsm(fp, 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._MH_ON:
                            putAsm(fp, (int)enmMCK.MCK_SET_FDS_HWEFFECT);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._MH_OFF:
                            putAsm(fp, (int)enmMCK.MCK_SET_FDS_HWEFFECT);
                            putAsm(fp, 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._VRC7_TONE:
                            putAsm(fp, (int)enmMCK.MCK_SET_TONE);
                            putAsm(fp, cmd[cmdPtr].param[0] | 0x40);
                            cmdPtr++;
                            break;

                        // HuSIC
                        case (int)enmMML._FMLFO_FRQ:
                            putAsm(fp, 0xec);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._FMLFO_SET:
                            putAsm(fp, 0xed);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._FMLFO_OFF:
                            putAsm(fp, 0xed);
                            putAsm(fp, 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._L_PAN:
                            panvol = (panvol & 0x0f) | ((cmd[cmdPtr].param[0] & 0x0f) << 4);
                            putAsm(fp, 0xf0);
                            putAsm(fp, panvol);
                            cmdPtr++;
                            break;
                        case (int)enmMML._R_PAN:
                            panvol = (panvol & 0xf0) | (cmd[cmdPtr].param[0] & 0x0f);
                            putAsm(fp, 0xf0);
                            putAsm(fp, panvol);
                            cmdPtr++;
                            break;
                        case (int)enmMML._C_PAN:
                            panvol = (cmd[cmdPtr].param[0] & 0x0f) | ((cmd[cmdPtr].param[0] & 0x0f) << 4);
                            putAsm(fp, 0xf0);
                            putAsm(fp, panvol);
                            cmdPtr++;
                            break;
                        case (int)enmMML._PAN:
                            panvol = cmd[cmdPtr].param[0];
                            putAsm(fp, 0xf0);
                            putAsm(fp, panvol);
                            cmdPtr++;
                            break;
                        case (int)enmMML._NOISE_SW:
                            putAsm(fp, 0xf2);
                            putAsm(fp, cmd[cmdPtr].param[0]);
                            cmdPtr++;
                            break;
                        case (int)enmMML._WAVE_CHG:
                            putAsm(fp, 0xf1);
                            putAsm(fp, cmd[cmdPtr].param[0]);
                            // VOP設定の出力
                            putVOPData(fp, trk, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;

                            break;
                        case (int)enmMML._MODE_CHG:
                            putAsm(fp, 0xef);
                            putAsm(fp, cmd[cmdPtr].param[0]);
                            cmdPtr++;
                            break;

                        // MoonDriver
                        case (int)enmMML._JUMP_FLAG:
                            putAsm(fp, (int)enmMCK.MDR_JUMP);
                            putAsm(fp, 0x00);
                            cmdPtr++;
                            break;
                        case (int)enmMML._REVERB_SET:
                            putAsm(fp, (int)enmMCK.MDR_REVERB);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._DAMP_SET:
                            putAsm(fp, (int)enmMCK.MDR_DAMP);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;

                        case (int)enmMML._SET_OPBASE:
                            putAsm(fp, (int)enmMCK.MDR_OPBASE);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._LOAD_OP2:
                            putAsm(fp, (int)enmMCK.MDR_LDOP2);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            // VOP設定の出力
                            putVOPData(fp, trk, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;

                            break;
                        case (int)enmMML._SET_TVP:
                            putAsm(fp, (int)enmMCK.MDR_TVP);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._DRUM_SW:
                            putAsm(fp, (int)enmMCK.MDR_DRUM);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._SET_FBS:
                            putAsm(fp, (int)enmMCK.MDR_FBS);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._SET_OPM:
                            putAsm(fp, (int)enmMCK.MDR_OPMODE);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;


                        case (int)enmMML._SUN5B_HARD_SPEED:
                            putAsm(fp, (int)enmMCK.MCK_SET_SUN5B_HARD_SPEED);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            putAsm(fp, (cmd[cmdPtr].param[0] >> 8) & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._SUN5B_HARD_ENV:
                            putAsm(fp, (int)enmMCK.MCK_SUN5B_HARD_ENV);
                            ps.env = (cmd[cmdPtr].param[0] & 0x0f) | 0x10 | 0x80;
                            putAsm(fp, ps.env);
                            cmdPtr++;
                            break;
                        case (int)enmMML._SUN5B_NOISE_FREQ:
                            putAsm(fp, (int)enmMCK.MCK_SET_SUN5B_NOISE_FREQ);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0x1f);
                            cmdPtr++;
                            break;
                        case (int)enmMML._NEW_BANK:
                            doNewBank(fp, trk, ref cmd[cmdPtr]);
                            cmdPtr++;
                            break;
                        case (int)enmMML._DATA_WRITE:
                            putAsm(fp, (int)enmMCK.MCK_DATA_WRITE);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            putAsm(fp, (cmd[cmdPtr].param[0] >> 8) & 0xff);
                            putAsm(fp, cmd[cmdPtr].param[1] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._DATA_WRITE_OFS:
                            {
                                int sel = 0x00;
                                int addr = (cmd[cmdPtr].param[0] & 0xff);
                                int opl3_head = opl4_track_num;
                                int data = cmd[cmdPtr].param[1];

                                // OPL3のトラック範囲内
                                if (trk >= opl3_head && trk < opl3_head + OPL3_MAX)
                                {
                                    // tmptrk = 0 - 17
                                    int tmptrk = trk - opl3_head;

                                    int opl_half = (OPL3_MAX / 2);

                                    if (tmptrk < opl_half)
                                    {
                                        sel = 0x01; // first half
                                    }
                                    else
                                    {
                                        sel = 0x02; // second half
                                        tmptrk -= opl_half;
                                    }

                                    // 特定のアドレスは特殊な変換を行う
                                    if ((addr >= 0x20 && addr < 0xa0) ||
                                            (addr >= 0xe0 && addr < 0x100))
                                    {
                                        tmptrk = ((tmptrk / 3) * 8) + (tmptrk % 3);
                                    }
                                    addr += tmptrk;

                                }

                                putAsm(fp, (int)enmMCK.MCK_DATA_WRITE);
                                putAsm(fp, addr & 0xff);
                                putAsm(fp, sel & 0xff);
                                putAsm(fp, data & 0xff);
                                cmdPtr++;
                            }
                            break;

                        case (int)enmMML._DATA_THRUE:
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            putAsm(fp, cmd[cmdPtr].param[1] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._REPEAT_ST2:
                            t = string.Format("{0}_{1:d02}_lp_{2:d04}:", songlabel, trk, repeat_index);
                            fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                            repeat_depth++;
                            putAsm_pos = 0;
                            cmdPtr++;
                            break;
                        case (int)enmMML._REPEAT_END2:
                            if (--repeat_depth < 0)
                            {
                                dispError((int)enmErrNum.DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0, cmd[cmdPtr].filename, cmd[cmdPtr].line);
                            }
                            else
                            {
                                if (repeat_esc_flag != 0)
                                {
                                    // 常に戻る
                                    putAsm(fp, (int)enmMCK.MCK_GOTO);
                                }
                                else
                                {
                                    putAsm(fp, (int)enmMCK.MCK_REPEAT_END);
                                    putAsm(fp, cmd[cmdPtr].param[0] & 0x7f);
                                }
                                t = string.Format("bank({0}_{1:d02}_lp_{2:d04})", songlabel, trk, repeat_index);
                                fp.Add(new MmlDatum2(string.Format("\n\tdb\t{0}\n", t), -3, t));
                                bank_usage[curr_bank]++;
                                t = string.Format("{0}_{1:d02}_lp_{2:d04}", songlabel, trk, repeat_index);
                                fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                                bank_usage[curr_bank] += 2;

                                t = string.Format("{0}_{1:d02}_lp_exit_{2:d04}:", songlabel, trk, repeat_index);
                                fp.Add(new MmlDatum2(string.Format("{0}\n", t), -2, t));
                                repeat_index++;
                                putAsm_pos = 0;
                                /* 2004.09.02 やっぱりやめる
                                if ( cmd->param[1] > 0 ) {
                                    putAsm( fp, MCK_WAIT );
                                    putAsm( fp, cmd->param[1]&0xFF);
                                } */
                                if (repeat_esc_flag != 0)
                                {
                                    for (i = 0; i < ps.last_note.Length; i++)
                                    {
                                        ps.last_note[i] = ps.last_note_keep[i];
                                    }
                                    repeat_esc_flag = 0;
                                }
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._REPEAT_ESC2:
                            if ((repeat_depth - 1) < 0)
                            {
                                dispError((int)enmErrNum.DATA_ENDED_BY_LOOP_DEPTH_EXCEPT_0, cmd[cmdPtr].filename, cmd[cmdPtr].line);
                            }
                            else
                            {
                                putAsm(fp, (int)enmMCK.MCK_REPEAT_ESC);
                                putAsm(fp, cmd[cmdPtr].param[0] & 0x7f);
                                t = string.Format("bank({0}_{1:d02}_lp_exit_{2:d04})", songlabel, trk, repeat_index);
                                fp.Add(new MmlDatum2(string.Format("\n\tdb\t{0}\n", t), -3, t));
                                bank_usage[curr_bank]++;
                                t = string.Format("{0}_{1:d02}_lp_exit_{2:d04}", songlabel, trk, repeat_index);
                                fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                                bank_usage[curr_bank] += 2;
                                putAsm_pos = 0;
                                repeat_esc_flag = 1;
                                for (i = 0; i < ps.last_note.Length; i++)
                                {
                                    ps.last_note_keep[i] = ps.last_note[i];
                                }
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._SELF_DELAY_ON:
                            if (cmd[cmdPtr].param[0] == 255)
                            {
                                ps.self_delay = -1;
                            }
                            else
                            {
                                ps.self_delay = cmd[cmdPtr].param[0];
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._SELF_DELAY_OFF:
                            ps.self_delay = -1;
                            cmdPtr++;
                            break;
                        case (int)enmMML._SELF_DELAY_QUEUE_RESET:
                            for (i = 0; i < ps.last_note.Length; i++)
                            {
                                ps.last_note[i] = -1;
                                ps.last_note_keep[i] = -1;
                            }
                            cmdPtr++;
                            break;
                        case (int)enmMML._SHIFT_AMOUNT:
                            putAsm(fp, (int)enmMCK.MCK_SET_SHIFT_AMOUNT);
                            putAsm(fp, cmd[cmdPtr].param[0] & 0xff);
                            cmdPtr++;
                            break;
                        case (int)enmMML._TRACK_END:
                            break;
                        case (int)enmMML._KEY:
                        default:
                            {
                                int note;
                                int delta_time; /* 発音から次のイベントまでのフレーム数 */
                                int gate_time; /* 発音からキーオフまでのフレーム数 */
                                int left_time; /* キーオフから次のイベントまでの残りフレーム数 */
                                GATE_Q temp_gate = new GATE_Q();

                                if (cmd[cmdtempPtr].cmd == (int)enmMML._KEY)
                                {
                                    note = cmd[cmdPtr].param[0] & 0xffff;
                                }
                                else
                                {
                                    note = cmd[cmdtempPtr].cmd;
                                    if (note < MIN_NOTE || MAX_NOTE < note)
                                    {
                                        dispError((int)enmErrNum.COMMAND_NOT_DEFINED, cmd[cmdPtr].filename, cmd[cmdPtr].line);
                                        cmdPtr++;
                                        break;
                                    }
                                }


                                delta_time = 0;
                                cmdPtr = getDeltaTime(cmd, cmdPtr, ref delta_time, 1);

                                // スラーの場合はゲートタイムを無視する
                                if (isNextSlar(cmd, cmdPtr) != 0)
                                {
                                    temp_gate.rate = 8;
                                    temp_gate.adjust = 0;
                                    gate_time = calcGateTime(delta_time, ref temp_gate);
                                }
                                else
                                    gate_time = calcGateTime(delta_time, ref ps.gate_q);

                                left_time = delta_time - gate_time;

                                if (delta_time == 0)
                                {
                                    dispWarning((int)enmSys.FRAME_LENGTH_IS_0, cmd[cmdtempPtr].filename, cmd[cmdtempPtr].line);
                                    break;
                                }


                                if (ps.last_written_env != ps.env)
                                {       // 最後に書き込んだエンべロープor音量と、現在の通常のエンベロープor音量が違う
                                        // if ( (trk == BFMTRACK) && (ps.env > 0xFF) ) {
                                        //	putAsm( fp, MCK_SET_FDS_HWENV );	// ハードエンベ出力
                                        //	putAsm( fp, (ps.env & 0xff) );
                                        // } else
                                    {
                                        putAsm(fp, (int)enmMCK.MCK_SET_VOL);    // エンベロープ出力
                                        putAsm(fp, ps.env);
                                    }
                                    ps.last_written_env = ps.env;
                                }

                                if (ps.last_written_tone != ps.tone)
                                {   // 最後に書き込んだ音色と、現在の通常の音色が違う
                                    putAsm(fp, (int)enmMCK.MCK_SET_TONE);   // 音色出力
                                    putAsm(fp, ps.tone);
                                    ps.last_written_tone = ps.tone;
                                }

                                if ((ps.tone == -1) &&
                                        ((trk == BTRACK(0)) || (trk == BTRACK(1)) ||
                                         (trk == BMMC5TRACK()) || (trk == BMMC5TRACK() + 1)))
                                {
                                    // 内蔵矩形波＆MMC5は音色未指定時@0に
                                    putAsm(fp, (int)enmMCK.MCK_SET_TONE);
                                    ps.tone = 0x80;
                                    putAsm(fp, ps.tone);
                                    ps.last_written_tone = ps.tone;
                                }

                                if (cmd[cmdtempPtr].cmd == (int)enmMML._KEY)
                                {
                                    putAsm(fp, (int)enmMCK.MCK_DIRECT_FREQ);
                                    putAsm(fp, note & 0xff);
                                    if (((trk >= BVRC6TRACK()) && (trk <= BVRC6SAWTRACK())) ||
                                             ((trk >= BFME7TRACK()) && (trk <= BFME7TRACK() + 2)))
                                    {
                                        // VRC6＆SUN5Bは12bit
                                        putAsm(fp, (note >> 8) & 0x0f);
                                    }
                                    else
                                    {
                                        // 2A03＆MMC5は11bit
                                        putAsm(fp, (note >> 8) & 0x07);
                                    }
                                }
                                else
                                {
                                    if (note < 0)
                                    {               /* 最低音の対策 */
                                        note += 16;
                                    }
                                    putAsm(fp, note);


                                    for (i = ps.last_note.Length - 1; i > 0; i--)
                                    {
                                        ps.last_note[i] = ps.last_note[i - 1];
                                    }
                                    ps.last_note[0] = note;
                                }


                                putLengthAndWait(fp, (int)enmMCK.MCK_WAIT, gate_time, ref cmd[cmdtempPtr]);
                                ps.key_pressed = 1;

                                // クオンタイズ処理
                                if (left_time != 0)
                                {
                                    putReleaseEffect(fp, left_time, ref cmd[cmdtempPtr], ref ps);
                                    ps.key_pressed = 0;
                                }

                                drum_note_flag = 0;

                            }
                            break;
                    } // switch (cmdtemp.cmd)

                    if (drum_note_flag != 0)
                        drum_note_count++;

                    putAsmFlash(fp);

                } while (cmd[cmdPtr].cmd != (int)enmMML._TRACK_END);


                track_count[mml_idx][trk][0].cnt = cmd[cmdPtr].cnt;
                track_count[mml_idx][trk][0].frm = cmd[cmdPtr].frm;

                if (loop_flag == 0)
                {
                    track_count[mml_idx][trk][1].cnt = 0;
                    track_count[mml_idx][trk][1].frm = 0;

                    t = string.Format("{0}:", loop_point_label);
                    fp.Add(new MmlDatum2(string.Format("\n{0}\n", t), -2, t));
                    putAsm_pos = 0;
                    putAsm(fp, (int)enmMCK.MCK_REST);
                    putAsm(fp, 0xff);
                }
                else
                {
                    track_count[mml_idx][trk][1].cnt = cmd[cmdPtr].lcnt;
                    track_count[mml_idx][trk][1].frm = cmd[cmdPtr].lfrm;
                }

                // putAsm( fp, MCK_DATA_END );
                putAsm(fp, (int)enmMCK.MCK_GOTO);
                fp.Add(new MmlDatum2(string.Format("\n\tdb\tbank({0})\n", loop_point_label), -3, string.Format("bank({0})", loop_point_label)));
                bank_usage[curr_bank]++;
                fp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", loop_point_label), -3, loop_point_label));
                bank_usage[curr_bank] += 2;
                fp.Add(new MmlDatum2("\n", 0));
            }
        }


        ///*--------------------------------------------------------------

        //--------------------------------------------------------------*/
        private void setSongLabel()
        {
            songlabel = string.Format("song_{0:d03}", mml_idx);
        }


        /*--------------------------------------------------------------
            リザルト表示ルーチン
            i:trk number
            trk: track symbol
        --------------------------------------------------------------*/

        private void display_counts_sub(int i, char trk)
        {
            string msg = "";
            msg = string.Format("   {0}   |", trk);
            if (track_count[mml_idx][i][0].cnt != 0)
            {
                msg += string.Format(" {0:d6}   {1:d5}|", double2int(track_count[mml_idx][i][0].cnt), track_count[mml_idx][i][0].frm);
            }
            else
            {
                msg += "               |";
            }
            if (track_count[mml_idx][i][1].cnt != 0)
            {
                msg += string.Format(" {0:d6}   {1:d5}|", double2int(track_count[mml_idx][i][1].cnt), track_count[mml_idx][i][1].frm);
            }
            else
            {
                msg += "               |";
            }
            Log.WriteLine(LogLevel.INFO, msg);
        }



        /*--------------------------------------------------------------
            データ作成ルーチン
         Input:
            無し
         Return:
            ==0:正常 !=0:異常
        --------------------------------------------------------------*/
        public int data_make()
        {
            int i, j, track_ptr;
            int tone_max, envelope_max, pitch_env_max, pitch_mod_max;
            int arpeggio_max, fm_tone_max, dpcm_max, n106_tone_max, vrc7_tone_max;
            int hard_effect_max, effect_wave_max;

            int wtb_tone_max, xpcm_max; // HuSIC
            int tonetbl_max;
            int opl3tbl_max;

            string t;
            byte b;
            List<MmlDatum2> efFp = new List<MmlDatum2>();
            List<MmlDatum2> oufp = new List<MmlDatum2>();
            List<MmlDatum2> infp = new List<MmlDatum2>();

            LINE[][] line_ptr = new LINE[work.MML_MAX][];
            CMD[] cmd_buf;
            int[] trk_flag = new int[_TRACK_MAX];

            for (i = 0; i < _TRACK_MAX; i++)
            {
                bank_sel[i] = -1; // 初期状態は切り替え無し
            }

            for (i = 0; i < _DPCM_MAX; i++)
            {
                dpcm_tbl[i] = new DPCMTBL();
                dpcm_tbl[i].flag = 0;
                dpcm_tbl[i].index = -1;
            }


            /* 全てのMMLからエフェクトを読み込み */
            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                line_ptr[mml_idx] = readMmlFile(wk.mml_names[mml_idx], wk.mml_short_names[mml_idx]);
                if (line_ptr[mml_idx] == null) return -1;
                getLineStatus(line_ptr[mml_idx], 0);

                //for (i = 1; i < line_ptr[mml_idx].Length; i++)
                //{
                //    Log.WriteLine(LogLevel.TRACE, string.Format("{0:d4} : {1:X04}", i, line_ptr[mml_idx][i].status));
                //}

                getTone(line_ptr[mml_idx]);
                //getEnvelope(line_ptr[mml_idx]);
                getPitchEnv(line_ptr[mml_idx]);
                //getPitchMod(line_ptr[mml_idx]);
                //getArpeggio(line_ptr[mml_idx]);
                //getDPCM(line_ptr[mml_idx]);
                //getXPCM(line_ptr[mml_idx]);
                //getFMTone(line_ptr[mml_idx]);
                //getWTBTone(line_ptr[mml_idx]);
                //getToneTable(line_ptr[mml_idx]);
                getOPL3tbl(line_ptr[mml_idx]);
                //getVRC7Tone(line_ptr[mml_idx]);
                //getN106Tone(line_ptr[mml_idx]);
                //getHardEffect(line_ptr[mml_idx]);
                //getEffectWave(line_ptr[mml_idx]);
            }

            tone_max = checkLoop(tone_tbl, _TONE_MAX);
            envelope_max = checkLoop(envelope_tbl, _ENVELOPE_MAX);
            pitch_env_max = checkLoop(pitch_env_tbl, _PITCH_ENV_MAX);
            //    pitch_mod_max = getMaxLFO(pitch_mod_tbl, _PITCH_MOD_MAX);
            arpeggio_max = checkLoop(arpeggio_tbl, _ARPEGGIO_MAX);
            //    dpcm_max = getMaxDPCM(dpcm_tbl);
            //    fm_tone_max = getMaxTone(fm_tone_tbl, _FM_TONE_MAX);
            //    n106_tone_max = getMaxTone(n106_tone_tbl, _N106_TONE_MAX);
            //    vrc7_tone_max = getMaxTone(vrc7_tone_tbl, _VRC7_TONE_MAX);
            //    hard_effect_max = getMaxHardEffect(hard_effect_tbl, _HARD_EFFECT_MAX);
            //    effect_wave_max = getMaxEffectWave(effect_wave_tbl, _EFFECT_WAVE_MAX);

            //    xpcm_max = getMaxDPCM(xpcm_tbl);
            //    wtb_tone_max = getMaxTone(wtb_tone_tbl, _WTB_TONE_MAX);

            tonetbl_max = getMaxToneTable(tonetbl_tbl, _TONETBL_MAX);
            opl3tbl_max = getMaxOpl3tbl(opl3op_tbl, _OPL3TBL_MAX);

            //    xpcm_size = checkXPCMSize(xpcm_tbl);


            //    sortDPCM(dpcm_tbl);					// 音色のダブりを削除
            //    dpcm_size = checkDPCMSize(dpcm_tbl);
            //    //printf("dpcmsize $%x\n",dpcm_size);
            //    if (!allow_bankswitching && (dpcm_size > _DPCM_TOTAL_SIZE))
            //    {	// サイズをチェック
            //        dispError(DPCM_FILE_TOTAL_SIZE_OVER, NULL, 0);
            //        dpcm_size = 0;
            //    }
            //    else
            //    {
            //        dpcm_data = malloc(dpcm_size);
            //        readDPCM(dpcm_tbl);
            //    }

            /* ピッチエンベロープのパラメータ修正 */
            for (i = 0; i < pitch_env_max; i++)
            {
                if (pitch_env_tbl[i][0] != 0)
                {
                    for (j = 1; j <= pitch_env_tbl[i][0]; j++)
                    {
                        if (0 < pitch_env_tbl[i][j] && pitch_env_tbl[i][j] < 127)
                        {
                            pitch_env_tbl[i][j] = pitch_env_tbl[i][j] | 0x80;
                        }
                        else if (0 >= pitch_env_tbl[i][j] && pitch_env_tbl[i][j] >= -127)
                        {
                            pitch_env_tbl[i][j] = (0 - pitch_env_tbl[i][j]);
                        }
                    }
                }
            }





            {


                ///* 音色書き込み */
                writeTone(efFp, tone_tbl, "dutyenve", tone_max);
                ///* エンベロープ書き込み */
                writeTone(efFp, envelope_tbl, "softenve", envelope_max);
                /* ピッチエンベロープ書き込み */
                writeTone(efFp, pitch_env_tbl, "pitchenve", pitch_env_max);
                ///* ノートエンベロープ書き込み */
                writeTone(efFp, arpeggio_tbl, "arpeggio", arpeggio_max);
                ///* LFO書き込み */
                //fprintf(fp, "lfo_data:\n");
                //if (pitch_mod_max != 0)
                //{
                //    for (i = 0; i < pitch_mod_max; i++)
                //    {
                //        if (pitch_mod_tbl[i][0] != 0)
                //        {
                //            fprintf(fp, "\tdb\t$%02x,$%02x,$%02x,$%02x\n",
                //                pitch_mod_tbl[i][1], pitch_mod_tbl[i][2],
                //                pitch_mod_tbl[i][3], pitch_mod_tbl[i][4]);
                //        }
                //        else
                //        {
                //            fprintf(fp, "\tdb\t$00,$00,$00,$00\n");
                //        }
                //    }
                //    fprintf(fp, "\n");
                //}
                ///* FM音色書き込み */
                //writeToneFM(fp, fm_tone_tbl, "fds", fm_tone_max);
                //writeHardEffect(fp, hard_effect_tbl, "fds", hard_effect_max);
                //writeEffectWave(fp, effect_wave_tbl, "fds", effect_wave_max);
                ///* namco106音色書き込み */
                //writeToneN106(fp, n106_tone_tbl, "n106", n106_tone_max);
                efFp.Add(new MmlDatum2("db 0;dummy N106_channel", -1, 0));
                ///* VRC7音色書き込み */
                //writeToneVRC7(fp, vrc7_tone_tbl, "vrc7", vrc7_tone_max);
                ///* DPCM書き込み */
                //writeDPCM(fp, dpcm_tbl, "dpcm_data", dpcm_max);
                //writeDPCMSample(fp);

                //// HuSIC
                ///* WTB音色書き込み */
                //writeToneWTB(fp, wtb_tone_tbl, "pce", wtb_tone_max);

                //// ToneTable
                writeToneTable(efFp, tonetbl_tbl, "ttbl", tonetbl_max);

                // OPL3 FM音色
                writeOPL3tbl(efFp, "opl3tbl", opl3tbl_max);

                ///* XPCM書き込み */
                //writeXPCM(fp, xpcm_tbl, "xpcm_data", xpcm_max);


                // MMLファイル書き込み
                if (wk.include_flag != 0)
                {
                    t = string.Format("\t.include\t\"{0}\"", wk.out_name);
                    efFp.Add(new MmlDatum2(t, -4, t));
                }

            }




            /* MML->ASMデータ変換 */

            /* 出力ファイルにタイトル/作曲者/打ち込み者の情報をコメントとして書き込み */
            writeSongInfo(oufp);

            Log.WriteLine(LogLevel.DEBUG, string.Format(" test info:vrc7:{0} vrc6:{0} n106:{0}", vrc7_track_num, vrc6_track_num, n106_track_num));

            track_ptr = 0;

            for (i = 0; i < _TRACK_MAX; i++)
                trk_flag[i] = 0;

            if (opl4_track_num != 0)
            {
                for (i = track_ptr; i < track_ptr + opl4_track_num; i++)
                    trk_flag[i] = 1;

                track_ptr += opl4_track_num;
            }

            if (opl3_track_num != 0)
            {
                for (i = track_ptr; i < track_ptr + opl3_track_num; i++)
                    trk_flag[i] = 1;

                track_ptr += opl3_track_num;
            }

            oufp.Add(new MmlDatum2(string.Format("\t.if TOTAL_SONGS > 1\n"), -4, ".if TOTAL_SONGS > 1"));
            oufp.Add(new MmlDatum2(string.Format("song_addr_table:\n"), -2, "song_addr_table:"));
            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                setSongLabel();
                t = string.Format("{0}_track_table", songlabel);
                oufp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
            }

            oufp.Add(new MmlDatum2("\t.if (ALLOW_BANK_SWITCH)\n", -4, ".if (ALLOW_BANK_SWITCH)"));
            oufp.Add(new MmlDatum2("song_bank_table:\n", -2, "song_bank_table:"));
            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                setSongLabel();
                t = string.Format("{0}_bank_table", songlabel);
                oufp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
            }
            oufp.Add(new MmlDatum2("\t.endif ; ALLOW_BANK_SWITCH\n", -4, ".endif"));
            oufp.Add(new MmlDatum2("\t.endif ; TOTAL_SONGS > 1\n", -4, ".endif"));

            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                setSongLabel();
                oufp.Add(new MmlDatum2("sound_data_table:\n", -2, "sound_data_table:"));


                for (i = 0; i < _TRACK_MAX; i++)
                {
                    t = string.Format("{0}_{1:d02}", songlabel, i);
                    if (trk_flag[i] != 0) oufp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                }

                oufp.Add(new MmlDatum2("\t.if (ALLOW_BANK_SWITCH)\n", -4, ".if (ALLOW_BANK_SWITCH)"));
                oufp.Add(new MmlDatum2("sound_data_bank:\n", -2, "sound_data_bank:"));
                //fp.Add(string.Format("{0}_bank_table:\n", songlabel));
                for (i = 0; i < _TRACK_MAX; i++)
                {
                    t = string.Format("bank({0}_{1:d02})", songlabel, i);
                    if (trk_flag[i] != 0) oufp.Add(new MmlDatum2(string.Format("\tdb\t{0}\n", t), -3, t));
                }

                oufp.Add(new MmlDatum2("loop_point_table:\n", -2, "loop_point_table:"));
                for (i = 0; i < _TRACK_MAX; i++)
                {
                    t = string.Format("{0}_{1:d02}_lp", songlabel, i);
                    if (trk_flag[i] != 0) oufp.Add(new MmlDatum2(string.Format("\tdw\t{0}\n", t), -3, t));
                }

                oufp.Add(new MmlDatum2("loop_point_bank:\n", -2, "loop_point_bank:"));
                for (i = 0; i < _TRACK_MAX; i++)
                {
                    t = string.Format("bank({0}_{1:d02}_lp)", songlabel, i);
                    if (trk_flag[i] != 0) oufp.Add(new MmlDatum2(string.Format("\tdb\t{0}\n", t), -3, t));
                }

                oufp.Add(new MmlDatum2("\n", 0));
                oufp.Add(new MmlDatum2("\t.endif\n", -4, ".endif"));
            }

            curr_bank = 0x00;

            /* 全てのMMLについて */
            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                setSongLabel();
                /* トラック単位でデータ変換 */
                for (i = 0; i < _TRACK_MAX; i++)
                {
                    if (bank_sel[i] != -1 && auto_bankswitch == 0)
                    {
                        if (trk_flag[i] == 0)
                        {
                            if (wk.message_flag == 0)
                            {
                                Log.WriteLine(LogLevel.WARNING, string.Format("Warning: 未使用トラック({0})に対しての#SETBANKを無視します\n", str_track[i]));
                            }
                            else
                            {
                                Log.WriteLine(LogLevel.WARNING, string.Format("Warning: Ignored #SETBANK on unused track({0})\n", str_track[i]));
                            }
                        }
                        else if ((bank_sel[i] == 2 || bank_sel[i] == 3) && dpcm_bankswitch != 0)
                        {
                            dispError((int)enmErrNum.CANT_USE_BANK_2_OR_3_WITH_DPCMBANKSWITCH, null, 0);
                        }
                        else
                        {
                            curr_bank = bank_sel[i];
                            oufp.Add(new MmlDatum2("\n\n", 0));
                            t = string.Format(".bank\t{0}", bank_sel[i]);
                            oufp.Add(new MmlDatum2(string.Format("\t{0}\n", t), -4, t));
                            putBankOrigin(oufp, bank_sel[i]);
                        }
                    }

                    if (trk_flag[i] != 0)
                    {
                        cmd_buf = new CMD[32 * 1024];// malloc(sizeof(CMD) * 32 * 1024);
                        developeData(oufp, i, cmd_buf, line_ptr[mml_idx]);
                        cmd_buf = null;
                    }
                }
            }




            {
                t = string.Format("TOTAL_SONGS\tequ\t${0:x02}", wk.mml_num);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("SOUND_GENERATOR\tequ\t${0:x02}", sndgen_flag);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("SOUND_USERPCM\tequ\t${0:x02}", use_pcm);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));

                t = string.Format("USE_OPL3_TRACK\t\tequ\t{0:d2}", opl3_track_num);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("OPL3_BASETRACK\t\tequ\t{0:d2}", BOPL3TRACK());
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));

                //fprintf( fp, "INITIAL_WAIT_FRM\t\tequ\t%2d\n", 0x26);
                t = string.Format("PITCH_CORRECTION\t\tequ\t{0:d2}", pitch_correction);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("DPCM_RESTSTOP\t\tequ\t{0:d2}", dpcm_reststop);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("DPCM_BANKSWITCH\t\tequ\t{0:d2}", dpcm_bankswitch);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("DPCM_EXTRA_BANK_START\t\tequ\t{0:d2}", bank_maximum + 1);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                t = string.Format("BANK_MAX_IN_4KB\t\tequ\t({0:d} + {1:d})*2+1", bank_maximum, dpcm_extra_bank_num);
                infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));

                // (!dpcm_bankswitch && (bank_maximum + dpcm_extra_bank_num <= 3))
                if (allow_bankswitching == 0)
                {
                    t = "ALLOW_BANK_SWITCH\t\tequ\t0";
                    infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                }
                else
                {
                    t = "ALLOW_BANK_SWITCH\t\tequ\t1";
                    infp.Add(new MmlDatum2(string.Format("{0}\n", t), -5, t));
                    t = "BANKSWITCH_INIT_MACRO\t.macro";
                    infp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
                    switch (bank_maximum)
                    {
                        case 0:
                            infp.Add(new MmlDatum2("\tdb\t0,1,0,0,0,0,0,0\n", -1, 0, -1, 1, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0));
                            break;
                        case 1:
                            infp.Add(new MmlDatum2("\tdb\t0,1,2,3,0,0,0,0\n", -1, 0, -1, 1, -1, 2, -1, 3, -1, 0, -1, 0, -1, 0, -1, 0));
                            break;
                        case 2:
                            infp.Add(new MmlDatum2("\tdb\t0,1,2,3,4,5,0,0\n", -1, 0, -1, 1, -1, 2, -1, 3, -1, 4, -1, 5, -1, 0, -1, 0));
                            break;
                        case 3:
                        default:
                            infp.Add(new MmlDatum2("\tdb\t0,1,2,3,4,5,6,7\n", -1, 0, -1, 1, -1, 2, -1, 3, -1, 4, -1, 5, -1, 6, -1, 7));
                            break;
                    }
                    t = "\t.endm";
                    infp.Add(new MmlDatum2(string.Format("{0}\n", t), -4, t));
                }

                /* 出力ファイルにタイトル/作曲者/打ち込み者の情報をマクロとして書き込み */
                writeSongInfoMacro(infp);

                infp.Add(new MmlDatum2("\n\n", -1));
                //fclose(fp);

            }


            if (error_flag != 0)
            {
                wk.out_name = "";               /* エラーがあったときは出力ファイルを削除 */
                wk.ef_name = "";
                return -1;
            }

            //ファイル出力 / バッファ出力
            if (compiler.isSrc)
            {
                StringBuilder sb = new StringBuilder();
                foreach (MmlDatum2 s in efFp) sb.Append(s.code);
                System.IO.File.WriteAllText(wk.ef_name, sb.ToString());

                sb = new StringBuilder();
                foreach (MmlDatum2 s in oufp) sb.Append(s.code);
                System.IO.File.WriteAllText(wk.out_name, sb.ToString());

                sb = new StringBuilder();
                foreach (MmlDatum2 s in infp) sb.Append(s.code);
                System.IO.File.WriteAllText(wk.inc_name, sb.ToString());
            }
            //else
            {
                Assemble asm = new Assemble();
                List<List<MmlDatum2>> dest = asm.build(wk, efFp, oufp, infp);
                List<MmlDatum2> des = new List<MmlDatum2>();
                for (int m = 0; m < dest.Count; m++)
                {
                    int org = (m & 1) == 0 ? 0x8000 : 0xa000;
                    for (int k = org; k < dest[m].Count; k++)
                    {
                        MmlDatum2 md = dest[m][k];
                        des.Add(md);
                    }
                    for(int k = dest[m].Count; k < org + 0x2000; k++)
                    {
                        MmlDatum2 md = new MmlDatum2();
                        md.dat = 0xff;
                        des.Add(md);
                    }
                }

                wk.destBuf = des.ToArray();
            }


            /* 全てのMMLについて */
            for (mml_idx = 0; mml_idx < wk.mml_num; mml_idx++)
            {
                Log.WriteLine(LogLevel.INFO, "");
                if (wk.mml_num > 1)
                {
                    Log.WriteLine(LogLevel.INFO, string.Format("Song {0}: {1}", mml_idx + 1, wk.mml_names[mml_idx]));
                }
                Log.WriteLine(LogLevel.INFO, "-------+---------------+---------------+");
                Log.WriteLine(LogLevel.INFO, "Track  |    Total      |    Loop       |");
                Log.WriteLine(LogLevel.INFO, " Symbol|(count)|(frame)|(count)|(frame)|");
                Log.WriteLine(LogLevel.INFO, "-------+-------+-------+-------+-------+");
                for (i = 0; i < _TRACK_MAX; i++)
                {
                    if (trk_flag[i] != 0)
                        display_counts_sub(i, str_track[i]);
                }
                Log.WriteLine(LogLevel.INFO, "-------+-------+-------+-------+-------+");
            }

            return 0;

        }

    }
}
