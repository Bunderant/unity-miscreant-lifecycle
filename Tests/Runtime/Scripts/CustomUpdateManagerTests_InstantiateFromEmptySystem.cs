using NUnit.Framework;
using System;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests_InstantiateFromEmptySystem
	{
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

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allActiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromEmptySystem_AddedToSystem(ObjectToggleConfig config)
		{
			// Arrange
			FakeEnvironment.GetExpectedUpdateFlagsFromConfig(
				config,
				out bool updateExpected,
				out bool fixedUpdateExpected
			);

			if (!(updateExpected || fixedUpdateExpected))
			{
				throw new ArgumentException(
					"Invalid input for test. Config must be expected to have at least one type of update callback registered:\n" + config
				);
			}

			// Act
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.manager.CheckSystemForComponent(components[0], out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should have at least one update callback registered with the manager, matching what was expected.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allInactiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromEmptySystem_NotAddedToSystem(ObjectToggleConfig config)
		{
			// Arrange
			FakeEnvironment.GetExpectedUpdateFlagsFromConfig(
				config,
				out bool updateExpected,
				out bool fixedUpdateExpected
			);

			if (updateExpected || fixedUpdateExpected)
			{
				throw new ArgumentException(
					"Invalid input for test. Config must specify NONE of the update callbacks to be registered:\n" + config
				);
			}

			// Act
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.manager.CheckSystemForComponent(components[0], out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should not have any update callbacks registered with the manager.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}
	}
}