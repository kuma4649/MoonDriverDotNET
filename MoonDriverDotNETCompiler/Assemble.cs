using musicDriverInterface;
using System;
using System.Collections.Generic;

namespace MoonDriverDotNET.Compiler
{
    public class Assemble
    {

        private List<MmlDatum2> asFp;
        private List<MmlDatum2> efFp;
        private List<MmlDatum2> ouFp;
        private List<MmlDatum2> inFp;

        private List<MmlDatum2> asm;
        private Dictionary<string, List<MmlDatum2>> dicMacroBlock;
        private Dictionary<string, MmlDatum2> dicDefine;
        private Dictionary<string, List<Tuple<int, int, object>>> dicRefLabel=new Dictionary<string, List<Tuple<int, int, object>>>();
        private Dictionary<string, MmlDatum2> dicLabel;

        private List<List<MmlDatum2>> dest = new List<List<MmlDatum2>>();
        private int currentBank = 0;
        private int currentAddress = 0;
        private Stack<bool> assembleBlockStack = new Stack<bool>();
        private bool assembleBlockLatest = false;

        public List<List<MmlDatum2>> build(work wk, List<MmlDatum2> efFp, List<MmlDatum2> ouFp, List<MmlDatum2> inFp)
        {
            //とりあえず積んでおく
            assembleBlockStack.Push(false);
            UpdateAssembleBlockLatest();

            //
            GetAsmList();
            this.efFp = efFp;
            this.ouFp = ouFp;
            this.inFp = inFp;

            //インクルードを参照し、各リストを一つにまとめる
            Step1_Append();
            //マクロのブロックを収集する
            Step2_GetMacro();
            //マクロのブロックを置換する
            Step3_ReplaceMacro();
            //定数を収集する
            Step4_GetDefine();
            //ラベルを収集する
            Step5_GetLabel();
            //アセンブル
            Assemblling();
            //ラベル参照展開
            SetLabel();

            return dest;
        }

        private void UpdateAssembleBlockLatest()
        {
            assembleBlockLatest = assembleBlockStack.Contains(true);
        }

