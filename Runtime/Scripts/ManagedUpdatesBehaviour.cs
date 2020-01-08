using System;
using UnityEngine;

namespace Miscreant.Lifecycle
{
	public abstract class ManagedUpdatesBehaviour : MonoBehaviour
	{
		[Serializable]
		public struct Config
		{
			[SerializeField]
			private ManagedExecutionGroup _executionGroup;
			public ManagedExecutionGroup ExecutionGroup { get { return _executionGroup; } }

			[SerializeField]
			private bool _update;
			public bool update
			{
				get { return _update; }
				set { SetValue(ref _update, value); }
			}

			[SerializeField]
			private bool _fixedUpdate;
			public bool fixedUpdate
			{
				get { return _fixedUpdate; }
				set { SetValue(ref _fixedUpdate, value); }
			}

			[NonSerialized]
			public Action<bool> valueChangedAction;

			public Config(bool update, bool fixedUpdate)
			{
				this._executionGroup = null;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueChangedAction = null;
			}

			public Config(ManagedExecutionGroup executionGroup, bool update, bool fixedUpdate)
			{
				this._executionGroup = executionGroup;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueChangedAction = null;
			}

			private void SetValue(ref bool originalValue, bool newValue)
			{
				bool changed = originalValue != newValue;
				originalValue = newValue;

				if (changed && valueChangedAction != null)
				{
					valueChangedAction.Invoke(newValue);
				}
			}
		}

		public Config updateConfig = new Config(true, true);

		/// <summary>
        /// Previous update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		[NonSerialized]
		internal ManagedUpdatesBehaviour previousUpdate;
		/// <summary>
        /// Next update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		[NonSerialized]
		internal ManagedUpdatesBehaviour nextUpdate;

		[NonSerialized]
		/// <summary>
        /// Previous FIXED update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		internal ManagedUpdatesBehaviour previousFixedUpdate;
		[NonSerialized]
		/// <summary>
        /// Next FIXED update link. ONLY modify from IntrusiveList or its subclasses. 
        /// </summary>
		internal ManagedUpdatesBehaviour nextFixedUpdate;

		// State Tracking
		/// <summary>
		/// Mimics the built-in MonoBehaviour property so it can be checked without bridging the gap from managed to unmanaged code. 
		/// </summary>
		private bool _isActiveAndEnabled;
		internal bool ShouldUpdate { get { return _isActiveAndEnabled && updateConfig.update; } }
		internal bool ShouldFixedUpdate { get { return _isActiveAndEnabled && updateConfig.fixedUpdate; } }

		public static T Create<T>(
			Config config, bool gameObjectActive, bool componentEnabled, Transform parent = null
			) where T : ManagedUpdatesBehaviour
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

			updateConfig.ExecutionGroup.TryRegister(this);
			updateConfig.valueChangedAction = HandleUpdateModeChanged;
		}

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnDisable()
		{
			_isActiveAndEnabled = false;
			
			updateConfig.ExecutionGroup.TryUnregister(this);
			updateConfig.valueChangedAction = null;
		}

		#endregion

		private void HandleUpdateModeChanged(bool becameEnabled)
		{
			if (becameEnabled)
			{
				updateConfig.ExecutionGroup.TryRegister(this);
			}
			else
			{
				updateConfig.ExecutionGroup.TryUnregister(this);
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

