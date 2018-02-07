namespace MSCMPMessages.Messages
{
	[NetMessageDesc(MessageIds.WeatherSync)]
	class WeatherUpdateMessage
	{
		/// <summary>
		/// 3 types of weather in My Summer Car.
		/// </summary>
		public enum WeatherType {
			RAIN = 0,
			THUNDER = 1,
			SUNNY = 2,
		}

		/// <summary>
		/// Current weather type
		/// </summary>
		WeatherType weatherType;

		/// <summary>
		/// Cloud GameObject X position.
		/// </summary>
		float weatherPos;

		/// <summary>
		/// Cloud GameObject Z position.
		/// </summary>
		float weatherPosSecond;

		/// <summary>
		/// Cloud Texture offset.
		/// </summary>
		float weatherOffset;

		/// <summary>
		/// Cloud GameObject rotation.
		/// </summary>
		float weatherRot;
	}
}
