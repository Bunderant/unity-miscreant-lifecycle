using System;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Utilities.Lifecycle) + "/" + nameof(CustomUpdateManager))]
	public sealed class CustomUpdateManager : ScriptableObject
	{
		public enum UpdateType : byte
		{
			None,
			Normal,
			Fixed
		}

		/// <summary>
		/// Congig data for use by individual components that use this system. 
		/// </summary>
		[Serializable]
		public struct Config
		{
			[SerializeField]
			private CustomUpdateManager _manager;
			public CustomUpdateManager Manager { get { return _manager; } }

			[SerializeField]
			private CustomUpdatePriority _priorityGroup;
			public CustomUpdatePriority PriorityGroup { get { return _priorityGroup; } }

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
				this._manager = null;
				this._priorityGroup = null;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueChangedAction = null;
			}

			public Config(CustomUpdateManager manager, CustomUpdatePriority priorityGroup, bool update, bool fixedUpdate)
			{
				this._manager = manager;
				this._priorityGroup = priorityGroup;

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

		[SerializeField]
		private List<CustomUpdatePriority> _priorities = new List<CustomUpdatePriority>();

		private PriorityGroup[] _priorityGroups = null;
		private readonly struct PriorityGroup
		{
			public readonly CustomUpdatePriority priority;
			public readonly IntrusiveUpdateList updateList;
			public readonly IntrusiveFixedUpdateList fixedUpdateList;

			public PriorityGroup(CustomUpdatePriority priority)
			{
				this.priority = priority;
				updateList = new IntrusiveUpdateList();
				fixedUpdateList = new IntrusiveFixedUpdateList();
			}

			public void TraverseForType(UpdateType type, Action<CustomUpdateBehaviour> perElementAction)
			{
				IntrusiveList list = GetListForType(type);
				list.Traverse(perElementAction);
			}

			private IntrusiveList GetListForType(UpdateType type)
			{
				switch (type)
				{
					case UpdateType.Normal:
						return updateList;
					case UpdateType.Fixed:
						return fixedUpdateList;
					default:
						throw new ArgumentException($"Can't get {nameof(IntrusiveList)} for {nameof(UpdateType)}: {type}");
				}
			}
		}

		private ushort _groupCount;
		private bool _initialized;

#if UNITY_EDITOR
		/// <summary>
		/// Class used exclusively for displaying CustomUpdateManager's runtime data in the inspector.
		/// Kept in this file so as not to expose private variables to non-editor classes. 
		/// </summary>
		public class RuntimeData : ScriptableObject
		{
			[SerializeField]
			private CustomUpdatePriority[] _priorities;
			[SerializeField]
			private RuntimeGroup[] _updateGroups;
			[SerializeField]
			private RuntimeGroup[] _fixedUpdateGroups;

			public void Initialize(CustomUpdateManager updateManager)
			{
				int priorityCount = updateManager._priorities.Count;

				_priorities = new CustomUpdatePriority[priorityCount];
				_updateGroups = new RuntimeGroup[priorityCount];
				_fixedUpdateGroups = new RuntimeGroup[priorityCount];

				for (int i = 0; i < priorityCount; i++)
				{
					List<CustomUpdateBehaviour> updateGroup = new List<CustomUpdateBehaviour>();
					List<CustomUpdateBehaviour> fixedUpdateGroup = new List<CustomUpdateBehaviour>();

					PriorityGroup currentGroup = updateManager._priorityGroups[i];

					currentGroup.updateList.Traverse(
						(c) => { updateGroup.Add(c); }
					);

					currentGroup.fixedUpdateList.Traverse(
						(c) => { fixedUpdateGroup.Add(c); }
					);

					_priorities[i] = updateManager._priorities[i];
					_updateGroups[i] = new RuntimeGroup(updateGroup.ToArray());
					_fixedUpdateGroups[i] = new RuntimeGroup(fixedUpdateGroup.ToArray());
				}
			}
		}

		/// <summary>
		/// Wrapper class to keep Unity's built-in serialization happy, a workaround to serialize
		/// nested arrays. 
		/// </summary>
		[System.Serializable]
		public class RuntimeGroup
		{
			public CustomUpdateBehaviour[] value;

			public RuntimeGroup(CustomUpdateBehaviour[] group)
			{
				this.value = group;
			}
		}
#endif

		#region ScriptableObject

		private void OnEnable()
		{
			Initialize();
		}

		private void OnDisable()
		{
			_groupCount = 0;

			// TODO: Miscreant: Need to define expected behavior when a manager is unloaded and the application continues. 

			_initialized = false;
		}

		#endregion

		private void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			_groupCount = (ushort)_priorities.Count;
			_initialized |= _groupCount > 0;

			_priorityGroups = new PriorityGroup[_groupCount];
			for (int i = 0; i < _groupCount; i++)
			{
				_priorityGroups[i] = new PriorityGroup(_priorities[i]);
			}

			UpdatePriorities();
		}

		/// <summary>
		/// Can only execute successfully if the current priority list is empty. This is designed
		/// for situations where the manager object was created at runtime. Once the manager is
		/// initialized with a non-empty priority list, it can't be initialized with another. 
		/// </summary>
		/// <param name="priorities">Priority groups to use with this manager</param>
		public void Initialize(params CustomUpdatePriority[] priorities)
		{
			if (_initialized)
			{
				return;
			}

			this._priorities = new List<CustomUpdatePriority>(priorities);
			Initialize();
		}

		internal void UpdatePriorities()
		{
			for (int i = 0; i < _priorities.Count; i++)
			{
				var currentPriority = _priorities[i];
				if (currentPriority == null)
				{
					continue;
				}

				currentPriority.SetIndex(i);
			}
		}

		/// <summary>
		/// Checks that the heads and tails of all lists in the system are null for every UpdateType. 
		/// </summary>
		/// <returns>True if the system is empty, false otherwise.</returns>
		public bool CheckAllGroupsEmpty()
		{
			bool isEmpty = true;

			for (int i = 0; i < _groupCount && isEmpty; i++)
			{
				PriorityGroup currentGroup = _priorityGroups[i];

				isEmpty &= (
					ReferenceEquals(currentGroup.updateList.head, null) &&
					ReferenceEquals(currentGroup.fixedUpdateList.head, null)
				);
			}

			return isEmpty;
		}

		/// <summary>
		/// Checks that the heads and tails of all Update lists are null.
		/// </summary>
		/// <returns>True if all Update groups are empty, false otherwise.</returns>
		public bool CheckAllUpdateGroupsEmpty()
		{
			bool isEmpty = true;

			for (int i = 0; i < _groupCount && isEmpty; i++)
			{
				isEmpty &= ReferenceEquals(_priorityGroups[i].updateList.head, null);
			}

			return isEmpty;
		}

		/// <summary>
		/// Checks that the heads and tails of all FixedUpdate lists are null.
		/// </summary>
		/// <returns>True if all FixedUpdate groups are empty, false otherwise.</returns>
		public bool CheckAllFixedUpdateGroupsEmpty()
		{
			bool isEmpty = true;

			for (int i = 0; i < _groupCount && isEmpty; i++)
			{
				isEmpty &= ReferenceEquals(_priorityGroups[i].fixedUpdateList.head, null);
			}

			return isEmpty;
		}

		/// <summary>
		/// Gets the number of elements in the whole system matching the specified UpdateType. 
		/// </summary>
		/// <param name="updateType">UpdateType to match.</param>
		/// <returns>The count.</returns>
		public int GetCountForAllGroups(UpdateType updateType)
		{
			int count = 0;
			for (int i = 0; i < _groupCount; i++)
			{
				count += GetCountForGroup(_priorities[i], updateType);
			}
			return count;
		}

		/// <summary>
		/// Gets the number of elements in the priority group matching the specified UpdateType. 
		/// </summary>
		/// <param name="priority">Group to count.</param>
		/// <param name="updateType">UpdateType to match.</param>
		/// <returns>The count.</returns>
		public int GetCountForGroup(CustomUpdatePriority priority, UpdateType updateType)
		{
			int total = 0;
			void IncrementTotal(CustomUpdateBehaviour component)
			{
				total++;
			}

			_priorityGroups[priority.Index].TraverseForType(updateType, IncrementTotal);

			return total;
		}

		/// <summary>
		/// Add a CustomUpdateBehaviour to the system. ONLY invoke from its OnEnable callback, or by setting the
		/// update config flags (either via code or toggling from the Inspector).
		/// Will not add the component to any update groups for which it is already a part of (no duplicates).
		/// Does nothing if the component and/or associated gameObject aren't active in the hierarchy, or the update
		/// config flags are all false. 
		/// </summary>
		/// <param name="component">The component to add to the system.</param>
		public void TryAdd(CustomUpdateBehaviour component)
		{
			var config = component.updateConfig;
			int priorityIndex = config.PriorityGroup.Index;

			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (component.isActiveAndEnabled && config.update)
			{
				_priorityGroups[priorityIndex].updateList.AddToTail(component);
			}
			if (component.isActiveAndEnabled && config.fixedUpdate)
			{
				_priorityGroups[priorityIndex].fixedUpdateList.AddToTail(component);
			}
		}

		public void TryRemove(CustomUpdateBehaviour component)
		{
			var config = component.updateConfig;
			int priorityIndex = config.PriorityGroup.Index;

			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (!component.isActiveAndEnabled || !config.update)
			{
				_priorityGroups[priorityIndex].updateList.Remove(component);
			}
			if (!component.isActiveAndEnabled || !config.fixedUpdate)
			{
				_priorityGroups[priorityIndex].fixedUpdateList.Remove(component);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_priorityGroups[i].updateList.ExecuteAll();
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_priorityGroups[i].fixedUpdateList.ExecuteAll();
			}
		}

		/// <summary>
		/// Scans the all priority groups to look for a component. Also validates that the component cannot appear in the
		/// system more than once for each update type. 
		/// </summary>
		/// <param name="component"></param>
		/// <param name="updateFound"></param>
		/// <param name="fixedUpdateFound"></param>
		public void CheckSystemForComponent(CustomUpdateBehaviour component, out bool updateFound, out bool fixedUpdateFound)
		{
			updateFound = false;
			fixedUpdateFound = false;
			int referenceCount = 0;

			for (int i = 0; i < _groupCount; i++)
			{
				PriorityGroup group = _priorityGroups[i];

				referenceCount = 0;
				group.TraverseForType(UpdateType.Normal, IncrementReferenceCountIfFound);
				updateFound |= referenceCount == 1;

				referenceCount = 0;
				group.TraverseForType(UpdateType.Fixed, IncrementReferenceCountIfFound);
				fixedUpdateFound |= referenceCount == 1;
			}

			void IncrementReferenceCountIfFound(CustomUpdateBehaviour current)
			{
				if (ReferenceEquals(current, component))
				{
					referenceCount++;
					if (referenceCount > 1)
					{
						throw new DuplicateReferenceException();
					}
				}
			}
		}
	}
}