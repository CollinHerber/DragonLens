﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace DragonLens.Core.Loaders.UILoading
{
	/// <summary>
	/// Automatically loads SmartUIStates ala IoC.
	/// </summary>
	class UILoader : ILoadable
	{
		/// <summary>
		/// The collection of automatically craetaed UserInterfaces for SmartUIStates.
		/// </summary>
		public static List<UserInterface> UserInterfaces = new();

		/// <summary>
		/// The collection of all automatically loaded SmartUIStates.
		/// </summary>
		public static List<SmartUIState> UIStates = new();

		/// <summary>
		/// Uses reflection to scan through and find all types extending SmartUIState that arent abstract, and loads an instance of them.
		/// </summary>
		/// <param name="mod"></param>
		public void Load(Mod mod)
		{
			if (Main.dedServ)
				return;

			UserInterfaces = new List<UserInterface>();
			UIStates = new List<SmartUIState>();

			foreach (Type t in mod.Code.GetTypes())
			{
				if (!t.IsAbstract && t.IsSubclassOf(typeof(SmartUIState)))
				{
					var state = (SmartUIState)Activator.CreateInstance(t, null);
					var userInterface = new UserInterface();
					userInterface.SetState(state);
					state.UserInterface = userInterface;

					UIStates?.Add(state);
					UserInterfaces?.Add(userInterface);
				}
			}
		}

		public void Unload()
		{
			UIStates.ForEach(n => n.Unload());
			UserInterfaces = null;
			UIStates = null;
		}

		/// <summary>
		/// Helper method for creating and inserting a LegacyGameInterfaceLayer automatically
		/// </summary>
		/// <param name="layers">The vanilla layers</param>
		/// <param name="userInterface">the UserInterface to bind to the layer</param>
		/// <param name="state">the UIState to bind to the layer</param>
		/// <param name="index">Where this layer should be inserted</param>
		/// <param name="visible">The logic dictating the visibility of this layer</param>
		/// <param name="scale">The scale settings this layer should scale with</param>
		public static void AddLayer(List<GameInterfaceLayer> layers, UserInterface userInterface, UIState state, int index, bool visible, InterfaceScaleType scale)
		{
			string name = state == null ? "Unknown" : state.ToString();
			layers.Insert(index, new LegacyGameInterfaceLayer("BrickAndMortar: " + name,
				delegate
				{
					if (visible)
					{
						userInterface.Update(Main._drawInterfaceGameTime);
						state.Draw(Main.spriteBatch);
					}

					return true;
				}, scale));
		}

		/// <summary>
		/// Gets the autoloaded SmartUIState instance for a given SmartUIState subclass
		/// </summary>
		/// <typeparam name="T">The SmartUIState subclass to get the instance of</typeparam>
		/// <returns>The autoloaded instance of the desired SmartUIState</returns>
		public static T GetUIState<T>() where T : SmartUIState
		{
			return UIStates.FirstOrDefault(n => n is T) as T;
		}

		/// <summary>
		/// Forcibly reloads a SmartUIState and it's associated UserInterface
		/// </summary>
		/// <typeparam name="T">The SmartUIState subclass to reload</typeparam>
		public static void ReloadState<T>() where T : SmartUIState
		{
			int index = UIStates.IndexOf(GetUIState<T>());
			UIStates[index] = (T)Activator.CreateInstance(typeof(T), null);
			UserInterfaces[index] = new UserInterface();
			UserInterfaces[index].SetState(UIStates[index]);
		}
	}

	/// <summary>
	/// Handles the insertion of the automatically loaded UIs
	/// </summary>
	class AutoUISystem : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			for (int k = 0; k < UILoader.UIStates.Count; k++)
			{
				SmartUIState state = UILoader.UIStates[k];
				UILoader.AddLayer(layers, UILoader.UserInterfaces[k], state, state.InsertionIndex(layers), state.Visible, state.Scale);
			}
		}
	}
}
