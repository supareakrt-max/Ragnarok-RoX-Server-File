using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ROManager;

internal class Prompt : Form
{
	private readonly List<Control> inputs = new List<Control>();

	private readonly TableLayoutPanel tbl;

	public Prompt(string title)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		((Control)this).Text = title;
		((Control)this).Font = new Font("Tahoma", 9f);
		((Form)this).FormBorderStyle = (FormBorderStyle)3;
		bool maximizeBox = (((Form)this).MinimizeBox = false);
		((Form)this).MaximizeBox = maximizeBox;
		((Form)this).StartPosition = (FormStartPosition)4;
		((Control)this).AutoSize = true;
		((Form)this).AutoSizeMode = (AutoSizeMode)0;
		((Control)this).Padding = new Padding(12);
		TableLayoutPanel val = new TableLayoutPanel();
		val.ColumnCount = 2;
		((Control)val).AutoSize = true;
		((Control)val).Dock = (DockStyle)5;
		tbl = val;
		tbl.ColumnStyles.Add(new ColumnStyle((SizeType)0));
		tbl.ColumnStyles.Add(new ColumnStyle((SizeType)1, 240f));
		((Control)this).Controls.Add((Control)(object)tbl);
	}

	private void AddRow(string label, Control c)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		c.Dock = (DockStyle)5;
		TableLayoutControlCollection controls = tbl.Controls;
		Label val = new Label();
		((Control)val).Text = label;
		((Control)val).AutoSize = true;
		((Control)val).Anchor = (AnchorStyles)4;
		((Control)val).Margin = new Padding(3, 8, 8, 3);
		((Control.ControlCollection)controls).Add((Control)(object)val);
		((Control.ControlCollection)tbl.Controls).Add(c);
		inputs.Add(c);
	}

	public Prompt Text_(string label, string value = "", bool password = false)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		TextBox val = new TextBox();
		((Control)val).Text = value;
		val.UseSystemPasswordChar = password;
		AddRow(label, (Control)(object)val);
		return this;
	}

	public Prompt Combo(string label, IEnumerable<string> items, int selected = 0)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		ComboBox val = new ComboBox();
		val.DropDownStyle = (ComboBoxStyle)2;
		ComboBox val2 = val;
		foreach (string item in items)
		{
			val2.Items.Add((object)item);
		}
		if (val2.Items.Count > 0)
		{
			((ListControl)val2).SelectedIndex = Math.Min(Math.Max(selected, 0), val2.Items.Count - 1);
		}
		AddRow(label, (Control)(object)val2);
		return this;
	}

	public Prompt Number(string label, decimal value, decimal min, decimal max)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		NumericUpDown val = new NumericUpDown();
		val.Minimum = min;
		val.Maximum = max;
		val.Value = value;
		AddRow(label, (Control)(object)val);
		return this;
	}

	public Prompt Note(string text)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label();
		((Control)val).Text = text;
		((Control)val).AutoSize = true;
		((Control)val).ForeColor = Theme.Muted;
		((Control)val).MaximumSize = new Size(360, 0);
		((Control)val).Margin = new Padding(3, 6, 3, 3);
		Label val2 = val;
		((Control.ControlCollection)tbl.Controls).Add((Control)(object)val2);
		tbl.SetColumnSpan((Control)(object)val2, 2);
		return this;
	}

	public string[] Ask(IWin32Window owner)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I4
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		FlowLayoutPanel val = new FlowLayoutPanel();
		val.FlowDirection = (FlowDirection)2;
		((Control)val).AutoSize = true;
		((Control)val).Dock = (DockStyle)5;
		((Control)val).Margin = new Padding(0, 10, 0, 0);
		FlowLayoutPanel val2 = val;
		Button val3 = new Button();
		((Control)val3).Text = "ตกลง";
		val3.DialogResult = (DialogResult)1;
		((Control)val3).Width = 90;
		((Control)val3).Height = 28;
		Button val4 = val3;
		val3 = new Button();
		((Control)val3).Text = "ยกเล\u0e34ก";
		val3.DialogResult = (DialogResult)2;
		((Control)val3).Width = 90;
		((Control)val3).Height = 28;
		Button val5 = val3;
		((Control)val2).Controls.Add((Control)(object)val5);
		((Control)val2).Controls.Add((Control)(object)val4);
		((Control.ControlCollection)tbl.Controls).Add((Control)(object)val2);
		tbl.SetColumnSpan((Control)(object)val2, 2);
		((Form)this).AcceptButton = (IButtonControl)(object)val4;
		((Form)this).CancelButton = (IButtonControl)(object)val5;
		Theme.Apply((Control)(object)this);
		if ((int)((Form)this).ShowDialog(owner) != 1)
		{
			return null;
		}
		string[] array = new string[inputs.Count];
		for (int i = 0; i < inputs.Count; i++)
		{
			Control val6 = inputs[i];
			if (val6 is ComboBox)
			{
				array[i] = ((ListControl)(ComboBox)val6).SelectedIndex.ToString();
			}
			else if (val6 is NumericUpDown)
			{
				array[i] = ((NumericUpDown)val6).Value.ToString();
			}
			else
			{
				array[i] = val6.Text;
			}
		}
		return array;
	}
}
