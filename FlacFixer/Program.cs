/*
 * Program Title: FLACFixer
 * Version: 0.9
 * Author: Joseph Cassano (http://jplc.ca)
 * Year: 2014
 * Description:
 * 		Interface for using the flac and metaflac programs
 * 		to create proper FLAC files out of FLAC or raw
 * 		files with bad headers.
 * License:
 * 		MIT License (see LICENSE.txt in the project's root
 * 		directory for details).
 * Target Framework:
 * 		Mono / .NET 4.0
 * References:
 * 		System
 * 		System.Xml
 * Confirmed Compatibility:
 * 		Windows 7 64-bit
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FLACFixer
{
	class MainClass
	{
		public static FileInfo exeFile;

		public static void Main(string[] args)
		{
			Console.WriteLine("FLACFixer STARTED!");
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
			Console.WriteLine("FLACFixer DONE!");
			exeFile = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
			Console.WriteLine(exeFile.FullName);
			Config config;
			string xmlPath = String.Concat(exeFile.DirectoryName, @"\config.xml");
         	if (!File.Exists(xmlPath))
			{
				config = new Config();
				SerializeToXML(config, xmlPath);
			}
			else
			{
				config = DeserializeFromXML(xmlPath);
			}
			Console.WriteLine(config.exeFlacPath);
			Console.WriteLine(config.exeMetaFlacPath);
			Console.Write("Press any key to close... ");
			Console.ReadKey();
		}

		static public void SerializeToXML(Config config, string xmlPath)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Config));
			TextWriter textWriter = new StreamWriter(xmlPath, false, System.Text.Encoding.UTF8);
			serializer.Serialize(textWriter, config);
			textWriter.Close();
		}

		static public Config DeserializeFromXML(string xmlPath)
		{
			XmlSerializer deserializer = new XmlSerializer(typeof(Config));
			TextReader textReader = new StreamReader(xmlPath, System.Text.Encoding.UTF8);
			Config config;
			config = (Config)deserializer.Deserialize(textReader);
			textReader.Close();
			return config;
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
	}

	static class Fixer
	{
		public static Process process;
		public static ProcessStartInfo startInfo;
		public static string dirFlac;
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
			dirFlac = @"C:\Program Files (x86)\FLAC Frontend\tools\";
			exeFlac = new FileInfo(String.Concat(dirFlac, "flac.exe"));
			exeMetaFlac = new FileInfo(String.Concat(dirFlac, "metaflac.exe"));
		}

		public static void Run(string initialFilePath, bool forceOverwrite, bool keepTemp, bool deleteOriginal)
		{
			FileInfo initialFile = new FileInfo(initialFilePath);
			if (File.Exists(initialFile.FullName))
			{
				FileInfo initialFlac;
				Console.WriteLine(initialFile.Extension);
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
							Console.WriteLine(wantedString);
						}
					}
				}
				//
				string overwriteString;
				if (forceOverwrite)
				{
					overwriteString = " -f";
				}
				else
				{
					overwriteString = "";
				}
				//
				FileInfo tempRaw = new FileInfo(String.Concat(initialFile.DirectoryName, @"\TempOutput", ExtRaw));
				startInfo.Arguments = String.Concat("/C \"\"", exeFlac.FullName, "\"", overwriteString, " -d \"", initialFlac.FullName, "\" -o \"", tempRaw.FullName, "\" --force-raw-format --endian=little --sign=signed\"");
				process.StartInfo = startInfo;
				process.Start();
				process.WaitForExit();
				process.Close();
				//
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
				//
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
				//
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