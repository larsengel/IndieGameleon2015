/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2014
 *	
 *	"SceneChanger.cs"
 * 
 *	This script handles the changing of the scene, and stores
 *	which scene was previously loaded, for use by PlayerStart.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Handles the changing of the scene, and keeps track of which scene was previously loaded.
	 * It should be placed on the PersistentEngine prefab.
	 */
	public class SceneChanger : MonoBehaviour
	{

		/** Info about the previous scene */
		public SceneInfo previousSceneInfo;

		private Player playerOnTransition = null;
		private Texture2D textureOnTransition = null;
		private bool isLoading = false;
		private float loadingProgress = 0f;


		private void Awake ()
		{
			previousSceneInfo = new SceneInfo ("", -1);
			isLoading = false;
		}


		public float GetLoadingProgress ()
		{
			if (KickStarter.settingsManager.useAsyncLoading)
			{
				if (isLoading)
				{
					return loadingProgress;
				}
			}
			else
			{
				Debug.LogWarning ("Cannot get the loading progress because asynchronous loading is not enabled in the Settings Manager.");
			}
			return 0f;
		}


		public bool IsLoading ()
		{
			return isLoading;
		}


		/**
		 * <summary>Loads a new scene.</summary>
		 * <param name = "nextSceneInfo">Info about the scene to load</param>
		 * <param name = "sceneNumber">The number of the scene to load, if sceneName = ""</param>
		 * <param name = "saveRoomData">If True, then the states of the current scene's Remember scripts will be recorded in LevelStorage</param>
		 */
		public void ChangeScene (SceneInfo nextSceneInfo, bool saveRoomData)
		{
			if (!isLoading)
			{
				PrepareSceneForExit (!KickStarter.settingsManager.useAsyncLoading, saveRoomData);
				LoadLevel (nextSceneInfo, KickStarter.settingsManager.useLoadingScreen, KickStarter.settingsManager.useAsyncLoading);
			}
		}


		/**
		 * <summary>Gets the Player prefab that was active during the last scene transition.</summary>
		 * <returns>The Player prefab that was active during the last scene transition</returns>
		 */
		public Player GetPlayerOnTransition ()
		{
			return playerOnTransition;
		}


		/**
		 * Destroys the Player prefab that was active during the last scene transition.
		 */
		public void DestroyOldPlayer ()
		{
			if (playerOnTransition)
			{
				Debug.Log ("New player prefab found - " + playerOnTransition.name + " deleted");
				DestroyImmediate (playerOnTransition.gameObject);
			}
		}


		/*
		 * <summary>Stores a texture used as an overlay during a scene transition. This texture can be retrieved with GetAndResetTransitionTexture().</summary>
		 * <param name = "_texture">The Texture2D to store</param>
		 */
		public void SetTransitionTexture (Texture2D _texture)
		{
			textureOnTransition = _texture;
		}


		/**
		 * <summary>Gets, and removes from memory, the texture used as an overlay during a scene transition.</summary>
		 * <returns>The texture used as an overlay during a scene transition</returns>
		 */
		public Texture2D GetAndResetTransitionTexture ()
		{
			Texture2D _texture = textureOnTransition;
			textureOnTransition = null;
			return _texture;
		}


		private void LoadLevel (SceneInfo nextSceneInfo, bool useLoadingScreen, bool useAsyncLoading)
		{
			if (useLoadingScreen)
			{
				StartCoroutine (LoadLoadingScreen (nextSceneInfo, new SceneInfo (KickStarter.settingsManager.loadingSceneIs, KickStarter.settingsManager.loadingSceneName, KickStarter.settingsManager.loadingScene), useAsyncLoading));
			}
			else
			{
				if (useAsyncLoading)
				{
					StartCoroutine (LoadLevelAsync (nextSceneInfo));
				}
				else
				{
					StartCoroutine (LoadLevel (nextSceneInfo));
				}
			}
		}


		private IEnumerator LoadLoadingScreen (SceneInfo nextSceneInfo, SceneInfo loadingSceneInfo, bool loadAsynchronously = false)
		{
			isLoading = true;
			loadingProgress = 0f;

			if (KickStarter.player != null)
			{
				KickStarter.player.transform.position += new Vector3 (0f, -10000f, 0f);
			}

			loadingSceneInfo.LoadLevel ();
			yield return null;

			PrepareSceneForExit (true, false);
			if (loadAsynchronously)
			{
				yield return new WaitForSeconds (KickStarter.settingsManager.loadingDelay);

				AsyncOperation aSync = nextSceneInfo.LoadLevelASync ();
				if (KickStarter.settingsManager.loadingDelay > 0f)
				{
					aSync.allowSceneActivation = false;

					while (aSync.progress < 0.9f)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				
					isLoading = false;
					yield return new WaitForSeconds (KickStarter.settingsManager.loadingDelay);
					aSync.allowSceneActivation = true;
				}
				else
				{
					while (!aSync.isDone)
					{
						loadingProgress = aSync.progress;
						yield return null;
					}
				}
				KickStarter.stateHandler.GatherObjects ();
			}
			else
			{
				nextSceneInfo.LoadLevel ();
			}

			isLoading = false;
		}


		private IEnumerator LoadLevelAsync (SceneInfo nextSceneInfo)
		{
			isLoading = true;
			loadingProgress = 0f;
			PrepareSceneForExit (true, false);

			AsyncOperation aSync = nextSceneInfo.LoadLevelASync ();
			while (!aSync.isDone)
			{
				loadingProgress = aSync.progress;
				yield return null;
			}

			KickStarter.stateHandler.GatherObjects ();
			isLoading = false;
		}


		private IEnumerator LoadLevel (SceneInfo nextSceneInfo)
		{
			isLoading = true;
			yield return new WaitForEndOfFrame ();

			nextSceneInfo.LoadLevel ();
			isLoading = false;
		}


		private void PrepareSceneForExit (bool isInstant, bool saveRoomData)
		{
			if (isInstant)
			{
				KickStarter.mainCamera.FadeOut (0f);
				
				if (KickStarter.player)
				{
					KickStarter.player.Halt ();
					
					if (KickStarter.settingsManager.movementMethod == MovementMethod.UltimateFPS)
					{
						UltimateFPSIntegration.SetCameraEnabled (false, true);
					}
				}
				
				KickStarter.stateHandler.gameState = GameState.Normal;
			}
			
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				sound.TryDestroy ();
			}
			KickStarter.stateHandler.GatherObjects ();
			
			KickStarter.playerMenus.ClearParents ();
			if (KickStarter.dialog)
			{
				KickStarter.dialog.KillDialog (true, true);
			}
			
			if (saveRoomData)
			{
				KickStarter.levelStorage.StoreCurrentLevelData ();
				previousSceneInfo = new SceneInfo (Application.loadedLevelName, Application.loadedLevel);
			}

			playerOnTransition = KickStarter.player;
		}

	}


	/**
	 * A container for information about a scene that can be loaded.
	 */
	public struct SceneInfo
	{

		/** The scene's name */
		public string name;
		/** The scene's number. If name is left empty, this number will be used to reference the scene instead */
		public int number;


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (string _name, int _number)
		{
			number = _number;
			name = _name;
		}


		/**
		 * <summary>A Constructor.</summary>
		 * <param name = "chooseSeneBy">The method by which the scene is referenced (Name, Number)</param>
		 * <param name = "_name">The scene's name</param>
		 * <param name = "_number">The scene's number. If name is left empty, this number will be used to reference the scene instead</param>
		 */
		public SceneInfo (ChooseSceneBy chooseSceneBy, string _name, int _number)
		{
			number = _number;

			if (chooseSceneBy == ChooseSceneBy.Number)
			{
				name = "";
			}
			else
			{
				name = _name;
			}
		}


		/**
		 * Loads the scene normally.
		 */
		public void LoadLevel ()
		{
			if (name != "")
			{
				Application.LoadLevel (name);
			}
			else
			{
				Application.LoadLevel (number);
			}
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public AsyncOperation LoadLevelASync ()
		{
			if (name != "")
			{
				return Application.LoadLevelAsync (name);
			}
			return Application.LoadLevelAsync (number);
		}

	}

}