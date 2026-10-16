using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WeekNumberLite
{
    internal static class WeekIcon
    {
        private const float EmSize = 180;

        internal static Icon Create(int weekNumber)
        {
            return Create(weekNumber, SystemInformation.SmallIconSize.Width, GetForegroundColor());
        }

        internal static Icon Create(int weekNumber, int size, Color color)
        {
            using (Bitmap bitmap = Render(weekNumber, size, color))
            {
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon borrowed = Icon.FromHandle(handle))
                        return (Icon)borrowed.Clone();
                }
                finally { DestroyIcon(handle); }
            }
        }

        // Explicit inputs let the production renderer be exercised without changing Windows settings.
        internal static Bitmap Render(int weekNumber, int size, Color color)
        {
            if (weekNumber < 1 || weekNumber > 53) throw new ArgumentOutOfRangeException(nameof(weekNumber));
            if (size < 16 || size > 256) throw new ArgumentOutOfRangeException(nameof(size));
            using (Bitmap canvas = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(canvas))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.ScaleTransform(canvas.Width / 256f, canvas.Height / 256f);
                using (Brush brush = new SolidBrush(color))
                using (FontFamily family = new FontFamily("Segoe UI"))
                using (StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone())
                using (GraphicsPath number = new GraphicsPath())
                {
                    string digits = weekNumber.ToString("D2", CultureInfo.InvariantCulture);
                    // Use a shared scale for all digits so narrow ones keep their shape.
                    RectangleF digitBounds = RectangleF.Empty;
                    for (int value = 0; value <= 9; value++)
                    {
                        using (GraphicsPath reference = new GraphicsPath())
                        {
                            reference.AddString(value.ToString(CultureInfo.InvariantCulture), family,
                                (int)FontStyle.Bold, EmSize, Point.Empty, format);
                            RectangleF bounds = reference.GetBounds();
                            digitBounds.Width = Math.Max(digitBounds.Width, bounds.Width);
                            digitBounds.Height = Math.Max(digitBounds.Height, bounds.Height);
                        }
                    }
                    // Logical 16px layout; preserve its proportions at higher DPI.
                    float inset = 8;
                    float top = 48;
                    float gap = 16;
                    float cellWidth = (256 - inset * 2 - gap) / 2;
                    float scale = Math.Min(cellWidth / digitBounds.Width, (256 - top - inset) / digitBounds.Height);
                    for (int index = 0; index < digits.Length; index++)
                    {
                        using (GraphicsPath digit = new GraphicsPath())
                        {
                            digit.AddString(digits[index].ToString(), family, (int)FontStyle.Bold, EmSize, Point.Empty, format);
                            RectangleF bounds = digit.GetBounds();
                            float x = inset + index * (cellWidth + gap) + (cellWidth - bounds.Width * scale) / 2;
                            float y = top + (256 - top - inset - digitBounds.Height * scale) / 2;
                            using (Matrix transform = new Matrix(scale, 0, 0, scale,
                                x - bounds.X * scale, y - bounds.Y * scale))
                                digit.Transform(transform);
                            number.AddPath(digit, false);
                        }
                    }
                    graphics.FillPath(brush, number);
                }
                Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics target = Graphics.FromImage(bitmap))
                    {
                        target.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        target.DrawImage(canvas, 0, 0, size, size);
                        // Draw the header at the final resolution for crisp, pixel-aligned strokes.
                        target.SmoothingMode = SmoothingMode.None;
                        int stroke = Math.Max(1, (int)Math.Round(size / 16f));
                        int leftBinding = size / 4;
                        int rightBinding = size - 1 - leftBinding;
                        using (Brush header = new SolidBrush(color))
                        {
                            target.FillRectangle(header, stroke, stroke, size - 2 * stroke, stroke);
                            target.FillRectangle(header, leftBinding, 0, stroke, 3 * stroke);
                            target.FillRectangle(header, rightBinding - stroke + 1, 0, stroke, 3 * stroke);
                        }
                    }
                    return bitmap;
                }
                catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }
        }

        private static Color GetForegroundColor()
        {
            if (SystemInformation.HighContrast) return SystemColors.WindowText;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                return ForegroundForTheme(key == null ? null : key.GetValue("SystemUsesLightTheme"));
        }

        internal static Color ForegroundForTheme(object value)
        {
            // Missing or unexpected registry data must not terminate the tray application.
            return value is int && (int)value == 1
                ? Color.FromArgb(48, 52, 58) : Color.FromArgb(240, 240, 240);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
