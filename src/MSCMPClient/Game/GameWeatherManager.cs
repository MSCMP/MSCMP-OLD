using HutongGames.PlayMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace MSCMP.Game
{
	/// <summary>
	/// Class managing state of the doors in game.
	/// </summary>
	class GameWeatherManager
	{
		private const string RAIN_ENAME = "MPRAIN";
		private const string THUNDER_ENAME = "MPTHUNDER";
		private const string SUNNY_ENAME = "MPNOWEATHER";

		/// <summary>
		/// Weather manager instance.
		/// </summary>
		public static GameWeatherManager Instance = null;

		/// <summary>
		/// Play Maker FSM for weather system.
		/// </summary>
		public PlayMakerFSM weatherSystemFSM = null;

		public string WeatherDebug {
			get {
				return $"Current Weather: {CurrentWeather}\n -> Pos: {WeatherPos}\n -> Pos2: {WeatherPosSecond}\n -> Offset: {WeatherOffset}\n -> Rot: {WeatherRot}";
			}
		}

		/// <summary>
		/// Current weather.
		/// </summary>
		public Network.Messages.WeatherType CurrentWeather {
			get {
				switch (weatherSystemFSM.Fsm.PreviousActiveState.Name)
				{
					case "Rain":
						return Network.Messages.WeatherType.RAIN;
					case "Thunder":
						return Network.Messages.WeatherType.THUNDER;
					case "No weather":
						return Network.Messages.WeatherType.SUNNY;
				}
				return Network.Messages.WeatherType.SUNNY;
			}
		}

		/// <summary>
		/// Cloud GameObject X position.
		/// </summary>
		public float WeatherPos {
			get {
				return weatherSystemFSM.FsmVariables.GetFsmFloat("Pos").Value;
			}

			set {
				weatherSystemFSM.FsmVariables.GetFsmFloat("Pos").Value = value;
			}
		}

		/// <summary>
		/// Cloud GameObject Z position.
		/// </summary>
		public float WeatherPosSecond {
			get {
				return weatherSystemFSM.FsmVariables.GetFsmFloat("Pos2").Value;
			}

			set {
				weatherSystemFSM.FsmVariables.GetFsmFloat("Pos2").Value = value;
			}
		}

		/// <summary>
		/// Cloud texture offset.
		/// </summary>
		public float WeatherOffset {
			get {
				return weatherSystemFSM.FsmVariables.GetFsmFloat("Offset").Value;
			}

			set {
				weatherSystemFSM.FsmVariables.GetFsmFloat("Offset").Value = value;
			}
		}

		/// <summary>
		/// Cloud GameObject rotation.
		/// </summary>
		public float WeatherRot {
			get {
				return weatherSystemFSM.FsmVariables.GetFsmFloat("Rot").Value;
			}

			set {
				weatherSystemFSM.FsmVariables.GetFsmFloat("Rot").Value = value;
			}
		}

		public GameWeatherManager() {
			Instance = this;

			GameCallbacks.onWorldLoad += () => {
				OnWorldLoad();
			};
		}

		/// <summary>
		/// Finds Cloud System and adds custom Multiplayer weather events.
		/// </summary>
		public void OnWorldLoad() {
			GameObject cloudSystem = GameObject.Find("Clouds");

			Client.Assert(cloudSystem != null, "cloudSystem couldn't be found!");

			weatherSystemFSM = Utils.GetPlaymakerScriptByName(cloudSystem, "Weather");

			FsmEvent rainEvent = weatherSystemFSM.Fsm.GetEvent(RAIN_ENAME);
			FsmEvent thunderEvent = weatherSystemFSM.Fsm.GetEvent(THUNDER_ENAME);
			FsmEvent noweatherEvent = weatherSystemFSM.Fsm.GetEvent(SUNNY_ENAME);

			PlayMakerUtils.AddNewGlobalTransition(weatherSystemFSM, rainEvent, "Rain");
			PlayMakerUtils.AddNewGlobalTransition(weatherSystemFSM, thunderEvent, "Thunder");
			PlayMakerUtils.AddNewGlobalTransition(weatherSystemFSM, noweatherEvent, "No weather");
		}

		/// <summary>
		/// Set current weather from network message.
		/// </summary>
		/// <param name="message">Message to set weather state from.</param>
		public void SetWeather(Network.Messages.WeatherUpdateMessage message) {
			if (message.weatherType != CurrentWeather) {
				switch (message.weatherType) {
					case Network.Messages.WeatherType.RAIN:
						weatherSystemFSM.SendEvent(RAIN_ENAME);
						break;
					case Network.Messages.WeatherType.THUNDER:
						weatherSystemFSM.SendEvent(THUNDER_ENAME);
						break;
					case Network.Messages.WeatherType.SUNNY:
						weatherSystemFSM.SendEvent(SUNNY_ENAME);
						break;
				}
			}
			WeatherPos = message.weatherPos;
			WeatherPosSecond = message.weatherPosSecond;
			WeatherOffset = message.weatherOffset;
			WeatherRot = message.weatherRot;
		}

		/// <summary>
		/// Writes weather state into network update message.
		/// </summary>
		/// <param name="message">The message to write weather state to.</param>
		public void WriteWeather(Network.Messages.WeatherUpdateMessage message) {
			message.weatherType = CurrentWeather;
			message.weatherPos = WeatherPos;
			message.weatherPosSecond = WeatherPosSecond;
			message.weatherOffset = WeatherOffset;
			message.weatherRot = WeatherRot;
		}
	}
}
