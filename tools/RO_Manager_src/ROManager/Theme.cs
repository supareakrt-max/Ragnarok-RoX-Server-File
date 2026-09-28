using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace ROManager;

internal static class Theme
{
	private class DarkRenderer : ToolStripProfessionalRenderer
	{
		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			SolidBrush val = new SolidBrush(Panel);
			try
			{
				e.Graphics.FillRectangle((Brush)(object)val, e.AffectedBounds);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
		}
	}

	public static bool Dark = true;

	public static Color Bg => (!Dark) ? SystemColors.Control : Color.FromArgb(27, 30, 37);

	public static Color Panel => (!Dark) ? SystemColors.Control : Color.FromArgb(34, 38, 46);

	public static Color Surface => (!Dark) ? SystemColors.Window : Color.FromArgb(40, 44, 53);

	public static Color Border => (!Dark) ? SystemColors.ControlDark : Color.FromArgb(62, 68, 80);

	public static Color Text => (!Dark) ? SystemColors.ControlText : Color.FromArgb(228, 231, 236);

	public static Color Muted => (!Dark) ? Color.DimGray : Color.FromArgb(150, 156, 168);

	public static Color Accent => Color.FromArgb(224, 184, 90);

	public static Color Good => (!Dark) ? Color.ForestGreen : Color.FromArgb(110, 214, 128);

	public static Color Bad => (!Dark) ? Color.Firebrick : Color.FromArgb(255, 118, 108);

	public static Color Warn => (!Dark) ? Color.DarkOrange : Color.FromArgb(255, 176, 76);

	public static Color Btn => (!Dark) ? SystemColors.Control : Color.FromArgb(50, 56, 68);

	public static Color BtnHover => (!Dark) ? SystemColors.ControlLight : Color.FromArgb(64, 72, 88);

	public static Color BtnGo => (!Dark) ? Color.FromArgb(220, 245, 225) : Color.FromArgb(38, 92, 58);

	public static Color BtnStop => (!Dark) ? Color.FromArgb(250, 225, 225) : Color.FromArgb(110, 44, 44);

	public static Color BtnSend => (!Dark) ? Color.FromArgb(220, 235, 250) : Color.FromArgb(36, 74, 118);

	public static Color GridAlt => (!Dark) ? Color.FromArgb(245, 247, 250) : Color.FromArgb(34, 38, 46);

	public static Color Head => (!Dark) ? SystemColors.Control : Color.FromArgb(46, 51, 62);

	public static Color EditHead => (!Dark) ? Color.FromArgb(255, 244, 200) : Color.FromArgb(92, 76, 36);

	public static Color Select => (!Dark) ? SystemColors.Highlight : Color.FromArgb(58, 88, 130);

