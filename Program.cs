using Dingus.IHCSdkWR;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace IhcMqttGateway {

	class Program {

		static bool verbose = false;
		static string confpath = "ihcmqttgateway.conf";

		static Configuration conf = new Configuration();
		static IHCController ihc;
		static MqttClient mqttclient;

		static Dictionary<int, string> ihcstates;
		static Dictionary<int, string> mqttstates;
		static Dictionary<int, Type> ihctypes;


		static void Main(string[] args) {

			Console.WriteLine( String.Format( "IhcMqttGateway {0} (c) 2017 J.Ø.N. Dingus.dk", GetVersion()));
			if (!ProcessCmdLine(args)) return;
			if (!conf.Load( confpath)) {
				Console.Error.WriteLine("Error loading the configuration: ihcmqttgateway.conf");
				return;
			}

			ihcstates = new Dictionary<int, string>();
			mqttstates = new Dictionary<int, string>();
			ihctypes = new Dictionary<int, Type>();

			Console.WriteLine("Connectiong to ihc controller: " + conf.IhcUrl);
			ihc = new IHCController( conf.IhcUrl);
			if (!ihc.Authenticate( conf.IhcUser,conf.IhcPassword)) {
				Console.WriteLine("Authentication failed");
				return;
			}
			Console.WriteLine("Authentication succecfull");

			foreach( var i in conf.IhcOut) {
				ihc.RegisterNotify(i.Key, ResourceChanges);
			}
			mqttclient = new MqttClient( conf.MqttHost);
			mqttclient.MqttMsgPublishReceived += MqttMsgPublishReceived;
			string clientId = Guid.NewGuid().ToString();
			Console.WriteLine("Conneting to MQTT");
			mqttclient.Connect(clientId);
			Console.WriteLine("Subscribe to MQTT");
			// Subscribe to all topics!
			mqttclient.Subscribe(new string[] { "#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

			ihc.StartNotify();
			Console.WriteLine("READY  (press 'q' to exit)");

			while (Console.ReadKey().KeyChar != 'q') { }

			Console.WriteLine("\r\nDisconnecting");
			mqttclient.Disconnect();
			ihc.Disconnect();
		}

		static void MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {

			try {
				if (!conf.IhcIn.ContainsKey(e.Topic)) return;
				int ihcid = conf.IhcIn[e.Topic];
				string value = Encoding.ASCII.GetString(e.Message);
				if ( verbose)
					Console.Out.WriteLine("MQTT: " + e.Topic + " -> " + value);
				mqttstates[ihcid] = value;
				if (!ihctypes.ContainsKey(ihcid)) {
					var values = ihc.GetRuntimeValues(new int[] { ihcid });
					var v = values[0];
					ihctypes[ ihcid] = v.GetType();
					ihcstates[ihcid] = ValueToString(v);
				}
				if (ihcstates.ContainsKey(ihcid) && ihcstates[ihcid] == value) return;
				ihcstates[ihcid] = value;
				switch (ihctypes[ihcid].Name) {
					case "Boolean":
						ihc.SetRuntimeValueBool(ihcid, value == conf.BooleanTrue);
						break;
					case "Int32":
						ihc.SetRuntimeValueInt(ihcid, int.Parse( value));
						break;
					case "Double":
					case "Single":
						ihc.SetRuntimeValueFloat(ihcid, float.Parse(value));
						break;
				}
			}
#pragma warning disable CS0168
			catch (Exception ex) { 
			}

		}

		static private void ResourceChanges( int ihcid,object v) {

			try {
				if (!conf.IhcOut.ContainsKey(ihcid)) return;
				if (!ihctypes.ContainsKey(ihcid)) {
					ihctypes[ihcid] = v.GetType();
				}
				string value = ValueToString( v);
				if (verbose)
					Console.WriteLine("IHC change: " + ihcid.ToString() + "->" + value);
				string msg = conf.IhcOut[ihcid];
				ihcstates[ihcid] = value;
				if (mqttstates.ContainsKey(ihcid) && mqttstates[ihcid] == value) return;
				mqttstates[ihcid] = value;
				mqttclient.Publish(msg, Encoding.ASCII.GetBytes(value));
			}
#pragma warning disable CS0168
			catch (Exception e) {
			}
		}

		static String ValueToString(  object value) {

			if (value is bool) {
				bool b = (bool)value;
				return b ? conf.BooleanTrue : conf.BooleanFalse;
			}
			return value.ToString();
		}


		static private bool ProcessCmdLine(string[] args) {

			if (args.Length == 0) return true;
			int i = 0;
			if (args[0].StartsWith("-")) {
				if (args[0] == "-v") {
					verbose = true;
					i++;
				}
				else {
					Console.Error.WriteLine("Unknown option");
					Help();
					return false;
				}
			}
			if (args.Length <= i) return true;
			confpath = args[i];
			return true;
		}

		static private void Help() {

			Console.WriteLine("Syntax:");
			Console.WriteLine("IhcMqttGateway.exe [-v] pathtoconffile");
			Console.WriteLine("-v option for extra logging (IHC or Mqtt changes)");

		}
		static private string GetVersion() {
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}

	}
}
