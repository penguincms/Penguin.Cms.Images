using Penguin.Images.Extensions;
using Penguin.Persistence.Abstractions;
using System.Drawing;
using System.IO;

namespace Penguin.Cms.Images
{
    public class ImageData : KeyedObject
    {
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

            Data = image.ToByteArray();
            Mime = mime;
            Height = image.Height;
            Width = image.Width;
        }

        public ImageData(byte[] image, string mime)
        {
            using MemoryStream ms = new(image);
            Bitmap bmp = new(ms);

            Data = image;
            Mime = mime;
            Height = bmp.Height;
            Width = bmp.Width;
        }
    }
}