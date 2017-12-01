
using System.Collections.Generic;
using UnityEngine;

namespace MSCMP.Game {
	/// <summary>
	/// Game animation database.
	/// </summary>
	public class GameAnimDatabase {

		public static GameAnimDatabase Instance = null;

		Dictionary<string, AnimationClip> gameClips = new Dictionary<string, AnimationClip>();
		Dictionary<string, AnimationClip> customClips = new Dictionary<string, AnimationClip>();

		public GameAnimDatabase() {
			Instance = this;
		}

		~GameAnimDatabase() {
			Instance = null;
		}

		/// <summary>
		/// Register custom animation clip from external source.
		/// </summary>
		/// <param name="clip">The clip to register.</param>
		public void RegisterCustomClip(AnimationClip clip) {
			customClips.Add(clip.name, clip);
		}

		/// <summary>
		/// Rebuilds the database.
		/// </summary>
		public void Rebuild() {
			gameClips.Clear();

			GameObject[] gos = GameObject.FindObjectsOfType<GameObject>();
			foreach (GameObject go in gos) {
				Animation anim = go.GetComponent<Animation>();
				if (anim == null) continue;
				foreach (AnimationState state in anim) {
					if (!gameClips.ContainsKey(state.name)) {
						gameClips.Add(state.name, state.clip);
					}
				}
			}
		}

		/// <summary>
		/// Get clip by it's name.
		/// </summary>
		/// <param name="name">Name of the clip to get.</param>
		/// <returns>Animation clip.</returns>
		public AnimationClip GetClipByName(string name)  {
			if (customClips.ContainsKey(name)) {
				return customClips[name];
			}
			return gameClips[name];
		}
	}
}
