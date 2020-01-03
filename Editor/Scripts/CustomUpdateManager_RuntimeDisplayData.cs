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
		private CustomUpdatePriority[] _priorities;
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
			int priorityCount = updateManager.Priorities.Count;

			_priorities = new CustomUpdatePriority[priorityCount];
			_updateGroups = new RuntimeGroup[priorityCount];
			_fixedUpdateGroups = new RuntimeGroup[priorityCount];

			for (int i = 0; i < priorityCount; i++)
			{
				List<CustomUpdateBehaviour> updateList = new List<CustomUpdateBehaviour>();
				List<CustomUpdateBehaviour> fixedUpdateList = new List<CustomUpdateBehaviour>();

				CustomUpdatePriority currentGroup = updateManager.Priorities[i];

				currentGroup.TraverseForType(
					UpdateType.Normal,
					(c) => { updateList.Add(c); }
				);

				currentGroup.TraverseForType(
					UpdateType.Fixed,
					(c) => { fixedUpdateList.Add(c); }
				);

				_priorities[i] = currentGroup;
				_updateGroups[i] = new RuntimeGroup(updateList.ToArray());
				_fixedUpdateGroups[i] = new RuntimeGroup(fixedUpdateList.ToArray());
			}
		}
	}
}

