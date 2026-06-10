using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

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
        [SupportedOSPlatform("windows")]
        public static byte[] GenerateCaptchaImage(string captchaText)
        {
            int width = 160;
            int height = 50;
            var random = new Random();

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Random background color (light colors)
                graphics.Clear(Color.FromArgb(random.Next(220, 255), random.Next(220, 255), random.Next(220, 255)));

                // 2. Add background noise (curves instead of straight lines)
                for (int i = 0; i < 15; i++)
                {
                    // Random light colors for background lines
                    using (Pen pen = new Pen(Color.FromArgb(random.Next(150, 200), random.Next(150, 200), random.Next(150, 200)), random.Next(1, 3)))
                    {
                        graphics.DrawBezier(pen,
                            random.Next(width), random.Next(height),
                            random.Next(width), random.Next(height),
                            random.Next(width), random.Next(height),
                            random.Next(width), random.Next(height));
                    }
                }

                // 3. Draw text with varying fonts, colors, and slight rotation
                string[] fontFamilies = { "Arial", "Verdana", "Georgia", "Courier New", "Times New Roman", "Sans"};
                float x = 10; // Starting X position

                for (int i = 0; i < captchaText.Length; i++)
                {
                    string fontName = fontFamilies[random.Next(fontFamilies.Length)];

                    // Random font size and bold/italic styling
                    using (Font font = new Font(fontName, random.Next(22, 28), FontStyle.Bold | FontStyle.Italic))

                    // Random dark color for the text
                    using (Brush brush = new SolidBrush(Color.FromArgb(random.Next(0, 100), random.Next(0, 100), random.Next(0, 100))))
                    {
                        // Rotate each character slightly to warp the text
                        graphics.ResetTransform();
                        graphics.TranslateTransform(x, random.Next(5, 15));
                        graphics.RotateTransform(random.Next(-15, 15));

                        graphics.DrawString(captchaText[i].ToString(), font, brush, 0, 0);

                        // Move X forward for the next character, with random spacing
                        x += font.Size + random.Next(-2, 5);
                    }
                }

                // Reset transform back to normal before drawing dots
                graphics.ResetTransform();

                // 4. Add foreground noise (scatter 100 static dots) to make it less visible
                for (int i = 0; i < 100; i++)
                {
                    bitmap.SetPixel(random.Next(width), random.Next(height),
                        Color.FromArgb(random.Next(100, 150), random.Next(100, 150), random.Next(100, 150)));
                }

                // Convert to byte array
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}