	public static Color Glass(int a)
	{
		return (!Dark) ? Color.FromArgb(a, 255, 255, 255) : Color.FromArgb(a, 24, 27, 34);
	}

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTheme(IntPtr h, string app, string id);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);

	public static void DarkTitle(Form f)
	{
		if (!Dark)
		{
			return;
		}
		((Control)f).HandleCreated += delegate
		{
			try
			{
				int val = 1;
				if (DwmSetWindowAttribute(((Control)f).Handle, 20, ref val, 4) != 0)
				{
					DwmSetWindowAttribute(((Control)f).Handle, 19, ref val, 4);
				}
			}
			catch
			{
			}
		};
	}

	private static void DarkScroll(Control c)
	{
		if (!Dark)
		{
			return;
		}
		Action set = delegate
		{
			try
			{
				SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
			}
			catch
			{
			}
		};
		if (c.IsHandleCreated)
		{
			set();
			return;
		}
		c.HandleCreated += delegate
		{
			set();
		};
	}

	public static void Apply(Control root)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		if (Dark)
		{
			if (root is Form)
			{
				root.BackColor = Bg;
				root.ForeColor = Text;
				DarkTitle((Form)root);
			}
			Walk(root);
		}
	}

	private static void Walk(Control c)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		Style(c);
		foreach (Control item in (ArrangedElementCollection)c.Controls)
		{
			Control c2 = item;
			Walk(c2);
		}
		c.ControlAdded += (ControlEventHandler)delegate(object s, ControlEventArgs e)
		{
			Walk(e.Control);
		};
	}

	private static bool Default(Color col)
	{
		return col == SystemColors.ControlText || col == SystemColors.WindowText || col == Color.Black;
	}

	private static void Style(Control c)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Expected O, but got Unknown
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Expected O, but got Unknown
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Expected O, but got Unknown
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		if (Default(c.ForeColor))
		{
			c.ForeColor = Text;
		}
		if (c is Button)
		{
			Button val = (Button)c;
			((ButtonBase)val).FlatStyle = (FlatStyle)0;
			((ButtonBase)val).UseVisualStyleBackColor = false;
			if (((Control)val).BackColor == SystemColors.Control || ((Control)val).BackColor.A == 0 || ((Control)val).BackColor == Bg)
			{
				((Control)val).BackColor = Btn;
			}
			((ButtonBase)val).FlatAppearance.BorderColor = Border;
			((ButtonBase)val).FlatAppearance.MouseOverBackColor = ControlPaint.Light(((Control)val).BackColor, 0.15f);
			((ButtonBase)val).FlatAppearance.MouseDownBackColor = ControlPaint.Dark(((Control)val).BackColor, 0.05f);
			((Control)val).ForeColor = Text;
		}
		else if (c is TabPage)
		{
			TabPage val2 = (TabPage)c;
			val2.UseVisualStyleBackColor = false;
			((Control)val2).BackColor = Bg;
		}
		else if (c is RichTextBox)
		{
			DarkScroll(c);
		}
		else if (c is TextBoxBase)
		{
			c.BackColor = Surface;
			c.ForeColor = Text;
			((TextBoxBase)c).BorderStyle = (BorderStyle)1;
			DarkScroll(c);
		}
		else if (c is NumericUpDown)
		{
			c.BackColor = Surface;
			c.ForeColor = Text;
			((UpDownBase)(NumericUpDown)c).BorderStyle = (BorderStyle)1;
		}
		else if (c is ComboBox)
		{
			c.BackColor = Surface;
			c.ForeColor = Text;
			((ComboBox)c).FlatStyle = (FlatStyle)0;
		}
		else if (c is ListBox)
		{
			c.BackColor = Surface;
			c.ForeColor = Text;
			((ListBox)c).BorderStyle = (BorderStyle)1;
			DarkScroll(c);
		}
		else if (c is ListView)
		{
			StyleListView((ListView)c);
		}
		else if (c is DataGridView)
		{
			StyleGrid((DataGridView)c);
		}
		else if (c is CheckBox)
		{
			((ButtonBase)(CheckBox)c).FlatStyle = (FlatStyle)0;
		}
		else if (c is RadioButton)
		{
			((ButtonBase)(RadioButton)c).FlatStyle = (FlatStyle)0;
		}
		else if (c is StatusStrip)
		{
			StatusStrip val3 = (StatusStrip)c;
			((ToolStrip)val3).Renderer = (ToolStripRenderer)(object)new DarkRenderer();
			((ToolStrip)val3).BackColor = Panel;
			((ToolStrip)val3).ForeColor = Text;
		}
		else if (c is SplitContainer)
		{
			c.BackColor = Border;
			((Control)((SplitContainer)c).Panel1).BackColor = Bg;
			((Control)((SplitContainer)c).Panel2).BackColor = Bg;
		}
	}

	public static void StyleGrid(DataGridView g)
	{
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		if (!Dark)
		{
			return;
		}
		g.EnableHeadersVisualStyles = false;
		g.BackgroundColor = Bg;
		g.GridColor = Border;
		g.BorderStyle = (BorderStyle)0;
		g.DefaultCellStyle.BackColor = Surface;
		g.DefaultCellStyle.ForeColor = Text;
		g.DefaultCellStyle.SelectionBackColor = Select;
		g.DefaultCellStyle.SelectionForeColor = Color.White;
		g.AlternatingRowsDefaultCellStyle.BackColor = GridAlt;
		g.AlternatingRowsDefaultCellStyle.ForeColor = Text;
		g.ColumnHeadersDefaultCellStyle.BackColor = Head;
		g.ColumnHeadersDefaultCellStyle.ForeColor = Text;
		g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Head;
		g.ColumnHeadersBorderStyle = (DataGridViewHeaderBorderStyle)1;
		g.RowHeadersDefaultCellStyle.BackColor = Head;
		g.RowHeadersDefaultCellStyle.ForeColor = Text;
		DarkScroll((Control)(object)g);
		foreach (Control item in (ArrangedElementCollection)((Control)g).Controls)
		{
			Control c = item;
			DarkScroll(c);
		}
	}

	private static void StyleListView(ListView lv)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		((Control)lv).BackColor = Surface;
		((Control)lv).ForeColor = Text;
		lv.BorderStyle = (BorderStyle)1;
		lv.OwnerDraw = true;
		lv.DrawColumnHeader += (DrawListViewColumnHeaderEventHandler)delegate(object s, DrawListViewColumnHeaderEventArgs e)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Expected O, but got Unknown
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Invalid comparison between Unknown and I4
			SolidBrush val = new SolidBrush(Head);
			try
			{
				e.Graphics.FillRectangle((Brush)(object)val, e.Bounds);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			Pen val2 = new Pen(Border);
			try
			{
				e.Graphics.DrawRectangle(val2, e.Bounds.X - 1, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height - 1);
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
			TextRenderer.DrawText((IDeviceContext)(object)e.Graphics, e.Header.Text, ((Control)lv).Font, Rectangle.Inflate(e.Bounds, -4, 0), Text, (TextFormatFlags)(4 | (((int)e.Header.TextAlign == 1) ? 2 : 0)));
		};
		lv.DrawItem += (DrawListViewItemEventHandler)delegate(object s, DrawListViewItemEventArgs e)
		{
			e.DrawDefault = true;
		};
		lv.DrawSubItem += (DrawListViewSubItemEventHandler)delegate(object s, DrawListViewSubItemEventArgs e)
		{
			e.DrawDefault = true;
		};
		DarkScroll((Control)(object)lv);
	}
}
