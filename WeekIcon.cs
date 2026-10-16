using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using Microsoft.Win32;

namespace WeekNumberLite
{
    internal static class WeekIcon
    {
        internal static Icon GetIcon(int weekNumber)
        {
            // Keep a minimal calendar header above large, fitted digits.
            int size = System.Windows.Forms.SystemInformation.SmallIconSize.Width;
            using (Bitmap canvas = new Bitmap(size * 4, size * 4))
            using (Graphics graphics = Graphics.FromImage(canvas))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.ScaleTransform(canvas.Width / 256f, canvas.Height / 256f);
                Color color = GetForegroundColor();
                using (Brush brush = new SolidBrush(color))
                using (Font font = new Font("Segoe UI", 180, FontStyle.Bold, GraphicsUnit.Pixel))
                using (StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
                using (GraphicsPath number = new GraphicsPath())
                {
                    string digits = weekNumber.ToString("D2", CultureInfo.InvariantCulture);
                    // Use a shared scale for all digits so narrow ones keep their shape.
                    RectangleF digitBounds;
                    using (GraphicsPath reference = new GraphicsPath())
                    {
                        reference.AddString("0", font.FontFamily, (int)FontStyle.Bold, font.Size, Point.Empty, format);
                        digitBounds = reference.GetBounds();
                    }
                    float inset = 128f / size;
                    float top = 768f / size;
                    float gap = 256f / size;
                    float cellWidth = (256 - inset * 2 - gap) / 2;
                    float scale = Math.Min(cellWidth / digitBounds.Width, (256 - top - inset) / digitBounds.Height);
                    for (int index = 0; index < digits.Length; index++)
                    {
                        using (GraphicsPath digit = new GraphicsPath())
                        {
                            digit.AddString(digits.Substring(index, 1), font.FontFamily, (int)FontStyle.Bold, font.Size, Point.Empty, format);
                            RectangleF bounds = digit.GetBounds();
                            float x = inset + index * (cellWidth + gap) + (cellWidth - bounds.Width * scale) / 2;
                            float y = top + (256 - top - inset - digitBounds.Height * scale) / 2;
                            using (Matrix transform = new Matrix(scale, 0, 0, scale,
                                x - bounds.X * scale, y - digitBounds.Y * scale))
                                digit.Transform(transform);
                            number.AddPath(digit, false);
                        }
                    }
                    graphics.FillPath(brush, number);
                }
                using (Bitmap bitmap = new Bitmap(size, size))
                using (Graphics target = Graphics.FromImage(bitmap))
                {
                    target.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    target.DrawImage(canvas, 0, 0, size, size);
                    // Draw the header at the final resolution for crisp one-pixel strokes.
                    target.SmoothingMode = SmoothingMode.None;
                    using (Pen header = new Pen(color, 1))
                    {
                        target.DrawLine(header, 1, 1, size - 2, 1);
                        target.DrawLine(header, size / 4, 0, size / 4, 2);
                        target.DrawLine(header, size - 1 - size / 4, 0, size - 1 - size / 4, 2);
                    }
                    IntPtr handle = bitmap.GetHicon();
                    try
                    {
                        using (Icon borrowed = Icon.FromHandle(handle))
                            return (Icon)borrowed.Clone();
                    }
                    finally { NativeMethods.DestroyIcon(handle); }
                }
            }
        }

        internal static void CleanupIcon(ref Icon icon)
        {
            if (icon == null) return;
            icon.Dispose();
            icon = null;
        }

        private static Color GetForegroundColor()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                return (int)(key == null ? 0 : key.GetValue("SystemUsesLightTheme", 0)) == 1
                    ? Color.FromArgb(48, 52, 58) : Color.FromArgb(240, 240, 240);
        }

    }
}
