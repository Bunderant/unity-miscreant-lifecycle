using System;
using UnityEngine;

namespace Miscreant.Lifecycle
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
		/// <summary>
		/// Mimics the built-in MonoBehaviour property so it can be checked without bridging the gap from managed to unmanaged code. 
		/// </summary>
		private bool _isActiveAndEnabled;
		internal bool ShouldUpdate { get { return _isActiveAndEnabled && updateConfig.update; } }
		internal bool ShouldFixedUpdate { get { return _isActiveAndEnabled && updateConfig.fixedUpdate; } }

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
			_isActiveAndEnabled = true;

			updateConfig.PriorityGroup.TryRegister(this);
			updateConfig.valueChangedAction = HandleUpdateModeChanged;
		}

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnDisable()
		{
			_isActiveAndEnabled = false;
			
			updateConfig.PriorityGroup.TryUnregister(this);
			updateConfig.valueChangedAction = null;
		}

		#endregion

		private void HandleUpdateModeChanged(bool becameEnabled)
		{
			if (becameEnabled)
			{
				updateConfig.PriorityGroup.TryRegister(this);
			}
			else
			{
				updateConfig.PriorityGroup.TryUnregister(this);
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

