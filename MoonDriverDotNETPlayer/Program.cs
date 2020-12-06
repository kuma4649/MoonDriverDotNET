using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using musicDriverInterface;
using NAudio.Wave;
using Nc86ctl;
using NScci;
using MoonDriverDotNET.Common;
using MoonDriverDotNET.Driver;

namespace MoonDriverDotNET.Player
{
    static class Program
    {
        private static DirectSoundOut audioOutput = null;
        public delegate int naudioCallBack(short[] buffer, int offset, int sampleCount);
        private static naudioCallBack callBack = null;
        private static Thread trdMain = null;
        private static Stopwatch sw = null;
        private static double swFreq = 0;
        public static bool trdClosed = false;
        private static object lockObj = new object();
        private static bool _trdStopped = true;
        public static bool trdStopped
        {
            get
            {
                lock (lockObj)
                {
                    return _trdStopped;
                }
            }
            set
            {
                lock (lockObj)
                {
                    _trdStopped = value;
                }
            }
        }
        private static readonly uint SamplingRate = 55467;//44100;
        private static readonly uint samplingBuffer = 1024;
        private static short[] frames = new short[samplingBuffer * 4];
        private static MDSound.MDSound mds = null;
        private static short[] emuRenderBuf = new short[2];
        private static musicDriverInterface.iDriver drv = null;
        private static readonly uint opl4MasterClock = 33868800;
        private static int device = 0;
        private static int loop = 0;
        private static NScci.NScci nScci;
        private static Nc86ctl.Nc86ctl nc86ctl;
        private static RSoundChip rsc;

        private static bool isGimicOPNA = false;
        private static string[] envMoonDriver = null;
        private static string[] envMoonDriverOpt = null;
        private static string srcFile = null;

