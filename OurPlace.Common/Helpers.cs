#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Web;

namespace OurPlace.Common
{
    public static class Helpers
    {
        public static string GetUrlParam(Uri uri, string query)
        {
            return HttpUtility.ParseQueryString(uri.Query).Get(query);
        }

        public static int AppVersionNumber = 38;

        public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static bool AlmostEquals(this double double1, double double2, double precision)
        {
            return (Math.Abs(double1 - double2) <= precision);
        }

        public static string DecToDMS(double coord)
        {
            coord = coord > 0 ? coord : -coord;  // -105.9876543 -> 105.9876543
            string sOut = (int)coord + "/1,";   // 105/1,
            coord = (coord % 1) * 60;         // .987654321 * 60 = 59.259258
            sOut = sOut + (int)coord + "/1,";   // 105/1,59/1,
            coord = (coord % 1) * 60000;             // .259258 * 60000 = 15555
            sOut = sOut + (int)coord + "/1000";   // 105/1,59/1,15555/1000
            return sOut;
        }

        public static string Truncate(string source, int length)
        {
            if (source == null || source.Length < length)
            {
                return source;
            }
            int nextSpace = source.LastIndexOf(" ", length, StringComparison.Ordinal);
            return string.Format("{0}...", source.Substring(0, (nextSpace > 0) ? nextSpace : length).Trim());
        }
    }
}
