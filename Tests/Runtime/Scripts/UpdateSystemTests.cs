using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	public sealed class UpdateSystemTests
	{
		[System.Flags]
		private enum MockObjectToggleConfig : int
		{
			None = 0,
			GameObjectActive = 1,
			ComponentEnabled = 2,
			Update = 4,
			FixedUpdate = 8,

			AllActiveAndEnabled = GameObjectActive | ComponentEnabled | Update | FixedUpdate,
			UpdateActiveAndEnabled = AllActiveAndEnabled & ~FixedUpdate,
			FixedUpdateActiveAndEnabled = AllActiveAndEnabled & ~Update,
			NoUpdatesActiveAndEnabled = GameObjectActive | ComponentEnabled
		}

		private sealed class MockEnvironment : System.IDisposable
		{
			public readonly CustomUpdateManager manager;
			public readonly ReadOnlyDictionary<string, CustomUpdatePriority> priorities;

			public MockEnvironment(params string[] priorityGroupNames)
			{
				int groupCount = priorityGroupNames.Length;

				if (groupCount == 0)
				{
					throw new System.ArgumentException("Mock update system enviromnent must have at least one priority group.");
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
			}

			public void InstantiateManagedComponents<T>(string groupName, params MockObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
			{
				var group = priorities[groupName];
				foreach (var config in toggleConfig)
				{
					CustomUpdateBehaviour.Create<T>(
						new CustomUpdateManager.Config(
							manager,
							group,
							config.HasFlag(MockObjectToggleConfig.Update),
							config.HasFlag(MockObjectToggleConfig.FixedUpdate)),
						config.HasFlag(MockObjectToggleConfig.GameObjectActive),
						config.HasFlag(MockObjectToggleConfig.ComponentEnabled)
					);
				}
			}

			public void Dispose()
			{
				Object.Destroy(manager);

				foreach (var kvp in priorities)
				{
					Object.Destroy(kvp.Value);
				}
			}
		}

		[Test]
		public void Instantiate_BasicManagedUpdateActive_AddedToSystem()
		{
			string groupName = "Default";
			using (MockEnvironment env = new MockEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					groupName,
					MockObjectToggleConfig.UpdateActiveAndEnabled
				);

				Assert.That(
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Normal) == 1 &&
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Fixed) == 0,
					$"There should be exactly one {nameof(CustomUpdateBehaviour)} with UPDATE running in the system."
				);
			}
		}

		[Test]
		public void Instantiate_BasicManagedFixedUpdateActive_AddedToSystem()
		{
			string groupName = "Default";
			using (MockEnvironment env = new MockEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					groupName,
					MockObjectToggleConfig.FixedUpdateActiveAndEnabled
				);

				Assert.That(
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Normal) == 0 &&
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Fixed) == 1,
					$"There should be exactly one {nameof(CustomUpdateBehaviour)} with FIXED UPDATE running in the system."
				);
			}
		}
	}
}