        static int Main(string[] args)
        {
            Log.writeLine += WriteLine;
#if DEBUG
            //Log.writeLine += WriteLineF;
            Log.level = LogLevel.INFO;
#else
            Log.level = LogLevel.INFO;
#endif
            int fnIndex = AnalyzeOption(args);
            int mIndex = -1;

            if (args != null)
            {
                for (int i = fnIndex; i < args.Length; i++)
                {
                    if ((Path.GetExtension(args[i]).ToLower().IndexOf(Common.Common.objExtension) < 0)
                        && (Path.GetExtension(args[i]).ToUpper().IndexOf(".XML") < 0)
                        ) continue;
                    mIndex = i;
                    break;
                }
            }

            if (mIndex < 0)
            {
                Log.WriteLine(LogLevel.INFO, string.Format("引数({0}ファイル)１個欲しいよぉ...", Common.Common.objExtension));
                return -1;
            }

            srcFile = args[mIndex];

            if (!File.Exists(args[mIndex]))
            {
                Log.WriteLine(LogLevel.ERROR, string.Format("ファイル[{0}]が見つかりません", args[mIndex]));
                return -1;
            }

            rsc = CheckDevice();

            try
            {

                SineWaveProvider16 waveProvider;
                int latency = 1000;

                switch (device)
                {
                    case 0:
                        waveProvider = new SineWaveProvider16();
                        waveProvider.SetWaveFormat((int)SamplingRate, 2);
                        callBack = EmuCallback;
                        audioOutput = new DirectSoundOut(latency);
                        audioOutput.Init(waveProvider);
                        break;
                    case 1:
                    case 2:
                        trdMain = new Thread(new ThreadStart(RealCallback));
                        trdMain.Priority = ThreadPriority.Highest;
                        trdMain.IsBackground = true;
                        trdMain.Name = "trdVgmReal";
                        sw = Stopwatch.StartNew();
                        swFreq = Stopwatch.Frequency;
                        break;
                }

                MDSound.ymf278b ymf278b = new MDSound.ymf278b();
                MDSound.MDSound.Chip chip = new MDSound.MDSound.Chip
                {
                    type = MDSound.MDSound.enmInstrumentType.YMF278B,
                    ID = 0,
                    Instrument = ymf278b,
                    Update = ymf278b.Update,
                    Start = ymf278b.Start,
                    Stop = ymf278b.Stop,
                    Reset = ymf278b.Reset,
                    SamplingRate = SamplingRate,
                    Clock = opl4MasterClock,
                    Volume = 0,
                    Option = new object[] { GetApplicationFolder() }
                };

                mds = new MDSound.MDSound(SamplingRate, samplingBuffer, new MDSound.MDSound.Chip[] { chip });
                //ppz8em = new PPZ8em(SamplingRate);
                //ppsdrv = new PPSDRV(SamplingRate);



                Common.Environment env = new Common.Environment();
                env.AddEnv("moondriver");
                env.AddEnv("moondriveropt");
                envMoonDriver = env.GetEnvVal("moondriver");
                envMoonDriverOpt = env.GetEnvVal("moondriveropt");

                List<string> opt = (envMoonDriverOpt == null) ? (new List<string>()) : envMoonDriverOpt.ToList();
                for (int i = fnIndex; i < args.Length; i++)
                {
                    opt.Add(args[i]);
                }
                mIndex += (envMoonDriverOpt == null ? 0 : envMoonDriverOpt.Length) - fnIndex;

#if NETCOREAPP
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif
                drv = new Driver.Driver();
                Driver.MoonDriverDotNETOption dop = new Driver.MoonDriverDotNETOption();
                //dop.isAUTO = isAUTO;
                //dop.isNRM = isNRM;
                //dop.isSPB = isSPB;
                //dop.isVA = isVA;
                //dop.usePPS = usePPS;
                //dop.usePPZ = usePPZ;
                //dop.isLoadADPCM = false;
                //dop.loadADPCMOnly = false;
                //dop.ppz8em = ppz8em;
                //dop.ppsdrv = ppsdrv;
                //dop.envPmd = envPmd;
                //dop.srcFile = srcFile;
                //dop.jumpIndex = -1;// 112;// -1;
                List<string> pop = new List<string>();
                //bool pmdvolFound = false;
                for (int i = 0; i < opt.Count; i++)
                {
                    if (i == mIndex) continue;
                    string op = opt[i].ToUpper().Trim();
                    pop.Add(op);
                    //if (op.IndexOf("-D") >= 0 || op.IndexOf("/D") >= 0)
                    //    pmdvolFound = true;
                }

                Log.WriteLine(LogLevel.INFO, "");

                ((Driver.Driver)drv).Init(
                    srcFile
                    , OPL4Write
                    , SamplingRate
                    , dop
                    , pop.ToArray()
                    , appendFileReaderCallback
                    );


                ////AUTO指定の場合に構成が変わるので、構成情報を受け取ってから音量設定を行う
                //isNRM = dop.isNRM;
                //isSPB = dop.isSPB;
                //isVA = dop.isVA;
                //usePPS = dop.usePPS;
                //usePPZ = dop.usePPZ;
                //string[] pmdOptionVol = SetVolume();
                ////ユーザーがコマンドラインでDオプションを指定していない場合はpmdVolを適用させる
                //if (!pmdvolFound && pmdOptionVol != null && pmdOptionVol.Length > 0)
                //{
                //    ((Driver.Driver)drv).resetOption(pmdOptionVol);//
                //}


                List<Tuple<string, string>> tags = drv.GetTags();
                if (tags != null)
                {
                    foreach (Tuple<string, string> tag in tags)
                    {
                        if (tag.Item1 == "") continue;
                        WriteLine2(LogLevel.INFO, string.Format("{0,-16} : {1}", tag.Item1, tag.Item2), 16 + 3);
                    }
                }

                Log.WriteLine(LogLevel.INFO, "");

                drv.StartRendering((int)SamplingRate
                    , new Tuple<string, int>[] { new Tuple<string, int>("YMF278B", (int)opl4MasterClock) });

                drv.MusicSTART(0);

                switch (device)
                {
                    case 0:
                        audioOutput.Play();
                        break;
                    case 1:
                    case 2:
                        trdMain.Start();
                        break;
                }

                Log.WriteLine(LogLevel.INFO, "演奏を終了する場合は何かキーを押してください(実chip時は特に。)");

                while (true)
                {
                    System.Threading.Thread.Sleep(1);
                    if (Console.KeyAvailable)
                    {
                        break;
                    }
                    //ステータスが0(終了)又は0未満(エラー)の場合はループを抜けて終了
                    if (drv.GetStatus() <= 0)
                    {
                        if (drv.GetStatus() == 0)
                        {
                            System.Threading.Thread.Sleep((int)(latency * 2.0));//実際の音声が発音しきるまでlatency*2の分だけ待つ
                        }
                        break;
                    }

                    if (loop != 0 && drv.GetNowLoopCounter() > loop)
                    {
                        System.Threading.Thread.Sleep((int)(latency * 2.0));//実際の音声が発音しきるまでlatency*2の分だけ待つ
                        break;
                    }
                }

                drv.MusicSTOP();
                drv.StopRendering();
                ((Driver.Driver)drv).dispStatus();
            }
            catch (MoonDriverException pe)
            {
                Log.WriteLine(LogLevel.ERROR, pe.Message);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.FATAL, "演奏失敗");
                Log.WriteLine(LogLevel.FATAL, string.Format("message:{0}", ex.Message));
                Log.WriteLine(LogLevel.FATAL, string.Format("stackTrace:{0}", ex.StackTrace));
            }
            finally
            {
                if (((Driver.Driver)drv).renderingException != null)
                {
                    Log.WriteLine(LogLevel.FATAL, "演奏失敗");
                    Log.WriteLine(LogLevel.FATAL, string.Format("message:{0}", ((Driver.Driver)drv).renderingException.Message));
                    Log.WriteLine(LogLevel.FATAL, string.Format("stackTrace:{0}", ((Driver.Driver)drv).renderingException.StackTrace));
                }

                if (audioOutput != null)
                {
                    audioOutput.Stop();
                    while (audioOutput.PlaybackState == PlaybackState.Playing) { Thread.Sleep(1); }
                    audioOutput.Dispose();
                    audioOutput = null;
                }
                if (trdMain != null)
                {
                    trdClosed = true;
                    while (!trdStopped) { Thread.Sleep(1); }
                }
                if (nc86ctl != null)
                {
                    nc86ctl.deinitialize();
                    nc86ctl = null;
                }
                if (nScci != null)
                {
                    nScci.Dispose();
                    nScci = null;
                }
            }

