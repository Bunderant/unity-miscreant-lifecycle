using System;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	public abstract class CustomUpdateBehaviour : MonoBehaviour
	{
		public CustomUpdateManager.Config updateConfig = new CustomUpdateManager.Config(true, true);

		/// <summary>
        /// Previous update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		[NonSerialized]
		internal CustomUpdateBehaviour previousUpdate;
		/// <summary>
        /// Next update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		[NonSerialized]
		internal CustomUpdateBehaviour nextUpdate;

		[NonSerialized]
		/// <summary>
        /// Previous FIXED update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		internal CustomUpdateBehaviour previousFixedUpdate;
		[NonSerialized]
		/// <summary>
        /// Next FIXED update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		internal CustomUpdateBehaviour nextFixedUpdate;

		// State Tracking
		// TODO: Miscreant: Don't mask the built-in field in case someone really wants to use it. Could lead to elusive bugs. 
		/// <summary>
		/// Masks the bult-in MonoBehaviour property so it can be checked without bridging the gap from managed to unmanaged code. 
		/// </summary>
		[NonSerialized]
		public new bool isActiveAndEnabled;

		public static T Create<T>(
			CustomUpdateManager.Config config, bool gameObjectActive, bool componentEnabled, Transform parent = null
			) where T : CustomUpdateBehaviour
		{
			var gameObject = new GameObject();
			gameObject.transform.SetParent(parent);

			// Disable the GameObject until everything is initialized to prevent interaction with the update system
			gameObject.SetActive(false);
			
			var component = gameObject.AddComponent<T>();
			component.enabled = componentEnabled;

			component.updateConfig = config;

			// Finally, set the GameObject's active state to the passed in flag
			gameObject.SetActive(gameObjectActive);

			return component;
		}

		#region MonoBehaviour

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnEnable()
		{
			isActiveAndEnabled = true;

			updateConfig.Manager.TryAdd(this);
			updateConfig.valueChangedAction = HandleUpdateModeChanged;
		}

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnDisable()
		{
			isActiveAndEnabled = false;
			
			updateConfig.Manager.TryRemove(this);
			updateConfig.valueChangedAction = null;
		}

		#endregion

		private void HandleUpdateModeChanged(bool becameEnabled)
		{
			if (becameEnabled)
			{
				updateConfig.Manager.TryAdd(this);
			}
			else
			{
				updateConfig.Manager.TryRemove(this);
			}
		}

		/// <summary>
		/// Called by an external update system from MonoBehaviour's "Update" loop. 
		/// </summary>
		public virtual void ManagedUpdate() { }

		/// <summary>
		/// Called by an external update system from MonoBehaviour's "FixedUpdate" loop. 
		/// </summary>
		public virtual void ManagedFixedUpdate() { }
	}
}

