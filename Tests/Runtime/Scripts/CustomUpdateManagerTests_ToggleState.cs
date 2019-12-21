using UnityEngine;
using System.Collections.Generic;
using System;
using NUnit.Framework;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public class CustomUpdateManagerTests_ToggleState
	{
		private static ObjectToggleConfig[] _allActiveToggleStates = new ObjectToggleConfig[] {
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.FixedUpdate,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update | ObjectToggleConfig.FixedUpdate
		};

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedGameObject = GetModifiedValues(
			_allActiveToggleStates,
			ObjectToggleConfig.GameObjectActive,
			(a, b) => a & ~b
		);

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedComponent = GetModifiedValues(
			_allActiveToggleStates,
			ObjectToggleConfig.ComponentEnabled,
			(a, b) => a & ~b
		);

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedUpdate = GetModifiedValues(
			_allActiveToggleStates,
			ObjectToggleConfig.Update,
			(a, b) => a & ~b
		);

		[Test, Sequential]
		public void TryAdd_SingleGameObjectToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedGameObject))] ObjectToggleConfig initialConfig)
		{
			using (FakeEnvironment env = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME))
			{
				RunToggleTest(env, initialConfig, initialConfig | ObjectToggleConfig.GameObjectActive, true);
			}
		}

		[Test, Sequential]
		public void TryAdd_SingleComponentToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedComponent))] ObjectToggleConfig initialConfig)
		{
			using (FakeEnvironment env = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME))
			{
				RunToggleTest(env, initialConfig, initialConfig | ObjectToggleConfig.ComponentEnabled, true);
			}
		}

		[Test, Sequential]
		public void TryAdd_SingleUpdateFlagToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedUpdate))] ObjectToggleConfig initialConfig)
		{
			using (FakeEnvironment env = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME))
			{
				RunToggleTest(env, initialConfig, initialConfig | ObjectToggleConfig.Update, true);
			}
		}

		private static void RunToggleTest(FakeEnvironment env, ObjectToggleConfig initialConfig, ObjectToggleConfig finalConfig, bool isExpectedInSystem)
		{
			//
			// Arrange
			//
			TestBasicManagedUpdatesComponent[] components;
			env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				initialConfig
			);

			//
			// Act
			//
			CustomUpdateBehaviour component = components[0];
			env.SetToggleConfig(component, finalConfig);

			//
			// Assert
			//
			Assert.That(
				isExpectedInSystem == env.manager.CheckSystemForComponent(component),
				$"Final configuration should leave the component {(isExpectedInSystem ? "REGISTERED" : "UNREGISTERED")} for the system.\n" +
					$"Initial config {initialConfig}\n" +
					$"Final config: {finalConfig}"
			);
		}

		private static TValue[] GetModifiedValues<TValue, TModifier>(TValue[] values, TModifier modifier, Func<TValue, TModifier, TValue> operation)
		{
			int count = values.Length;
			List<TValue> modifiedValues = new List<TValue>(count);
			for (int i = 0; i < count; i++)
			{
				TValue currentModifiedValue = operation(values[i], modifier);
				if (!modifiedValues.Contains(currentModifiedValue))
				{
					modifiedValues.Add(currentModifiedValue);
				}
			}

			return modifiedValues.ToArray();
		}

		/// <summary>
		/// Generates all permutations of a 'flags' Enum type, filtering out named combinations of flags.
		/// </summary>
		public static T[] GenerateAllTogglePermutations<T>() where T : Enum
		{
			if (!typeof(T).IsDefined(typeof(FlagsAttribute), false))
			{
				throw new ArgumentException($"{typeof(FlagsAttribute).Name} must be defined to generate the permutations of the given Enum: {typeof(T).Name}");
			}

			var allNamedConfigValues = System.Enum.GetValues(typeof(ObjectToggleConfig)) as ObjectToggleConfig[];
			int allToggledOnValue = 0;
			foreach (ObjectToggleConfig toggleConfig in allNamedConfigValues)
			{
				int toggleConfigValue = (int)toggleConfig;
				double exponent = System.Math.Log(toggleConfigValue, 2);

				// If the base 2 log of the config value is an integer, we know the value represents a single flag and not a named combination
				bool isInteger = System.Math.Ceiling(exponent) == System.Math.Floor(exponent);
				if (isInteger)
				{
					allToggledOnValue |= toggleConfigValue;
				}
			}

			var permutations = new SortedSet<T>();
			for (int currentValue = 0; currentValue <= allToggledOnValue; currentValue++)
			{
				if ((allToggledOnValue | currentValue) == allToggledOnValue)
				{
					permutations.Add((T)(ValueType)currentValue);
				}
			}

			var permutationsArray = new T[permutations.Count];
			permutations.CopyTo(permutationsArray);

			return permutationsArray;
		}
	}
}