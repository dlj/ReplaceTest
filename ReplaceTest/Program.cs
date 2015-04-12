using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ReplaceTest
{
    class Program
    {
        private static string startMatch = "[Replace";
        private static string endMatch = "]";
        // Avoid random giving back the same values.
        private static Random rand;


        static void Main(string[] args)
        {
            rand = new Random();
            benchmark(100, 5, 2);
            benchmark(100, 200, 20);
            benchmark(100, 300, 50);
            benchmark(100, 400, 75);
            benchmark(100, 500, 100);
            Console.ReadLine();

            // Note. RegEx needs to be compiled, so first iteration is super slow
        }

        private static void benchmark(int iterations, int words, int tokens)
        {
            List<double>[] benchmarkOutput = new List<double>[3] { new List<double>(), new List<double>(), new List<double>() };
            var timer = new Stopwatch();
            timer.Start();
            Console.WriteLine(" ---- Benchmark with {0} iterations, {1} words and {2} tokens ---- ", iterations, words,tokens);

            for (int i = 0; i < iterations; i++)
            {
                var lookupDictionary = new Dictionary<string, string>();
                var originalText = createOriginalText(words, 5, 50);
                var replaceText = createReplaceMarkers(originalText, tokens, ref lookupDictionary);

                benchmarkOutput[0].Add(replaceWithRegEx(replaceText, originalText, lookupDictionary));
                benchmarkOutput[1].Add(replaceWithSplit(replaceText,originalText, lookupDictionary));
                benchmarkOutput[2].Add(replaceWithWhile(replaceText, originalText, lookupDictionary));
            }

            timer.Stop();

            Console.WriteLine("RegEx Fastest : {0}, Slowest : {1}, Avrenge : {2}", benchmarkOutput[0].Min(), benchmarkOutput[0].Max(), benchmarkOutput[0].Average());
            Console.WriteLine("Split Fastest : {0}, Slowest : {1}, Avrenge : {2}", benchmarkOutput[1].Min(), benchmarkOutput[1].Max(), benchmarkOutput[1].Average());
            Console.WriteLine("While Fastest : {0}, Slowest : {1}, Avrenge : {2}", benchmarkOutput[2].Min(), benchmarkOutput[2].Max(), benchmarkOutput[2].Average());

            Console.WriteLine(" ---- Benchmark done! Took {0} seconds ---- ", timer.Elapsed);
           
        }

        private static long replaceWithRegEx(string replaceText, string originalText, Dictionary<string, string> lookupDictionary)
        {
            var finishedText = "";
            var pattern = string.Format(@"(\{0})(.*?)(\{1})", startMatch, endMatch);
            var replaceOffset = 0;
            var timer = new Stopwatch();

            timer.Start();

            MatchCollection matchCollection = Regex.Matches(replaceText, pattern);

            var lastIndex = 0;
            foreach (Match m in matchCollection)
            {
                // Dont really know which method i like the most. No need for "Include last index" in the first replace code.
                // But substring seems to be faster...

                //finishedText = finishedText.Remove(m.Index + replaceOffset, m.Length).Insert(m.Index + replaceOffset, lookupDictionary[m.Value]);
                //replaceOffset += lookupDictionary[m.Value].Length - m.Length;

                finishedText += replaceText.Substring(lastIndex, m.Index - lastIndex) + lookupDictionary[m.Value]; //+ replaceText.Substring(lastIndex,m.Length);
                lastIndex = m.Length + m.Index;
            }
            // Include the last text.
            finishedText += replaceText.Substring(lastIndex, replaceText.Length - lastIndex);

            timer.Stop();

            Debug.Assert(originalText.Equals(finishedText));
            return timer.Elapsed.Ticks;
        }

        private static long replaceWithWhile(string replaceText, string originalText, Dictionary<string, string> lookupDictionary)
        {
            var tempText = replaceText;
            var finishedText = "";
            var timer = new Stopwatch();
            timer.Start();
            var startIndex = -1;
            var lastIndex = 0;
            while ((startIndex = tempText.IndexOf(startMatch, startIndex + 1, StringComparison.Ordinal)) >= 0)
            {
                // Use the index from start match for better performance. 
                var endIndex = tempText.IndexOf(endMatch, startIndex, StringComparison.Ordinal);
                if (endIndex < 0)
                    throw new Exception("Congratulations your code does not work (Or data is crap). No end token found");

                var token = tempText.Substring(startIndex, endIndex - startIndex + 1);

                finishedText += tempText.Substring(lastIndex, startIndex - lastIndex) + lookupDictionary[token];
                lastIndex = startIndex + token.Length;
            }

            finishedText += tempText.Substring(lastIndex);

            timer.Stop();
            Debug.Assert(originalText.Equals(finishedText));

            return timer.Elapsed.Ticks;
        }

        private static long replaceWithSplit(string replaceText, string originalText, Dictionary<string, string> lookupDictionary)
        {
            var finishedText = "";
            var timer = new Stopwatch();
            timer.Start();
            
            var replaceTextSplit = replaceText.Split(new string[] { startMatch }, StringSplitOptions.None);
            var startIndex = 0;
            string replaceToken = "";

            for (int i = 0; i < replaceTextSplit.Length; i++)
            {
                startIndex = replaceTextSplit[i].IndexOf(endMatch,0, StringComparison.Ordinal);
                // If an index has no matches, just add the result and skip the iteration
                if (startIndex < 0)
                {
                    finishedText += replaceTextSplit[i];
                    continue;
                }

                replaceToken = startMatch + replaceTextSplit[i].Substring(0, startIndex + 1);
                finishedText += lookupDictionary[replaceToken] + replaceTextSplit[i].Substring(startIndex + 1, replaceTextSplit[i].Length - startIndex - 1);
               // finishedText += replaceTextSplit[i].Remove(0, startIndex + 1).Insert(0, lookupDictionary[replaceToken]);
            }

            timer.Stop();
                       
            Debug.Assert(originalText.Equals(finishedText));

            return timer.Elapsed.Ticks;
        }

        private static string createOriginalText(int wordCount, int minCharacters, int maxCharacters)
        {
            string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            StringBuilder word = new StringBuilder();
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < wordCount; i++)
            {
                for (int y = 0; y < rand.Next(minCharacters, maxCharacters); ++y)
                    word.Append(chars[rand.Next(chars.Length)]);

                text.Append(word);
                text.Append(" ");
                word.Clear();
            }
            return text.ToString();
        }

        private static string createReplaceMarkers(string text, int markers, ref Dictionary<string, string> lookupDictionary)
        {
            // Split the text up on spaces.
            var dic = text.Split(' ');
            if (dic.Length < markers)
                throw new Exception("It is quite impossible to have more tokens than words");

            for (int i = 0; i < markers; i++)
            {
                // Get a random index and save the old value before we add it.
                var rndIndex = rand.Next(0, dic.Length);

                // If we already replace it, skip. This way we can also parse in texts
                // that has been replaced more times.
                if (dic[rndIndex].StartsWith("[Replace"))
                {
                    i--;
                    continue;
                }

                var replaceMarker = string.Format("[Replace{0}]", i);

                lookupDictionary.Add(replaceMarker, dic[rndIndex]);
                dic[rndIndex] = replaceMarker;
            }

            StringBuilder str = new StringBuilder();

            for (int i = 0; i < dic.Length; i++)
                str.Append(dic[i] + " ");

            // Remove last space.. dont really care about it anyways.
            str.Remove(str.Length - 1, 1);

            return str.ToString();
        }
    }
}
