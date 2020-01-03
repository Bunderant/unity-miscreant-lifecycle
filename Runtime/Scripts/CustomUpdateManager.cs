using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		internal ReadOnlyCollection<CustomUpdatePriority> Priorities;

		#region ScriptableObject

		private void OnEnable()
		{
			Priorities = new ReadOnlyCollection<CustomUpdatePriority>(_priorities);
		}

		#endregion

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
			Priorities = new ReadOnlyCollection<CustomUpdatePriority>(Priorities);
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

			// As part of the loop condition, bail out if we've already found all update types. 
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