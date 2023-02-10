using Penguin.Cms.Entities;
using Penguin.Images.Extensions;
using Penguin.Images.Objects;
using Penguin.Persistence.Abstractions.Attributes.Control;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using Penguin.Web.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Drawing = System.Drawing;

namespace Penguin.Cms.Images
{
    [Table("Images")]

    public partial class Image : AuditableEntity
    {
        [DontAllow(DisplayContexts.Any)]
        public virtual ImageData Content { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int ContentHeight { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int ContentWidth { get; set; }

        public DateTime? DateTaken { get; set; }

        [DontAllow(DisplayContexts.List)]
        public string Desc { get; set; }

        public string FileName { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public virtual ImageData Full { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int FullHeight { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int FullWidth { get; set; }

        public bool IsNSFW { get; set; }

        public bool IsVisible { get; set; }

        public string Name { get; set; }

        [DontAllow(DisplayContexts.List)]
        public List<string> Tags
        {
            get => TagString == null ? new List<string>() : TagString.Split(',').ToList();
            set => TagString = string.Join(",", value);
        }

        [DontAllow(DisplayContexts.Edit)]
        public virtual ImageData Thumb { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int ThumbHeight { get; set; }

        [DontAllow(DisplayContexts.Any)]
        public int ThumbWidth { get; set; }

        [NotMapped]
        [DontAllow(DisplayContexts.List | DisplayContexts.Edit)]

        public string Uri
        {
            get => ExternalId;
            set => ExternalId = value;
        }

        [Mapped]
        private string TagString { get; set; }

        public Image()
        {
        }

        public Image(string source)
        {
            Uri = source;
            IsVisible = true;
            FileName = Path.GetFileName(Uri);
        }

        public Image(byte[] array, string fileName)
        {
            Full = new ImageData(array, MimeMappings.GetMimeType(Path.GetExtension(fileName)));
            FileName = fileName;
        }

        public void Refresh()
        {
            bool FromDisk = File.Exists(Uri);
            Bitmap bmp;

            if (FromDisk)
            {
                DateTaken = GetDateTakenFromImage(Uri);
                bmp = new Bitmap(Uri);
                EXIFextractor exif = new(ref bmp, "n"); // get source from http://www.codeproject.com/KB/graphics/exifextractor.aspx?fid=207371

                if (exif["Orientation"] != null)
                {
                    RotateFlipType flip = OrientationToFlipType(exif["Orientation"]?.ToString());

                    // don't flip of orientation is correct
                    if (flip != RotateFlipType.RotateNoneFlipNone)
                    {
                        bmp.RotateFlip(flip);
                        //exif.SetTag(0x112, "1");
                    }
                }

                Full = new ImageData(bmp, MimeMappings.GetMimeType(Path.GetExtension(Uri)));
                FullWidth = Full.Width;
                FullHeight = Full.Height;
            }
            else
            {
                if (Full is null)
                {
                    throw new NullReferenceException("File not saved on disk, but no image data exists within the object");
                }
                using MemoryStream ms = new(Full.Data);
                bmp = new Bitmap(ms);
            }

            Content = new ImageData(bmp.ScaleImage(1080), MimeMappings.GetMimeType(Path.GetExtension(Uri)));
            ContentWidth = Content.Width;
            ContentHeight = Content.Height;

            Thumb = new ImageData(bmp.ScaleImage(200), MimeMappings.GetMimeType(Path.GetExtension(Uri)));
            ThumbWidth = Thumb.Width;
            ThumbHeight = Thumb.Height;

            Name = !string.IsNullOrEmpty(Name) ? Name : Path.GetFileNameWithoutExtension(FileName);

            bmp.Dispose();
        }

        private static DateTime? GetDateTakenFromImage(string path)
        {
            System.Text.RegularExpressions.Regex r = new(":");
            try
            {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
                using Drawing.Image myImage = Drawing.Image.FromStream(fs, false, false);
                try
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return (DateTime)SqlDateTime.MinValue;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static RotateFlipType OrientationToFlipType(string orientation)
        {
            if (orientation is null)
            {
                return RotateFlipType.RotateNoneFlipNone;
            }
            int o = int.Parse(orientation.Substring(0, 1), CultureInfo.CurrentCulture);

            return o switch
            {
                1 => RotateFlipType.RotateNoneFlipNone,
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.Rotate180FlipX,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            };
            ;
        }
    }
}
