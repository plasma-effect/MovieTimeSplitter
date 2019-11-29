using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TimeSplitter
{
    static class Program
    {
        static (int, int) SplitTo2Numbers(string str, char separator = ' ')
        {
            var ar = str.Split(separator).Select(int.Parse).ToArray();
            return (ar[0], ar[1]);
        }
        static (int FullLength, int TargetLenth, List<(string Name, int Time)> SplitList, List<(string Name, int Start, int End)> CutsceneList) ReadData(TextReader reader)
        {
            var (length, target) = SplitTo2Numbers(reader.ReadLine());
            var splitList = new List<(string Name, int Time)>
            {
                ("Start",1)
            };
            var cutsceneList = new List<(string Name, int Start, int End)>();
            var S = int.Parse(reader.ReadLine());
            foreach (var _ in Enumerable.Range(0, S))
            {
                var sar = reader.ReadLine().Split(':');
                splitList.Add((sar[0], int.Parse(sar[1])));
            }
            splitList.Add(("Movie End", length + 1));

            var C = int.Parse(reader.ReadLine());
            foreach (var _ in Enumerable.Range(0, C))
            {
                var sar = reader.ReadLine().Split(':');
                var (start, end) = SplitTo2Numbers(sar[1]);
                cutsceneList.Add((sar[0], start, end));
            }
            return (length, target, splitList, cutsceneList);
        }

        static bool IsInternal(int val, int min, int max)
        {
            return min <= val && val <= max;
        }

        static int GetMin(int val)
        {
            var ret = val;
            foreach (var d in Enumerable.Range(2, 15))
            {
                ret = Math.Min(ret, val / d + val % d);
            }
            return ret;
        }

        static void Message(string str)
        {
            Console.Error.WriteLine(str);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Message("ファイル名を入力してください");
                Environment.Exit(1);
            }
            int fullLength, targetLength;
            List<(string Name, int Time)> splitList;
            List<(string Name, int Start, int End)> cutsceneList;

            using (var reader = new StreamReader(args[0]))
            {
                (fullLength, targetLength, splitList, cutsceneList) = ReadData(reader);
            }

            var result = CreateResult(targetLength, splitList, cutsceneList);
            result.Sort();
            if (result.Last().Split + 1 != splitList.Count)
            {
                PrintErrorResult(splitList, cutsceneList, result);
            }
            else
            {
                PrintResult(targetLength, splitList, cutsceneList, result);
            }
        }

        private static void PrintErrorResult(List<(string Name, int Time)> splitList, List<(string Name, int Start, int End)> cutsceneList, List<(int Split, int CSFirst, int CSLast, int Min, int Max)> result)
        {
            Message("正常に処理できませんでした、スプリットが足りていない可能性があります");
            Console.WriteLine($"※参考データです");
            foreach (var (split, csfirst, cslast, min, max) in result)
            {
                Console.WriteLine($"{splitList[split].Name}: {min}-{max}");
                for (var i = csfirst; i < cslast; ++i)
                {
                    Console.WriteLine($"  {cutsceneList[i].Name}: {cutsceneList[i].End - cutsceneList[i].Start}");
                }
            }
            using (var debug = new StreamWriter("debug.txt"))
            {
                foreach (var i in Enumerable.Range(0, splitList.Count))
                {
                    var mins = 0;
                    var sum = 0;
                    var csindex = 0;
                    for (; csindex < cutsceneList.Count; ++csindex)
                    {
                        if (splitList[i].Time <= cutsceneList[csindex].Start)
                        {
                            break;
                        }
                    }
                    debug.WriteLine($"{splitList[i].Name}");
                    for (var j = i + 1; j < splitList.Count; ++j)
                    {
                        var len = splitList[j].Time - splitList[i].Time;
                        for (; csindex < cutsceneList.Count; ++csindex)
                        {
                            if (splitList[j].Time < cutsceneList[csindex].End)
                            {
                                break;
                            }
                            var cslen = cutsceneList[csindex].End - cutsceneList[csindex].Start;
                            mins += GetMin(cslen);
                            sum += cslen;
                        }
                        debug.WriteLine($"  {splitList[j].Name}:{len - sum + mins}-{len}");
                    }
                }
            }
        }

        private static void PrintResult(int targetLength, List<(string Name, int Time)> splitList, List<(string Name, int Start, int End)> cutsceneList, List<(int Split, int CSFirst, int CSLast, int Min, int Max)> result)
        {
            foreach (var (index, first, last, min, max) in result)
            {
                var list = new List<(string CutsceneName, int Start, int End)>();
                for (var i = first; i < last; ++i)
                {
                    list.Add(cutsceneList[i]);
                }
                var len = max;
                var cslist = new List<(string CutsceneName, int Start, int End, int Speed)>();
                if (targetLength <= len)
                {
                    var rest = len - targetLength;
                    var dp = new (string csname, int start, int speed, int length)?[rest + 1];
                    dp[0] = ("", 0, 0, 0);
                    foreach (var (csname, start, end) in list.OrderBy(key => key.End - key.Start).Reverse())
                    {
                        var cslen = end - start;
                        var map = new (int Speed, int Length)?[rest + 1];
                        foreach (var s in Enumerable.Range(2, 15).Reverse())
                        {
                            for (var length = 0; s * length <= cslen; ++length)
                            {
                                var a = (s - 1) * length;
                                if (!map[a].HasValue)
                                {
                                    map[a] = (s, length);
                                }
                            }
                        }
                        for (var i = rest; 0 <= i; --i)
                        {
                            if (!map[i].HasValue)
                            {
                                continue;
                            }
                            for (var j = rest - i; 0 <= j; --j)
                            {
                                if (!dp[i + j].HasValue && dp[j].HasValue && dp[j].Value.csname != csname)
                                {
                                    dp[i + j] = (csname, start, map[i].Value.Speed, map[i].Value.Length);
                                }
                            }
                        }
                    }
                    while (rest != 0)
                    {
                        var (csname, start, sp, length) = dp[rest].Value;
                        cslist.Add((csname, start, start + sp * length, sp));
                        rest -= (sp - 1) * length;
                    }
                }
                else
                {
                    foreach (var (csname, start, end) in list)
                    {
                        cslist.Add((csname, start, end, 1));
                    }
                }
                Console.WriteLine($"{splitList[index].Name}: {splitList[index].Time}");
                foreach (var (csname, start, end, s) in cslist.OrderBy(key => key.Start))
                {
                    Console.WriteLine($"  {csname}: {start}->{end}(speed:{s})");
                }
            }
        }

        private static List<(int Split, int CSFirst, int CSLast, int Min, int Max)> CreateResult(int targetLength, List<(string Name, int Time)> splitList, List<(string Name, int Start, int End)> cutsceneList)
        {
            using (var debug = new StreamWriter("createdebug.txt"))
            {
                var dp = new (int prev, int csfirst, int cslast, int min, int max)?[splitList.Count];
                var queue = new Queue<int>();
                queue.Enqueue(0);
                dp[0] = (-1, 0, 0, 0, 0);
                var last = 0;
                while (last + 1 != splitList.Count)
                {
                    if (queue.Count != 0)
                    {
                        var prev = queue.Dequeue();
                        last = Math.Max(last, prev);
                        var (_, _, csfirst, _, _) = dp[prev].Value;
                        for (; csfirst < cutsceneList.Count; ++csfirst)
                        {
                            if (splitList[prev].Time <= cutsceneList[csfirst].Start)
                            {
                                break;
                            }
                        }
                        var csindex = csfirst;
                        var mins = 0;
                        var sum = 0;
                        for (var i = prev + 1; i < splitList.Count; ++i)
                        {
                            var len = splitList[i].Time - splitList[prev].Time;
                            for (; csindex < cutsceneList.Count; ++csindex)
                            {
                                if (splitList[i].Time < cutsceneList[csindex].End)
                                {
                                    break;
                                }
                                var cslen = cutsceneList[csindex].End - cutsceneList[csindex].Start;
                                mins += GetMin(cslen);
                                sum += cslen;
                            }
                            if (targetLength < len - sum + mins)
                            {
                                break;
                            }
                            if (!dp[i].HasValue && (IsInternal(targetLength, len - sum + mins, len) || i + 1 == splitList.Count))
                            {
                                debug.WriteLine($"{splitList[i].Name}: {len - sum + mins}-{len}");
                                dp[i] = (prev, csfirst, csindex, len - sum + mins, len);
                                queue.Enqueue(i);
                            }
                        }
                    }
                    else
                    {
                        var (_, _, csfirst, _, _) = dp[last].Value;
                        for (; csfirst < cutsceneList.Count; ++csfirst)
                        {
                            if (splitList[last].Time <= cutsceneList[csfirst].Start)
                            {
                                break;
                            }
                        }
                        var csindex = csfirst;
                        var mins = 0;
                        var sum = 0;
                        var prevcsindex = csindex;
                        var prevmins = 0;
                        var prevsum = 0;
                        for (var i = last + 1; i < splitList.Count; ++i)
                        {
                            var len = splitList[i].Time - splitList[last].Time;
                            for (; csindex < cutsceneList.Count; ++csindex)
                            {
                                if (splitList[i].Time < cutsceneList[csindex].End)
                                {
                                    break;
                                }
                                var cslen = cutsceneList[csindex].End - cutsceneList[csindex].Start;
                                mins += GetMin(cslen);
                                sum += cslen;
                            }
                            if (targetLength < len - sum + mins && i - 1 != last) 
                            {
                                var prevlen = splitList[i - 1].Time - splitList[last].Time;
                                dp[i - 1] = (last, csfirst, prevcsindex, prevlen - prevsum + prevmins, prevlen);
                                queue.Enqueue(i - 1);
                            }
                            prevcsindex = csindex;
                            prevmins = mins;
                            prevsum = sum;
                        }
                        if (queue.Count == 0)
                        {
                            break;
                        }
                    }
                }

                var ret = new List<(int Split, int CSFirst, int CSLast, int Min, int Max)>();
                while (last != 0)
                {
                    var (prev, csfirst, cslast, min, max) = dp[last].Value;
                    ret.Add((last, csfirst, cslast, min, max));
                    last = prev;
                }
                return ret;
            }
        }
    }
}
