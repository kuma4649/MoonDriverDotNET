using musicDriverInterface;
using MoonDriverDotNET.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace MoonDriverDotNET.Compiler
{
    public class Compiler : iCompiler
    {
        public iEncoding enc = null;
        public string[] args = null;
        public string[] env { get; set; }
        public bool isSrc = false;
        public bool doPackPCM = false;
        public string pcmFileName = "";

        //内部
        private string srcBuf = null;
        public string origpath = null;
        private bool isIDE = false;
        private Point skipPoint = Point.Empty;
        private Func<string, Stream> appendFileReaderCallback;
        public work work = new work();
        public mck mck = null;


        public Compiler(iEncoding enc = null)
        {
            this.enc = enc ?? myEncoding.Default;
        }

        public void Init()
        {
            this.isIDE = false;
            this.skipPoint = Point.Empty;
            this.args = null;
        }

        public MmlDatum[] Compile(Stream sourceMML, Func<string, Stream> appendFileReaderCallback)
        {
            using (var ms = ReadAllBytesToMemoryStream(sourceMML))
            {
                ms.Seek(0, SeekOrigin.Begin);
                int c = 0;
                int offset = 0;
                while ((c = ms.ReadByte()) >= 0)
                {
                    if (c == 0x1a)
                    {
                        ms.SetLength(offset);
                        break;
                    }
                    offset++;
                }
                ms.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(ms, Encoding.GetEncoding("Shift_JIS")))
                {
                    srcBuf = sr.ReadToEnd();
                }
            }

            //Console.WriteLine(srcBuf);

            this.appendFileReaderCallback = appendFileReaderCallback;

            work.srcBuf = srcBuf;

            mck = new mck();
            List<MmlDatum> ret = new List<MmlDatum>();
            
            //if (isIDE)
            //{
            //    args = new string[] { "-i", "dummy.mdl" };
            //}

            MmlDatum2[] dest = mck.main(this, args, work, env);
            if (dest == null || dest.Length < 1) return null;
            //ほしいのはmmlDatumnなのでキャスト(?)して作り直す
            foreach (MmlDatum2 md2 in dest)
            {
                ret.Add(md2 == null ? null : md2.ToMmlDatumn());
            }

            return ret.ToArray();
        }

        public bool Compile(FileStream sourceMML, Stream destCompiledBin, Func<string, Stream> appendFileReaderCallback)
        {
            var dat = Compile(sourceMML, appendFileReaderCallback);
            if (dat == null)
            {
                return false;
            }
            foreach (MmlDatum md in dat)
            {
                if (md == null)
                {
                    destCompiledBin.WriteByte(0);
                }
                else
                {
                    destCompiledBin.WriteByte((byte)md.dat);
                }
            }
            return true;
        }

        public CompilerInfo GetCompilerInfo()
        {
            if (mck == null) return null;
            return mck.GetCompilerInfo();
        }

        public GD3Tag GetGD3TagInfo(byte[] srcBuf)
        {
            return null;
        }

        public void SetCompileSwitch(params object[] param)
        {
            if (param == null) return;

            foreach (object prm in param)
            {
                if (prm is Func<string, Stream>)
                {
                    appendFileReaderCallback = (Func<string, Stream>)prm;
                    continue;
                }

                if (!(prm is string)) continue;

                if ((string)prm == "SRC")
                {
                    this.isSrc = true;
                }

                //PCMPACK指定の場合は単独で指定する必要あり
                if ((string)prm == "PCMPACK")
                {
                    this.doPackPCM = true;
                    this.pcmFileName = (string)param[1];
                    return;
                }

                //IDEフラグオン
                if ((string)prm == "IDE")
                {
                    this.isIDE = true;
                }

                //スキップ再生指定
                if (((string)prm).IndexOf("SkipPoint=") == 0)
                {
                    try
                    {
                        string[] p = ((string)prm).Split('=')[1].Split(':');
                        int r = int.Parse(p[0].Substring(1));
                        int c = int.Parse(p[1].Substring(1));
                        this.skipPoint = new Point(c, r);
                    }
                    catch
                    {
                        continue;
                    }
                }

                //オリジナルファイルの所在
                if (((string)prm).IndexOf("ORIGPATH=") == 0)
                {
                    try
                    {
                        this.origpath = ((string)prm).Split('=')[1];
                    }
                    catch
                    {
                        continue;
                    }
                }

                //MoonDriver自体のオプション
                if (((string)prm).IndexOf("MoonDriverOption=") == 0)
                {
                    try
                    {
                        string p = ((string)prm).Split('=')[1];
                        List<string> larg;
                        if (args != null)
                        {
                            larg = new List<string>(args);
                        }
                        else
                        {
                            larg = new List<string>();
                        }
                        larg.Add(p);
                        args = larg.ToArray();
                    }
                    catch
                    {
                        continue;
                    }
                }

            }
        }

        public Tuple<string, string>[] GetTags(string srcText, Func<string, Stream> appendFileReaderCallback)
        {
            return null;
        }

        private MemoryStream ReadAllBytesToMemoryStream(Stream stream)
        {
            if (stream == null) return null;

            var buf = new byte[8192];
            var ms = new MemoryStream();
            while (true)
            {
                var r = stream.Read(buf, 0, buf.Length);
                if (r < 1)
                {
                    break;
                }
                ms.Write(buf, 0, r);
            }
            return ms;
        }
    }
}
