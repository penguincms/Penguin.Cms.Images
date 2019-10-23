using Penguin.Images.Extensions;
using Penguin.Persistence.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;

namespace Penguin.Cms.Images
{
    public class ImageData : KeyedObject
    {
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
        public byte[] Data { get; set; } = System.Array.Empty<byte>();

        public int Height { get; set; }

        public string Mime { get; set; } = string.Empty;

        public int Width { get; set; }

        public ImageData()
        {
        }

        public ImageData(System.Drawing.Image image, string mime)
        {
            if (image is null)
            {
                throw new System.ArgumentNullException(nameof(image));
            }

            this.Data = image.ToByteArray();
            this.Mime = mime;
            this.Height = image.Height;
            this.Width = image.Width;
        }

        public ImageData(byte[] image, string mime)
        {
            using (MemoryStream ms = new MemoryStream(image))
            {
                Bitmap bmp = new Bitmap(ms);

                this.Data = image;
                this.Mime = mime;
                this.Height = bmp.Height;
                this.Width = bmp.Width;
            }
        }
    }
}