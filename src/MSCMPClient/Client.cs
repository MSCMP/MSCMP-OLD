using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MSCMP
{
    public class Client
    {

		public static void Start() {


			GameObject go = new GameObject("Multiplayer Controller");
			go.AddComponent<MPController>();
		}

		public static string GetPath(string file) {
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + file;
		}

	}
}
