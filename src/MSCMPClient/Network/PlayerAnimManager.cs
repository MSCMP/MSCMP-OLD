using System.Collections.Generic;
using UnityEngine;

namespace MSCMP.Network {
	/// <summary>
	/// Class managing the animations of the player.
	/// </summary>
	class PlayerAnimManager {
		public static PlayerAnimManager Instance = null;

		public PlayerAnimManager() {
			Instance = this;
		}

		~PlayerAnimManager() {
			Instance = null;
		}

		static List<AnimState> states = new List<AnimState>();

		static GameObject characterGameObject = null;
		static Animation characterAnimationComponent = null;

		/// <summary>
		/// Currently played animation id.
		/// </summary>
		public AnimationId currentAnim = AnimationId.Standing;
		public AnimationState activeAnimationState = null;

		/// <summary>
		/// The amount of movement packets needed left to send animation sync packet
		/// </summary>
		public int PACKETS_LEFT_TO_SYNC = 0;

		/// <summary>
		/// The total amount of movement packets we need
		/// </summary>
		public int PACKETS_TOTAL_FOR_SYNC = 2;

		#region Animations
		/// <summary>
		/// The animation ids.
		/// </summary>
		public enum AnimationId {
			Walk,
			Standing,
			Jumping,
			Drunk,
			Leaning,
			Finger,
			Hitchhike,
			Crouching,
			CrouchingLow,
			CrouchingWalk,
			CrouchingLowWalk,
			Running,
			Hitting,
			Pushing,
			Drinking
		}

		private string[] AnimationNames = new string[] {
			"Walk",
			"Idle",
			"Jump",
			"Drunk",
			"Lean",
			"Finger",
			"Hitchhike",
			"Crouch",
			"CrouchLow",
			"CrouchWalk",
			"CrouchLowWalk",
			"Run",
			"Hit",
			"Push",
			"Drink"
		};

		/// <summary>
		/// Convert animation id to it's name.
		/// </summary>
		/// <param name="animation">The id of the animation.</param>
		/// <returns>Name of the animation.</returns>
		private string GetAnimationName(AnimationId animation) {
			return AnimationNames[(int)animation];
		}
		#endregion
		#region Stances (Stand/Crouch/Crouch Low)
		private enum StanceId {
			Standing,
			Crouching,
			CrouchingLow
		}

		/// <summary>
		/// Convert stance id to animation id.
		/// </summary>
		/// <param name="stance">The id of the stance.</param>
		/// <param name="standingAnim">True if it's standing, or else moving.</param>
		/// <returns>Id of the animation.</returns>
		private AnimationId GetAnimationFromStance(StanceId stance, bool standingAnim = true) {
			switch (stance) {
				case StanceId.Crouching:
					if (standingAnim) return AnimationId.Crouching;
					else return AnimationId.CrouchingWalk;
				case StanceId.CrouchingLow:
					if (standingAnim) return AnimationId.CrouchingLow;
					else return AnimationId.CrouchingLowWalk;
				default:
					if (standingAnim) return AnimationId.Standing;
					else return AnimationId.Walk;
			}
		}

		#endregion
		#region HandStates
		/// <summary>
		/// The hand state ids.
		/// </summary>
		public enum HandStateId {
			MiddleFingering,
			Lifting,
			Hitting,
			Pushing,
			Drinking
		}

		/// <summary>
		/// The hand state GameObject names.
		/// </summary>
		private string[] HandStateNames = new string[] {
			"MiddleFinger",
			"Lift",
			"Fist",
			"Hand Push",
			"Drink/Hand"
		};

		/// <summary>
		/// Convert hand state id to it's name.
		/// </summary>
		/// <param name="handState">The id of the hand state.</param>
		/// <returns>Name of the hand state.</returns>
		public HandStateId GetHandState(byte handState) {
			return (HandStateId)handState;
		}

