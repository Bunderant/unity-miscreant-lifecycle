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

		private static ObjectToggleConfig[] _inactiveToggleStatesNeedFixedUpdate = GetModifiedValues(
			_allActiveToggleStates,
			ObjectToggleConfig.FixedUpdate,
			(a, b) => a & ~b
		);

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
		public void TryAdd_SingleGameObjectToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedGameObject))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.GameObjectActive, true);
		}

		[Test, Sequential]
		public void TryAdd_SingleComponentToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedComponent))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.ComponentEnabled, true);
		}

		[Test, Sequential]
		public void TryAdd_SingleUpdateFlagToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.Update, true);
		}

		[Test, Sequential]
		public void TryAdd_SingleFixedUpdateFlagToggledOn_AddedToSystem([ValueSource(nameof(_inactiveToggleStatesNeedFixedUpdate))] ObjectToggleConfig initialConfig)
		{
			RunToggleTest(_environment, initialConfig, initialConfig | ObjectToggleConfig.FixedUpdate, true);
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
	}
}