using System;
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
		/// Add a CustomUpdateBehaviour to the system. ONLY invoke from its OnEnable callback, or by setting the
		/// update config flags (either via code or toggling from the Inspector).
		/// Will not add the component to any update groups for which it is already a part of (no duplicates).
		/// Does nothing if the component and/or associated gameObject aren't active in the hierarchy, or the update
		/// config flags are all false. 
		/// </summary>
		/// <param name="component">The component to add to the system.</param>
		public void TryRegister(CustomUpdateBehaviour component)
		{
			if (component.isActiveAndEnabled && component.updateConfig.update)
			{
				_updateList.AddToTail(component);
			}
			if (component.isActiveAndEnabled && component.updateConfig.fixedUpdate)
			{
				_fixedUpdateList.AddToTail(component);
			}
		}

		public void TryUnregister(CustomUpdateBehaviour component)
		{
			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (!component.isActiveAndEnabled || !component.updateConfig.update)
			{
				_updateList.Remove(component);
			}
			if (!component.isActiveAndEnabled || !component.updateConfig.fixedUpdate)
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

