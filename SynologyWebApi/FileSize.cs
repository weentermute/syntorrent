using System;

namespace SynologyWebApi
{
    /// <summary>
    /// Helper class to represent human readable and sortable file sizes.
    /// </summary>
    public class FileSize : IComparable
    {
        public FileSize(string value, string rate = "")
        {
            SizeBytes = long.Parse(value);
            Rate = rate;
        }

        public FileSize(long value, string rate = "")
        {
            SizeBytes = value;
            Rate = rate;
        }

        public long SizeBytes = 0;
        public string Rate;

        private static string[] sizes = { "B", "KB", "MB", "GB" };
        public static string FormatFileSize(long value, string rate)
        {
            double len = value;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0.##} {1}{2}", len, sizes[order], rate);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            FileSize other = obj as FileSize;
            if (other != null)
                return this.SizeBytes.CompareTo(other.SizeBytes);
            else
                throw new ArgumentException("Object is not a FileSize");
        }

        public override string ToString()
        {
            return FormatFileSize(SizeBytes, Rate);
        }
    }
}
