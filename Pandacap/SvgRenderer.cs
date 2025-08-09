using SkiaSharp;
using Svg.Skia;

namespace Pandacap
{
    public class SvgRenderer()
    {
        public void RenderPng(Stream input, Stream output)
        {
            using var svg = new SKSvg();
            using var picture = svg.Load(input)!;

            var scale = 1f;
            var width = picture.CullRect.Width;
            var height = picture.CullRect.Height;

            var longest = Math.Max(width, height);
            if (longest > 1200)
            {
                scale = 1200f / longest;
                width *= scale;
                height *= scale;
            }

            using var bitmap = new SKBitmap((int)width, (int)height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            canvas.Scale(scale);
            canvas.DrawPicture(picture);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            data.SaveTo(output);
        }
    }
}
