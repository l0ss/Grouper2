using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Grouper2.Utility
{
	// Stripped down version of https://github.com/finarfin/GroupPolicy.Parser/blob/master/RegistryFile.cs
	// Very light / basic registry.pol parser.
	// Refactored to allow re-use of InfAssess code in the AssessHandlers
	public class RegistryPolParser
	{
		private const uint SIGNATURE = 0x67655250; // "PReg"

		public static JObject Read(string path)
		{
			// Store the parsed data
			var result = new JObject();

			// Open the file
			using (var file = File.OpenRead(path))
			using (var reader = new BinaryReader(file, Encoding.Unicode))
			{
				// Check against the file signature
				var Signature = reader.ReadUInt32();
				if (Signature != SIGNATURE)
				{
					throw new NotSupportedException("File format is not supported");
				}

				// Get the file version
				var version = reader.ReadUInt32();

				// Get the length of the file
				var length = reader.BaseStream.Length;

				// Read the contents
				while (reader.BaseStream.Position < length)
				{
					// Storing properties of the key / value in this object to match the infAssess format
					List<object> values = new List<object>();

					reader.ReadChar();
					var name = ReadString(reader);	// This is the Reg key
					reader.ReadChar();
					name += "\\" + ReadString(reader); // This is the value
					reader.ReadChar();
					values.Add(reader.ReadUInt32()); // This is the value type
					reader.ReadChar();
					var size = reader.ReadUInt32(); // This is the size of the value data
					reader.ReadChar();
					byte[] binaryData = reader.ReadBytes((int)size); // value data
					reader.ReadChar();

					// Convert the raw data into a usable format
					GetData(binaryData, ref values);

					// Turn that list of properties into an array
					object[] value = values.ToArray();

					// The name of the key\value becomes a new property of the output object
					// and the data about the value (type, actual value data) are an array
					result[name] = JArray.FromObject(value);
				}
			}

			return result;
		}

		// Reads a null terminated string from a binary stream.
		private static string ReadString(BinaryReader reader)
		{
			char current;
			string temp = string.Empty;
			while ((current = reader.ReadChar()) != '\0')
			{
				temp += current;
			}

			return temp;
		}

		// converts the raw binary value data into a usable format
		private static void GetData(byte[] binaryData, ref List<object> values)
		{
			UInt32 type = (UInt32)values[0];
			switch ((RegistryValueKind)type)
			{
				case RegistryValueKind.String:
				case RegistryValueKind.ExpandString:
					values.Add(Encoding.Unicode.GetString(binaryData).TrimEnd('\0'));
					return;

				case RegistryValueKind.DWord:
					values.Add(BitConverter.ToUInt32(binaryData, 0));
					return;

				case RegistryValueKind.QWord:
					values.Add(BitConverter.ToUInt64(binaryData, 0));
					return;

				case RegistryValueKind.MultiString:
					if (binaryData.Length == 0) return;

					var collection = new List<string>();
					using (var stream = new MemoryStream(binaryData))
					using (var reader = new BinaryReader(stream, Encoding.Unicode))
					{
						var length = reader.BaseStream.Length;
						while (reader.BaseStream.Position < length)
						{
							values.Add(ReadString(reader));
							if (reader.PeekChar() == '\0')
								break;
						}
						return;
					}

				default:
					values.Add(binaryData);
					return;
			}
		}
	}
}
