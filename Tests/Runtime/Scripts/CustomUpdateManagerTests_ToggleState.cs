using UnityEngine;
using System.Collections.Generic;
using System;
using NUnit.Framework;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public class CustomUpdateManagerTests_ToggleState
	{
		private static ObjectToggleConfig[] _validToggleStatesActiveInSystem = new ObjectToggleConfig[] {
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.FixedUpdate,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update | ObjectToggleConfig.FixedUpdate
		};

		private static ObjectToggleConfig[] _invalidToggleStatesNeedActiveGameObject = GetModifiedValues(
			_validToggleStatesActiveInSystem,
			ObjectToggleConfig.GameObjectActive,
			(a, b) => a & ~b
		);

		[Test, Sequential]
		public void TryAdd_GameObjectToggledOn_AddedToSystem([ValueSource(nameof(_invalidToggleStatesNeedActiveGameObject))] ObjectToggleConfig initialConfig)
		{
			const string groupName = CustomUpdateManagerTests.DEFAULT_GROUP_NAME;
			using (FakeEnvironment env = new FakeEnvironment(groupName))
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

				// Make sure that the system doesn't contain the instantiated component before proceeding
				CustomUpdateBehaviour component = components[0];
				if (env.manager.CheckSystemForComponent(component))
				{
					throw new Exception("Tried to begin activation test with an already registered component. Configuration: " + initialConfig);
				}

				//
				// Act
				//
				ObjectToggleConfig finalConfig = initialConfig | ObjectToggleConfig.GameObjectActive;
				env.SetToggleConfig(component, finalConfig);

				//
				// Assert
				//
				Assert.IsTrue(
					env.manager.CheckSystemForComponent(component),
					"Final configuration should make the component active in the system.\n" +
						$"Initial config {initialConfig}\n" +
						$"Final config: {finalConfig}"
				);
			}
		}

		private static TValue[] GetModifiedValues<TValue, TModifier>(TValue[] values, TModifier modifier, Func<TValue, TModifier, TValue> operation)
		{
			int count = values.Length;
			TValue[] modifiedValues = new TValue[count];
			for (int i = 0; i < count; i++)
			{
				modifiedValues[i] = operation(values[i], modifier);
			}

			return modifiedValues;
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

