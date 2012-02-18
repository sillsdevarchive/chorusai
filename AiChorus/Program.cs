using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using AiChorus.Properties;
using Chorus.UI.Clone;
using ECInterfaces;
using SilEncConverters40;

namespace AiChorus
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (args.Length == 0)
			{
				MessageBox.Show(Resources.UsageString, Resources.AiChorusCaption);
				return;
			}

			if ((args[0] == "/c") || String.IsNullOrEmpty(Settings.Default.LastProjectFolder))
				DoClone();
			else if (args[0] == "/e")
				DoEdit();
			else
				LaunchProgram("Chorus.exe", Settings.Default.LastProjectFolder);
		}

		private static void DoEdit()
		{
			var theEc =
				DirectableEncConverter.EncConverters.AutoSelectWithTitle(ConvType.Unicode_to_from_Unicode,
																		 "Choose the 'Lookup in ...' item whose Knowledge base you want to edit and click 'OK'");

			if (theEc == null)
				return;

			if (!(theEc is AdaptItEncConverter))
			{
				if (MessageBox.Show(Resources.MustUseAiLookupConverterToEdit,
								Resources.AiChorusCaption,
								MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
					DoEdit();
				return;
			}

			var theAiEc = (AdaptItEncConverter) theEc;
			var strData = GrabDataPoint(theAiEc.ConverterIdentifier);
			strData = theAiEc.EditKnowledgeBase(strData);
			theAiEc.Configurator.DisplayTestPage(DirectableEncConverter.EncConverters,
												 theAiEc.Name,
												 theAiEc.ConverterIdentifier,
												 ConvType.Unicode_to_from_Unicode,
												 strData);
		}

		private static void DoClone()
		{
			var strAdaptItWorkFolder = AdaptItWorkFolder;
			if (!Directory.Exists(strAdaptItWorkFolder))
				Directory.CreateDirectory(strAdaptItWorkFolder);
#if DEBUG
			const string cstrHindiToUrdu = "Hindi to Urdu adaptations";
			var strHindiToUrduProjectFolder = Path.Combine(strAdaptItWorkFolder, cstrHindiToUrdu);
			if (Directory.Exists(strHindiToUrduProjectFolder))
				Directory.Delete(strHindiToUrduProjectFolder, true);
#endif

			var model = new GetCloneFromInternetModel(strAdaptItWorkFolder)
			{
#if DEBUG
				ProjectId = "aikb-hindi-urdu",
				AccountName = "bobeaton",
				Password = "helpmepld",
				LocalFolderName = cstrHindiToUrdu,
#endif
				SelectedServerLabel = Resources.IDS_DefaultRepoServer
			};

			using (var dlg = new GetCloneFromInternetDialog(model))
			{
				if (DialogResult.Cancel == dlg.ShowDialog())
					return;

				var strProjectFolder = dlg.PathToNewProject;
				Settings.Default.LastProjectFolder = strProjectFolder;
				Settings.Default.Save();
				InitializeLookupConverter(strProjectFolder);
			}
		}

		private static void InitializeLookupConverter(string strProjectFolder)
		{
			string strProjectName = Path.GetFileNameWithoutExtension(strProjectFolder);

			// in case AI isn't installed yet, it really doesn't like not having an Adaptations sub-folder
			var strAdaptationsFolder = Path.Combine(strProjectFolder, "Adaptations");
			if (!Directory.Exists(strAdaptationsFolder))
				Directory.CreateDirectory(strAdaptationsFolder);

			var strFriendlyName = "Lookup in " + strProjectName;
			var strConverterSpec = Path.Combine(strProjectFolder, strProjectName + ".xml");
			var aEcs = new EncConverters(true);
			aEcs.AddConversionMap(strFriendlyName, strConverterSpec, ConvType.Unicode_to_from_Unicode,
								  "SIL.AdaptItKB", "UNICODE", "UNICODE", ProcessTypeFlags.DontKnow);

			// we can save this information so we can use it automatically during the next restart
			var aEc = aEcs[strFriendlyName];

			var strData = GrabDataPoint(strConverterSpec);
			aEc.Configurator.DisplayTestPage(aEcs, strFriendlyName, strConverterSpec, ConvType.Unicode_to_from_Unicode, strData);
		}

		// kind of a brute force approach, but it's easy.
		//  This will return the first source word in the KB as a data point
		private static string GrabDataPoint(string strConverterSpec)
		{
			var doc = XDocument.Load(strConverterSpec);
			var strData = doc.Descendants("TU").First().Attributes("k").First().Value;
			return strData;
		}

		public static string AdaptItWorkFolder
		{
			get
			{
				var strAdaptItWorkFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
														"Adapt It Unicode Work");
				return strAdaptItWorkFolder;
			}
		}

		public static void ShowException(Exception ex)
		{
			string strErrorMsg = ex.Message;
			if (ex.InnerException != null)
				strErrorMsg += String.Format("{0}{0}{1}",
											Environment.NewLine,
											ex.InnerException.Message);
			MessageBox.Show(strErrorMsg, Resources.AiChorusCaption);
		}

		static void LaunchProgram(string strProgram, string strArguments)
		{
			try
			{
				var myProcess = new Process
				{
					StartInfo =
					{
						FileName = strProgram,
						Arguments = "\"" + strArguments + "\""
					}
				};

				myProcess.Start();
			}
			catch { }    // we tried...
		}
	}
}
