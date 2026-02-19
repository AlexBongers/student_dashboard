using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System;
using System.Runtime.InteropServices;

class Program
{
    static void Main()
    {
        int size = 64;
        
        using (var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using (var brush = new SolidBrush(Color.FromArgb(255, 79, 70, 229))) // #4f46e5 PrimaryColor
            {
                g.FillEllipse(brush, 1, 1, size - 2, size - 2);
            }

            using (var font = new Font("Segoe UI", size * 0.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("S", font, brush, new RectangleF(0, 0, size, size), format);
            }

            // Convert to native Icon handle
            IntPtr Hicon = bmp.GetHicon();
            
            using (Icon icon = Icon.FromHandle(Hicon))
            using (FileStream fs = new FileStream("AppIcon.ico", FileMode.Create))
            {
                icon.Save(fs);
            }
        }
    }
}
