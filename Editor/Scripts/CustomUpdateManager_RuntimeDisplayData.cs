using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Lifecycle.Editor
{
	using UpdateType = ManagedExecutionSystem.UpdateType;

	/// <summary>
	/// Class used exclusively for displaying execution system's runtime data in the inspector.
	/// </summary>
	public class CustomUpdateManager_RuntimeDisplayData : ScriptableObject
	{
		[SerializeField]
		private ManagedExecutionGroup[] _executionGroups;
		[SerializeField]
		private RuntimeGroup[] _updateGroups;
		[SerializeField]
		private RuntimeGroup[] _fixedUpdateGroups;

		/// <summary>
		/// Wrapper class to keep Unity's built-in serialization happy, a workaround to serialize nested arrays. 
		/// </summary>
		[System.Serializable]
		public class RuntimeGroup
		{
			public ManagedUpdatesBehaviour[] value;

			public RuntimeGroup(ManagedUpdatesBehaviour[] group)
			{
				this.value = group;
			}
		}

		public void Initialize(ManagedExecutionSystem system)
		{
			int groupCount = system.ExecutionGroups.Count;

			_executionGroups = new ManagedExecutionGroup[groupCount];
			_updateGroups = new RuntimeGroup[groupCount];
			_fixedUpdateGroups = new RuntimeGroup[groupCount];

			for (int i = 0; i < groupCount; i++)
			{
				List<ManagedUpdatesBehaviour> updateList = new List<ManagedUpdatesBehaviour>();
				List<ManagedUpdatesBehaviour> fixedUpdateList = new List<ManagedUpdatesBehaviour>();

				ManagedExecutionGroup currentGroup = system.ExecutionGroups[i];

				currentGroup.TraverseForType(
					UpdateType.Normal,
					(c) => { updateList.Add(c); }
				);

				currentGroup.TraverseForType(
					UpdateType.Fixed,
					(c) => { fixedUpdateList.Add(c); }
				);

				_executionGroups[i] = currentGroup;
				_updateGroups[i] = new RuntimeGroup(updateList.ToArray());
				_fixedUpdateGroups[i] = new RuntimeGroup(fixedUpdateList.ToArray());
			}
		}
	}
}

