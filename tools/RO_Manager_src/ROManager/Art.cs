using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace ROManager;

internal static class Art
{
	private static Image bg;

	public static Image Bg
	{
		get
		{
			if (bg == null)
			{
				try
				{
					Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ROManager.bg.jpg");
					if (manifestResourceStream != null)
					{
						bg = Image.FromStream(manifestResourceStream);
					}
				}
				catch
				{
				}
			}
			return bg;
		}
	}

	public static void DrawCover(Graphics g, Image img, Rectangle dst, float focusX, float focusY, float minScaleRows = 0f)
	{
		if (img != null && dst.Width > 0 && dst.Height > 0)
		{
			float num = Math.Max((float)dst.Width / (float)img.Width, (float)dst.Height / (float)img.Height);
			if (minScaleRows > 0f)
			{
				num = Math.Max(num, (float)dst.Height / minScaleRows);
			}
			float num2 = (float)dst.Width / num;
			float num3 = (float)dst.Height / num;
			float x = Math.Max(0f, Math.Min((float)img.Width - num2, (float)img.Width * focusX - num2 / 2f));
			float y = Math.Max(0f, Math.Min((float)img.Height - num3, (float)img.Height * focusY - num3 / 2f));
			g.InterpolationMode = (InterpolationMode)7;
			g.PixelOffsetMode = (PixelOffsetMode)2;
			g.DrawImage(img, (RectangleF)dst, new RectangleF(x, y, num2, num3), (GraphicsUnit)2);
		}
	}
}
