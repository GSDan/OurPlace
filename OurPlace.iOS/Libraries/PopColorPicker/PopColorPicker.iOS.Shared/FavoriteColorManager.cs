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
using System.Linq;
using System.IO;
using System.Threading.Tasks;

#if __UNIFIED__
using Foundation;
#else
using MonoTouch.Foundation;
#endif

namespace PopColorPicker.iOS
{
    public class FavoriteColorManager
    {
        private readonly string path;

        public FavoriteColorManager()
        {
            var documents = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)[0];
            path = Path.Combine(documents.Path, "FavoriteColors.txt");
        }

        public void Add(string colorText, bool rewriter = false)
        {
            var fileModel = rewriter == true ? FileMode.Create : FileMode.Append;

            using (var file = new FileStream(path, fileModel, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var writer = new StreamWriter(file))
                {
                     writer.WriteLine(colorText);
                     writer.Flush();
                }
            }
        }

        public List<string> List()
        {
            var list = new List<string>();

            using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(file))
                {
                    var content =  reader.ReadToEnd();
                    list = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }

            return list;
        }

        public  void Delete(string colorText)
        {
            var content = string.Empty;
            var list = new List<string>();

            using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(file))
                {
                    content =  reader.ReadToEnd();

                    list = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    list.Remove(colorText);
                }
            }

            Add(string.Join(Environment.NewLine, list), true);
        }
    }
}

