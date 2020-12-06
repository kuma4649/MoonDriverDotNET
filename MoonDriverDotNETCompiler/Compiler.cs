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
        public string[] mckArgs = null;
        public string[] env { get; set; }
        public bool isSrc = false;

        //内部
        private string srcBuf = null;
        private bool isIDE = false;
        private Point skipPoint = Point.Empty;
        private Func<string, Stream> appendFileReaderCallback;
        public work work = new work();


        public Compiler(iEncoding enc = null)
        {
            this.enc = enc ?? myEncoding.Default;
        }

        public void Init()
        {
            this.isIDE = false;
            this.skipPoint = Point.Empty;
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

            mck mck = new mck();
            MmlDatum[] ret = mck.main(this, mckArgs, work, env);
            return ret;
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
            throw new NotImplementedException();
        }

        public GD3Tag GetGD3TagInfo(byte[] srcBuf)
        {
            throw new NotImplementedException();
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

                if((string)prm=="SRC")
                {
                    this.isSrc = true;
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

                //mck option 指定
                if (((string)prm).IndexOf("PmdOption=") == 0)
                {
                    try
                    {
                        string[] p = ((string)prm).Split('=')[1].Split(' ');
                        mckArgs = p;
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
