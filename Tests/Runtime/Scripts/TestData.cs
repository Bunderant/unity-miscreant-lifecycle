using System;
using System.Linq;
using System.Collections.Generic;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public static class TestData
	{
		public const string DEFAULT_GROUP_NAME = "Default";

		internal static ObjectToggleConfig[] allTogglePermutations = GetAllPermutations<ObjectToggleConfig>();

		internal static ObjectToggleConfig[] allActiveTogglePermutations = new ObjectToggleConfig[] {
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.FixedUpdate,
			ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.ComponentEnabled | ObjectToggleConfig.Update | ObjectToggleConfig.FixedUpdate
		};

		internal static ObjectToggleConfig[] allInactiveTogglePermutations = allTogglePermutations.Except(
			allActiveTogglePermutations
		).ToArray();

		internal static ObjectToggleConfig[] activeUpdateTogglePermutations = allActiveTogglePermutations.Where(
			x => { return x.HasFlag(ObjectToggleConfig.Update); }).Distinct().ToArray();

		internal static ObjectToggleConfig[] activeFixedUpateTogglePermutations = allActiveTogglePermutations.Where(
			x => { return x.HasFlag(ObjectToggleConfig.FixedUpdate); }).Distinct().ToArray();

		internal static ObjectToggleConfig[] inactiveTogglePermutationsNeedGameObject = allActiveTogglePermutations.Select(
			x => { return x & ~ObjectToggleConfig.GameObjectActive; }).Distinct().ToArray();

		internal static ObjectToggleConfig[] inactiveTogglePermutationsNeedComponent = allActiveTogglePermutations.Select(
			x => { return x & ~ObjectToggleConfig.ComponentEnabled; }).Distinct().ToArray();

		internal static ObjectToggleConfig[] inactiveTogglePermutationsNeedUpdate = allActiveTogglePermutations.Select(
			x => { return x & ~ObjectToggleConfig.Update; }).Distinct().ToArray();

		internal static ObjectToggleConfig[] inactiveTogglePermutationsNeedFixedUpdate = allActiveTogglePermutations.Select(
			x => { return x & ~ObjectToggleConfig.FixedUpdate; }).Distinct().ToArray();

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
        public static T[] GetAllPermutations<T>() where T : Enum
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
