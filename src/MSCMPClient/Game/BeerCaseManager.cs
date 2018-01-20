using System.Collections.Generic;
using UnityEngine;
using MSCMP.Game.Objects;
using MSCMP.Game;

namespace MSCMP.Game {
	/// <summary>
	/// Class managing state of the beercases in game.
	/// </summary>
	class BeerCaseManager {
		/// <summary>
		/// Singleton of the beercase manager.
		/// </summary>
		public static BeerCaseManager Instance = null;

		/// <summary>
		/// List of the beercases.
		/// </summary>
		public List<BeerCase> beercases = new List<BeerCase>();

		/// <summary>
		/// Amount of bottles in a full beercase.
		/// </summary>
		public int FullCaseBottles = 24;

		public delegate void OnBottleConsumed(GameObject beer);

		/// <summary>
		/// Callback called when player consumes a beer bottle
		/// </summary>
		public OnBottleConsumed onBottleConsumed;

		public BeerCaseManager() {
			Instance = this;
		}

		~BeerCaseManager() {
			Instance = null;
		}

		/// <summary>
		/// Builds beercase list on world load.
		/// </summary>
		public void OnWorldLoad() {
			beercases.Clear();
			GameObject[] gos = GameObject.FindObjectsOfType<GameObject>();

			//Register all beercases in game.
			foreach (var go in gos) {
				//AddBeerCase(go);
			}
		}

		/// <summary>
		/// Adds beercase by GameObject
		/// </summary>
		/// <param name="beerGO">BeerCase GameObject.</param>
		public void AddBeerCase(GameObject beerGO) {
			var metaData = beerGO.GetComponent<Game.Components.PickupableMetaDataComponent>();

			if (metaData.PrefabDescriptor.type == GamePickupableDatabase.PrefabType.BeerCase) {
				bool isDuplicate = false;

				foreach(BeerCase beer in beercases) {
					GameObject beerGameObject = beer.GetGameObject;
					if (beerGameObject == beerGO) {
						Logger.Debug($"Duplicate beercase rejected: {beerGameObject.name}");
						isDuplicate = true;
					}
				}
				if (isDuplicate == false) {
					BeerCase beer = new BeerCase(beerGO);
					beercases.Add(beer);

					beer.onConsumedBeer = (beerObj) => {
						onBottleConsumed(beer.GetGameObject);
					};
				}
			}
		}

		/// <summary>
		/// Find beercases from GameObject
		/// </summary>
		/// <param name="name">BeerCase.</param>
		/// <returns></returns>
		public BeerCase FindBeerCase(GameObject beerGO) {
			foreach (var beer in beercases) {
				if (beer.GetGameObject == beerGO) {
					Logger.Debug($"Found beercase! {beer.GetGameObject.name}");
					return beer;
				}
			}
			return null;
		}

		/// <summary>
		/// Sets bottle count of beercase.
		/// </summary>
		/// <param name="beerGO"></param>
		/// <param name="bottleCount"></param>
		public void SetBottleCount(GameObject beerGO, int bottleCount) {
			BeerCase beer = FindBeerCase(beerGO);
			if (beer != null) {
				beer.RemoveBottles(BeerCaseManager.Instance.FullCaseBottles - bottleCount);
			}
			else {
				Logger.Log("SetBottleCount: Beercase not found!");
			}
		}
	}
}
