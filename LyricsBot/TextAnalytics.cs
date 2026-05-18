using System;
using System.Linq;

namespace SpotifyLyricsBot
{
    public static class TextAnalytics
    {
        // Твій алгоритм Левенштейна
        public static int CalculateLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 99;
            int n = s.Length, m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (t[j - 1] == s[i - 1] ? 0 : 1));
            return d[n, m];
        }

        // Твій просунутий алгоритм визначення мови
        public static string DetectActualLanguage(string text, string claimedLanguage)
        {
            if (string.IsNullOrEmpty(text)) return claimedLanguage;

            int latin = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == 'é' || c == 'ñ' || c == 'ö');
            int cyrillic = text.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'і' || c == 'ї' || c == 'є' || c == 'ґ');
            int asian = text.Count(c => (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3040 && c <= 0x30FF) || (c >= 0xAC00 && c <= 0xD7AF));

            if (asian > 10)
            {
                string[] asianLangs = { "японська", "корейська", "китайська" };
                if (asianLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "азійська (японська/корейська/китайська)";
            }

            if (latin > cyrillic * 2)
            {
                string[] europeanLangs = { "французька", "німецька", "іспанська", "італійська", "польська", "турецька", "португальська", "шведська", "фінська", "нідерландська", "чеська", "румунська", "угорська", "данська", "норвезька", "словацька", "хорватська" };
                if (europeanLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "англійська";
            }

            if (cyrillic > latin * 2)
            {
                string[] cyrillicLangs = { "російська", "білоруська", "болгарська", "сербська" };
                if (cyrillicLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "українська";
            }

            return claimedLanguage;
        }
    }
}