		/// <summary>
		/// Gets the active hand state of the gameObject
		/// </summary>
		/// <param name="gameObject">The object to get it from.</param>
		/// <returns>The ID of the active state or else 255 if none.</returns>
		public byte GetActiveHandState(GameObject gameObject) {
			GameObject HandHandleObject = gameObject.transform.FindChild("Pivot/Camera/FPSCamera/FPSCamera").gameObject;

			for (byte i = 0; i < HandStateNames.Length; i++) {
				string HandStateName = HandStateNames[i];
				GameObject HandStateObject = HandHandleObject.transform.FindChild(HandStateName).gameObject;

				if (HandStateObject.activeInHierarchy) return i;
			}

			return 255;
		}
		#endregion
		#region Drink States
		static List<GameObject> Drinks = new List<GameObject>();
		GameObject ourDrinkObject = null;

		/// <summary>
		/// The drink GameObject names.
		/// </summary>
		private string[] DrinkObjectNames = new string[] {
			"HandJuice",
			"HandMilk",
			"HandSpray",
			"Coffee",
			"CoffeeGranny",
			"BeerBottle",
			"BoozeBottle",
			"ShotGlass",
			"MilkGlass"
		};

		private float[,] DrinkOffsets = new float[,] {
			{ -0.008f, -0.016f, 0.005f },
			{ -0.025f, -0.02f, 0.015f },
			{ -0.01f, 0.0f, 0.01f },
			{ -0.015f, 0.01f, 0.01f },
			{ -0.015f, 0.011f, 0.01f },
			{ -0.012f, -0.008f, 0.015f },
			{ -0.02f, -0.02f, 0.021f },
			{ -0.02f, 0.01f, 0.01f },
			{ -0.02f, 0.005f, 0.012f }
		};

		private float[,] DrinkRotations = new float[,] {
			{ 5, 140, 295 },
			{ 5, 140, 295 },
			{ 350, 190, 210 },
			{ 310, 150, 273 },
			{ 310, 150, 273 },
			{ 308, 147, 295 },
			{ 308, 147, 295 },
			{ 308, 147, 295 },
			{ 310, 150, 273 }
		};

		public bool AreDrinksPreloaded() { return Drinks.Count != 0; }

		/// <summary>
		/// Preloads the drink game objects of the game player to use them later while drinking
		/// </summary>
		/// <param name="character">The player object to get the drink objects from</param>
		public void PreloadDrinkObjects(GameObject character) {
			GameObject HandHandleObject = character.transform.FindChild("Pivot/Camera/FPSCamera/FPSCamera/Drink/Hand").gameObject;

			for (byte i = 0; i < DrinkObjectNames.Length; i++) {
				GameObject DrinkObject = HandHandleObject.transform.FindChild(DrinkObjectNames[i]).gameObject;
				Drinks.Add(DrinkObject);
			}
		}

		/// <summary>
		/// Gets the drink game object player is using
		/// </summary>
		/// <param name="character">The player object to get the drink object from</param>
		/// <returns>255 if player is not drinking, or else its ID</returns>
		public byte GetDrinkingObject(GameObject character) {
			GameObject HandHandleObject = character.transform.FindChild("Pivot/Camera/FPSCamera/FPSCamera/Drink/Hand").gameObject;

			for (byte i = 0; i < DrinkObjectNames.Length; i++) {
				GameObject DrinkObject = HandHandleObject.transform.FindChild(DrinkObjectNames[i]).gameObject;
				if (DrinkObject.activeInHierarchy) return i;
			}

			return 255;
		}

