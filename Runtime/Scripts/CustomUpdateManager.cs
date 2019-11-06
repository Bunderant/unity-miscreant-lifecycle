using System;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	[CreateAssetMenu(menuName = nameof(Miscreant) + "/" + nameof(Miscreant.Utilities.Lifecycle) + "/" + nameof(CustomUpdateManager))]
	public sealed class CustomUpdateManager : ScriptableObject
	{
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
			public Action valueEnabledAction;

			public Config(bool update, bool fixedUpdate)
			{
				this._manager = null;
				this._priorityGroup = null;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueEnabledAction = null;
			}

			public Config(CustomUpdateManager manager, CustomUpdatePriority priorityGroup, bool update, bool fixedUpdate)
			{
				this._manager = manager;
				this._priorityGroup = priorityGroup;

				this._update = update;
				this._fixedUpdate = fixedUpdate;

				this.valueEnabledAction = null;
			}

			private void SetValue(ref bool originalValue, bool newValue)
			{
				bool becameEnabled = CheckValueEnabled(originalValue, newValue);
				originalValue = newValue;

				if (becameEnabled && valueEnabledAction != null)
				{
					valueEnabledAction.Invoke();
				}
			}

			private bool CheckValueEnabled(bool originalValue, bool newValue)
			{
				return !(originalValue == newValue || newValue == false);
			}
		}

		[SerializeField]
		private List<CustomUpdatePriority> _priorities = new List<CustomUpdatePriority>();

		private CustomUpdateBehaviour[] _updateHeads = null;
		private CustomUpdateBehaviour[] _updateTails = null;

		private CustomUpdateBehaviour[] _fixedUpdateHeads = null;
		private CustomUpdateBehaviour[] _fixedUpdateTails = null;

		private int _groupCount;
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

					var updateHead = updateManager._updateHeads[i];
					var currentUpdate = updateHead;
					if (updateHead != null)
					{
						do
						{
							updateGroup.Add(currentUpdate);
							currentUpdate = currentUpdate.updateLink;
						} while (currentUpdate != null);
					}

					var fixedHead = updateManager._fixedUpdateHeads[i];
					var currentFixed = fixedHead;
					if (fixedHead != null)
					{
						do
						{
							fixedUpdateGroup.Add(currentFixed);
							currentFixed = currentFixed.fixedUpdateLink;
						} while (currentFixed != null);
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

			_groupCount = _priorities.Count;
			_initialized |= _groupCount > 0;

			_updateHeads = new CustomUpdateBehaviour[_groupCount];
			_updateTails = new CustomUpdateBehaviour[_groupCount];

			_fixedUpdateHeads = new CustomUpdateBehaviour[_groupCount];
			_fixedUpdateTails = new CustomUpdateBehaviour[_groupCount];

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
		/// Add a CustomUpdateBehaviour to the system. ONLY invoke from its OnEnable callback, or by setting the
		/// update config flags (either via code or toggling from the Inspector).
		/// Will not add the component to any update groups for which it is already a part of (no duplicates).
		/// Does nothing if the component and/or associated gameObject aren't active in the heirarchy, or the update
		/// config flags are all false. 
		/// </summary>
		/// <param name="behaviour">The component to add to the system.</param>
		public void TryAdd(CustomUpdateBehaviour behaviour)
		{
			var config = behaviour.updateConfig;

			if (behaviour.isActiveAndEnabled && config.update && !behaviour.updateActive)
			{
				int priorityIndex = config.PriorityGroup.Index;
				ref CustomUpdateBehaviour tail = ref _updateTails[priorityIndex];
				if (ReferenceEquals(tail, null))
				{
					_updateHeads[priorityIndex] = behaviour;
				}
				else
				{
					tail.updateLink = behaviour;
				}

				behaviour.updateLink = null;
				tail = behaviour;
				behaviour.updateActive = true;
			}

			if (behaviour.isActiveAndEnabled && config.fixedUpdate && !behaviour.fixedUpdateActive)
			{
				int priorityIndex = config.PriorityGroup.Index;
				ref CustomUpdateBehaviour tail = ref _fixedUpdateTails[priorityIndex];
				if (ReferenceEquals(tail, null))
				{
					_fixedUpdateHeads[priorityIndex] = behaviour;
				}
				else
				{
					tail.fixedUpdateLink = behaviour;
				}

				behaviour.fixedUpdateLink = null;
				tail = behaviour;
				behaviour.fixedUpdateActive = true;
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				ref CustomUpdateBehaviour head = ref _updateHeads[i];
				CustomUpdateBehaviour previous = null;
				CustomUpdateBehaviour current = head;

				while (!ReferenceEquals(current, null)) // Avoid the '==' operator (overridden by Unity) so we don't cross the managed to unmanaged gap.
				{
					if (current.isActiveAndEnabled && current.updateConfig.update)
					{
						current.ManagedUpdate();

						previous = current;
						current = current.updateLink;
					}
					else
					{
						if (ReferenceEquals(current, head))
						{
							// If we removed the head, the previous link must be null, so no need to populate its link field.
							head = current.updateLink;
						}
						else
						{
							// Otherwise, set the previous element's link field to skip the element we just removed.
							previous.updateLink = current.updateLink;
						}

						var next = current.updateLink;
						current.updateLink = null;
						current.updateActive = false;
						current = next;
					}
				}

				// Set the tail to the last active element processed. Will be null if the list was empty. 
				_updateTails[i] = previous;
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			for (int i = 0; i < _groupCount; i++)
			{
				ref CustomUpdateBehaviour head = ref _fixedUpdateHeads[i];
				CustomUpdateBehaviour previous = null;
				CustomUpdateBehaviour current = head;

				while (!ReferenceEquals(current, null)) // Avoid the '==' operator (overridden by Unity) so we don't cross the managed to unmanaged gap.
				{
					if (current.isActiveAndEnabled && current.updateConfig.fixedUpdate)
					{
						current.ManagedFixedUpdate();

						previous = current;
						current = current.fixedUpdateLink;
					}
					else
					{
						if (ReferenceEquals(current, head))
						{
							// If we removed the head, the previous link must be null, so no need to populate its link field.
							head = current.fixedUpdateLink;
						}
						else
						{
							// Otherwise, set the previous element's link field to skip the element we just removed.
							previous.fixedUpdateLink = current.fixedUpdateLink;
						}

						var next = current.fixedUpdateLink;
						current.fixedUpdateLink = null;
						current.fixedUpdateActive = false;
						current = next;
					}
				}

				// Set the tail to the last active element processed. Will be null if the list was empty. 
				_fixedUpdateTails[i] = previous;
			}
		}
	}
}

