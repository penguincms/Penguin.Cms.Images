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
            get => this.TagString == null ? new List<string>() : this.TagString.Split(',').ToList();
            set => this.TagString = string.Join(",", value);
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
            get => this.ExternalId;
            set => this.ExternalId = value;
        }

        [Mapped]
        private string TagString { get; set; }

        public Image()
        {
        }

        public Image(string source)
        {
            this.Uri = source;
            this.IsVisible = true;
            this.FileName = Path.GetFileName(this.Uri);
        }

        public Image(byte[] array, string fileName)
        {
            this.Full = new ImageData(array, MimeMappings.GetMimeType(Path.GetExtension(fileName)));
            this.FileName = fileName;
        }

        public void Refresh()
        {
            bool FromDisk = File.Exists(this.Uri);
            Bitmap bmp;

            if (FromDisk)
            {
                this.DateTaken = GetDateTakenFromImage(this.Uri);
                bmp = new Bitmap(this.Uri);
                EXIFextractor exif = new EXIFextractor(ref bmp, "n"); // get source from http://www.codeproject.com/KB/graphics/exifextractor.aspx?fid=207371

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

                this.Full = new ImageData(bmp, MimeMappings.GetMimeType(Path.GetExtension(this.Uri)));
                this.FullWidth = this.Full.Width;
                this.FullHeight = this.Full.Height;
            }
            else
            {
                if (this.Full is null)
                {
                    throw new NullReferenceException("File not saved on disk, but no image data exists within the object");
                }
                using (MemoryStream ms = new MemoryStream(this.Full.Data))
                {
                    bmp = new Bitmap(ms);
                }
            }

            this.Content = new ImageData(bmp.ScaleImage(1080), MimeMappings.GetMimeType(Path.GetExtension(this.Uri)));
            this.ContentWidth = this.Content.Width;
            this.ContentHeight = this.Content.Height;

            this.Thumb = new ImageData(bmp.ScaleImage(200), MimeMappings.GetMimeType(Path.GetExtension(this.Uri)));
            this.ThumbWidth = this.Thumb.Width;
            this.ThumbHeight = this.Thumb.Height;

            this.Name = !string.IsNullOrEmpty(this.Name) ? this.Name : Path.GetFileNameWithoutExtension(this.FileName);

            bmp.Dispose();
        }

        private static DateTime? GetDateTakenFromImage(string path)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(":");
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (Drawing.Image myImage = Drawing.Image.FromStream(fs, false, false))
                    {
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

            switch (o)
            {
                case 1:
                    return RotateFlipType.RotateNoneFlipNone;

                case 2:
                    return RotateFlipType.RotateNoneFlipX;

                case 3:
                    return RotateFlipType.Rotate180FlipNone;

                case 4:
                    return RotateFlipType.Rotate180FlipX;

                case 5:
                    return RotateFlipType.Rotate90FlipX;

                case 6:
                    return RotateFlipType.Rotate90FlipNone;

                case 7:
                    return RotateFlipType.Rotate270FlipX;

                case 8:
                    return RotateFlipType.Rotate270FlipNone;

                default:
                    return RotateFlipType.RotateNoneFlipNone;
            };
        }
    }
}