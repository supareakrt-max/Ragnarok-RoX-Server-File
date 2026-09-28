using System;
using System.Drawing;
using System.Windows.Forms;

namespace ROManager;

internal class ThemedTab : TabControl
{
	public ThemedTab()
	{
		if (Theme.Dark)
		{
			this.SetStyle((ControlStyles)139282, true);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		if (!Theme.Dark)
		{
			base.OnPaint(e);
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.Clear(Theme.Bg);
		Rectangle displayRectangle = ((Control)this).DisplayRectangle;
		displayRectangle.Inflate(2, 2);
		Pen val = new Pen(Theme.Border);
		try
		{
			graphics.DrawRectangle(val, displayRectangle.X, displayRectangle.Y, displayRectangle.Width - 1, displayRectangle.Height - 1);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		for (int i = 0; i < ((TabControl)this).TabCount; i++)
		{
			Rectangle rectangle = ((TabControl)this).GetTabRect(i);
			bool flag = i == ((TabControl)this).SelectedIndex;
			if (flag)
			{
				rectangle = new Rectangle(rectangle.X, rectangle.Y - 2, rectangle.Width, rectangle.Height + 3);
			}
			SolidBrush val2 = new SolidBrush((!flag) ? Theme.Bg : Theme.Panel);
			try
			{
				graphics.FillRectangle((Brush)(object)val2, rectangle);
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
			Pen val3 = new Pen(Theme.Border);
			try
			{
				graphics.DrawRectangle(val3, rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
			if (flag)
			{
				SolidBrush val4 = new SolidBrush(Theme.Accent);
				try
				{
					graphics.FillRectangle((Brush)(object)val4, rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, 2);
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			TextRenderer.DrawText((IDeviceContext)(object)graphics, ((Control)((TabControl)this).TabPages[i]).Text, ((Control)this).Font, rectangle, (!flag) ? Theme.Text : Theme.Accent, (TextFormatFlags)2053);
		}
	}
}
