using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ROManager;

internal class BannerPanel : Panel
{
	public BannerPanel()
	{
		this.DoubleBuffered = true;
		this.SetStyle((ControlStyles)139280, true);
		this.Dock = (DockStyle)1;
		this.Height = 150;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		if (this.Parent != null)
		{
			int num = Math.Max(120, Math.Min(190, (int)((double)this.Width * 0.13)));
			if (this.Height != num)
			{
				this.Height = num;
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected O, but got Unknown
		Graphics graphics = e.Graphics;
		Rectangle clientRectangle = this.ClientRectangle;
		Image bg = Art.Bg;
		if (bg == null)
		{
			graphics.Clear(Color.FromArgb(40, 50, 70));
			return;
		}
		Art.DrawCover(graphics, bg, clientRectangle, 0.5f, 0f, (float)bg.Height * 0.21f);
		int num = Math.Min(40, clientRectangle.Height / 3);
		Rectangle rectangle = new Rectangle(0, clientRectangle.Bottom - num, clientRectangle.Width, num);
		LinearGradientBrush val = new LinearGradientBrush(new Rectangle(rectangle.X, rectangle.Y - 1, rectangle.Width, rectangle.Height + 2), Color.FromArgb(0, Theme.Bg), Theme.Bg, 90f);
		try
		{
			graphics.FillRectangle((Brush)(object)val, rectangle);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		graphics.TextRenderingHint = (TextRenderingHint)3;
		Font val2 = new Font("Tahoma", 13f, (FontStyle)1);
		try
		{
			string text = "Server Manager";
			SizeF sizeF = graphics.MeasureString(text, val2);
			float num2 = (float)clientRectangle.Right - sizeF.Width - 16f;
			float num3 = (float)clientRectangle.Bottom - sizeF.Height - 14f;
			SolidBrush val3 = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
			try
			{
				for (int i = -1; i <= 2; i++)
				{
					for (int j = -1; j <= 2; j++)
					{
						if (i != 0 || j != 0)
						{
							graphics.DrawString(text, val2, (Brush)(object)val3, num2 + (float)i, num3 + (float)j);
						}
					}
				}
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
			SolidBrush val4 = new SolidBrush(Color.FromArgb(255, 250, 236, 190));
			try
			{
				graphics.DrawString(text, val2, (Brush)(object)val4, num2, num3);
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}
}
