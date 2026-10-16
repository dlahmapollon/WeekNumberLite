using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using WeekNumberLite;

internal static class IconTests
{
    private static int assertions;
    [STAThread]
    private static int Main(string[] args)
    {
        string output = Path.GetFullPath(args.Length == 0 ? "artifacts/icons" : args[0]);
        Directory.CreateDirectory(output);
        try
        {
            InvalidInputs();
            Check(WeekIcon.ForegroundForTheme(1).ToArgb() == Color.FromArgb(48,52,58).ToArgb(), "light foreground");
            foreach (object value in new object[] { null, 0, "1", 1L, new byte[0] })
                Check(WeekIcon.ForegroundForTheme(value).ToArgb() == Color.FromArgb(240,240,240).ToArgb(), "registry fallback");
            // Exercise the real calendar calculation across ISO week-year boundaries.
            foreach (string item in new[] { "2015-12-31:53", "2016-01-01:53", "2016-01-04:1", "2021-01-01:53", "2021-01-04:1", "2026-10-16:42", "2027-01-01:53" })
            {
                string[] pair = item.Split(':');
                Check(Week.Of(DateTime.ParseExact(pair[0], "yyyy-MM-dd", CultureInfo.InvariantCulture)) == int.Parse(pair[1], CultureInfo.InvariantCulture), item);
            }
            foreach (int size in new[] { 16, 20, 24, 28, 32, 48, 64 })
                foreach (bool light in new[] { true, false })
                    RenderSet(size, light, output);
            NativeLifetime();
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " assertions on " + Environment.OSVersion + Environment.NewLine);
            Console.WriteLine("PASS: {0} assertions. Images: {1}", assertions, output);
            return 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(output, "result.txt"), error.ToString());
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void RenderSet(int size, bool light, string output)
    {
        string name = (light ? "light-" : "dark-") + size;
        string directory = Path.Combine(output, name);
        Directory.CreateDirectory(directory);
        Color background = light ? Color.FromArgb(243,243,243) : Color.FromArgb(32,32,32);
        Color foreground = WeekIcon.ForegroundForTheme(light ? 1 : 0);
        Check(Contrast(foreground, background) >= 7, name + " solid foreground contrast");
        var masks = new HashSet<string>();
        int cell = size + 16;
        using (Bitmap sheet = new Bitmap(cell * 10, (cell + 16) * 6))
        using (Graphics page = Graphics.FromImage(sheet))
        using (Font label = new Font("Segoe UI", 8))
        using (Brush ink = new SolidBrush(foreground))
        {
            page.Clear(background);
            for (int week = 1; week <= 53; week++)
            {
                using (Bitmap bitmap = WeekIcon.Render(week, size, foreground))
                using (Bitmap repeat = WeekIcon.Render(week, size, foreground))
                using (Icon icon = WeekIcon.Create(week, size, foreground))
                using (Bitmap roundtrip = icon.ToBitmap())
                {
                    string context = name + " week " + week;
                    Check(bitmap.Width == size && bitmap.Height == size && icon.Size == bitmap.Size, context + " dimensions");
                    Check(bitmap.GetPixel(0,0).A == 0 && bitmap.GetPixel(size-1,0).A == 0, context + " transparent corners");
                    Check(roundtrip.GetPixel(0,0).A == 0, context + " native transparency");
                    Check(roundtrip.GetPixel(size/2, size/8 - 1).A >= 240, context + " native header opacity");
                    int[] digitPixels = new int[2];
                    int[] minY = { size, size }, maxY = { 0, 0 };
                    int translucent = 0;
                    var mask = new System.Text.StringBuilder();
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            Color pixel = bitmap.GetPixel(x,y);
                            Check(pixel.ToArgb() == repeat.GetPixel(x,y).ToArgb(), context + " deterministic pixels");
                            if (pixel.A > 0 && pixel.A < 255) translucent++;
                            if (y < size / 4) continue;
                            mask.Append(pixel.A >= 128 ? '1' : '0');
                            if (pixel.A < 192) continue;
                            int half = x < size/2 ? 0 : 1;
                            digitPixels[half]++;
                            minY[half] = Math.Min(minY[half], y);
                            maxY[half] = Math.Max(maxY[half], y);
                            // Contrast after compositing also catches faint/incorrect digit cores.
                            Color composite = Blend(pixel, background);
                            Check(Contrast(composite, background) >= 4.5, context + " digit core contrast");
                            Check(roundtrip.GetPixel(x,y).A >= 128, context + " native digit alpha");
                        }
                    Check(translucent > 0, context + " antialiased edges");
                    for (int half = 0; half < 2; half++)
                    {
                        Check(digitPixels[half] >= size * size / 40, context + " digit ink");
                        Check(minY[half] <= size / 2, context + " digit starts within upper half");
                        Check(maxY[half] < size - 1, context + " digit core not clipped at bottom");
                        Check(maxY[half] - minY[half] >= size / 3, context + " digit height");
                    }
                    // A fully transparent central column keeps the two digits separate.
                    int gapInk = 0;
                    for (int y = size/4; y < size; y++) if (bitmap.GetPixel(size/2,y).A >= 128) gapInk++;
                    Check(gapInk == 0, context + " digit separation");
                    Check(masks.Add(mask.ToString()), context + " distinguishable threshold mask");
                    bitmap.Save(Path.Combine(directory, week.ToString("D2") + ".png"), ImageFormat.Png);
                    int sx = ((week-1)%10)*cell + 8, sy = ((week-1)/10)*(cell+16) + 4;
                    page.DrawImageUnscaled(bitmap, sx, sy);
                    page.DrawString(week.ToString("D2"), label, ink, sx, sy + size + 2);
                    if (size == 16)
                    {
                        using (Bitmap zoom = new Bitmap(128,128))
                        using (Graphics g = Graphics.FromImage(zoom))
                        {
                            g.Clear(background);
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.Half;
                            g.DrawImage(bitmap, new Rectangle(0,0,128,128));
                            if (week == 11 || week == 18 || week == 38 || week == 42 || week == 53)
                                zoom.Save(Path.Combine(directory, week.ToString("D2") + "-8x.png"), ImageFormat.Png);
                        }
                    }
                }
            }
            sheet.Save(Path.Combine(output, name + "-sheet.png"), ImageFormat.Png);
        }
    }

    private static void InvalidInputs()
    {
        foreach (int week in new[] { -1, 0, 54, 100 })
            Throws(delegate { using (Bitmap b = WeekIcon.Render(week,16,Color.Black)) { } });
        foreach (int size in new[] { -1, 0, 15, 257 })
            Throws(delegate { using (Bitmap b = WeekIcon.Render(42,size,Color.Black)) { } });
    }
    private static void Throws(Action action)
    {
        try { action(); } catch (ArgumentOutOfRangeException) { assertions++; return; }
        throw new Exception("Expected ArgumentOutOfRangeException");
    }
    private static void NativeLifetime()
    {
        for (int i=0; i<10; i++) using (Icon icon = WeekIcon.Create(42,16,Color.White)) { }
        IntPtr process = Process.GetCurrentProcess().Handle;
        int user = GetGuiResources(process,1), gdi = GetGuiResources(process,0);
        for (int i=0; i<300; i++)
            using (Icon icon = WeekIcon.Create(1+i%53,16,Color.White))
            using (Bitmap bitmap = icon.ToBitmap())
                Check(bitmap.Width == 16, "clone survives original HICON destruction");
        Check(GetGuiResources(process,1) <= user+8, "USER handles bounded");
        Check(GetGuiResources(process,0) <= gdi+8, "GDI handles bounded");
    }
    private static Color Blend(Color pixel, Color background)
    {
        double a = pixel.A / 255.0;
        return Color.FromArgb((int)Math.Round(pixel.R*a+background.R*(1-a)), (int)Math.Round(pixel.G*a+background.G*(1-a)), (int)Math.Round(pixel.B*a+background.B*(1-a)));
    }
    private static double Linear(byte c) { double v=c/255.0; return v<=0.04045 ? v/12.92 : Math.Pow((v+0.055)/1.055,2.4); }
    private static double Luminance(Color c) { return 0.2126*Linear(c.R)+0.7152*Linear(c.G)+0.0722*Linear(c.B); }
    private static double Contrast(Color a, Color b) { double x=Luminance(a), y=Luminance(b); return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); }
    private static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    [DllImport("user32.dll")]
    private static extern int GetGuiResources(IntPtr process, int flags);
}
