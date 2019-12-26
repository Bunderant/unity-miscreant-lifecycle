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

					CustomUpdatePriority currentGroup = updateManager._priorities[i];

					currentGroup.TraverseForType(
						UpdateType.Normal,
						(c) => { updateGroup.Add(c); }
					);

					currentGroup.TraverseForType(
						UpdateType.Fixed,
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

			for (int i = 0; i < _groupCount; i++)
			{
				_priorities[i].Initialize(i);
			}
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

		/// <summary>
		/// Checks that the heads and tails of all lists in the system are null for every UpdateType. 
		/// </summary>
		/// <returns>True if the system is empty, false otherwise.</returns>
		public bool CheckAllGroupsEmpty()
		{
			bool isEmpty = true;

			for (int i = 0; i < _groupCount && isEmpty; i++)
			{
				isEmpty &= _priorities[i].IsEmpty;
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
				isEmpty &= _priorities[i].UpdateEmpty;
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
				isEmpty &= _priorities[i].FixedUpdateEmpty;
			}

			return isEmpty;
		}

		/// <summary>
		/// Gets the number of elements in the whole system matching the specified UpdateType. 
		/// </summary>
		/// <param name="updateType">UpdateType to match.</param>
		/// <returns>The count.</returns>
		public ulong GetCountForAllGroups(UpdateType updateType)
		{
			ulong count = 0;
			for (int i = 0; i < _groupCount; i++)
			{
				count += _priorities[i].GetCountForType(updateType);
			}
			return count;
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
			Config config = component.updateConfig;

			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (component.isActiveAndEnabled && config.update)
			{
				config.PriorityGroup.AddUpdate(component);
			}
			if (component.isActiveAndEnabled && config.fixedUpdate)
			{
				config.PriorityGroup.AddFixedUpdate(component);
			}
		}

		public void TryRemove(CustomUpdateBehaviour component)
		{
			Config config = component.updateConfig;

			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (!component.isActiveAndEnabled || !config.update)
			{
				config.PriorityGroup.RemoveUpdate(component);
			}
			if (!component.isActiveAndEnabled || !config.fixedUpdate)
			{
				config.PriorityGroup.RemoveFixedUpdate(component);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_priorities[i].ExecuteAllForType(UpdateType.Normal);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_priorities[i].ExecuteAllForType(UpdateType.Fixed);
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
				CustomUpdatePriority group = _priorities[i];

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