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

		private abstract class IntrusiveList
		{
			public CustomUpdatePriority priorityGroup;
			public CustomUpdateBehaviour head = null;
			public uint count = 0;

			protected CustomUpdateBehaviour current;

			public IntrusiveList(CustomUpdatePriority priorityGroup)
			{
				this.priorityGroup = priorityGroup;
			}

			public void ExecuteAll()
			{
				if (ReferenceEquals(head, null))
				{
					return;
				}

				current = head;
				do
				{
					ExecuteCurrent();
					Advance();
				} while (!ReferenceEquals(current, head));
				current = null;
			}

			protected abstract void ExecuteCurrent();
			protected abstract void Advance();

			internal abstract void AddToTail(CustomUpdateBehaviour component);
			internal abstract void Remove(CustomUpdateBehaviour component);
		}

		private sealed class IntrusiveUpdateList : IntrusiveList
		{
			public IntrusiveUpdateList(CustomUpdatePriority priorityGroup) : base(priorityGroup)
			{ }

			protected override void ExecuteCurrent()
			{
				current.ManagedUpdate();
			}

			protected override void Advance()
			{
				current = current.nextUpdate;
			}

			internal override void AddToTail(CustomUpdateBehaviour component)
			{
				if (!ReferenceEquals(component.nextUpdate, null) || !ReferenceEquals(component.previousUpdate, null))
				{
					return;
				}
				count++;

				if (ReferenceEquals(head, null))
				{
					head = component;
					head.previousUpdate = head;
					head.nextUpdate = head;
					return;
				}

				Add(component, head.previousUpdate, head);
			}

			private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
			{
				node.nextUpdate = next;
				node.previousUpdate = prev;

				next.previousUpdate = node;
				prev.nextUpdate = node;
			}

			internal override void Remove(CustomUpdateBehaviour component)
			{
				if (ReferenceEquals(component.nextUpdate, null) && ReferenceEquals(component.previousUpdate, null))
				{
					return;
				}
				count--;

				if (count == 0)
				{
					// TODO: Miscreant: Restore this under a "strict" conditional
					// if (!ReferenceEquals(component, head))
					// {
					// 	throw new ArgumentException("component is not the root of the list");
					// }

					// TODO: MIscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 

					head.previousUpdate = null;
					head.nextUpdate = null;
					head = null;
					return;
				}

				if (ReferenceEquals(component, head))
				{
					head = head.nextUpdate;
				}

				component.nextUpdate.previousUpdate = component.previousUpdate;
				component.previousUpdate.nextUpdate = component.nextUpdate;

				if (ReferenceEquals(current, component))
				{
					current = component.previousUpdate;
				}

				component.previousUpdate = null;
				component.nextUpdate = null;
			}
		}

		private sealed class IntrusiveFixedUpdateList : IntrusiveList
		{
			public IntrusiveFixedUpdateList(CustomUpdatePriority priorityGroup) : base(priorityGroup)
			{ }

			protected override void ExecuteCurrent()
			{
				current.ManagedFixedUpdate();
			}

			protected override void Advance()
			{
				current = current.nextFixedUpdate;
			}

			internal override void AddToTail(CustomUpdateBehaviour component)
			{
				if (!ReferenceEquals(component.nextFixedUpdate, null) || !ReferenceEquals(component.previousFixedUpdate, null))
				{
					return;
				}
				count++;

				if (ReferenceEquals(head, null))
				{
					head = component;
					head.previousFixedUpdate = head;
					head.nextFixedUpdate = head;
					return;
				}

				Add(component, head.previousFixedUpdate, head);
			}

			private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
			{
				node.nextFixedUpdate = next;
				node.previousFixedUpdate = prev;

				next.previousFixedUpdate = node;
				prev.nextFixedUpdate = node;
			}

			internal override void Remove(CustomUpdateBehaviour component)
			{
				if (ReferenceEquals(component.nextFixedUpdate, null) && ReferenceEquals(component.previousFixedUpdate, null))
				{
					return;
				}
				count--;

				if (count == 0)
				{
					// TODO: Miscreant: Restore this under a "strict" conditional
					// if (!ReferenceEquals(component, head))
					// {
					// 	throw new ArgumentException("component is not the root of the list");
					// }

					// TODO: MIscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 

					head.previousFixedUpdate = null;
					head.nextFixedUpdate = null;
					head = null;
					return;
				}

				if (ReferenceEquals(component, head))
				{
					head = head.nextFixedUpdate;
				}

				component.nextFixedUpdate.previousFixedUpdate = component.previousFixedUpdate;
				component.previousFixedUpdate.nextFixedUpdate = component.nextFixedUpdate;

				if (ReferenceEquals(current, component))
				{
					current = component.previousFixedUpdate;
				}

				component.previousFixedUpdate = null;
				component.nextFixedUpdate = null;
			}
		}

		private IntrusiveList[] _updateLists = null;
		private IntrusiveList[] _fixedUpdateLists = null;

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

					var updateList = updateManager._updateLists[i];
					var currentUpdate = updateList.head;
					if (!ReferenceEquals(currentUpdate, null))
					{
						do
						{
							updateGroup.Add(currentUpdate);
							currentUpdate = currentUpdate.nextUpdate;
						} while (!ReferenceEquals(currentUpdate, updateList.head));
					}

					var fixedList = updateManager._fixedUpdateLists[i];
					var currentFixed = fixedList.head;
					if (!ReferenceEquals(currentFixed, null))
					{
						do
						{
							fixedUpdateGroup.Add(currentFixed);
							currentFixed = currentFixed.nextFixedUpdate;
						} while (!ReferenceEquals(currentFixed, fixedList.head));
					}

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

			_updateLists = new IntrusiveList[_groupCount];
			_fixedUpdateLists = new IntrusiveList[_groupCount];

			for (int i = 0; i < _groupCount; i++)
			{
				CustomUpdatePriority group = _priorities[i];
				_updateLists[i] = new IntrusiveUpdateList(group);
				_fixedUpdateLists[i] = new IntrusiveFixedUpdateList(group);
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
				IntrusiveList updateList = _updateLists[i];
				IntrusiveList fixedUpdateList = _fixedUpdateLists[i];

				isEmpty &= (
					ReferenceEquals(updateList.head, null) &&
					ReferenceEquals(fixedUpdateList.head, null)
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
				IntrusiveList updateList = _updateLists[i];
				isEmpty &= ReferenceEquals(updateList.head, null);
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
				IntrusiveList fixedUpdateList = _fixedUpdateLists[i];
				isEmpty &= ReferenceEquals(fixedUpdateList.head, null);
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
		/// <param name="priorityGroup">Group to count.</param>
		/// <param name="updateType">UpdateType to match.</param>
		/// <returns>The count.</returns>
		public int GetCountForGroup(CustomUpdatePriority priorityGroup, UpdateType updateType)
		{
			int total = 0;
			void IncrementTotal()
			{
				total++;
			}

			TraverseGroupForType(priorityGroup, updateType, IncrementTotal);
			return total;
		}

		/// <summary>
		/// Traverses the list matching specific priority group and UpdateType, performing the given action once per element.
		/// </summary>
		/// <param name="priorityGroup">Group to traverse.</param>
		/// <param name="updateType">Update type to match.</param>
		/// <param name="perElementAction">Action to perform (cannot be null).</param>
		public void TraverseGroupForType(CustomUpdatePriority priorityGroup, UpdateType updateType, Action perElementAction)
		{
			IntrusiveList list;
			CustomUpdateBehaviour currentElement;
			Func<CustomUpdateBehaviour> GetNext;

			switch (updateType)
			{
				case UpdateType.Normal:
					list = _updateLists[priorityGroup.Index];
					currentElement = list.head;
					GetNext = () => { return currentElement.nextUpdate; };
					break;
				case UpdateType.Fixed:
					list = _fixedUpdateLists[priorityGroup.Index];
					currentElement = list.head;
					GetNext = () => { return currentElement.nextFixedUpdate; };
					break;
				default:
					throw new ArgumentException($"Update type not supported for enumeration: {updateType}");
			}

			if (!ReferenceEquals(currentElement, null))
			{
				do
				{
					perElementAction.Invoke();
					currentElement = GetNext();
				} while (!ReferenceEquals(currentElement, list.head));
			}
		}

		/// <summary>
		/// Traverses the list matching specific priority group and UpdateType, performing the given action once per element.
		/// </summary>
		/// <param name="priorityGroup">Group to traverse.</param>
		/// <param name="updateType">Update type to match.</param>
		/// <param name="perElementAction">Action to perform (cannot be null).</param>
		public void TraverseGroupForType(CustomUpdatePriority priorityGroup, UpdateType updateType, Action<CustomUpdateBehaviour> perElementAction)
		{
			IntrusiveList list;
			CustomUpdateBehaviour currentElement;
			Func<CustomUpdateBehaviour> GetNext;

			switch (updateType)
			{
				case UpdateType.Normal:
					list = _updateLists[priorityGroup.Index];
					currentElement = list.head;
					GetNext = () => { return currentElement.nextUpdate; };
					break;
				case UpdateType.Fixed:
					list = _fixedUpdateLists[priorityGroup.Index];
					currentElement = list.head;
					GetNext = () => { return currentElement.nextFixedUpdate; };
					break;
				default:
					throw new ArgumentException($"Update type not supported for enumeration: {updateType}");
			}

			if (!ReferenceEquals(currentElement, null))
			{
				do
				{
					perElementAction.Invoke(currentElement);
					currentElement = GetNext();
				} while (!ReferenceEquals(currentElement, list.head));
			}
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
				_updateLists[priorityIndex].AddToTail(component);
			}
			if (component.isActiveAndEnabled && config.fixedUpdate)
			{
				_fixedUpdateLists[priorityIndex].AddToTail(component);
			}
		}

		public void TryRemove(CustomUpdateBehaviour component)
		{
			var config = component.updateConfig;
			int priorityIndex = config.PriorityGroup.Index;

			// TODO: Miscreant: Make sure there aren't any redundant checks here
			if (!component.isActiveAndEnabled || !config.update)
			{
				_updateLists[priorityIndex].Remove(component);
			}
			if (!component.isActiveAndEnabled || !config.fixedUpdate)
			{
				_fixedUpdateLists[priorityIndex].Remove(component);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_updateLists[i].ExecuteAll();
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				_fixedUpdateLists[i].ExecuteAll();
			}
		}
	}
}