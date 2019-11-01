using System;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	public abstract class CustomUpdateBehaviour : MonoBehaviour
	{
		public CustomUpdateManager.Config updateConfig = new CustomUpdateManager.Config(true, true);

		/// <summary>
		/// Specifies the next behavior to have its update method executed in this priority group by the update manager. Part of the
		/// intrusive linked list implementation. 
		/// </summary>
		[NonSerialized]
		internal CustomUpdateBehaviour updateLink;
		/// <summary>
		/// Specifies the next behavior to have its fixed update method executed in this priority group by the update manager. Part 
		/// of the intrusive linked list implementation.
		/// </summary>
		[NonSerialized]
		internal CustomUpdateBehaviour fixedUpdateLink;

		// State Tracking
		// TODO: Miscreant: Don't mask the built-in field in case someone really wants to use it. Could lead to elusive bugs. 
		/// <summary>
		/// Masks the bult-in MonoBehaviour property so it can be checked without bridging the gap from managed to unmanaged code. 
		/// </summary>
		[NonSerialized]
		public new bool isActiveAndEnabled;
		/// <summary>
		/// ONLY set from the UpdateSystem. Marks whether this compoenent is already in an update group. Prevents duplicates.
		/// </summary>
		[NonSerialized]
		internal bool updateActive;
		/// <summary>
		/// ONLY set from the UpdateSystem. Marks whether this component is already in a fixed update group. Prevents duplicates.
		/// </summary>
		[NonSerialized]
		internal bool fixedUpdateActive;

		#region MonoBehaviour

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnEnable()
		{
			isActiveAndEnabled = true;

			updateConfig.Manager.TryAdd(this);
			updateConfig.valueEnabledAction = HandleUpdateModeEnabled;
		}

		/// <summary>
		/// Be sure to ALWAYS call this base method if overriding it in a subclass. 
		/// </summary>
		protected virtual void OnDisable()
		{
			isActiveAndEnabled = false;
			
			updateConfig.valueEnabledAction = null;
		}

		#endregion

		private void HandleUpdateModeEnabled()
		{
			updateConfig.Manager.TryAdd(this);
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

