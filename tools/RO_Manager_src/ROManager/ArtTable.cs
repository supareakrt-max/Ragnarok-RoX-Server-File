using System;
using System.Drawing;
using System.Windows.Forms;

namespace ROManager;

internal class ArtTable : TableLayoutPanel
{
	private Bitmap cache;

	public ArtTable()
	{
		this.DoubleBuffered = true;
		this.SetStyle((ControlStyles)139280, true);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
		Rectangle clientRectangle = this.ClientRectangle;
		if (clientRectangle.Width <= 0 || clientRectangle.Height <= 0)
		{
			return;
		}
		if (cache == null || ((Image)cache).Size != clientRectangle.Size)
		{
			if (cache != null)
			{
				((Image)cache).Dispose();
			}
			cache = new Bitmap(clientRectangle.Width, clientRectangle.Height);
			Graphics val = Graphics.FromImage((Image)(object)cache);
			try
			{
				val.Clear(Theme.Bg);
				if (Art.Bg != null)
				{
					Art.DrawCover(val, Art.Bg, clientRectangle, 0.5f, 0.52f);
					SolidBrush val2 = new SolidBrush((!Theme.Dark) ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(120, 10, 12, 18));
					try
					{
						val.FillRectangle((Brush)(object)val2, clientRectangle);
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		e.Graphics.DrawImageUnscaled((Image)(object)cache, 0, 0);
	}
}
