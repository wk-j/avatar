using System;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using IO = System.IO;

namespace Avartar {
    static class Program {

        static async Task Main(string[] args) {
            if (args.Length != 1) return;

            var url = args[0];

            Console.WriteLine($@"> downloading ""{url}""");
            var image = await DownloadImage(url);
            var fileName = IO.Path.GetFileName(image);
            var newName = "avatar-" + IO.Path.ChangeExtension(fileName, ".png");

            using (var img = Image.Load(image)) {
                using (Image<Rgba32> destRound = img.Clone(x => x.ConvertToAvatar(new Size(300, 300), 15))) {
                    Console.WriteLine($@"> saving ""{newName}""");
                    destRound.Save(newName);
                }
            }
            IO.File.Delete(image);
        }

        static async Task<string> DownloadImage(string url) {
            using (var client = new HttpClient()) {
                var rs = await client.GetAsync(url);
                var content = await rs.Content.ReadAsByteArrayAsync();
                var localPath = new Uri(url).LocalPath;
                var fileName = IO.Path.GetFileName(localPath);
                var temp = IO.Path.Combine(IO.Path.GetTempPath(), fileName);
                IO.File.WriteAllBytes(temp, content);
                return temp;
            }
        }
        private static IImageProcessingContext<Rgba32> ConvertToAvatar(this IImageProcessingContext<Rgba32> processingContext, Size size, float cornerRadius) {
            return processingContext.Resize(new ResizeOptions {
                Size = size,
                Mode = ResizeMode.Crop
            }).Apply(i => ApplyRoundedCorners(i, cornerRadius));
        }

        private static Image<Rgba32> CloneAndConvertToAvatarWithoutApply(this Image<Rgba32> image, Size size, float cornerRadius) {
            Image<Rgba32> result = image.Clone(
                ctx => ctx.Resize(
                    new ResizeOptions {
                        Size = size,
                        Mode = ResizeMode.Crop
                    }));

            ApplyRoundedCorners(result, cornerRadius);
            return result;
        }

        public static void ApplyRoundedCorners(Image<Rgba32> img, float cornerRadius) {
            IPathCollection corners = BuildCorners(img.Width, img.Height, cornerRadius);

            img.Mutate(x => x.Fill(Rgba32.Transparent, corners, new GraphicsOptions(true) {
                BlenderMode = PixelBlenderMode.Src
            }));
        }

        public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius) {
            var rect = new RectangularePolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            IPath cornerToptLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            var center = new Vector2(imageWidth / 2F, imageHeight / 2F);

            float rightPos = imageWidth - cornerToptLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerToptLeft.Bounds.Height + 1;

            IPath cornerTopRight = cornerToptLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerToptLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerToptLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerToptLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}
