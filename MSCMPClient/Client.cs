using UnityEngine;

namespace MSCMP
{
    public class Client
    {

		public static void Start() {
			GameObject go = new GameObject("Multiplayer Controller");
			go.AddComponent<MPGameObject>();

		}



	}
}
