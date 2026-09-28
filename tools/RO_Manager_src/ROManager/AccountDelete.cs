// Ro-X: "ลบไอดี" button on the account tab — deletes an account with all its
// characters, items and account storage.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace ROManager;

internal partial class MainForm
{
	private static readonly string[] CharTables = {
		"inventory", "cart_inventory", "skill", "char_reg_num", "char_reg_str", "sc_data", "hotkey",
		"quest", "achievement", "memo", "skillcooldown", "bonus_script", "guild_member", "pet",
		"homunculus", "mercenary", "mercenary_owner", "elemental"
	};

	private static readonly string[] AccountTables = {
		"storage", "acc_reg_num", "acc_reg_str", "global_acc_reg_num", "global_acc_reg_str"
	};

	private void DeleteAccount()
	{
		if (!NeedDb() || !SelectedAccount(out var id, out var userid))
			return;
		try
		{
			DataTable chars = db.Query("SELECT `char_id`, `name`, `online` FROM `char` WHERE `account_id`=" + id);
			List<DataRow> rows = chars.Rows.Cast<DataRow>().ToList();
			if (rows.Any(r => Convert.ToInt32(r["online"]) == 1))
			{
				MessageBox.Show(this, "ไอดีนี้ยังมีตัวละครออนไลน์อยู่\n\nให้ออกเกม (หรือ @kick / หยุดบอท) ก่อนลบ", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			string ids = string.Join(",", rows.Select(r => Convert.ToString(r["char_id"])));
			if (ids.Length > 0 && Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM `guild` WHERE `char_id` IN (" + ids + ")")) > 0)
			{
				MessageBox.Show(this, "ตัวละครในไอดีนี้เป็นหัวหน้ากิลด์อยู่\n\nยุบกิลด์หรือโอนหัวหน้าก่อนลบ", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			int group = Convert.ToInt32(db.Scalar("SELECT `group_id` FROM `login` WHERE `account_id`=" + id));
			string names = rows.Count == 0 ? "(ไม่มีตัวละคร)" : string.Join(", ", rows.Select(r => Convert.ToString(r["name"])));
			string msg = "ลบไอดี " + userid + " (account_id " + id + ") ถาวร?\n\n"
				+ "ตัวละคร: " + names + "\n\n"
				+ "จะลบตัวละคร ของในตัว/รถเข็น/คลัง สกิล และข้อมูลทั้งหมดของไอดีนี้\nกู้คืนไม่ได้";
			if (MessageBox.Show(this, msg, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;
			if (group >= 10 && MessageBox.Show(this, "ไอดีนี้เป็น GM (กลุ่ม " + group + ")\n\nยืนยันลบอีกครั้ง?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			foreach (DataRow r in rows)
			{
				string cid = Convert.ToString(r["char_id"]);
				foreach (string table in CharTables)
				{
					try { db.Exec("DELETE FROM `" + table + "` WHERE `char_id`=" + cid); }
					catch { } // table missing in this database version
				}
				try { db.Exec("DELETE FROM `friends` WHERE `char_id`=" + cid + " OR `friend_id`=" + cid); }
				catch { }
			}
			foreach (string table in AccountTables)
			{
				try { db.Exec("DELETE FROM `" + table + "` WHERE `account_id`=" + id); }
				catch { }
			}
			db.Exec("DELETE FROM `char` WHERE `account_id`=" + id);
			db.Exec("DELETE FROM `login` WHERE `account_id`=" + id);

			// AI bot account: also take it out of tools\aibot\config.json
			if (id >= BotAccountMin && id <= BotAccountMax && rows.Count > 0)
				RunPy("manage.py remove --names \"" + string.Join(",", rows.Select(r => Convert.ToString(r["name"]))) + "\"");

			tool.Add("[Tool] ลบไอดี " + userid + " (ตัวละคร: " + names + ")");
			SearchAccounts();
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
