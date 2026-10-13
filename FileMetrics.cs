using System;
using System.IO;

internal static class FileMetrics
{
    
    internal static double Entropy(string path)
    {
        try
        {
            byte[] b = new byte[16384]; int n;
            using (var fs = File.OpenRead(path)) n = fs.Read(b, 0, b.Length);
            if (n < 1024) return -1;
            int[] counts = new int[256];
            for (int i = 0; i < n; i++) counts[b[i]]++;
            double h = 0;
            for (int i = 0; i < 256; i++) if (counts[i] > 0) { double p = (double)counts[i] / n; h -= p * Math.Log(p, 2); }
            return h;
        }
        catch { return -1; }
    }
}
