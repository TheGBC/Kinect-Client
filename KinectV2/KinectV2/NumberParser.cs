using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KinectV2 {
  class NumberParser {
    private static readonly Dictionary<string, int> NUMS = new Dictionary<string, int>() {
      {"one", 1}, {"won", 1},
      {"two", 2}, {"too", 2}, {"to", 2},
      {"three", 3}, {"tree", 3},
      {"four", 4}, {"for", 4},
      {"five", 5},
      {"six", 6},
      {"seven", 7},
      {"eight", 8}, {"ate", 8},
      {"nine", 9},
      {"ten", 10},
      {"zero", 0}, {"oh", 0}, {"owe", 0},
      {"eleven", 11}, {"twelve", 12}, {"thirteen", 13},
      {"fourteen", 14}, {"fiftteen", 15}, {"sixteen", 16},
      {"seventeen", 17}, {"eighteen", 18}, {"nineteen", 19},
      {"twenty", 20}, {"thirty", 30}, {"forty", 40},
      {"fifty", 50}, {"sixty", 60}, {"seventy", 70},
      {"eighty", 80}, {"ninety", 90},
    };

    // Regex that will make your eyes bleed, but works very well
    private static readonly string regexMagic = @"(?<n>negative|minus)?(\\s)?"
+ "(((?<h_both>(twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)(\\s)(one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine))|"
+ "(?<h_tenonly>twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)|"
+ "(?<h_oneonly>one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine))|"
+ "(?<h_teen>ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen))?(\\s)?"
+ "(?<h>hundred)?(\\s)?(and)?(\\s)?"
+ "(((?<both>(twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|oh|owe)(\\s)(one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine))|"
+ "(?<tenonly>twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)|"
+ "(?<oneonly>one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine))|"
+ "(?<teen>ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen))?(\\s)?"
+ "(?<dec>point\\s((?<dec_n>one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine|zero|oh|owe)(\\s))*"
+ "(?<dec_f>one|won|two|too|to|three|tree|four|for|five|six|seven|eight|ate|nine|zero|oh|owe))?";

    private NumberParser() { }
    public static float? parseNumber(string number) {
      Console.WriteLine(number);
      if (Regex.IsMatch(number, regexMagic)) {
        float val = 0;
        Match m = Regex.Match(number, regexMagic);
        Group n = m.Groups["n"];
        Group h_both = m.Groups["h_both"];
        Group h_tenonly = m.Groups["h_tenonly"];
        Group h_oneonly = m.Groups["h_oneonly"];
        Group h_teen = m.Groups["h_teen"];
        Group h = m.Groups["h"];
        Group both = m.Groups["both"];
        Group tenonly = m.Groups["tenonly"];
        Group oneonly = m.Groups["oneonly"];
        Group teen = m.Groups["teen"];
        Group dec = m.Groups["dec"];
        Group dec_n = m.Groups["dec_n"];
        Group dec_f = m.Groups["dec_f"];

        bool hasUnder100 = both.Success || tenonly.Success || oneonly.Success || teen.Success;

        if (h_both.Success) {
          string[] parts = h_both.Value.ToString().Trim().Split(' ');
          if (hasUnder100 || h.Success) {
            val += 100 * (NUMS[parts[0]] + NUMS[parts[1]]);
          } else {
            val += (NUMS[parts[0]] + NUMS[parts[1]]);
          }
        } else if (h_tenonly.Success) {
          if (hasUnder100 || h.Success) {
            val += 100 * NUMS[h_tenonly.Value.ToString().Trim()];
          } else {
            val += NUMS[h_tenonly.Value.ToString().Trim()];
          }
        } else if (h_oneonly.Success) {
          if (hasUnder100 || h.Success) {
            val += 100 * NUMS[h_oneonly.Value.ToString().Trim()];
          } else {
            val += NUMS[h_oneonly.Value.ToString().Trim()];
          }
        } else if (h_teen.Success) {
          if (hasUnder100 || h.Success) {
            val += 100 * NUMS[h_teen.Value.ToString().Trim()];
          } else {
            val += NUMS[h_teen.Value.ToString().Trim()];
          }
        } else if (h.Success) {
          val += 100;
        }

        if (both.Success) {
          string[] parts = both.Value.ToString().Trim().Split(' ');
          val += (NUMS[parts[0]] + NUMS[parts[1]]);
        } else if (tenonly.Success) {
          val += NUMS[tenonly.Value.ToString().Trim()];
        } else if (oneonly.Success) {
          val += NUMS[oneonly.Value.ToString().Trim()];
        } else if (teen.Success) {
          val += NUMS[teen.Value.ToString().Trim()];
        }

        if (dec.Success) {
          float shift = .1f;
          foreach (Capture capture in dec_n.Captures) {
            val += shift * NUMS[capture.Value.ToString().Trim()];
            shift /= 10;
          }
          val += shift * NUMS[dec_f.Value.ToString()];
        }
        return val * (n.Success ? -1 : 1);
      }
      return null;
    }
  }
}
