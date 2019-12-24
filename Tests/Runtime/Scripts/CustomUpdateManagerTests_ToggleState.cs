using UnityEngine;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using System.Linq;

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

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedGameObject = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.GameObjectActive; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedComponent = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.ComponentEnabled; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedUpdate = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.Update; }).Distinct().ToArray();

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedFixedUpdate = _allActiveToggleStates.Select(
			x => { return x & ~ObjectToggleConfig.FixedUpdate; }).Distinct().ToArray();

		private FakeEnvironment _environment;

		[SetUp]
		public void SetUp()
		{
			_environment = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME);
		}

		[TearDown]
		public void TearDown()
		{
			_environment.Dispose();
			_environment = null;
		}

		[Test, Sequential]
		public void TryAdd_SingleGameObjectToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedGameObject))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.GameObjectActive);
		}

		[Test, Sequential]
		public void TryAdd_SingleComponentToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedComponent))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.ComponentEnabled);
		}

		[Test, Sequential]
		public void TryAdd_SingleUpdateFlagToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.Update);
		}

		[Test, Sequential]
		public void TryAdd_SingleFixedUpdateFlagToggledOn_CorrectFlagsAddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedFixedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.FixedUpdate);
		}

		[Test, Sequential]
		public void TryRemove_SingleGameObjectToggledOff_RemovedFromSystem([ValueSource(nameof(_allActiveToggleStates))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.GameObjectActive);
		}

		[Test, Sequential]
		public void TryRemove_SingleComponentToggledOff_RemovedFromSystem([ValueSource(nameof(_allActiveToggleStates))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig & ~ObjectToggleConfig.ComponentEnabled);
		}

		private static void RunToggleTest(FakeEnvironment env, ObjectToggleConfig initialConfig, ObjectToggleConfig finalConfig)
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
			bool updateExpected = finalConfig.HasFlag(
				ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update
			);
			bool fixedUpdateExpected = finalConfig.HasFlag(
				ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.FixedUpdate
			);

			env.manager.CheckSystemForComponent(component, out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Final configuration's expected state was not reflected in the manager.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		/// <summary>
		/// Creates a collection of all unique power-of-two values from an Enum type. Intended for use on types with the "Flags" attribute 
		/// defined, and explicit constants for its power-of-two values. This is a convenience to filter out named flag combinations. 
		/// </summary>
		/// <typeparam name="T">The enum type.</typeparam>
		/// <returns>A collection of unique power-of-two values, sorted according to the System.Enum.GetValues specification.</returns>
		public static ICollection<T> GetPowerOfTwoValues<T>() where T : Enum
		{
			return GetPowerOfTwoValues<T>((T[])System.Enum.GetValues(typeof(T)));
		}

		/// <summary>
		/// Creates a collection of all unique power-of-two values from the given collection. Intended for use on enum types with the "Flags"
		/// attribute defined, and explicit constants for its power-of-two values. This is a convenience to filter out named flag combinations. 
		/// </summary>
		/// <param name="unfilteredEnumValues">Existing collection.</param>
		/// <typeparam name="T">The enum type.</typeparam>
		/// <returns>An array of unique power-of-two values, in the same order as they appeared in the original collection.</returns>
		public static T[] GetPowerOfTwoValues<T>(T[] unfilteredEnumValues) where T : Enum
		{
			List<T> powersOfTwo = new List<T>(unfilteredEnumValues.Length);

			int foundPowersOfTwo = 0;
			foreach (T enumValue in unfilteredEnumValues)
			{
				int intValue = (int)(ValueType)enumValue;
				
				if ((intValue != 0) && ((intValue & (intValue - 1)) == 0) && 	// If the value is a power of two and...
					((intValue & foundPowersOfTwo) == 0))						// it hasn't been added yet, then...
				{
					powersOfTwo.Add(enumValue);
					foundPowersOfTwo |= intValue;
				}
			}

			return powersOfTwo.ToArray();
		}

		/// <summary> 
		/// Generates all permutations of the given Enum type. Intended for use on enum types with the "Flags" attribute defined, and explicit 
		/// constants for its power-of-two values
		/// </summary>
		/// <typeparam name="T">The enum type.</typeparam>
		/// <returns>An array of all permutations for values defined in the enum, in ascending order.</returns> 
        public static T[] GetAllPowerOfTwoPermutations<T>() where T : Enum
        {
			ICollection<T> uniqueValues = GetPowerOfTwoValues<T>();

			// Get the sum of all values in the set for the given type
            int allToggledOnValue = 0;
            foreach (T value in uniqueValues)
            {
                int intValue = (int)(ValueType)value;
				allToggledOnValue |= intValue;
            }

            List<T> permutations = new List<T>();
			for (int currentValue = 0; currentValue <= allToggledOnValue; currentValue++)
			{
				if ((allToggledOnValue | currentValue) == allToggledOnValue)
				{
					permutations.Add((T)(ValueType)currentValue);
				}
			}

            return permutations.ToArray();
        }
	}
}