        private void GetAsmList()
        {
            string t;

            asFp = new List<MmlDatum2>();

            t = ".include \"define.inc\""; asFp.Add(new MmlDatum2(t, -4, t));

            t = "DATA_BANK equ 0";        asFp.Add(new MmlDatum2(t, -5, t));

            t = ".bank 0";                 asFp.Add(new MmlDatum2(t, -4, ".bank", 0));
            t = ".org  $8000";             asFp.Add(new MmlDatum2(t, -4, t));
            t = ".code";                   asFp.Add(new MmlDatum2(t, -4, t));

            asFp.Add(new MmlDatum2("ds $80"
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                , -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0, -1, 0
                ));

            t = ".org  $8000"; asFp.Add(new MmlDatum2(t, -4, t));

            t = "db \"MDRV\""; asFp.Add(new MmlDatum2(t, -1, 'M', -1, 'D', -1, 'R', -1, 'V'));
            t = "dw $0004 ; version"; asFp.Add(new MmlDatum2(t, -1, 4, -1, 0));
            t = "db $00   ; num of used channels ( 0 = auto )"; asFp.Add(new MmlDatum2(t, -1, 0));

            t = "db SOUND_GENERATOR  ; device flags"; asFp.Add(new MmlDatum2(t, -6, "b:SOUND_GENERATOR"));

            t = "dw $0000 ; adr title string ( terminated with zero )"; asFp.Add(new MmlDatum2(t, -1, 0, -1, 0));
            t = "dw $0000 ; adr artist string(terminated with zero)"; asFp.Add(new MmlDatum2(t, -1, 0, -1, 0));
            t = "dw $0000 ; adr comment string(terminated with zero)"; asFp.Add(new MmlDatum2(t, -1, 0, -1, 0));
            t = "db SOUND_USERPCM ; User PCM flag"; asFp.Add(new MmlDatum2(t, -6, "b:SOUND_USERPCM"));
            t = "db $00 ; reserved"; asFp.Add(new MmlDatum2(t, -1, 0));

            t = "dw sound_data_table; adr track table"; asFp.Add(new MmlDatum2(t, -3, "sound_data_table"));
            t = "dw sound_data_bank; adr track bank table"; asFp.Add(new MmlDatum2(t, -3, "sound_data_bank"));


            t = "dw loop_point_table; adr loop table"; asFp.Add(new MmlDatum2(t, -3, "loop_point_table"));
            t = "dw loop_point_bank; adr loop bank table"; asFp.Add(new MmlDatum2(t, -3, "loop_point_bank"));


            t = "dw softenve_table; adr venv table"; asFp.Add(new MmlDatum2(t, -3, "softenve_table"));
            t = "dw softenve_lp_table; adr venv lp table"; asFp.Add(new MmlDatum2(t, -3, "softenve_lp_table"));


            t = "dw pitchenve_table; adr penv table"; asFp.Add(new MmlDatum2(t, -3, "pitchenve_table"));
            t = "dw pitchenve_lp_table; adr penv lp table"; asFp.Add(new MmlDatum2(t, -3, "pitchenve_lp_table"));


            t = "dw arpeggio_table; adr nenv table"; asFp.Add(new MmlDatum2(t, -3, "arpeggio_table"));
            t = "dw arpeggio_lp_table; adr nenv lp table"; asFp.Add(new MmlDatum2(t, -3, "arpeggio_lp_table"));


            t = "dw $0000; adr lfo  table"; asFp.Add(new MmlDatum2(t, -1, 0, -1, 0));
            t = "dw ttbl_data_table; adr inst table"; asFp.Add(new MmlDatum2(t, -3, "ttbl_data_table"));

            t = "dw opl3tbl_data_table; adr opl3 table"; asFp.Add(new MmlDatum2(t, -3, "opl3tbl_data_table"));

            t = "pcm_flags:"; asFp.Add(new MmlDatum2(t,-2,t));
            t = "db    $00"; asFp.Add(new MmlDatum2(t,-1,0));
            t = "db    $00"; asFp.Add(new MmlDatum2(t,-1,0));

            t = ".if (SOUND_USERPCM = 1)"; asFp.Add(new MmlDatum2(t, -4, t));
            t = "dw userpcm_string"; asFp.Add(new MmlDatum2(t,-3, "userpcm_string"));
            t = ".else"; asFp.Add(new MmlDatum2(t, -4, t));
            t = "dw  $0000"; asFp.Add(new MmlDatum2(t, -1, 0, -1, 0));
            t = ".endif"; asFp.Add(new MmlDatum2(t, -4, t));


            t = "dw tag_string"; asFp.Add(new MmlDatum2(t, -3, "tag_string"));

            t = "db $00; start address of OPL4 SRAM(x * 0x10000)"; asFp.Add(new MmlDatum2(t,-1,0));
            t = "db $00; start bank of PCM"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00; size of PCM banks"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00; size of last bank(x* 0x100)"; asFp.Add(new MmlDatum2(t, -1, 0));

            t = "db $00; large count of PCM banks"; asFp.Add(new MmlDatum2(t, -1, 0));

            t = ".org $8040"; asFp.Add(new MmlDatum2(t, -4, t));

            t = ".if (SOUND_USERPCM = 1)"; asFp.Add(new MmlDatum2(t, -4, t));
            t = "userpcm_string:"; asFp.Add(new MmlDatum2(t, -2, t));
            t = "PCMFILE"; asFp.Add(new MmlDatum2(t, -6, t));
            t = ".endif"; asFp.Add(new MmlDatum2(t, -4, t));

            t = ".org $8080"; asFp.Add(new MmlDatum2(t, -4, t));

            t = "tag_string:"; asFp.Add(new MmlDatum2(t, -2, t));
            t = "TITLE_TEXT ; Track name(en)"; asFp.Add(new MmlDatum2(t, -6, "TITLE_TEXT"));
            t = "db $00 ; Track name(jp)"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "MAKER_TEXT ; Game name(en)"; asFp.Add(new MmlDatum2(t,-6, "MAKER_TEXT"));
            t = "db $00 ; Game name(jp)"; asFp.Add(new MmlDatum2(t,-1,0));
            t = "db $00 ; System name(en)"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00 ; System name(jp)"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "COMPOSER_TEXT; Track author(en)"; asFp.Add(new MmlDatum2(t, -6, "COMPOSER_TEXT"));
            t = "db $00; Track author(jp)"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00; Release date"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00; Programmer"; asFp.Add(new MmlDatum2(t, -1, 0));
            t = "db $00; Notes"; asFp.Add(new MmlDatum2(t, -1, 0));

            t = ".include \"effect.h\""; asFp.Add(new MmlDatum2(t, -4, t));
        }

