﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BeatSaberNoUpdate {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
		}

		void LaunchUrl(string url) {
			Process.Start(new ProcessStartInfo(url) {
				UseShellExecute = true,
				Verb = "open"
			});
		}

		private void browseButton_Click(object sender, EventArgs e) {
			using(var dialog = new FolderBrowserDialog()) {
				if(dialog.ShowDialog() != DialogResult.OK)
					return;

				textbox_path.Text = dialog.SelectedPath;
			}
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			LaunchUrl("https://github.com/kinsi55?tab=repositories&q=BeatSaber");
		}

		private void Form1_Load(object sender, EventArgs e) {
			try {
				var p = Registry.GetValue(
					@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980",
					"InstallLocation", 
					null
				);

				if(p != null && CheckFolderPath((string)p))
					textbox_path.Text = (string)p;
			} catch { }
		}

		bool CheckFolderPath(string path) {
			if(!Directory.Exists(path))
				return false;

			if(!File.Exists(Path.Combine(path, "..", "..", "appmanifest_620980.acf")))
				return false;

			return true;
		}

		void Bad(string str) {
			MessageBox.Show(str, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		void SetKv(ref string input, string key, string val) {
			input = Regex.Replace(input, $"\"{key}\".*", $"\"{key}\"\t\"{val}\"", RegexOptions.IgnoreCase);
		}

		private void applyButton_Click(object sender, EventArgs e) {
			if(!CheckFolderPath(textbox_path.Text)) {
				Bad("It seems like the selected folder is incorrect. You can go to the properties of Beat Saber in Steam and click on 'Browse Game files' for an easy method to get the correct path.");
				return;
			}

			if(!Regex.IsMatch(textbox_manifest.Text, "[0-9]{19}")) {
				Bad("It seems like the entered Manifest ID is incorrect. It's supposed to be a 19-digit number. Make sure not to enter any spaces by accident.");
				return;
			}

			if(Process.GetProcesses().Any(x => x.ProcessName.ToLower() == "steam")) {
				Bad("Steam seems to be already running, please exit it to apply the patch.");
				return;
			}

			var p = Path.Combine(textbox_path.Text, "..", "..", "appmanifest_620980.acf");

			var acf = File.ReadAllText(p);

			if(acf.Contains(textbox_manifest.Text))
				Bad("Seems like this update is already applied. It will now be applied again for good measure.");

			SetKv(ref acf, "ScheduledAutoUpdate", "0");
			SetKv(ref acf, "LastUpdated", ((uint)DateTimeOffset.Now.ToUnixTimeSeconds()).ToString());
			SetKv(ref acf, "StateFlags", "4");
			SetKv(ref acf, "UpdateResult", "0");

			if(checkBox1.Checked)
				SetKv(ref acf, "AutoUpdateBehavior", "1");

			acf = Regex.Replace(acf, "(\"" + AppInfo.DEPOT_ID + "\".*?\"manifest\"\\s*?)\"[0-9]{19}\"", $"$1\"{textbox_manifest.Text}\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);

			File.WriteAllText(p, acf);

			MessageBox.Show("Patch applied. If everything worked out correctly, Steam should most likely not ask you to update Beat Saber anymore. If it still does, this may be related to DLC (assuming you have any) and should not update the game. Be sure to backup your game version.", "Success");
		}

		private void aboutButton_Click(object sender, EventArgs e) {
			MessageBox.Show(
				"This tool modifies a configuration file of Steam to spoof the version Saber Version to be the latest without actually updating your local version.\n" +
				"\n" +
				"You will need to run this tool again whenever an update for Beat Saber is released. Basically, whenever Steam prompts you to update the game prior to starting it, you will need to use this tool first and spoof the latest version."
			);
		}

        private async void getManifestButton_Click(object sender, EventArgs e) {
			getManifestButton.Enabled = false;
			getManifestButton.Text = "Loading...";

			AppInfo appInfo = new AppInfo();
			string manifest = await appInfo.TryRetrieve();
			// if successful fill in textbox
			if(manifest != null) {
				textbox_manifest.Text = manifest;
            } else {
				MessageBox.Show("Retrieving the Manifest ID failed. Copy the latest 'Manifest ID' from the website. Make sure that 'Last update' looks correct, to confirm the site has already spotted the latest update!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
				LaunchUrl($"https://steamdb.info/depot/{AppInfo.DEPOT_ID}/manifests");
			}

			getManifestButton.Enabled = true;
			getManifestButton.Text = "Retrieve";
		}
    }
}
