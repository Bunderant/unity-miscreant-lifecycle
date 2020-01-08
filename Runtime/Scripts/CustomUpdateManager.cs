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

		[SerializeField]
		private List<ManagedExecutionGroup> _executionGroups = new List<ManagedExecutionGroup>();
		internal ReadOnlyCollection<ManagedExecutionGroup> ExecutionGroups;

		#region ScriptableObject

		private void OnEnable()
		{
			ExecutionGroups = new ReadOnlyCollection<ManagedExecutionGroup>(_executionGroups);
		}

		#endregion

		/// <summary>
		/// This is designed for situations where the manager object along with the execution groups were created at runtime. 
		/// Once the manager is initialized with a non-empty group list, it can't be modified. 
		/// </summary>
		/// <param name="executionGroups">Execution groups to use with this manager</param>
		public void SetExecutionGroups(params ManagedExecutionGroup[] executionGroups)
		{
			// TODO: Miscreant: Additional validation on passed-in groups. 

			if (_executionGroups != null && _executionGroups.Count > 0)
			{
				throw new Exception(
					$"Cannot reinitialize a {nameof(CustomUpdateManager)} once its {nameof(ManagedExecutionGroup)} list has already been set."
				);
			}

			this._executionGroups = new List<ManagedExecutionGroup>(executionGroups);
			ExecutionGroups = new ReadOnlyCollection<ManagedExecutionGroup>(ExecutionGroups);
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's Update() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunUpdate()
		{
			foreach (var group in _executionGroups)
			{
				group.ExecuteAllForType(UpdateType.Normal);
			}
		}

		/// <summary>
		/// Should be called from a MonoBehaviour's FixedUpdate() loop. From a GameplayController class, for example. 
		/// </summary>
		public void RunFixedUpdate()
		{
			foreach (var group in _executionGroups)
			{
				group.ExecuteAllForType(UpdateType.Fixed);
			}
		}

		/// <summary>
		/// Scans the all execution groups to look for a component by instance id.
		/// </summary>
		/// <param name="instanceId"></param>
		/// <param name="updateFound"></param>
		/// <param name="fixedUpdateFound"></param>
		public void CheckSystemForComponent(int instanceId, out bool updateFound, out bool fixedUpdateFound)
		{
			bool didFindUpdate = false;
			bool didFindFixedUpdate = false;

			void CheckUpdate(ManagedUpdatesBehaviour component)
			{
				didFindUpdate |= (instanceId == component.GetInstanceID());
			}

			void CheckFixedUpdate(ManagedUpdatesBehaviour component)
			{
				didFindFixedUpdate |= (instanceId == component.GetInstanceID());
			}

			// As part of the loop condition, bail out if we've already found all update types. 
			for (int i = 0; i < _executionGroups.Count && !(didFindUpdate && didFindFixedUpdate); i++)
			{
				ManagedExecutionGroup group = _executionGroups[i];
				group.TraverseForType(UpdateType.Normal, CheckUpdate);
				group.TraverseForType(UpdateType.Fixed, CheckFixedUpdate);
			}

			updateFound = didFindUpdate;
			fixedUpdateFound = didFindFixedUpdate;
		}
	}
}