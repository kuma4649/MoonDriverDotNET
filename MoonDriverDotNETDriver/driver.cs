using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace MoonDriverDotNET.Driver
{
    public class Driver : iDriver
    {
        public Exception renderingException = null;
        private MoonDriver md = null;
        static MmlDatum[] srcBuf = null;
        private Action<ChipDatum> WriteOPL4;

        public void FadeOut()
        {
            throw new NotImplementedException();
        }

        public MmlDatum[] GetDATA()
        {
            throw new NotImplementedException();
        }

        public GD3Tag GetGD3TagInfo(byte[] srcBuf)
        {
            throw new NotImplementedException();
        }

        public int GetNowLoopCounter()
        {
            throw new NotImplementedException();
        }

        public byte[] GetPCMFromSrcBuf()
        {
            throw new NotImplementedException();
        }

        public ChipDatum[] GetPCMSendData()
        {
            throw new NotImplementedException();
        }

        public Tuple<string, ushort[]>[] GetPCMTable()
        {
            throw new NotImplementedException();
        }

        public int GetStatus()
        {
            return 1;
        }

        public List<Tuple<string, string>> GetTags()
        {
            return null;
        }

        public object GetWork()
        {
            return null;
        }


        public void Init(string fileName, Action<ChipDatum> oPNAWrite, double sampleRate,
            MoonDriverDotNETOption dop, string[] vs, Func<string, Stream> appendFileReaderCallback)
        {
            if (Path.GetExtension(fileName).ToLower() != ".xml")
            {
                byte[] srcBuf = File.ReadAllBytes(fileName);
                if (srcBuf == null || srcBuf.Length < 1) return;
                Init(fileName, srcBuf, oPNAWrite, sampleRate,
                    dop, vs,
                    appendFileReaderCallback ?? CreateAppendFileReaderCallback(Path.GetDirectoryName(fileName))
                    );
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MmlDatum[]), typeof(MmlDatum[]).GetNestedTypes());
                using (StreamReader sr = new StreamReader(fileName, new UTF8Encoding(false)))
                {
                    try
                    {
                        MmlDatum[] s = (MmlDatum[])serializer.Deserialize(sr);
                        Init(fileName, s, oPNAWrite, sampleRate,
                            dop, vs, appendFileReaderCallback ?? CreateAppendFileReaderCallback(Path.GetDirectoryName(fileName))
                            );
                    }
                    catch (System.InvalidOperationException e)
                    {
                    }
                }

            }
        }

        public void Init(
            string fileName,
            byte[] srcBuf,
            Action<ChipDatum> opnaWrite,
            double sampleRate,
            MoonDriverDotNETOption addtionalPMDDotNETOption, string[] addtionalPMDOption,
            Func<string, Stream> appendFileReaderCallback
            )
        {
            if (srcBuf == null || srcBuf.Length < 1) return;
            List<MmlDatum> bl = new List<MmlDatum>();
            foreach (byte b in srcBuf) bl.Add(new MmlDatum(b));
            Init(fileName, bl.ToArray(), opnaWrite,sampleRate,
                addtionalPMDDotNETOption, addtionalPMDOption, appendFileReaderCallback);
        }

        public void Init(
            string fileName,
            MmlDatum[] srcBuf,
            Action<ChipDatum> opl4Write,
            double sampleRate,
            MoonDriverDotNETOption addtionalPMDDotNETOption, string[] addtionalPMDOption,
            Func<string, Stream> appendFileReaderCallback
            )
        {
            if (srcBuf == null || srcBuf.Length < 1) return;

            Driver.srcBuf = srcBuf;

            WriteOPL4 = opl4Write;

            //work = new PW();
            GetTags();
            //addtionalPMDDotNETOption.PPCHeader = CheckPPC(appendFileReaderCallback);

            //work.SetOption(addtionalPMDDotNETOption, addtionalPMDOption);
            //work.timer = new OPNATimer(44100, 7987200);

            string pcmFn = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName)+".pcm");
            byte[] pcmData = null;
            using (Stream s = appendFileReaderCallback(pcmFn))
            {
                pcmData = Common.Common.ReadAllBytes(s);
            }

            //

            md = new MoonDriver();
            if (pcmData != null) md.ExtendFile = new Tuple<string, byte[]>(pcmFn, pcmData);
            md.init(srcBuf, WriteRegister, sampleRate);

            //if (!string.IsNullOrEmpty(pmd.pw.ppz1File) || !string.IsNullOrEmpty(pmd.pw.ppz2File)) pmd.pcmload.ppz_load(pmd.pw.ppz1File, pmd.pw.ppz2File);

        }

        public void MusicSTART(int musicNumber)
        {
        }

        public void MusicSTOP()
        {
        }

        public void Rendering()
        {
            md.oneFrameProc();
        }

        public void SetDriverSwitch(params object[] param)
        {
            throw new NotImplementedException();
        }

        public int SetLoopCount(int loopCounter)
        {
            throw new NotImplementedException();
        }

        public void ShotEffect()
        {
            throw new NotImplementedException();
        }

        public void StartRendering(int renderingFreq, Tuple<string, int>[] chipsMasterClock)
        {
        }

        public void StopRendering()
        {
        }

        public void WriteRegister(ChipDatum reg)
        {
            WriteOPL4(reg);
        }

        public void dispStatus()
        {
        }

        private static Func<string, Stream> CreateAppendFileReaderCallback(string dir)
        {
            return fname =>
            {
                if (!string.IsNullOrEmpty(dir))
                {
                    var path = Path.Combine(dir, fname);
                    if (File.Exists(path))
                    {
                        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                }
                if (File.Exists(fname))
                {
                    return new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                return null;
            };
        }

        public void Init(string fileName, Action<ChipDatum> chipWriteRegister, Action<long, int> chipWaitSend, MmlDatum[] srcBuf, object addtionalOption)
        {
            throw new NotImplementedException();
        }
    }
}