		/// <summary>
		/// Sets the drinking object for the specific player
		/// </summary>
		/// <param name="character">The player object to set the drink object</param>
		/// /// <param name="drinkingObjectId">The id of the drink object</param>
		public void SetDrinkingObject(byte drinkingObjectId) {
			if (drinkingObjectId == 255) {
				PlayActionAnim(AnimationId.Drinking, false);

				if (ourDrinkObject != null) {
					GameObject.DestroyObject(ourDrinkObject);
					ourDrinkObject = null;
				}
				return;
			}

			string drinkObjectName = DrinkObjectNames[(int)drinkingObjectId];

			GameObject ourDrinkObjectToSpawn = null;
			foreach (GameObject drink in Drinks) {
				if (drink.name == drinkObjectName) ourDrinkObjectToSpawn = drink;
			}

			if (ourDrinkObject != null) GameObject.DestroyObject(ourDrinkObject);
			ourDrinkObject = GameObject.Instantiate(ourDrinkObjectToSpawn);

			ourDrinkObject.SetActive(true);

			Transform playerFingers = characterGameObject.transform.FindChild("pelvis/spine_mid/shoulders/collar_left/shoulder(leftx)/arm(leftx)/hand_left/finger_left");
			ourDrinkObject.transform.SetParent(playerFingers);

			ourDrinkObject.transform.localPosition = new Vector3(DrinkOffsets[drinkingObjectId, 0], DrinkOffsets[drinkingObjectId, 1], DrinkOffsets[drinkingObjectId, 2]);
			ourDrinkObject.transform.localEulerAngles = new Vector3(DrinkRotations[drinkingObjectId, 0], DrinkRotations[drinkingObjectId, 1], DrinkRotations[drinkingObjectId, 2]);
			ourDrinkObject.layer = 0;

			if (ourDrinkObject.transform.childCount != 0) {
				if (ourDrinkObject.name.StartsWith("Hand")) {
					ourDrinkObject.transform.GetChild(2).gameObject.layer = 0;
					ourDrinkObject.transform.GetChild(2).localPosition = new Vector3(0, 0, 0);

					GameObject.DestroyObject(ourDrinkObject.transform.GetChild(0).gameObject); //Destroying 'Armature'
					GameObject.DestroyObject(ourDrinkObject.transform.GetChild(1).gameObject); //Destroying 'hand_rigged'
				}
				else ourDrinkObject.transform.GetChild(0).gameObject.layer = 0;
			}

			PlayActionAnim(AnimationId.Drinking, true);
		}
		#endregion

