using System;
using System.IO;
using SkiaSharp;

namespace UserRolePortal.Models
{
    public static class CaptchaService
    {
        // 1. Generates a random string
        public static string GenerateRandomString(int length = 7)
        { 
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ1234567890";
            var random = new Random();
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        // 2. Turns the string into an image (returns a byte array)
        public static byte[] GenerateCaptchaImage(string captchaText)
        {
            int width = 160;
            int height = 50;
            var random = new Random();

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            // 1. Random background color (light colors)
            canvas.Clear(new SKColor((byte)random.Next(220, 255), (byte)random.Next(220, 255), (byte)random.Next(220, 255)));

            // 2. Add background noise (curves instead of straight lines)
            using var paintNoise = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = random.Next(1, 3)
            };

            for (int i = 0; i < 15; i++)
            {
                paintNoise.Color = new SKColor((byte)random.Next(150, 200), (byte)random.Next(150, 200), (byte)random.Next(150, 200));
                
                var path = new SKPath();
                path.MoveTo(random.Next(width), random.Next(height));
                path.CubicTo(
                    random.Next(width), random.Next(height),
                    random.Next(width), random.Next(height),
                    random.Next(width), random.Next(height));
                    
                canvas.DrawPath(path, paintNoise);
            }

            // 3. Draw text with varying fonts, colors, and slight rotation
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Typeface = SKTypeface.Default, // Safest for Linux/Docker without installed fonts
                TextSize = random.Next(24, 30)
            };

            float x = 10; // Starting X position

            for (int i = 0; i < captchaText.Length; i++)
            {
                // Random dark color for the text
                textPaint.Color = new SKColor((byte)random.Next(0, 100), (byte)random.Next(0, 100), (byte)random.Next(0, 100));

                canvas.Save();
                
                // Rotate each character slightly to warp the text
                canvas.Translate(x, height / 2 + textPaint.TextSize / 3 + random.Next(-5, 5));
                canvas.RotateDegrees(random.Next(-15, 15));
                
                canvas.DrawText(captchaText[i].ToString(), 0, 0, textPaint);
                canvas.Restore();

                // Move X forward for the next character, with random spacing
                x += textPaint.MeasureText(captchaText[i].ToString()) + random.Next(-2, 5);
            }

            // 4. Add foreground noise (scatter 100 static dots) to make it less visible
            using var dotPaint = new SKPaint { Style = SKPaintStyle.Fill };
            for (int i = 0; i < 100; i++)
            {
                dotPaint.Color = new SKColor((byte)random.Next(100, 150), (byte)random.Next(100, 150), (byte)random.Next(100, 150));
                canvas.DrawCircle(random.Next(width), random.Next(height), 1.5f, dotPaint);
            }

            // Convert to byte array
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}