using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSDecoder.Decoder
{
    public class ReplayDecoding
    {
        internal class Offsets
        {
            internal int replayInfo;
            internal int zcYpPUYq3RAHg;
            internal int zJbk_r2VWJKCY;
            internal int zuWYxiBCiOOO;
            internal int zWVthFsNiRgp;
            internal int zjjCMbAVve_UJ;
            internal int zVWDENm4dsje6Q2VyAc0ji18;
            internal int zKQ4Y1J0ZmL3z7FdHXw;
            internal int zXAxGrPnBYXhi;
        }

        public class z4WxJzKJOoJtsHS8jj66mKkiYLIpACnYQ
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class ReplayInfoo
        {
            public string version { get; set; }
            public string hash { get; set; }
            public int difficulty { get; set; }
            public string mode { get; set; }
            public string environment { get; set; }
            public string[] modifiers { get; set; }
            public float noteJumpStartBeatOffset { get; set; }
            public bool leftHanded { get; set; }
            public float height { get; set; }
            public float rr { get; set; }
            public z4WxJzKJOoJtsHS8jj66mKkiYLIpACnYQ room { get; set; }
            public float st { get; set; }
            public int totalScore { get; set; }
            public float midDeviation { get; set; }
        }

        public struct ThreeFloats
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
        }

        public class FourFloats
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public float w { get; set; }
        }

        public struct Coordinates
        {
            public ThreeFloats p { get; set; }
            public FourFloats r { get; set; }
        }

        public class Frame
        {
            public Coordinates h { get; set; }
            public Coordinates l { get; set; }
            public Coordinates r { get; set; }
            public int i { get; set; }
            public float a { get; set; }
        }

        public class TwoFloat
        {
            public float h { get; set; }
            public float a { get; set; }
        }

        public struct Mathf
        {
            public static readonly float Epsilon = Mathf.MathfInternal.IsFlushToZeroEnabled ? Mathf.MathfInternal.FloatMinNormal : Mathf.MathfInternal.FloatMinDenormal;

            public static bool Approximately(float a, float b) => (double)Math.Abs(b - a) < (double)Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), Mathf.Epsilon * 8f);

            public static int RoundToInt(float f) => (int)Math.Round((double)f);

            public static float Clamp01(float value)
            {
                if ((double)value < 0.0)
                    return 0.0f;
                return (double)value <= 1.0 ? value : 1f;
            }

            public struct MathfInternal
            {
                public static volatile float FloatMinNormal = 1.175494E-38f;
                public static volatile float FloatMinDenormal = float.Epsilon;
                public static bool IsFlushToZeroEnabled = (double)Mathf.MathfInternal.FloatMinDenormal == 0.0;
            }
        }

        public struct DontKnow : IEquatable<DontKnow>
        {
            internal float songTime;
            internal int noteLineLayer;
            internal int lineIndex;
            internal int colorType;
            internal int cutDirection;

            public static bool operator ==(DontKnow left, DontKnow right) => Mathf.Approximately(left.songTime, right.songTime) && left.noteLineLayer == right.noteLineLayer && left.lineIndex == right.lineIndex && left.colorType == right.colorType && left.cutDirection == right.cutDirection;

            public static bool operator !=(DontKnow left, DontKnow right) => !(left == right);

            public override int GetHashCode() => this.songTime.GetHashCode() ^ this.noteLineLayer ^ this.lineIndex;

            public override bool Equals(object other) => this.Equals((DontKnow)other);

            public bool Equals(DontKnow other) => this == other;
        }

        public class SomethingBig
        {
            internal DontKnow noteData { get; set; }
            internal int type { get; set; }
            internal ThreeFloats cutPoint { get; set; }
            internal ThreeFloats cutNormal { get; set; }
            internal ThreeFloats saberDir { get; set; }
            internal int saberType { get; set; }
            internal bool directionOK { get; set; }
            internal float saberSpeed { get; set; }
            internal float cutAngle { get; set; }
            internal float cutDistanceToCenter { get; set; }
            internal float cutDirDeviation { get; set; }
            internal float beforeCutRating { get; set; }
            internal float afterCutRating { get; set; }
            internal float songTime { get; set; }
            internal float timeScale { get; set; }
            internal float timeScale2 { get; set; }
            internal int combo { get; set; }
        }

        public struct IntAndFloat
        {
            public int i { get; set; }
            public float a { get; set; }
        }

        public struct IntAndFloatFloat
        {
            internal int zKIFPqEA { get; set; }
            internal float z5MXGKIKoiUFk { get; set; }
            internal float zBiObSEA { get; set; }
        }

        public class Result
        {
            public ReplayInfoo info { get; set; }
            public List<Frame> frames { get; set; }
            public List<int> scores { get; set; }
            public List<int> combos { get; set; }
            public List<float> noteTime { get; set; }
            public List<string> noteInfos { get; set; }
            public List<TwoFloat> dynamicHeight { get; set; }
        }

        private byte[] buffer;
        private Offsets offsets;
        private ReplayInfoo info;
        private List<Frame> frames;
        private List<TwoFloat> automaticHeight;
        private List<SomethingBig> thirdArray;
        private List<IntAndFloat> fourthArray;
        private List<IntAndFloat> fifthArray;
        private List<IntAndFloatFloat> sixArray;
        private List<TwoFloat> seventhArray;

        public ReplayDecoding(byte[] buffer) => this.buffer = buffer;

        public Result Decode()
        {
            this.offsets = this.decodeOffsets();
            this.info = this.decodeInfo();
            this.frames = this.decodeFrames();
            this.automaticHeight = this.decodeFFArray(this.offsets.zJbk_r2VWJKCY);
            this.thirdArray = this.decodeThirdArray();
            this.fourthArray = this.decodeIAFArray(this.offsets.zWVthFsNiRgp);
            this.fifthArray = this.decodeIAFArray(this.offsets.zjjCMbAVve_UJ);
            this.sixArray = this.decodeIAFFArray(this.offsets.zVWDENm4dsje6Q2VyAc0ji18);
            this.seventhArray = this.decodeFFArray(this.offsets.zKQ4Y1J0ZmL3z7FdHXw);
            this.info.totalScore = this.fourthArray.Last<IntAndFloat>().i;
            Result result = new Result();
            result.frames = this.frames;
            List<int> intList1 = new List<int>();
            List<int> intList2 = new List<int>();
            List<float> floatList = new List<float>();
            List<string> stringList = new List<string>();
            List<SomethingBig> thirdArray = this.thirdArray;
            List<IntAndFloat> intAndFloatList = new List<IntAndFloat>((IEnumerable<IntAndFloat>)this.fifthArray);
            for (int index1 = this.fifthArray.Count - 1; index1 >= 0; --index1)
            {
                for (int index2 = 0; index2 < thirdArray.Count; ++index2)
                {
                    if ((double)thirdArray[index2].songTime == (double)this.fifthArray[index1].a && thirdArray[index2].combo == -1)
                    {
                        thirdArray[index2].combo = this.fifthArray[index1].i;
                        intAndFloatList.RemoveAt(index1);
                        break;
                    }
                }
            }
            float num1 = 0.0f;
            int num2 = 0;
            for (int index = 0; index < thirdArray.Count; ++index)
            {
                if (thirdArray[index].combo == -1)
                    thirdArray[index].combo = num2;
                else if ((double)thirdArray[index].songTime > (double)num1)
                {
                    num2 = thirdArray[index].combo;
                    num1 = thirdArray[index].songTime;
                }
            }
            thirdArray.Sort((Comparison<SomethingBig>)((x, y) => x.noteData.songTime.CompareTo(y.noteData.songTime)));
            for (int index = 0; index < thirdArray.Count; ++index)
            {
                SomethingBig somethingBig = thirdArray[index];
                int num3 = Mathf.RoundToInt(70f * somethingBig.beforeCutRating);
                int num4 = Mathf.RoundToInt(30f * somethingBig.afterCutRating);
                int num5 = Mathf.RoundToInt(15f * (1f - Mathf.Clamp01(somethingBig.cutDistanceToCenter / 0.3f)));
                if (somethingBig.type == 1)
                    intList1.Add(num3 + num4 + num5);
                else
                    intList1.Add(-somethingBig.type);
                intList2.Add(somethingBig.combo >= 0 ? somethingBig.combo : 1);
                floatList.Add((float)Math.Round((double)somethingBig.songTime, 5));
                stringList.Add(somethingBig.noteData.lineIndex.ToString() + somethingBig.noteData.noteLineLayer.ToString() + somethingBig.noteData.cutDirection.ToString() + somethingBig.noteData.colorType.ToString());
            }
            result.info = this.info;
            for (int index = 0; index < intAndFloatList.Count; ++index)
            {
                intList1.Add(-5);
                intList2.Add(intAndFloatList[index].i);
                floatList.Add(intAndFloatList[index].a);
            }
            result.scores = intList1;
            result.combos = intList2;
            result.noteTime = floatList;
            result.noteInfos = stringList;
            result.dynamicHeight = this.automaticHeight;
            return result;
        }

        private Offsets decodeOffsets()
        {
            int pointer = 0;
            return new Offsets()
            {
                replayInfo = this.DecodeInt(ref pointer),
                zcYpPUYq3RAHg = this.DecodeInt(ref pointer),
                zJbk_r2VWJKCY = this.DecodeInt(ref pointer),
                zuWYxiBCiOOO = this.DecodeInt(ref pointer),
                zWVthFsNiRgp = this.DecodeInt(ref pointer),
                zjjCMbAVve_UJ = this.DecodeInt(ref pointer),
                zVWDENm4dsje6Q2VyAc0ji18 = this.DecodeInt(ref pointer),
                zKQ4Y1J0ZmL3z7FdHXw = this.DecodeInt(ref pointer),
                zXAxGrPnBYXhi = this.DecodeInt(ref pointer)
            };
        }

        private ReplayInfoo decodeInfo()
        {
            ReplayInfoo replayInfoo = new ReplayInfoo();
            int replayInfo = this.offsets.replayInfo;
            replayInfoo.version = this.DecodeString(ref replayInfo);
            replayInfoo.hash = this.DecodeString(ref replayInfo);
            replayInfoo.difficulty = this.DecodeInt(ref replayInfo);
            replayInfoo.mode = this.DecodeString(ref replayInfo);
            replayInfoo.environment = this.DecodeString(ref replayInfo);
            replayInfoo.modifiers = this.DecodeStringArray(ref replayInfo);
            replayInfoo.noteJumpStartBeatOffset = this.DecodeFloat(ref replayInfo);
            replayInfoo.leftHanded = this.DecodeBool(ref replayInfo);
            replayInfoo.height = this.DecodeFloat(ref replayInfo);
            replayInfoo.rr = this.DecodeFloat(ref replayInfo);
            replayInfoo.room = this.decodeHZ1(ref replayInfo);
            replayInfoo.st = this.DecodeFloat(ref replayInfo);
            return replayInfoo;
        }

        private z4WxJzKJOoJtsHS8jj66mKkiYLIpACnYQ decodeHZ1(
          ref int pointer)
        {
            return new z4WxJzKJOoJtsHS8jj66mKkiYLIpACnYQ()
            {
                x = this.DecodeFloat(ref pointer),
                y = this.DecodeFloat(ref pointer),
                z = this.DecodeFloat(ref pointer)
            };
        }

        private List<Frame> decodeFrames()
        {
            int zcYpPuYq3RaHg = this.offsets.zcYpPUYq3RAHg;
            int num = this.DecodeInt(ref zcYpPuYq3RaHg);
            List<Frame> frameList = new List<Frame>();
            for (int index = 0; index < num; ++index)
                frameList.Add(this.DecodeFrame(ref zcYpPuYq3RaHg));
            return frameList;
        }

        private Frame DecodeFrame(ref int pointer) => new Frame()
        {
            h = this.Decode34(ref pointer),
            l = this.Decode34(ref pointer),
            r = this.Decode34(ref pointer),
            i = this.DecodeInt(ref pointer),
            a = this.DecodeFloat(ref pointer)
        };

        private Coordinates Decode34(ref int pointer) => new Coordinates()
        {
            p = this.Decode3(ref pointer),
            r = this.Decode4(ref pointer)
        };

        private ThreeFloats Decode3(ref int pointer) => new ThreeFloats()
        {
            x = this.DecodeFloat(ref pointer),
            y = this.DecodeFloat(ref pointer),
            z = this.DecodeFloat(ref pointer)
        };

        private FourFloats Decode4(ref int pointer) => new FourFloats()
        {
            x = this.DecodeFloat(ref pointer),
            y = this.DecodeFloat(ref pointer),
            z = this.DecodeFloat(ref pointer),
            w = this.DecodeFloat(ref pointer)
        };

        private List<TwoFloat> decodeFFArray(int startPointer)
        {
            int pointer = startPointer;
            int num = this.DecodeInt(ref pointer);
            List<TwoFloat> twoFloatList = new List<TwoFloat>();
            for (int index = 0; index < num; ++index)
                twoFloatList.Add(this.Decode2(ref pointer));
            return twoFloatList;
        }

        private TwoFloat Decode2(ref int pointer) => new TwoFloat()
        {
            h = this.DecodeFloatBig(ref pointer),
            a = this.DecodeFloatBig(ref pointer)
        };

        private DontKnow DecodeDK(ref int pointer) => new DontKnow()
        {
            songTime = this.DecodeFloat(ref pointer),
            noteLineLayer = this.DecodeInt(ref pointer),
            lineIndex = this.DecodeInt(ref pointer),
            colorType = this.DecodeInt(ref pointer),
            cutDirection = this.DecodeInt(ref pointer)
        };

        private List<SomethingBig> decodeThirdArray()
        {
            int zuWyxiBciOoo = this.offsets.zuWYxiBCiOOO;
            int num = this.DecodeInt(ref zuWyxiBciOoo);
            List<SomethingBig> somethingBigList = new List<SomethingBig>();
            for (int index = 0; index < num; ++index)
                somethingBigList.Add(this.DecodeSomethingBig(ref zuWyxiBciOoo));
            return somethingBigList;
        }

        private SomethingBig DecodeSomethingBig(ref int pointer) => new SomethingBig()
        {
            noteData = this.DecodeDK(ref pointer),
            type = this.DecodeInt(ref pointer),
            cutPoint = this.Decode3(ref pointer),
            cutNormal = this.Decode3(ref pointer),
            saberDir = this.Decode3(ref pointer),
            saberType = this.DecodeInt(ref pointer),
            directionOK = this.DecodeBool(ref pointer),
            saberSpeed = this.DecodeFloat(ref pointer),
            cutAngle = this.DecodeFloatBig(ref pointer),
            cutDistanceToCenter = this.DecodeFloatBig(ref pointer),
            cutDirDeviation = this.DecodeFloatBig(ref pointer),
            beforeCutRating = this.DecodeFloatBig(ref pointer),
            afterCutRating = this.DecodeFloatBig(ref pointer),
            songTime = this.DecodeFloatBig(ref pointer),
            timeScale = this.DecodeFloat(ref pointer),
            timeScale2 = this.DecodeFloat(ref pointer),
            combo = -1
        };

        private List<IntAndFloat> decodeIAFArray(int startPointer)
        {
            int pointer = startPointer;
            int num = this.DecodeInt(ref pointer);
            List<IntAndFloat> intAndFloatList = new List<IntAndFloat>();
            for (int index = 0; index < num; ++index)
                intAndFloatList.Add(new IntAndFloat()
                {
                    i = this.DecodeInt(ref pointer),
                    a = this.DecodeFloatBig(ref pointer)
                });
            return intAndFloatList;
        }

        private List<IntAndFloatFloat> decodeIAFFArray(int startPointer)
        {
            int pointer = startPointer;
            int num = this.DecodeInt(ref pointer);
            List<IntAndFloatFloat> intAndFloatFloatList = new List<IntAndFloatFloat>();
            for (int index = 0; index < num; ++index)
                intAndFloatFloatList.Add(new IntAndFloatFloat()
                {
                    zKIFPqEA = this.DecodeInt(ref pointer),
                    z5MXGKIKoiUFk = this.DecodeFloat(ref pointer),
                    zBiObSEA = this.DecodeFloat(ref pointer)
                });
            return intAndFloatFloatList;
        }

        private int DecodeInt(ref int pointer)
        {
            int int32 = BitConverter.ToInt32(this.buffer, pointer);
            pointer += 4;
            return int32;
        }

        public string DecodeString(ref int pointer)
        {
            int int32 = BitConverter.ToInt32(this.buffer, pointer);
            string str = Encoding.UTF8.GetString(this.buffer, pointer + 4, int32);
            pointer += int32 + 4;
            return str;
        }

        private string[] DecodeStringArray(ref int pointer)
        {
            int length = this.DecodeInt(ref pointer);
            string[] strArray = new string[length];
            for (int index = 0; index < length; ++index)
                strArray[index] = this.DecodeString(ref pointer);
            return strArray;
        }

        private float DecodeFloat(ref int pointer)
        {
            float single = BitConverter.ToSingle(this.buffer, pointer);
            pointer += 4;
            return single;
        }

        private float DecodeFloatBig(ref int pointer)
        {
            float single = BitConverter.ToSingle(this.buffer, pointer);
            pointer += 4;
            return single;
        }

        private bool DecodeBool(ref int pointer)
        {
            bool boolean = BitConverter.ToBoolean(this.buffer, pointer);
            ++pointer;
            return boolean;
        }
    }
}
