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
            _SizeBytes = long.Parse(value);
            _Rate = rate;
            _Formated = FormatFileSize(_SizeBytes, _Rate);
        }

        public FileSize(long value, string rate = "")
        {
            _SizeBytes = value;
            _Rate = rate;
            _Formated = FormatFileSize(_SizeBytes, _Rate);
        }

        public long SizeBytes
        {
            get { return _SizeBytes; }
        }

        private long _SizeBytes = 0;
        private string _Rate;
        private string _Formated;

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
                return this._SizeBytes.CompareTo(other._SizeBytes);
            else
                throw new ArgumentException("Object is not a FileSize");
        }

        public override string ToString()
        {
            return _Formated;
        }

        public static FileSize operator + (FileSize lhs, FileSize rhs)
        {
            return new FileSize(lhs._SizeBytes + rhs._SizeBytes, lhs._Rate);
        }
    }
}
