using System;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	using UpdateType = CustomUpdateManager.UpdateType;

	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Utilities.Lifecycle) + "/" + nameof(CustomUpdatePriority))]
	public sealed class CustomUpdatePriority : ScriptableObject
	{
		public int Index { get; private set; }

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
			Index = -1;
			_updateList = null;
			_fixedUpdateList = null;
		}

		#endregion

		/// <summary>
		/// Should only ever be called by a <see cref="CustomUpdateManager" /> ScriptableObject asset. 
		/// Always use a the manager object to control execution order. 
		/// </summary>
		/// <param name="index">The new priority value.</param>
		internal void Initialize(int index, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
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

			this.Index = index;

			_updateList = new IntrusiveUpdateList();
			_fixedUpdateList = new IntrusiveFixedUpdateList();
		}

		public void AddUpdate(CustomUpdateBehaviour component)
		{
			_updateList.AddToTail(component);
		}

		public void AddFixedUpdate(CustomUpdateBehaviour component)
		{
			_fixedUpdateList.AddToTail(component);
		}

		public void RemoveUpdate(CustomUpdateBehaviour component)
		{
			_updateList.Remove(component);
		}

		public void RemoveFixedUpdate(CustomUpdateBehaviour component)
		{
			_fixedUpdateList.Remove(component);
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