		/// <summary>
		/// Sets up the animation component and the layers for each animation. Also registers the animation states.
		/// </summary>
		/// <param name="animComponent">The animation component of the player.</param>
		public void SetupAnimations(GameObject character) {
			characterGameObject = character;
			characterAnimationComponent = characterGameObject.GetComponentInChildren<Animation>();

			characterAnimationComponent["Jump"].layer = 1;
			characterAnimationComponent["Drunk"].layer = 2;
			characterAnimationComponent["Drunk"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Lean"].layer = 3;
			characterAnimationComponent["Lean"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Finger"].layer = 3;
			characterAnimationComponent["Finger"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Hitchhike"].layer = 3;
			characterAnimationComponent["Hitchhike"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Hit"].layer = 3;
			characterAnimationComponent["Hit"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Push"].layer = 3;
			characterAnimationComponent["Push"].blendMode = AnimationBlendMode.Additive;
			characterAnimationComponent["Drink"].layer = 3;
			characterAnimationComponent["Drink"].blendMode = AnimationBlendMode.Additive;

			RegisterAnimStates();
		}

		/// <summary>
		/// Play selected animation.
		/// </summary>
		/// <param name="animation">The id of the animation.</param>
		/// <param name="force">If it should be forced, or just crossfaded</param>
		/// <param name="mainLayer">If it's into the main movement layer, or an action to be played simultaneously</param>
		public void PlayAnimation(AnimationId animation, bool force = false, bool mainLayer = true) {
			if (characterAnimationComponent == null) return;
			if (!force && currentAnim == animation && mainLayer) return;

			string animName = GetAnimationName(animation);

			if (force) characterAnimationComponent.Play(animName);
			else characterAnimationComponent.CrossFade(animName);

			if (mainLayer) {
				currentAnim = animation;
				activeAnimationState = characterAnimationComponent[animName];
			}
		}

		/// <summary>
		/// Blends out an animation smoothly (it's not getting disabled that way. That's why we use 'CheckBlendedOutAnimationStates' function to disable it there)
		/// </summary>
		/// <param name="animation">The name of the animation</param>
		private void BlendOutAnimation(AnimationId animation) {
			if (characterAnimationComponent == null) return;
			characterAnimationComponent.Blend(GetAnimationName(animation), 0);
		}

		/// <summary>
		/// Plays an Action Animation from start to end, or the opposite
		/// </summary>
		/// <param name="animName">The name of the animation</param>
		/// <param name="play">Start or Stop the animation</param>
		private void PlayActionAnim(AnimationId animation, bool play) {
			if (characterAnimationComponent == null) return;
			string animName = GetAnimationName(animation);

			if (play) {
				characterAnimationComponent[animName].wrapMode = WrapMode.ClampForever;
				characterAnimationComponent[animName].speed = 1;
				characterAnimationComponent[animName].enabled = true;
				characterAnimationComponent[animName].weight = 1.0f;
			}
			else {
				characterAnimationComponent[animName].wrapMode = WrapMode.Once;
				if (characterAnimationComponent[animName].time > characterAnimationComponent[animName].length) {
					characterAnimationComponent[animName].time = characterAnimationComponent[animName].length;
				}
				characterAnimationComponent[animName].speed = -1;
				characterAnimationComponent[animName].weight = 1.0f;
			}
		}

		/// <summary>
		/// Check if an animation has been blended with 0 weight and disables it
		/// </summary>
		public void CheckBlendedOutAnimationStates() {
			if (characterAnimationComponent == null) return;

			if (characterAnimationComponent["Jump"].time != 0.0f && characterAnimationComponent["Jump"].weight == 0.0f) {
				characterAnimationComponent["Jump"].enabled = false;
				characterAnimationComponent["Jump"].time = 0;
			}

			if (characterAnimationComponent["Drunk"].time != 0.0f && characterAnimationComponent["Drunk"].weight == 0.0f) {
				characterAnimationComponent["Drunk"].enabled = false;
				characterAnimationComponent["Drunk"].time = 0;
			}
		}

		private class AnimState : PlayerAnimManager {
			bool isActive = false;

			public virtual bool CanActivate(Messages.AnimSyncMessage msg) {
				// condition if this state can be activated (must also return true if state is active)
				return false;
			}

			public virtual void Activate() {
				// start anim here
			}

			public virtual void Deactivate() {
				// stop anim here
			}

			public void TryActivate(Messages.AnimSyncMessage msg) {
				bool canActivate = CanActivate(msg);
				if (!isActive && canActivate) {
					Activate();
					isActive = true;
				}
				else if (isActive && !canActivate) {
					Deactivate();
					isActive = false;
				}
			}
		}

		private class LeaningState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return msg.isLeaning; }
			public override void Activate() { PlayActionAnim(AnimationId.Leaning, true); }
			public override void Deactivate() { PlayActionAnim(AnimationId.Leaning, false); }
		}

		private class JumpState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return !msg.isGrounded; }
			public override void Activate() { PlayAnimation(AnimationId.Jumping, false, false); }
			public override void Deactivate() { BlendOutAnimation(AnimationId.Jumping); }
		}

		private class FingerState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return GetHandState(msg.activeHandState) == HandStateId.MiddleFingering; }
			public override void Activate() { PlayActionAnim(AnimationId.Finger, true); }
			public override void Deactivate() { PlayActionAnim(AnimationId.Finger, false); }
		}