        private void Step1_Append()
        {
            asm = new List<MmlDatum2>();
            Step1_start(asFp);
        }

        private void Step1_start(List<MmlDatum2> crnt)
        {
            foreach(MmlDatum2 md in crnt)
            {
                if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -4)
                {
                    asm.Add(md);
                    continue;
                }

                if(!(md.args[1] is string))
                {
                    asm.Add(md);
                    continue;
                }

                string wd = ((string)md.args[1]).Trim().ToLower();
                if (wd.IndexOf(".include") != 0)
                {
                    asm.Add(md);
                    continue;
                }

                wd = wd.Substring(".include".Length).ToLower().Trim();

                if (wd == "\"define.inc\"")
                {
                    Step1_start(inFp);
                }
                else if (wd == "\"effect.h\"")
                {
                    Step1_start(efFp);
                }
                else
                {
                    Step1_start(ouFp);
                }
            }

        }


        private void Step2_GetMacro()
        {
            dicMacroBlock = new Dictionary<string, List<MmlDatum2>>();

            for(int i = 0; i < asm.Count; i++)
            {
                MmlDatum2 md = asm[i];

                if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -4)
                {
                    continue;
                }

                if (!(md.args[1] is string))
                {
                    continue;
                }

                string wd = ((string)md.args[1]).Trim().ToLower();
                if (wd.IndexOf(".macro") < 0)
                {
                    continue;
                }

                string macroLabel = wd.Substring(0, wd.IndexOf('.')).Trim();
                asm.RemoveAt(i);
                List<MmlDatum2> mb = new List<MmlDatum2>();

                while (i<asm.Count)
                {
                    md = asm[i];
                    if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -4)
                    {
                        mb.Add(md);
                        asm.RemoveAt(i);
                        continue;
                    }

                    if (!(md.args[1] is string))
                    {
                        mb.Add(md);
                        asm.RemoveAt(i);
                        continue;
                    }
                    
                    wd = ((string)md.args[1]).Trim().ToLower();
                    if (wd.IndexOf(".endm") < 0)
                    {
                        mb.Add(md);
                        asm.RemoveAt(i);
                        continue;
                    }

                    asm.RemoveAt(i);
                    dicMacroBlock.Add(macroLabel, mb);
                    i--;
                    break;
                }

            }
        }


        private void Step3_ReplaceMacro()
        {
            bool f;

            do
            {
                f = false;
                for (int i = 0; i < asm.Count; i++)
                {
                    MmlDatum2 md = asm[i];

                    if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -6)
                    {
                        continue;
                    }

                    if (!(md.args[1] is string))
                    {
                        continue;
                    }

                    string wd = ((string)md.args[1]).Trim().ToLower();
                    foreach (string key in dicMacroBlock.Keys)
                    {
                        if (wd != key) continue;

                        f = true;
                        asm.RemoveAt(i);
                        foreach (MmlDatum2 val in dicMacroBlock[key])
                        {
                            asm.Insert(i++, val);
                        }
                    }
                }
            } while (f);

        }

        private void Step4_GetDefine()
        {
            dicDefine = new Dictionary<string, MmlDatum2>();

            for (int i = 0; i < asm.Count; i++)
            {
                MmlDatum2 md = asm[i];

                if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -5)
                {
                    continue;
                }

                if (!(md.args[1] is string))
                {
                    continue;
                }

                string[] wd = ((string)md.args[1]).Trim().ToLower().Replace("\t", " ").Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (wd[1]!="equ")
                {
                    continue;
                }

                string defineLabel = wd[0];
                md = new MmlDatum2(wd[2], -5, "");
                dicDefine.Add(defineLabel, md);

            }
        }

        private void Step5_GetLabel()
        {
            dicLabel = new Dictionary<string, MmlDatum2>();

            for (int i = 0; i < asm.Count; i++)
            {
                MmlDatum2 md = asm[i];

                if (md == null || md.args == null || md.args.Count < 2 || !(md.args[0] is int) || (int)md.args[0] != -2)
                {
                    continue;
                }

                if (!(md.args[1] is string))
                {
                    continue;
                }

                md.dat = i;//行数を入れてみる
                dicLabel.Add((string)md.args[1], md);

            }
        }

        private void Assemblling()
        {
            for(int i = 0; i < asm.Count; i++)
            {
                
                if (asm==null || asm[i].args == null || asm[i].args.Count < 2) continue;
                List<object> args = asm[i].args;

                string code = asm[i].code;
                while (code.IndexOf("\n") == code.Length - 1) code = code.Substring(0, code.Length - 1);
                while (code.IndexOf("\n") == 0) code = code.Substring(1);
                Log.WriteLine(LogLevel.TRACE, code);

                int ptr = 0;
                while (ptr < asm[i].args.Count)
                {
                    if(!(args[ptr ] is int))
                    {
                        ptr++;
                        continue;
                    }

                    int tp = (int)args[ptr];
                    switch (tp)
                    {
                        case -1://db
                            asmDb(asm[i], ref ptr);
                            break;
                        case -2://label
                            asmLabel(asm[i], ref ptr);
                            break;
                        case -3://db ref label
                            asmDbRefLabel(asm[i], ref ptr);
                            break;
                        case -4://macro
                            asmMacro(asm[i], ref ptr);
                            break;
                        case -5://define
                            ptr = asm[i].args.Count;
                            break;
                        case -6://db ref define
                            asmDbRefDefine(asm[i], ref ptr);
                            break;
                        default:
                            Log.WriteLine(LogLevel.ERROR, string.Format("Unknown type[{0}] error. ", tp));
                            ptr++;
                            break;
                    }
                }
            }
        }

        private void asmDb(MmlDatum2 asm, ref int ptr)
        {
            if (assembleBlockLatest) { ptr += 2; return; }

            List<object> args = asm.args;
            if (args[ptr + 1] is byte)
            {
                byte n = (byte)args[ptr + 1];
                ptr += 2;
                Poke(currentBank, currentAddress++, n, asm);
            }
            else if (args[ptr + 1] is int)
            {
                byte n = (byte)(int)args[ptr + 1];
                ptr += 2;
                Poke(currentBank, currentAddress++, n, asm);//int であってもbyte扱いです
            }
            else if (args[ptr + 1] is char)
            {
                byte n = (byte)(char)args[ptr + 1];
                ptr += 2;
                Poke(currentBank, currentAddress++, n, asm);
            }
            else if (args[ptr + 1] is string)
            {
                //複合型
                string sen = (string)args[ptr + 1];
                List<byte> wd = new List<byte>();
                for(int i = 0; i < sen.Length; i++)
                {
                    if (sen[i] == ' ' || sen[i] == '\t') continue;
                    if (sen[i] == ',')
                    {
                        continue;
                    }

                    int j;
                    string x = "";

                    if (sen[i] == '"')
                    {
                        x = "";
                        j = i + 1;
                        for (; j < sen.Length; j++)
                        {
                            if (sen[j] == '"') break;
                            x += sen[j];
                        }
                        i = j;

                        Common.myEncoding enc = new Common.myEncoding();
                        byte[] ary = enc.GetSjisArrayFromString(x);
                        foreach (byte b in ary) wd.Add(b);
                        continue;
                    }

                    x = "";
                    j = i;
                    for (; j < sen.Length; j++)
                    {
                        if (sen[i] == ' ' || sen[i] == '\t' || sen[i] == ',') break;
                        x += sen[j];
                    }
                    i = j;
                    int n = GetInt(x);
                    wd.Add((byte)n);
                }

                ptr += 2;
                foreach (byte b in wd) Poke(currentBank, currentAddress++, b, asm);

            }
            else
            {
                Log.WriteLine(LogLevel.ERROR, "Db error.");
                ptr++;
            }
        }

        private void asmLabel(MmlDatum2 asm, ref int ptr)
        {
            if (assembleBlockLatest) { ptr += 2; return; }

            List<object> args = asm.args;
            if (args[ptr + 1] is string)
            {
                string label = ((string)args[ptr + 1]).ToLower();

                if (dicLabel.ContainsKey(label))
                {
                    MmlDatum2 md = dicLabel[label];
                    ;
                    md.args.Add(currentBank);
                    md.args.Add(currentAddress);
                    ptr += 2;
                }

                ptr += 2;
            }
            else
            {
                Log.WriteLine(LogLevel.ERROR, "Db Label error.");
                ptr++;
            }
        }

        private void asmDbRefLabel(MmlDatum2 asm, ref int ptr)
        {
            if (assembleBlockLatest) { ptr += 2; return; }

            List<object> args = asm.args;
            if (args[ptr + 1] is string)
            {
                string label = ((string)args[ptr + 1]).ToLower();
                object byteFlg = false;
                if (label.IndexOf("b:") >= 0)
                {
                    byteFlg = true;
                    label = label.Substring(2);
                }

                if (label.IndexOf("bank(") >= 0)
                {
                    byteFlg = -1;
                    label = label.Substring(label.IndexOf("bank(") + 5, label.LastIndexOf(")") - label.IndexOf("bank(") - 5);
                }

                if (!dicRefLabel.ContainsKey(label))
                {
                    dicRefLabel.Add(label, new List<Tuple<int, int, object>>());// currentBank, currentAddress, byteFlg
                }

                dicRefLabel[label].Add(new Tuple<int, int, object>(currentBank, currentAddress, byteFlg));
                Poke(currentBank, currentAddress++, 0, asm);
                if (byteFlg is bool && !(bool)byteFlg) Poke(currentBank, currentAddress++, 0, asm);

                ptr += 2;
            }
            else
            {
                Log.WriteLine(LogLevel.ERROR, "Db ref Label error.");
                ptr++;
            }
        }

        private void asmDbRefDefine(MmlDatum2 asm, ref int ptr)
        {
            if (assembleBlockLatest) { ptr += 2; return; }

            List<object> args = asm.args;
            if (args[ptr + 1] is string)
            {
                string define = ((string)args[ptr + 1]).ToLower();
                bool byteFlg = false;
                if (define.IndexOf("b:") >= 0)
                {
                    byteFlg = true;
                    define = define.Substring(2);
                }
                int n = GetInt(dicDefine[define].code);

                ptr += 2;
                Poke(currentBank, currentAddress++, (byte)n, asm);
                if (!byteFlg) Poke(currentBank, currentAddress++, (byte)(n >> 8), asm);
            }
            else
            {
                Log.WriteLine(LogLevel.ERROR, "Db ref degine error.");
                ptr++;
            }
        }

        private void Poke(int bank, int adr, byte dat, MmlDatum2 src)
        {
            while (dest.Count < bank + 1)
            {
                dest.Add(new List<MmlDatum2>());
            }
            while (dest[bank].Count < adr + 1)
            {
                dest[bank].Add(null);
            }

            dest[bank][adr] = new MmlDatum2();
            dest[bank][adr].dat = dat;
            dest[bank][adr].args = null;
            dest[bank][adr].code = "";
            dest[bank][adr].linePos = src.linePos;
            dest[bank][adr].type = src.type;
            if (adr > 0x9b84)
            {
                ;
            }
            Log.WriteLine(LogLevel.TRACE, string.Format("{0:X02}:{1:X04}:{2:X02}",bank,adr,dat));
        }

        private void asmMacro(MmlDatum2 asm,ref int ptr)
        {
            string tmp = ((string)asm.args[ptr + 1]).Replace("\t", " ");
            string[] macros = tmp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            int n;

            switch (macros[ptr])
            {
                case ".bank":
                    if (assembleBlockLatest) break;
                    n = AnaFormula(asm.args);
                    currentBank = n;
                    ptr = asm.args.Count;
                    return;
                case ".org":
                    if (assembleBlockLatest) break;
                    n = GetInt(macros[ptr+1]);
                    currentAddress = n;
                    break;
                case ".code":
                    //無視
                    break;
                case ".if":
                    assembleBlockStack.Push(!anaCondition(macros));//ブロックするかどうかのフラグなので判定結果を反転させたものがセットされる
                    UpdateAssembleBlockLatest();
                    break;
                case ".else":
                    bool flg = assembleBlockStack.Pop();
                    assembleBlockStack.Push(!flg);
                    UpdateAssembleBlockLatest();
                    break;
                case ".endif":
                    assembleBlockStack.Pop();
                    UpdateAssembleBlockLatest();
                    break;
                default:
                    Log.WriteLine(LogLevel.ERROR, "macro error.");
                    ptr++;
                    return;
            }
            ptr += 2;
        }

        private bool anaCondition(string[] macros)
        {
            List<int> op = new List<int>();
            int con = -1000;
            for (int i = 1; i < macros.Length; i++)
            {
                //とりあえず(は無視する
                if (macros[i][0] == '(')
                    macros[i] = macros[i].Substring(1);
                if (macros[i][macros[i].Length - 1] == ')')
                    macros[i] = macros[i].Substring(0, macros[i].Length - 1);

                //定数かな
                if (dicDefine.ContainsKey(macros[i].ToLower()))
                {
                    op.Add(GetInt(dicDefine[macros[i].ToLower()].code));
                }
                else if (macros[i] == "=")
                {
                    con = 0;
                }
                else if (macros[i] == "<")
                {
                    con = -1;
                }
                else if (macros[i] == ">")
                {
                    con = 1;
                }
                else if (macros[i] == "<>")
                {
                    con = 2;
                }
                else
                {
                    try
                    {
                        op.Add(GetInt(macros[i]));
                    }
                    catch { }
                }
            }

            if(op.Count!=1 && (op.Count!=2 || con == -1000))
            {
                throw new Exception("anaCondition error");
            }

            if (op.Count == 1)
            {
                return op[0] != 0;
            }

            if (con == 0)
            {
                return op[0] == op[1];
            }
            if (con == -1)
            {
                return op[0] < op[1];
            }
            if (con == 1)
            {
                return op[0] > op[1];
            }
            if (con == 2)
            {
                return op[0] != op[1];
            }

            return false;
        }

        private int AnaFormula(List<object> a)
        {
            int ans = 0;
            int ope = -1;
            int n = 0;
            bool flg = false;
            for (int i = 2; i < a.Count; i++)
            {
                if (a[i] is string)
                {
                    string s = ((string)a[i]).ToLower();

                    //定数かな
                    if (dicDefine.ContainsKey(s))
                    {
                        n = GetInt(dicDefine[s].code);
                        flg = true;
                    }
                    else if (s == "+")
                    {
                        ope = 0;
                    }
                }
                else if (a[i] is int)
                {
                    n = (int)a[i];
                    flg = true;
                }

                if (!flg) continue;
                flg = false;
                if (ope == -1) ans = n;
                if (ope == 0) ans += n;
            }

            return ans;
        }

        private int GetInt(string v)
        {
            if (v == null || v.Length < 1)
            {
                throw new Exception("integer parse error");
            }
            if (v[0] == '$')
            {
                //16進数
                return Convert.ToInt32(v.Substring(1), 16);
            }
            else
            {
                return int.Parse(v);
            }
        }

        private void SetLabel()
        {
            foreach (string refkey in dicRefLabel.Keys)
            {
                string key = refkey + ":";
                if (!dicLabel.ContainsKey(key)) continue;
                MmlDatum2 md = dicLabel[key];
                byte bank = (byte)(int)md.args[2];
                int adr = (int)md.args[3];

                List<Tuple<int, int, object>> trgs = dicRefLabel[refkey];
                foreach (Tuple<int, int, object> trg in trgs)
                {
                    byte trgBank = (byte)trg.Item1;
                    int trgAdr = trg.Item2;
                    object trgByteFlg = trg.Item3;

                    MmlDatum2 m;
                    if (trgByteFlg is bool)
                    {
                        m = new MmlDatum2();
                        m.dat = (byte)adr;
                        dest[trgBank][trgAdr] = m;
                        if (!(bool)trgByteFlg)
                        {
                            m = new MmlDatum2();
                            m.dat = (byte)(adr >> 8);
                            dest[trgBank][trgAdr + 1] = m;
                        }
                    }
                    else
                    {
                        m = new MmlDatum2();
                        m.dat = bank;
                        dest[trgBank][trgAdr] = m;
                    }
                }
            }
        }

    }
}
