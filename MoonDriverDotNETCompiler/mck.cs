using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class mck
    {
        private Compiler compiler = null;
        private work wk = null;
        private datamake datamake = null;

        //extern void splitPath( const char* ptr, char* path, char* name, char* ext );
        //extern void makePath(char* ptr, const char* path, const char* name, const char* ext );
        //extern char* skipSpace(char* ptr);
        //extern char* patchstr;
        //extern char* hogereleasestr;

        //extern char* moon_verstr;

        //char* mml_names[MML_MAX];
        //char* mml_short_names[MML_MAX];
        //extern int data_make(void );
        //extern int message_flag;            // 表示メッセージの出力設定( 0:Jp 1:En )

        ///*--------------------------------------------------------------
        //    ヘルプ表示
        // Input:

        // Output:

        //--------------------------------------------------------------*/
        private void dispHelpMessage()
        {
            if (wk.message_flag == 0)
            {

                Log.WriteLine(LogLevel.INFO,
                    @"使用方法:mmckc [switch] InputFile.mml [OutputFile.h]
もしくは:mmckc [switch] -u InputFile1.mml InputFile2.mml ... 
    [switch]
    -h -?   : ヘルプを表示
    -i      : 音色/エンベロープファイルに曲データを追加する
    -m<num> : エラー/ワーニング表示の選択(0:Jpn 1:Eng)
    -o<str> : 音色/エンベロープファイルのファイル名を<str>にする
    -w      : Warningメッセージを表示しません
    -u      : 複数曲登録NSF作成"
                    );

            }
            else
            {
                Log.WriteLine(LogLevel.INFO,
                    @"Usage:mmckc [switch] InputFile.mml [OutputFile.h]
  or :mmckc [switch] -u InputFile1.mml InputFile2.mml ... 
    [switch]
    -h -?    : Display this help message
    -i       : Including song data in tone/envelope file
    -m<num>  : Select message language(0:Jpn 1:Eng)
    -o<str>  : Output tone/envelope file name is <str>
    -w       : Don't display warning message
    -u       : Multiple song NSF creation"
                    );
            }
            //exit(1);
        }




        /*--------------------------------------------------------------
            メインルーチン
         Input:
            int	argc		: コマンドライン引数の個数
            char *argv[]	: コマンドライン引数のポインタ
         Output:
            0:正常終了 0:以外以上終了
        --------------------------------------------------------------*/
        public MmlDatum2[] main(Compiler compiler, string[] mckArgs, work work, string[] env)
        {
            this.compiler = compiler;
            wk = work;

            int i, _in, _out;
            string path, name, ext;// 256size
            int multiple_song_nsf = 0;

            _in = _out = 0;

            // タイトル表示
            Log.WriteLine(LogLevel.INFO, string.Format("MML to MCK Data Converter Ver {0}.{1:d02} by Manbow-J",
                         (work.VersionNo / 100), (work.VersionNo % 100)));

            // サブタイトル表示
            Log.WriteLine(LogLevel.INFO, string.Format("{0}", version.moon_verstr));
            //printf("patches by [OK] and 2ch mck thread people\n");
            Log.WriteLine(LogLevel.INFO, string.Format("DATE: {0}", "2020/11/27"));// __DATE__);
            Log.WriteLine(LogLevel.INFO, string.Format("{0}", version.patchstr));
            Log.WriteLine(LogLevel.INFO, string.Format("{0}", version.hogereleasestr));

            // コマンドライン解析
            if (mckArgs == null || mckArgs.Length < 1)
            {
                dispHelpMessage();
                return null;
            }

            for (i = 0; i < mckArgs.Length; i++)
            {
                // スイッチ？
                if (mckArgs[i][0] == '-')
                {
                    switch (mckArgs[i].ToUpper()[1])
                    {
                        case 'H':
                        case '?':
                            dispHelpMessage();
                            return null;
                        case 'X':
                            wk.debug_flag = 1;
                            break;
                        case 'I':
                            wk.include_flag = 1;
                            break;
                        case 'M':
                            if (int.TryParse(mckArgs[i].Substring(2), out int res)) wk.message_flag = res;
                            if (wk.message_flag > 1)
                            {
                                dispHelpMessage();
                                return null;
                            }
                            break;
                        case 'N':
                            //obsolete
                            break;
                        case 'O':
                            wk.ef_name = mckArgs[i].Substring(2).Trim();
                            break;
                        case 'W':
                            wk.warning_flag = 0;
                            break;
                        case 'U':
                            multiple_song_nsf = 1;
                            break;
                        default:
                            if (wk.message_flag == 0)
                            {
                                Log.WriteLine(LogLevel.ERROR, "スイッチの指定が違います");
                            }
                            else
                            {
                                Log.WriteLine(LogLevel.ERROR, "Invalid switch!");
                            }
                            dispHelpMessage();
                            return null;
                    }
                    // 入力/出力ファイルの格納
                }
                else
                {
                    if (_in < work.MML_MAX)
                    {
                        wk.mml_names[_in] = mckArgs[i];
                        wk.mml_short_names[_in] = System.IO.Path.GetFileName(mckArgs[i]);
                        _in++;
                    }
                    else
                    {
                        if (wk.message_flag == 0)
                        {
                            Log.WriteLine(LogLevel.ERROR, "パラメータが多すぎます");
                        }
                        else
                        {
                            Log.WriteLine(LogLevel.ERROR, "Too many parameters!");
                        }
                        dispHelpMessage();
                        return null;
                    }
                }
            }

            if (_in == 0)
            {
                dispHelpMessage();
                return null;
            }

            if (multiple_song_nsf != 0)
            {
                wk.out_name = System.IO.Path.ChangeExtension(wk.mml_names[0], ".h");
                wk.mdr_name = System.IO.Path.ChangeExtension(wk.mml_names[0], ".mdr");
            }
            else
            {
                if (_in == 1)
                {
                    wk.out_name = System.IO.Path.ChangeExtension(wk.mml_names[0], ".h");
                    wk.mdr_name = System.IO.Path.ChangeExtension(wk.mml_names[0], ".mdr");
                }
                else if (_in == 2)
                {
                    wk.out_name = wk.mml_names[1];
                    _in--;
                }
                else
                {
                    if (wk.message_flag == 0)
                    {
                        Log.WriteLine(LogLevel.ERROR, "パラメータが多すぎます");
                    }
                    else
                    {
                        Log.WriteLine(LogLevel.ERROR, "Too many parameters!");
                    }
                    dispHelpMessage();
                    return null;
                }
            }

            wk.mml_num = _in;
            for (i = 0; i < _in - 1; i++)
            {
                Log.WriteLine(LogLevel.INFO, string.Format("{0} + ", wk.mml_names[i]));
            }
            Log.WriteLine(LogLevel.INFO, string.Format("{0} -> {1}", wk.mml_names[i], wk.out_name));

            // コンバート
            datamake = new datamake(compiler, wk);
            int ret = datamake.data_make();
            // 終了

            for (i = 0; i < _in; i++)
                wk.mml_short_names[i] = "";

            if (ret == 0)
            {
                if (wk.message_flag == 0)
                {
                    Log.WriteLine(LogLevel.INFO, "");
                    Log.WriteLine(LogLevel.INFO, "終了しました");
                }
                else
                {
                    Log.WriteLine(LogLevel.INFO, "");
                    Log.WriteLine(LogLevel.INFO, "Compleated!");
                }
                return wk.destBuf;
            }

            if (wk.message_flag == 0)
            {
                Log.WriteLine(LogLevel.INFO, "");
                Log.WriteLine(LogLevel.ERROR, "コンパイルに失敗しました");
            }
            else
            {
                Log.WriteLine(LogLevel.INFO, "");
                Log.WriteLine(LogLevel.ERROR, "Compilation failed!");
            }
            //return work.EXIT_FAILURE;
            return null;
        }

        public CompilerInfo GetCompilerInfo()
        {
            if (datamake == null) return null;
            CompilerInfo ci = datamake.GetCompilerInfo();
            return ci;
        }
    }
}