		private class HitchhikeState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return GetHandState(msg.activeHandState) == HandStateId.Lifting; }
			public override void Activate() { PlayActionAnim(AnimationId.Hitchhike, true); }
			public override void Deactivate() { PlayActionAnim(AnimationId.Hitchhike, false); }
		}

		private class DrunkState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return msg.isDrunk; }
			public override void Activate() { PlayAnimation(AnimationId.Drunk, false, false); }
			public override void Deactivate() { BlendOutAnimation(AnimationId.Drunk); }
		}

		private class HitState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return GetHandState(msg.activeHandState) == HandStateId.Hitting; }
			public override void Activate() { PlayActionAnim(AnimationId.Hitting, true); }
			public override void Deactivate() { PlayActionAnim(AnimationId.Hitting, false); }
		}

		private class PushState : AnimState {
			public override bool CanActivate(Messages.AnimSyncMessage msg) { return GetHandState(msg.activeHandState) == HandStateId.Pushing; }
			public override void Activate() { PlayActionAnim(AnimationId.Pushing, true); }
			public override void Deactivate() { PlayActionAnim(AnimationId.Pushing, false); }
		}

		private static void RegisterAnimStates() {
			states.Add(new LeaningState());
			states.Add(new JumpState());
			states.Add(new FingerState());
			states.Add(new HitchhikeState());
			states.Add(new DrunkState());
			states.Add(new HitState());
			states.Add(new PushState());
		}

		//Animation Variables
		bool isRunning = false;
		float aimRot = 0.0f;
		StanceId currentStance = StanceId.Standing;
		byte currentDrinkId = 255;

		/// <summary>
		/// Handles the Action Animations
		/// </summary>
		public void HandleAnimations(Messages.AnimSyncMessage msg) {
			isRunning = msg.isRunning;
			aimRot = msg.aimRot;
			HandleCrouchStates(msg.crouchPosition);
			HandleDrinking(msg.drinkId);
			HandleSwearing(msg.swearId);

			foreach (AnimState state in states) {
				state.TryActivate(msg);
			}
		}

		/// <summary>
		/// Handles the Foot Movement Animations
		/// </summary>
		public void HandleOnFootMovementAnimations(float speed) {
			if (speed > 0.001f) { //Moving
				if (isRunning) PlayAnimation(AnimationId.Running); //Running
				else PlayAnimation(GetAnimationFromStance(currentStance, false)); //Walking
			}
			else PlayAnimation(GetAnimationFromStance(currentStance)); //Standing
		}

		/// <summary>
		/// Moves the head according to the vertical look position
		/// </summary>
		public void SyncVerticalHeadLook(GameObject characterGameObject, float progress) {
			Transform head = characterGameObject.transform.FindChild("pelvis/spine_mid/shoulders/head");
			//float newAimRot = Mathf.LerpAngle(head.rotation.eulerAngles.z, aimRot, progress); COMMENTED OUT CAUSE IT DOESNT WORK! NEED NEW INTERPOLATION
			head.rotation *= Quaternion.Euler(0, 0, -aimRot);
		}

		/// <summary>
		/// Takes care of crouch states
		/// </summary>
		private void HandleCrouchStates(float crouchRotation) {
			if (crouchRotation < 0.85f) currentStance = StanceId.CrouchingLow;
			else if (crouchRotation < 1.4f) currentStance = StanceId.Crouching;
			else currentStance = StanceId.Standing;
		}

		/// <summary>
		/// Takes care of drinking objects and animation
		/// </summary>
		private void HandleDrinking(byte DrinkID) {
			byte oldDrinkingId = currentDrinkId;
			currentDrinkId = DrinkID;

			if (oldDrinkingId != currentDrinkId) SetDrinkingObject(currentDrinkId);
		}

		/// <summary>
		/// Takes care of Finger Swearing/Swearing/Saying 'Yes'/Drunk Speaking
		/// </summary>
		/// <param name="SwearID"></param>
		private void HandleSwearing(int SwearID) {
			if (SwearID != int.MaxValue && SwearID != currentSwearId) {
				if (SwearID >= DrunkSpeaking_Offset) MasterAudio.PlaySound3DFollowTransformAndForget("Drunk", characterGameObject.transform, 1, 1, 0, SwearID.ToString());
				else if (SwearID >= Agreeing_Offset) MasterAudio.PlaySound3DFollowTransformAndForget("Yes", characterGameObject.transform, 8, 1, 0, SwearID.ToString());
				else if (SwearID >= Swears_Offset) MasterAudio.PlaySound3DFollowTransformAndForget("Swearing", characterGameObject.transform, 1, 1, 0, SwearID.ToString());
				else MasterAudio.PlaySound3DFollowTransformAndForget("Fuck", characterGameObject.transform, 1, 1, 0, SwearID.ToString());
			}
			currentSwearId = SwearID;
		}
		int currentSwearId = int.MaxValue;

		public int Swears_Offset = 100;
		public int Agreeing_Offset = 200;
		public int DrunkSpeaking_Offset = 300;
	}
}
