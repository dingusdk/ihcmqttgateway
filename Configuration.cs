using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IhcMqttGateway {

	/// <summary>
	/// The class has the configuration properties and 2 dictionaries for the 
	/// mapping of ihc ids to mqtt topics
	/// </summary>
	public class Configuration {

		public string IhcUrl { get; protected set; }
		public string IhcUser { get; protected set; }
		public string IhcPassword { get; protected set; }
		public string MqttHost { get; protected set; }

		public string BooleanFalse { get; protected set; }
		public string BooleanTrue { get; protected set; }

		public Dictionary< string,int> IhcIn{ get; protected set; }
		public Dictionary< int, string> IhcOut { get; protected set; }

		public Configuration() {

			IhcIn = new Dictionary<string, int>();
			IhcOut = new Dictionary<int, string>();
			BooleanFalse = "0";
			BooleanTrue = "1";
		}

		/// <summary>
		/// Load the configuration from a file
		/// </summary>
		/// <param name="path">Configuration file</param>
		/// <returns>True if configuration was loaded successfully</returns>
		public bool Load(string path) {

			if (!File.Exists(path)) {
				Console.Error.WriteLine("Configuration file not found: " + path);
				return false;
			}
			using (var file = new FileStream(path, FileMode.Open, FileAccess.Read)) {
				using (var reader = new StreamReader(file)) {
					var properties = GetType().GetProperties();
					while (!reader.EndOfStream) {
						var line = reader.ReadLine().Trim();
						// Comment line?
						if (String.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#")) continue;
						if (char.IsDigit(line[0])) {
							bool input = false;
							bool output = false;
							int pos = line.IndexOf("<-");
							int pos3 = 0;
							if (pos > 0) {
								input = true;
								pos3 = pos + 2;
							}
							int pos2 = line.IndexOf("->");
							if (pos2 > 0) {
								if (pos < 0) pos = pos2;
								output = true;
								pos3 = pos2 + 2;
							}
							if (pos < 0) {
								Console.Error.WriteLine("Error in line: " + line);
								continue;
							}
							string id = line.Substring(0, pos).TrimEnd();
							int ihcid = int.Parse(id);
							string mqttmsg = line.Substring(pos3).Trim();
							if (input)
								IhcIn.Add(mqttmsg, ihcid);
							if (output)
								IhcOut.Add(ihcid, mqttmsg);
							continue;
						}
						// We will find the associated configuration property at set the value
						var s = line.Split(' ');
						string name = s[0].Trim();
						var p = properties.FirstOrDefault(x => String.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
						if (p == null) {
							Console.Error.WriteLine("Error in line: " + line);
							continue;
						}
						p.SetValue(this, s[1].Trim());
					}
				}
			}
			return true;
		}
	}
}
