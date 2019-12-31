using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Object = UnityEngine.Object;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using UpdateType = CustomUpdateManager.UpdateType;

	public sealed class FakeEnvironment : IDisposable
	{
		[Flags]
		public enum ObjectToggleConfig : int
		{
			None				= 0,
			GameObjectActive	= 1 << 0,
			ComponentEnabled	= 1 << 1,
			Update				= 1 << 2,
			FixedUpdate			= 1 << 3,

			AllActiveAndEnabled = GameObjectActive | ComponentEnabled | Update | FixedUpdate,
			UpdateActiveAndEnabled = AllActiveAndEnabled & ~FixedUpdate,
			FixedUpdateActiveAndEnabled = AllActiveAndEnabled & ~Update,
			NoUpdatesActiveAndEnabled = GameObjectActive | ComponentEnabled
		}

		public readonly CustomUpdateManager manager;
		public readonly ReadOnlyDictionary<string, CustomUpdatePriority> priorities;
		private readonly TestManagedUpdatesSceneController _runtimeController;

		public FakeEnvironment(params string[] priorityGroupNames)
		{
			int groupCount = priorityGroupNames.Length;

			if (groupCount == 0)
			{
				throw new System.ArgumentException("Fake environment must have at least one priority group.");
			}

			var priorityGroups = new CustomUpdatePriority[groupCount];
			var priorityLookup = new Dictionary<string, CustomUpdatePriority>(groupCount);
			for (int i = 0; i < groupCount; i++)
			{
				var currentGroup = priorityGroups[i] = ScriptableObject.CreateInstance<CustomUpdatePriority>();
				priorityLookup.Add(priorityGroupNames[i], currentGroup);
			}
			priorities = new ReadOnlyDictionary<string, CustomUpdatePriority>(priorityLookup);

			manager = ScriptableObject.CreateInstance<CustomUpdateManager>();
			manager.Initialize(priorityGroups);

			_runtimeController = new GameObject("Runtime Controller").AddComponent<TestManagedUpdatesSceneController>();
			_runtimeController.enabled = false;
		}

		internal static void GetExpectedUpdateFlagsFromConfig(ObjectToggleConfig config, out bool updateExpected, out bool fixedUpdateExpected)
		{
			updateExpected = config.HasFlag(
				ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update
			);

			fixedUpdateExpected = config.HasFlag(
				ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.FixedUpdate
			);
		}

		public void InstantiateManagedComponents<T>(string groupName, params ObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
		{
			CustomUpdatePriority group = priorities[groupName];
			Transform containerTransform = _runtimeController.transform;

			foreach (ObjectToggleConfig config in toggleConfig)
			{
				InstantiateManagedUpdateGameObject<T>(group, config, containerTransform);
			}
		}

		public void InstantiateManagedComponents<T>(
			out T[] components,
			string groupName,
			params ObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
		{
			CustomUpdatePriority group = priorities[groupName];
			Transform containerTransform = _runtimeController.transform;

			components = new T[toggleConfig.Length];
			for (int i = 0; i < toggleConfig.Length; i++)
			{
				components[i] = InstantiateManagedUpdateGameObject<T>(group, toggleConfig[i], containerTransform);
			}
		}

		public uint GetCountForGroup(string groupName, UpdateType updateType)
		{
			return priorities[groupName].GetCountForType(updateType);
		}

		private T InstantiateManagedUpdateGameObject<T>(
			CustomUpdatePriority group,
			ObjectToggleConfig config,
			Transform parent = null) where T : CustomUpdateBehaviour
		{
			return CustomUpdateBehaviour.Create<T>(
				new CustomUpdateManager.Config(
					group,
					config.HasFlag(ObjectToggleConfig.Update),
					config.HasFlag(ObjectToggleConfig.FixedUpdate)),
				config.HasFlag(ObjectToggleConfig.GameObjectActive),
				config.HasFlag(ObjectToggleConfig.ComponentEnabled),
				parent
			);
		}

		public void SetToggleConfig(CustomUpdateBehaviour component, ObjectToggleConfig toggleConfig)
		{
			component.gameObject.SetActive(toggleConfig.HasFlag(ObjectToggleConfig.GameObjectActive));
			component.enabled = toggleConfig.HasFlag(ObjectToggleConfig.ComponentEnabled);
			component.updateConfig.update = toggleConfig.HasFlag(ObjectToggleConfig.Update);
			component.updateConfig.fixedUpdate = toggleConfig.HasFlag(ObjectToggleConfig.FixedUpdate);
		}

		public void StartUpdating()
		{
			_runtimeController.OnMonoBehaviourUpdate = (() =>
			{
				manager.RunUpdate();
			});

			_runtimeController.OnMonoBehaviourFixedUpdate = (() =>
			{
				manager.RunFixedUpdate();
			});

			_runtimeController.enabled = true;
		}

		public void StopUpdating()
		{
			_runtimeController.enabled = false;
			_runtimeController.OnMonoBehaviourUpdate = null;
			_runtimeController.OnMonoBehaviourFixedUpdate = null;
		}

		public void Dispose()
		{
			StopUpdating();

			Object.Destroy(_runtimeController.gameObject);
			Object.Destroy(manager);

			foreach (var kvp in priorities)
			{
				Object.Destroy(kvp.Value);
			}
		}
	}
}