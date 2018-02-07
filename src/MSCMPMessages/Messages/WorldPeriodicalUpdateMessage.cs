using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSCMPMessages.Messages {
	[NetMessageDesc(MessageIds.WorldPeriodicalUpdate)]
	class WorldPeriodicalUpdateMessage {
		/// <summary>
		/// The sun clock with 2-hour precision used for visual time sync.
		/// </summary>
		Byte sunClock;

		/// <summary>
		/// Current world day of the week.
		/// </summary>
		Byte worldDay;

		/// <summary>
		/// Current in game weather.
		/// </summary>
		WeatherUpdateMessage currentWeather;
	}
}