            return 0;
        }

        public static string GetApplicationFolder()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(path))
            {
                path += path[path.Length - 1] == '\\' ? "" : "\\";
            }
            return path;
        }

        static void WriteLine(LogLevel level, string msg)
        {
            if (level == LogLevel.ERROR || level == LogLevel.FATAL)
                Console.ForegroundColor = ConsoleColor.Red;

#if DEBUG
            Console.WriteLine("[{0,-7}] {1}", level, msg);
#else
            Console.WriteLine("{0}", msg);
#endif

            if (level == LogLevel.ERROR || level == LogLevel.FATAL)
                Console.ResetColor();
        }

        static void WriteLine2(LogLevel level, string msg, int wrapPos = 0)
        {
            if (wrapPos == 0)
            {
                Log.WriteLine(level, msg);
            }
            else
            {
                string[] mes = msg.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                Log.WriteLine(level, mes[0]);
                for (int i = 1; i < mes.Length; i++)
                {
                    Log.WriteLine(level, string.Format("{0}{1}", new string(' ', wrapPos), mes[i]));
                }
            }
        }

        private static Stream appendFileReaderCallback(string arg)
        {
            string fn;
            fn = Path.Combine(
                Path.GetDirectoryName(srcFile)
                , arg
                );

            if (envMoonDriver != null)
            {
                int i = 0;
                while (!File.Exists(fn) && i < envMoonDriver.Length)
                {
                    fn = Path.Combine(
                        envMoonDriver[i++]
                        , arg
                        );
                }
            }

            FileStream strm;
            try
            {
                strm = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                strm = null;
            }

            return strm;
        }

        private static int AnalyzeOption(string[] args)
        {
            if (args == null || args.Length < 1) return 0;

            int i = 0;
            device = 0;
            loop = 0;

            while (i < args.Length && args[i] != null && args[i].Length > 0 && (args[i][0] == '-' || args[i][0] == '/'))
            {
                string op = args[i].Substring(1).ToUpper();
                if (op == "D=EMU") device = 0;
                else if (op == "D=GIMIC") device = 1;
                else if (op == "D=SCCI") device = 2;
                else if (op == "D=WAVE") device = 3;
                //else if (op.Length > 2 && op.Substring(0, 2) == "L=") OptionSetLoop(op);
                //else if (op == "H" || op == "?") OptionDispHelp();
                else break;

                i++;
            }

            if (device == 3 && loop == 0) loop = 1;//wave出力の場合、無限ループは1に変更
            return i;
        }

        private static RSoundChip CheckDevice()
        {
            SChipType ct = null;
            int iCount = 0;

            switch (device)
            {
                case 1://GIMIC存在チェック
                    nc86ctl = new Nc86ctl.Nc86ctl();
                    try
                    {
                        nc86ctl.initialize();
                        iCount = nc86ctl.getNumberOfChip();
                    }
                    catch
                    {
                        iCount = 0;
                    }
                    if (iCount == 0)
                    {
                        try { nc86ctl.deinitialize(); } catch { }
                        nc86ctl = null;
                        Log.WriteLine(LogLevel.ERROR, "Not found G.I.M.I.C");
                        device = 0;
                        break;
                    }
                    for (int i = 0; i < iCount; i++)
                    {
                        NIRealChip rc = nc86ctl.getChipInterface(i);
                        NIGimic2 gm = rc.QueryInterface();
                        ChipType cct = gm.getModuleType();
                        int o = -1;
                        if (cct == ChipType.CHIP_OPL3)
                        {
                            ct = new SChipType();
                            ct.SoundLocation = -1;
                            ct.BusID = i;
                            string seri = gm.getModuleInfo().Serial;
                            if (!int.TryParse(seri, out o))
                            {
                                o = -1;
                                ct = null;
                                continue;
                            }
                            ct.SoundChip = o;
                            ct.ChipName = gm.getModuleInfo().Devname;
                            ct.InterfaceName = gm.getMBInfo().Devname;
                            //isGimicOPNA = (ct.ChipName == "GMC-OPNA");
                            break;
                        }
                    }
                    RC86ctlSoundChip rsc = null;
                    if (ct == null)
                    {
                        nc86ctl.deinitialize();
                        nc86ctl = null;
                        Log.WriteLine(LogLevel.ERROR, "Not found G.I.M.I.C(OPNA module)");
                        device = 0;
                    }
                    else
                    {
                        rsc = new RC86ctlSoundChip(-1, ct.BusID, ct.SoundChip);
                        rsc.c86ctl = nc86ctl;
                        rsc.init();

                        rsc.SetMasterClock(opl4MasterClock);//SoundBoardII
                        //rsc.setSSGVolume(63);//PC-8801
                    }
                    return rsc;
                case 2://SCCI存在チェック
                    nScci = new NScci.NScci();
                    iCount = nScci.NSoundInterfaceManager_.getInterfaceCount();
                    if (iCount == 0)
                    {
                        nScci.Dispose();
                        nScci = null;
                        Log.WriteLine(LogLevel.ERROR, "Not found SCCI.");
                        device = 0;
                        break;
                    }
                    for (int i = 0; i < iCount; i++)
                    {
                        NSoundInterface iIntfc = nScci.NSoundInterfaceManager_.getInterface(i);
                        NSCCI_INTERFACE_INFO iInfo = nScci.NSoundInterfaceManager_.getInterfaceInfo(i);
                        int sCount = iIntfc.getSoundChipCount();
                        for (int s = 0; s < sCount; s++)
                        {
                            NSoundChip sc = iIntfc.getSoundChip(s);
                            int t = sc.getSoundChipType();
                            if (t == 1)
                            {
                                ct = new SChipType();
                                ct.SoundLocation = 0;
                                ct.BusID = i;
                                ct.SoundChip = s;
                                ct.ChipName = sc.getSoundChipInfo().cSoundChipName;
                                ct.InterfaceName = iInfo.cInterfaceName;
                                goto scciExit;
                            }
                        }
                    }
                scciExit:;
                    RScciSoundChip rssc = null;
                    if (ct == null)
                    {
                        nScci.Dispose();
                        nScci = null;
                        Log.WriteLine(LogLevel.ERROR, "Not found SCCI(OPNA module).");
                        device = 0;
                    }
                    else
                    {
                        rssc = new RScciSoundChip(0, ct.BusID, ct.SoundChip);
                        rssc.scci = nScci;
                        rssc.init();
                    }
                    return rssc;
            }

            return null;
        }

        private static int EmuCallback(short[] buffer, int offset, int count)
        {
            try
            {
                long bufCnt = count / 2;

                for (int i = 0; i < bufCnt; i++)
                {
                    mds.Update(emuRenderBuf, 0, 2, OneFrame);

                    buffer[offset + i * 2 + 0] = emuRenderBuf[0];
                    buffer[offset + i * 2 + 1] = emuRenderBuf[1];

                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.FATAL, string.Format("{0} {1}", ex.Message, ex.StackTrace));
            }

            return count;
        }

        private static void RealCallback()
        {

            double o = sw.ElapsedTicks / swFreq;
            double step = 1 / (double)SamplingRate;

            trdStopped = false;
            try
            {
                while (!trdClosed)
                {
                    Thread.Sleep(0);

                    double el1 = sw.ElapsedTicks / swFreq;
                    if (el1 - o >= step)
                    {
                        if (el1 - o >= step * SamplingRate / 100.0)//閾値10ms
                        {
                            do
                            {
                                o += step;
                            } while (el1 - o >= step);
                        }
                        else
                        {
                            o += step;
                        }

                        OneFrame();
                    }

                }
            }
            catch
            {
            }
            trdStopped = true;
        }

        private static void OneFrame()
        {
            drv.Rendering();
        }

        private static void OPL4Write(ChipDatum dat)
        {
            if (dat != null && dat.addtionalData != null)
            {
                MmlDatum md = (MmlDatum)dat.addtionalData;
                if (md.linePos != null)
                {
                    Log.WriteLine(LogLevel.TRACE, string.Format("! r{0} c{1}"
                        , md.linePos.row
                        , md.linePos.col
                        ));
                }
            }

#if DEBUG
            //if (dat.address == 0x29)
            //Log.WriteLine(LogLevel.INFO, string.Format("FM P{2} Out:Adr[{0:x02}] val[{1:x02}]", (int)dat.address, (int)dat.data, dat.port));
#endif

            switch (device)
            {
                case 0:
                    mds.WriteYMF278B(0, (byte)dat.port, (byte)dat.address, (byte)dat.data);
                    break;
                case 1:
                case 2:
                    rsc.setRegister(dat.port * 0x100 + dat.address, dat.data);
                    break;
            }
        }

        private static void OPNAWaitSend(long elapsed, int size)
        {
            switch (device)
            {
                case 0://EMU
                    return;
                case 1://GIMIC

                    //サイズと経過時間から、追加でウエイトする。
                    int m = Math.Max((int)(size / 20 - elapsed), 0);//20 閾値(magic number)
                    Thread.Sleep(m);

                    //ポートも一応見る
                    int n = nc86ctl.getNumberOfChip();
                    for (int i = 0; i < n; i++)
                    {
                        NIRealChip rc = nc86ctl.getChipInterface(i);
                        if (rc != null)
                        {
                            while ((rc.@in(0x0) & 0x83) != 0)
                                Thread.Sleep(0);
                            while ((rc.@in(0x100) & 0xbf) != 0)
                                Thread.Sleep(0);
                        }
                    }

                    break;
                case 2://SCCI
                    nScci.NSoundInterfaceManager_.sendData();
                    while (!nScci.NSoundInterfaceManager_.isBufferEmpty())
                    {
                        Thread.Sleep(0);
                    }
                    break;
            }
        }

        public class SineWaveProvider16 : WaveProvider16
        {

            public SineWaveProvider16()
            {
            }

            public override int Read(short[] buffer, int offset, int sampleCount)
            {

                return callBack(buffer, offset, sampleCount);

            }

        }
    }
}
