/*
 * Program Title: FlacFixer
 * Version: 0.9.0.1
 * Author: Joseph Cassano (http://jplc.ca)
 * Year: 2014
 * Description:
 * 		Interface for using the flac and metaflac programs
 * 		to create proper FLAC files out of FLAC or raw
 * 		files with bad headers.
 * 		File paths for the flac and metaflac programs
 * 		are stored in a config.xml file in the same
 * 		directory as the executable for FlacFixer.
 * License:
 * 		MIT License (see LICENSE.txt in the project's root
 * 		directory for details).
 * Target Framework:
 * 		Mono / .NET 4.0
 * References:
 * 		System
 * 		System.Xml
 * External programs used in this program:
 * 		flac
 * 		metaflac
 * Confirmed Compatibility:
 * 		Windows 7 64-bit
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FlacFixer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.WriteLine("FlacFixer STARTED!");
			if (args.Length > 0)
			{
				string filePath = args[0];
				bool forceOverwrite = false;
				bool keepTemp = false;
				bool deleteOriginal = false;
				if (args.Length > 1)
				{
					for (int i = 1; i < args.Length; i++)
					{
						string arg = args[i];
						if (arg == "-f")
						{
							forceOverwrite = true;
						}
						else if (arg == "-kt")
						{
							keepTemp = true;
						}
						else if (arg == "-do")
						{
							deleteOriginal = true;
						}
					}
				}
				Fixer.Run(filePath, forceOverwrite, keepTemp, deleteOriginal);
			}
			Console.WriteLine("FlacFixer DONE!");
			//Console.Write("Press any key to close... ");
			//Console.ReadKey();
			//Console.Write("\n");
		}
	}

	public static class ConfigManager
	{
		public static string exeFlacPath{ get; private set; }
		public static string exeMetaFlacPath{ get; private set; }

		private static string xmlPath;
		private static Config currentConfig;
		private static Config CurrentConfig
		{
			get
			{
				return currentConfig;
			}
			set
			{
				currentConfig = value;
				exeFlacPath = currentConfig.exeFlacPath;
				exeMetaFlacPath = currentConfig.exeMetaFlacPath;
			}
		}

		static ConfigManager()
		{
			xmlPath = String.Concat(new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName, @"\config.xml");
			CurrentConfig = new Config();
		}

		static public void SerializeToXml()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Config));
			TextWriter textWriter = new StreamWriter(xmlPath, false, System.Text.Encoding.UTF8);
			serializer.Serialize(textWriter, CurrentConfig);
			textWriter.Close();
		}

		static public void DeserializeFromXml()
		{
			if (!File.Exists(xmlPath))
			{
				Config tempConfig = new Config();
				if (CurrentConfig != tempConfig)
				{
					CurrentConfig = tempConfig;
				}
				SerializeToXml();
			}
			else
			{
				XmlSerializer deserializer = new XmlSerializer(typeof(Config));
				TextReader textReader = new StreamReader(xmlPath, System.Text.Encoding.UTF8);
				CurrentConfig = (Config)deserializer.Deserialize(textReader);
				textReader.Close();
			}
		}

		public class Config
		{
			public string exeFlacPath;
			public string exeMetaFlacPath;

			public Config()
			{
				exeFlacPath = @"C:\Program Files (x86)\FLAC Frontend\tools\flac.exe";
				exeMetaFlacPath = @"C:\Program Files (x86)\FLAC Frontend\tools\metaflac.exe";
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}

				Config config = obj as Config;
				/*
				if ((object)config == null)
				{
					return false;
				}

				return (exeFlacPath == config.exeFlacPath) && (exeMetaFlacPath == config.exeMetaFlacPath);
				*/
				return Equals(config);
			}

			public bool Equals(Config config)
			{
				if ((object)config == null)
				{
					return false;
				}

				return (exeFlacPath == config.exeFlacPath) && (exeMetaFlacPath == config.exeMetaFlacPath);
			}

			public override int GetHashCode()
			{
				return exeFlacPath.GetHashCode() ^ exeMetaFlacPath.GetHashCode();
			}

			public static bool operator ==(Config configA, Config configB)
			{
				if (object.ReferenceEquals(configA, configB))
				{
					return true;
				}

				if (((object)configA == null) || ((object)configB == null))
				{
					return false;
				}

				return (configA.exeFlacPath == configB.exeFlacPath) && (configA.exeMetaFlacPath == configB.exeMetaFlacPath);
			}

			public static bool operator !=(Config configA, Config configB)
			{
				return !(configA == configB);
			}
		}
	}

	static class Fixer
	{
		public static Process process;
		public static ProcessStartInfo startInfo;
		public static FileInfo exeFlac;
		public static FileInfo exeMetaFlac;

		private const string ExtFlac = ".flac";
		private const string ExtRaw = ".raw";

		private static string sampleRate;
		private static string channels;
		private static string bitsPerSample;

		static Fixer()
		{
			process = new Process();
			startInfo = new ProcessStartInfo();
			ConfigManager.DeserializeFromXml();
			exeFlac = new FileInfo(ConfigManager.exeFlacPath);
			exeMetaFlac = new FileInfo(ConfigManager.exeMetaFlacPath);
		}

		public static void Run(string initialFilePath, bool forceOverwrite, bool keepTemp, bool deleteOriginal)
		{
			FileInfo initialFile = new FileInfo(initialFilePath);
			if (File.Exists(initialFile.FullName))
			{
				FileInfo initialFlac;
				if (initialFile.Extension != ExtFlac)
				{
					byte[] byteArray = File.ReadAllBytes(initialFile.FullName);
					initialFlac = new FileInfo(String.Concat(initialFile.DirectoryName, @"\InitOutput", ExtFlac));
					File.WriteAllBytes(initialFlac.FullName, byteArray);
				}
				else
				{
					initialFlac = new FileInfo(initialFile.FullName);
				}
				startInfo.Verb = "runas";
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;
				startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
				startInfo.FileName = String.Concat(Environment.ExpandEnvironmentVariables("%SystemRoot%"), @"\System32\cmd.exe");
				startInfo.Arguments = String.Concat("/C \"\"", exeMetaFlac.FullName, "\" \"", initialFlac.FullName, "\" --no-utf8-convert --list\"");
				List<string> outputList = new List<string>(){};
				process.StartInfo = startInfo;
				string tempString = "";
				process.Start();
				while (!process.HasExited)
				{
					tempString = process.StandardOutput.ReadToEnd();
				}
				process.WaitForExit();
				process.Close();
				outputList = new List<string>(tempString.Split('\n'));
				string sharedString = ": ";
				string sampleRateCheck = String.Concat("sample_rate", sharedString);
				string channelsCheck = String.Concat("channels", sharedString);
				string bitsPerSampleCheck = String.Concat("bits-per-sample", sharedString);
				foreach (string line in outputList)
				{
					if (line != null)
					{
						string trimmedLine = line.Trim();
						if (trimmedLine.StartsWith(sampleRateCheck) || trimmedLine.StartsWith(channelsCheck) || trimmedLine.StartsWith(bitsPerSampleCheck))
						{
							string[] splitArray = trimmedLine.Split(' ');
							string wantedString = splitArray[1];
							if (trimmedLine.StartsWith(sampleRateCheck))
							{
								sampleRate = wantedString;
							}
							else if (trimmedLine.StartsWith(channelsCheck))
							{
								channels = wantedString;
							}
							else if (trimmedLine.StartsWith(bitsPerSampleCheck))
							{
								bitsPerSample = wantedString;
							}
						}
					}
				}

				string overwriteString;
				if (forceOverwrite)
				{
					overwriteString = " -f";
				}
				else
				{
					overwriteString = "";
				}

				FileInfo tempRaw = new FileInfo(String.Concat(initialFile.DirectoryName, @"\TempOutput", ExtRaw));
				startInfo.Arguments = String.Concat("/C \"\"", exeFlac.FullName, "\"", overwriteString, " -d \"", initialFlac.FullName, "\" -o \"", tempRaw.FullName, "\" --force-raw-format --endian=little --sign=signed\"");
				process.StartInfo = startInfo;
				process.Start();
				process.WaitForExit();
				process.Close();

				if (deleteOriginal)
				{
					File.Delete(initialFile.FullName);
				}
				string finalFlacPath;
				int initExtensionLength = initialFile.Extension.Length;
				int initPathLength = initialFile.FullName.Length;
				if (initialFile.Extension == ExtFlac)
				{
					if (deleteOriginal)
					{
						finalFlacPath = initialFile.FullName;
					}
					else
					{
						finalFlacPath = String.Concat(initialFile.FullName.Substring(0, (initPathLength - initExtensionLength)), "_NEW", ExtFlac);
					}
				}
				else
				{
					finalFlacPath = String.Concat(initialFile.FullName.Substring(0, (initPathLength - initExtensionLength)), ExtFlac);
				}
				FileInfo finalFlac = new FileInfo(finalFlacPath);
				startInfo.Arguments = String.Concat("/C \"\"", exeFlac.FullName, "\"", overwriteString, " -8 --force-raw-format --endian=little --channels=", channels, " --bps=",
					bitsPerSample, " --sample-rate=", sampleRate, " --sign=signed -o \"", finalFlac.FullName, "\" - < \"", tempRaw.FullName, "\"\"");
				process.StartInfo = startInfo;
				process.Start();
				process.WaitForExit();
				process.Close();

				startInfo.FileName = String.Concat(Environment.ExpandEnvironmentVariables("%SystemRoot%"), @"\System32\cmd.exe");
				startInfo.Arguments = String.Concat("/C \"\"", exeMetaFlac.FullName, "\" \"", finalFlac.FullName, "\" --no-utf8-convert --list\"");
				outputList = new List<string>(){};
				process.StartInfo = startInfo;
				tempString = "";
				process.Start();
				while (!process.HasExited)
				{
					tempString = process.StandardOutput.ReadToEnd();
				}
				process.WaitForExit();
				process.Close();
				outputList = new List<string>(tempString.Split('\n'));
				foreach (string line in outputList)
				{
					Console.WriteLine(line);
				}

				if (!keepTemp)
				{
					if (File.Exists(initialFlac.FullName))
					{
						if (initialFlac.FullName != finalFlac.FullName)
						{
							File.Delete(initialFlac.FullName);
						}
					}
					File.Delete(tempRaw.FullName);
				}
				Console.Write("Output File: ");
				Console.WriteLine(finalFlac.FullName);
			}
		}
	}
}