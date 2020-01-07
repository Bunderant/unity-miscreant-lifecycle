using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Lifecycle.Editor
{
	using UpdateType = CustomUpdateManager.UpdateType;

	/// <summary>
	/// Class used exclusively for displaying CustomUpdateManager's runtime data in the inspector.
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
			public CustomUpdateBehaviour[] value;

			public RuntimeGroup(CustomUpdateBehaviour[] group)
			{
				this.value = group;
			}
		}

		public void Initialize(CustomUpdateManager updateManager)
		{
			int groupCount = updateManager.ExecutionGroups.Count;

			_executionGroups = new ManagedExecutionGroup[groupCount];
			_updateGroups = new RuntimeGroup[groupCount];
			_fixedUpdateGroups = new RuntimeGroup[groupCount];

			for (int i = 0; i < groupCount; i++)
			{
				List<CustomUpdateBehaviour> updateList = new List<CustomUpdateBehaviour>();
				List<CustomUpdateBehaviour> fixedUpdateList = new List<CustomUpdateBehaviour>();

				ManagedExecutionGroup currentGroup = updateManager.ExecutionGroups[i];

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

