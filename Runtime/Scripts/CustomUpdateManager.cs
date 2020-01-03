using System;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Lifecycle
{
	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Lifecycle) + "/" + nameof(CustomUpdateManager))]
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
				this._priorityGroup = null;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueChangedAction = null;
			}

			public Config(CustomUpdatePriority priorityGroup, bool update, bool fixedUpdate)
			{
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

		/// <summary>
		/// This is designed for situations where the manager object along with the update groups were created at runtime. 
		/// Once the manager is initialized with a non-empty priority list, it can't be modified. 
		/// </summary>
		/// <param name="priorities">Priority groups to use with this manager</param>
		public void SetUpdateGroups(params CustomUpdatePriority[] priorities)
		{
			// TODO: Miscreant: Additional validation on passed-in groups. 

			if (_priorities != null && _priorities.Count > 0)
			{
				throw new Exception(
					$"Cannot reinitialize a {nameof(CustomUpdateManager)} once its {nameof(CustomUpdatePriority)} list has already been set."
				);
			}

			this._priorities = new List<CustomUpdatePriority>(priorities);
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			foreach (var group in _priorities)
			{
				group.ExecuteAllForType(UpdateType.Normal);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			foreach (var group in _priorities)
			{
				group.ExecuteAllForType(UpdateType.Fixed);
			}
		}

		/// <summary>
		/// Scans the all priority groups to look for a component by instance id.
		/// </summary>
		/// <param name="instanceId"></param>
		/// <param name="updateFound"></param>
		/// <param name="fixedUpdateFound"></param>
		public void CheckSystemForComponent(int instanceId, out bool updateFound, out bool fixedUpdateFound)
		{
			bool didFindUpdate = false;
			bool didFindFixedUpdate = false;

			void CheckUpdate(CustomUpdateBehaviour component)
			{
				didFindUpdate |= (instanceId == component.GetInstanceID());
			}

			void CheckFixedUpdate(CustomUpdateBehaviour component)
			{
				didFindFixedUpdate |= (instanceId == component.GetInstanceID());
			}

			for (int i = 0; i < _priorities.Count && !(didFindUpdate && didFindFixedUpdate); i++)
			{
				CustomUpdatePriority group = _priorities[i];
				group.TraverseForType(UpdateType.Normal, CheckUpdate);
				group.TraverseForType(UpdateType.Fixed, CheckFixedUpdate);
			}

			updateFound = didFindUpdate;
			fixedUpdateFound = didFindFixedUpdate;
		}
	}
}