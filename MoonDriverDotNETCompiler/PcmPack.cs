using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MoonDriverDotNET.Compiler
{
    public class PcmPack
    {
        //
        // pcmpack.c
        //

        private const string PRG_NAME = "PCMPACK";
        private const string PRG_VER = "Ver 0.1";
        private const string PRG_AUTHOR = "BouKiCHi";

        // MDRファイル定義
        private class _mdr
        {
            public int fp;
            public int size;
            public int header = 0;
            public string pcmname;//[PATH_MAX]; // pos:0x40 pcmname
            public int pcm_packed; // pos: 0x2a 1:pcm is packed
            public int pcm_startadrs; // pos:0x30 start address of PCM RAM(* 0x10000)
            public int pcm_startbank; // pos:0x31 start bank (* 8192)
            public int pcm_banks; // pos:0x32 number of PCM banks (* 8192)
            public int pcm_lastsize; // pos:0x32 size of last bank (* 0x100)

            // actual size = (pcm_banks * 0x2000) + (pcm_lastsize * 0x100)
        }

        // MDRファイル読み出し
        private int readMDRHeader(List<MmlDatum2> destBuf, string file, ref _mdr m)
        {
            try
            {
                m.size = destBuf.Count;

                // PCM文字列位置
                m.pcmname = "";
                int pcmpos = destBuf[m.header + 0x2c].dat + destBuf[m.header + 0x2d].dat * 0x100;

                // PCM位置
                if (pcmpos == 0x8040)
                {
                    byte[] fn = new byte[0x40];
                    for (int i = 0; i < 0x40; i++)
                    {
                        fn[i] = (byte)destBuf[m.header + i].dat;
                    }
                    Common.myEncoding enc = new Common.myEncoding();
                    m.pcmname = enc.GetStringFromSjisArray(fn);
                }

                // PCM設定値
                m.pcm_packed = destBuf[m.header + 0x2a].dat;
                m.pcm_startadrs = destBuf[m.header + 0x30].dat;
                m.pcm_startbank = destBuf[m.header + 0x31].dat;
                m.pcm_banks = destBuf[m.header + 0x32].dat;
                m.pcm_lastsize = destBuf[m.header + 0x33].dat;

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        // MDRヘッダ再構築
        private void writeMDRHeader(List<MmlDatum2> destBuf, _mdr m)
        {
            // PCM設定値
            destBuf[m.header + 0x2a].dat = m.pcm_packed;
            destBuf[m.header + 0x30].dat = m.pcm_startadrs;
            destBuf[m.header + 0x31].dat = m.pcm_startbank;
            destBuf[m.header + 0x32].dat = m.pcm_banks;
            destBuf[m.header + 0x33].dat = m.pcm_lastsize;
        }

        private const int BANK_SIZE = 0x2000;

        // MDRファイル読み出し
        private List<MmlDatum2> packPCMintoMDR(List<MmlDatum2> destBuf, string file, string pcm, ref _mdr m)
        {
            byte[] bank=new byte[BANK_SIZE];
            Log.WriteLine(LogLevel.INFO, "packing...");

            int start_pos = m.size;

            // パックされている場合はPCM先頭バングから計算する
            if (m.pcm_packed!=0)
            {
                start_pos = m.pcm_startbank * BANK_SIZE;
            }

            byte[] pcmBuf;

            try
            {
                if (File.Exists(pcm)) pcmBuf = File.ReadAllBytes(pcm);
                else
                {
                    pcm = Path.Combine(Path.GetDirectoryName(file), pcm);
                    pcmBuf = File.ReadAllBytes(pcm);
                }
            }
            catch
            {
                Log.WriteLine(LogLevel.ERROR, string.Format("File open error!:{0}", pcm));
                return null;
            }

            // PCMデータ出力位置
            Log.WriteLine(LogLevel.INFO, string.Format("PCM Start:{0:X08}h", start_pos));
            m.fp = start_pos;

            int block_len = 0;
            int pcm_blocks = 1;

            int i = 0;
            while (i < pcmBuf.Length)
            {
                MmlDatum2 md = new MmlDatum2("", pcmBuf[i]);
                destBuf.Insert(m.fp + i, md);
                i++;
                block_len++;
                if ((i % BANK_SIZE) == 0)
                {
                    pcm_blocks++;
                    block_len = 0;
                }
            }
            if (pcmBuf.Length > 0 && block_len == 0) pcm_blocks--;

            m.pcm_packed = 1;
            m.pcm_startadrs = 0x20; // SRAM開始アドレス
            m.pcm_startbank = (int)(start_pos / BANK_SIZE); // 開始バンク
            m.pcm_banks = pcm_blocks - 1; // ブロック数
            m.pcm_lastsize = (block_len + 0xff) / 0x100; // 最後のブロックサイズ

            Log.WriteLine(LogLevel.INFO, string.Format("PCM StartAdrs:{0:x02}h", m.pcm_startadrs));
            Log.WriteLine(LogLevel.INFO, string.Format("PCM StartBank:{0:x02}h", m.pcm_startbank));
            Log.WriteLine(LogLevel.INFO, string.Format("PCM Banks:{0:x02}h", m.pcm_banks));
            Log.WriteLine(LogLevel.INFO, string.Format("PCM LastSize:{0:x02}h", m.pcm_lastsize));

            writeMDRHeader(destBuf, m);

            Log.WriteLine(LogLevel.INFO, "ok!");
            return destBuf;
        }

        public List<MmlDatum2> Pack(List<MmlDatum2> destBuf,string mdrFn,string pcmFn="")
        {
            string pcmfile = null;
            string mdrfile;

            // タイトル
            Log.WriteLine(LogLevel.INFO, string.Format("{0} {1} by {2}", PRG_NAME, PRG_VER, PRG_AUTHOR));

            mdrfile = mdrFn;
            Log.writeLine(LogLevel.INFO, string.Format("File:{0}", mdrfile));
            if (!string.IsNullOrEmpty(pcmFn)) pcmfile = pcmFn;

            _mdr m = new _mdr();
            readMDRHeader(destBuf, mdrfile, ref m);

            Log.writeLine(LogLevel.INFO, string.Format("Size:{0}", m.size));

            if (string.IsNullOrEmpty(pcmfile))
            {
                if (!string.IsNullOrEmpty(m.pcmname))
                {
                    pcmfile = Path.GetDirectoryName(mdrfile);
                    pcmfile = Path.Combine(pcmfile, m.pcmname);
                }
            }

            if (string.IsNullOrEmpty(pcmfile))
            {
                Log.WriteLine(LogLevel.INFO, "PCM filename is not defined!");
                return destBuf;
            }

            Log.WriteLine(LogLevel.INFO, string.Format("PCM File:{0}", pcmfile));

            // PCMを詰め込む
            return packPCMintoMDR(destBuf, mdrfile, pcmfile, ref m);

        }
    }
}
