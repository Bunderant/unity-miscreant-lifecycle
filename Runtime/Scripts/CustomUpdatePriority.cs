﻿using System;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	using UpdateType = CustomUpdateManager.UpdateType;

	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Utilities.Lifecycle) + "/" + nameof(CustomUpdatePriority))]
	public sealed class CustomUpdatePriority : ScriptableObject
	{
		private IntrusiveUpdateList _updateList;
		private IntrusiveFixedUpdateList _fixedUpdateList;

		public bool UpdateEmpty { get { return _updateList.count == 0; } }
		public bool FixedUpdateEmpty { get { return _fixedUpdateList.count == 0; } }
		public bool IsEmpty { get { return UpdateEmpty && FixedUpdateEmpty; } }

		public uint UpdateCount { get { return _updateList.count; } }
		public uint FixedUpdateCount { get { return _fixedUpdateList.count; } }

		#region ScriptableObject Callbacks

		private void OnDisable()
		{
			_updateList = null;
			_fixedUpdateList = null;
		}

		#endregion

		/// <summary>
		/// Should only ever be called by a <see cref="CustomUpdateManager" /> ScriptableObject asset. 
		/// Always use a the manager object to control execution order. 
		/// </summary>
		internal void Initialize([System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
		{
			// TODO: Miscreant: Need to make sure the calling object is the right type, as well. 
			if (!string.Equals(callerName, nameof(CustomUpdateManager.Initialize)))
			{
				Debug.LogError(
					$"Can't set priority outside the {nameof(CustomUpdateManager.Initialize)} " +
					$"method of {nameof(CustomUpdateManager)}",
					this
				);
			}
			_updateList = new IntrusiveUpdateList();
			_fixedUpdateList = new IntrusiveFixedUpdateList();
		}

		/// <summary>
		/// Add a CustomUpdateBehaviour to the group. ONLY invoke from its OnEnable callback, or by setting the
		/// update config flags (either via code or toggling from the Inspector).
		/// Will not add the component to any update lists for which it is already a part of (no duplicates).
		/// Does nothing if the component isn't active and enabled, or the update config flags are all false. 
		/// </summary>
		/// <param name="component">The component to try adding to this group's lists.</param>
		public void TryRegister(CustomUpdateBehaviour component)
		{
			// TODO: Miscreant: For a "strict" mode, should check to make sure this is actually the given component's priority group. 

			// TODO: Miscreant: avoid calling properties here, just pass the registration flags in as parameters as a micro-optimization. 
			if (component.ShouldUpdate)
			{
				_updateList.AddToTail(component);
			}
			if (component.ShouldFixedUpdate)
			{
				_fixedUpdateList.AddToTail(component);
			}
		}

		/// <summary>
		/// Tries to remove a component from the group. ONLY Invoke from the component's OnDisable callback, or by setting
		/// the update config flags (either via code or toggling from the Inspector). 
		/// Does nothing if the component is not already registered with the group. 
		/// </summary>
		/// <param name="component">The component to try removing from this group's lists.</param>
		public void TryUnregister(CustomUpdateBehaviour component)
		{
			// TODO: Miscreant: For a "strict" mode, should check to make sure this is actually the given component's priority group. 

			// TODO: Miscreant: avoid calling properties here, just pass the registration flags in as parameters as a micro-optimization. 
			if (!component.ShouldUpdate)
			{
				_updateList.Remove(component);
			}
			if (!component.ShouldFixedUpdate)
			{
				_fixedUpdateList.Remove(component);
			}
		}

		public void TraverseForType(UpdateType type, Action<CustomUpdateBehaviour> perElementAction)
		{
			GetListForType(type).Traverse(perElementAction);
		}

		public uint GetCountForType(UpdateType type)
		{
			return GetListForType(type).count;
		}

		private IntrusiveList GetListForType(UpdateType type)
		{
			switch (type)
			{
				case UpdateType.Normal:
					return _updateList;
				case UpdateType.Fixed:
					return _fixedUpdateList;
				default:
					throw new InvalidUpdateTypeException(type);
			}
		}

		public void ExecuteAllForType(UpdateType type)
		{
			GetListForType(type).ExecuteAll();
		}
	}
}

